from __future__ import annotations

import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parents[1] / "ai_backend"))

import job_progress


def test_job_identity_and_event_history() -> None:
    job_id = job_progress.begin("test", "client-test-job")
    assert job_id == "client-test-job"
    job_progress.report("running_inference", "test detail", 45.0, "test-provider")
    snapshot = job_progress.get(job_id)
    assert snapshot is not None
    assert snapshot["kind"] == "test"
    assert snapshot["progress"] == 45.0
    assert snapshot["provider"] == "test-provider"
    assert len(snapshot["events"]) >= 2
    assert snapshot["sequence"] >= 2
    job_progress.complete("done", "test-provider")
    assert job_progress.get(job_id)["state"] == "completed"
    assert job_progress.get_events(job_id, 0)[-1]["stage"] == "completed"


def test_cancel_state() -> None:
    job_id = job_progress.begin("cancel-test", "client-cancel-job")
    assert not job_progress.is_cancel_requested(job_id)
    cancelled = job_progress.request_cancel(job_id)
    assert cancelled is not None
    assert cancelled["state"] == "cancelling"
    assert job_progress.is_cancel_requested(job_id)
    job_progress.fail("cancelled")


if __name__ == "__main__":
    test_job_identity_and_event_history()
    test_cancel_state()
    print("job_progress_tests: PASS")
