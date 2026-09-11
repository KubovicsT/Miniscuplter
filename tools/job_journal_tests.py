from __future__ import annotations

import os
import sys
from pathlib import Path
from tempfile import TemporaryDirectory

sys.path.insert(0, str(Path(__file__).resolve().parents[1] / "ai_backend"))

import job_journal
import job_progress
import resource_ownership


def _clear_process_state() -> None:
    owner = resource_ownership.snapshot()
    if owner.get("active"):
        resource_ownership.release(str(owner.get("owner_id") or ""))
    with job_progress._lock:
        job_progress._jobs.clear()
        job_progress._current_id = None
        job_progress._recovery_warning = None
    job_progress.bind(None)


def _with_data_root(root: Path):
    class EnvScope:
        def __enter__(self):
            self.previous = os.environ.get("MINISCULPTER_DATA")
            os.environ["MINISCULPTER_DATA"] = str(root)
            _clear_process_state()
            return self

        def __exit__(self, exc_type, exc, tb):
            _clear_process_state()
            if self.previous is None:
                os.environ.pop("MINISCULPTER_DATA", None)
            else:
                os.environ["MINISCULPTER_DATA"] = self.previous
            return False

    return EnvScope()


def test_clean_completion_is_persisted_without_large_history() -> None:
    with TemporaryDirectory() as raw:
        root = Path(raw).resolve()
        with _with_data_root(root):
            original = job_progress._record_completed_inference
            try:
                job_progress._record_completed_inference = lambda *_args: None
                context = {
                    "generation_job_id": "journal-complete",
                    "project_id": "project-a",
                    "project_revision": 4,
                    "input_image_revision_id": "image-r3",
                    "output_object_id": "object-a",
                    "unbounded_debug_payload": "x" * 10000,
                }
                job_id = job_progress.begin("3d-generate", "journal-complete", context)
                job_progress.report("running_inference", "working", 55, "triposr")
                job_progress.complete("mesh verified", "triposr")
                persisted = job_journal.load()
                assert persisted is not None
                assert persisted["job_id"] == job_id
                assert persisted["state"] == "completed"
                assert persisted["active"] is False
                assert persisted["provider"] == "triposr"
                assert "unbounded_debug_payload" not in persisted["context"]
                assert "events" not in persisted
                assert job_journal.journal_path().is_relative_to(root)
                assert not list(job_journal.journal_path().parent.glob("*.tmp"))

                _clear_process_state()
                restored = job_progress.recover_persisted_state()
                assert restored is not None
                assert restored["state"] == "completed"
                assert restored["active"] is False
                assert restored["context"]["project_revision"] == 4
                assert resource_ownership.snapshot()["active"] is False
            finally:
                job_progress._record_completed_inference = original


def test_running_job_becomes_interrupted_after_backend_restart() -> None:
    with TemporaryDirectory() as raw:
        root = Path(raw).resolve()
        with _with_data_root(root):
            job_id = job_progress.begin(
                "3d-generate",
                "journal-crash",
                {
                    "generation_job_id": "journal-crash",
                    "project_id": "project-crash",
                    "project_revision": 9,
                    "input_image_revision_id": "image-crash",
                    "output_object_id": "object-crash",
                },
            )
            job_progress.report("running_inference", "provider active", 61, "triposr")
            assert job_journal.load()["state"] == "running"
            assert resource_ownership.snapshot()["owner_id"] == job_id

            # Simulate the process boundary: process-local lease disappears, durable state remains.
            resource_ownership.release(job_id)
            with job_progress._lock:
                job_progress._jobs.clear()
                job_progress._current_id = None
            job_progress.bind(None)

            restored = job_progress.recover_persisted_state()
            assert restored is not None
            assert restored["job_id"] == job_id
            assert restored["state"] == "interrupted"
            assert restored["stage"] == "interrupted"
            assert restored["active"] is False
            assert restored["context"]["output_object_id"] == "object-crash"
            assert resource_ownership.snapshot()["active"] is False
            persisted = job_journal.load()
            assert persisted["state"] == "interrupted"
            assert "no output was accepted" in persisted["detail"]

            next_job = job_progress.begin("3d-generate", "journal-after-crash")
            assert resource_ownership.snapshot()["owner_id"] == next_job
            job_progress.fail("test cleanup")


def test_cancelling_job_becomes_cancelled_after_backend_restart() -> None:
    with TemporaryDirectory() as raw:
        root = Path(raw).resolve()
        with _with_data_root(root):
            job_id = job_progress.begin("3d-generate", "journal-cancelling")
            job_progress.request_cancel(job_id)
            assert job_journal.load()["state"] == "cancelling"
            assert resource_ownership.snapshot()["owner_id"] == job_id

            resource_ownership.release(job_id)
            with job_progress._lock:
                job_progress._jobs.clear()
                job_progress._current_id = None
            job_progress.bind(None)

            restored = job_progress.recover_persisted_state()
            assert restored is not None
            assert restored["state"] == "cancelled"
            assert restored["active"] is False
            assert restored["cancel_requested"] is True
            assert resource_ownership.snapshot()["active"] is False
            assert job_journal.load()["state"] == "cancelled"


def test_corrupt_journal_fails_closed_without_claiming_resource() -> None:
    with TemporaryDirectory() as raw:
        root = Path(raw).resolve()
        with _with_data_root(root):
            path = job_journal.journal_path()
            path.parent.mkdir(parents=True, exist_ok=True)
            path.write_text("{not-json", encoding="utf-8")

            restored = job_progress.recover_persisted_state()
            assert restored is None
            current = job_progress.current()
            assert current["state"] == "recovery-error"
            assert current["active"] is False
            assert "rejected as corrupt" in current["detail"]
            assert resource_ownership.snapshot()["active"] is False

            # A new explicit request may replace the untrusted tombstone atomically.
            new_job = job_progress.begin("3d-generate", "journal-recovery-new-job")
            assert job_progress.current()["state"] == "running"
            assert resource_ownership.snapshot()["owner_id"] == new_job
            job_progress.fail("test cleanup")
            assert job_journal.load()["state"] == "failed"


def test_oversized_journal_is_rejected_before_decode() -> None:
    with TemporaryDirectory() as raw:
        root = Path(raw).resolve()
        with _with_data_root(root):
            path = job_journal.journal_path()
            path.parent.mkdir(parents=True, exist_ok=True)
            path.write_bytes(b"{" + (b"x" * (job_journal._MAX_JOURNAL_BYTES + 1)))

            restored = job_progress.recover_persisted_state()
            assert restored is None
            current = job_progress.current()
            assert current["state"] == "recovery-error"
            assert current["active"] is False
            assert "unexpectedly large" in current["detail"]
            assert resource_ownership.snapshot()["active"] is False


if __name__ == "__main__":
    test_clean_completion_is_persisted_without_large_history()
    test_running_job_becomes_interrupted_after_backend_restart()
    test_cancelling_job_becomes_cancelled_after_backend_restart()
    test_corrupt_journal_fails_closed_without_claiming_resource()
    test_oversized_journal_is_rejected_before_decode()
    print("job_journal_tests: PASS")
