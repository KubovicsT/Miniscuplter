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


def test_only_verified_3d_completion_records_qualification() -> None:
    original = job_progress._record_completed_inference
    recorded: list[tuple[str, str | None, float]] = []
    try:
        job_progress._record_completed_inference = lambda kind, provider, elapsed: recorded.append((kind, provider, elapsed))

        image_job = job_progress.begin("2d-generate", "qualification-image-job")
        job_progress.complete("image done", "sdxl")
        assert job_progress.get(image_job)["state"] == "completed"

        failed_3d = job_progress.begin("3d-generate", "qualification-failed-3d")
        job_progress.report("running_inference", "provider running", 50, "hunyuan-mini")
        job_progress.fail("input-specific generation failure")
        assert job_progress.get(failed_3d)["state"] == "failed"

        successful_3d = job_progress.begin("3d-generate", "qualification-success-3d")
        # Simulate Auto falling back: the provider attached to the completed job must be the
        # final provider that actually produced the verified output.
        job_progress.report("loading_model", "fallback selected", 20, "triposr")
        job_progress.complete("mesh saved and verified", "sf3d")
        assert job_progress.get(successful_3d)["provider"] == "sf3d"

        assert len(recorded) == 2, "complete() should invoke the hook for completed jobs only"
        assert recorded[0][0] == "2d-generate"
        assert recorded[1][0] == "3d-generate"
        assert recorded[1][1] == "sf3d"
        assert recorded[1][2] >= 0.0
    finally:
        job_progress._record_completed_inference = original


def test_qualification_recording_failure_cannot_fail_completed_job() -> None:
    original = job_progress._record_completed_inference
    try:
        def broken(*_args):
            raise RuntimeError("simulated qualification persistence failure")
        job_progress._record_completed_inference = broken
        job_id = job_progress.begin("3d-generate", "qualification-persist-failure")
        job_progress.complete("mesh verified", "triposr")
        snapshot = job_progress.get(job_id)
        assert snapshot is not None
        assert snapshot["state"] == "completed"
        assert snapshot["progress"] == 100.0
    finally:
        job_progress._record_completed_inference = original


if __name__ == "__main__":
    test_job_identity_and_event_history()
    test_cancel_state()
    test_only_verified_3d_completion_records_qualification()
    test_qualification_recording_failure_cannot_fail_completed_job()
    print("job_progress_tests: PASS")
