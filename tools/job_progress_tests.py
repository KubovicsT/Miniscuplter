from __future__ import annotations

import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parents[1] / "ai_backend"))

import job_progress
import resource_ownership
import model_manager
import model_router


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


def test_stage_c_context_is_retained_and_snapshot_isolated() -> None:
    context = {
        "generation_job_id": "0123456789abcdef0123456789abcdef",
        "project_id": "11111111111111111111111111111111",
        "project_revision": 7,
        "input_image_revision_id": "22222222222222222222222222222222",
        "output_object_id": "33333333333333333333333333333333",
    }
    job_id = job_progress.begin("3d-generate", context["generation_job_id"], context)
    context["project_revision"] = 999
    first = job_progress.get(job_id)
    assert first is not None
    assert first["context"]["project_revision"] == 7
    first["context"]["project_revision"] = 123
    second = job_progress.get(job_id)
    assert second is not None
    assert second["context"]["project_revision"] == 7
    assert second["job_id"] == second["context"]["generation_job_id"]
    assert second["resource_owner"]["active"] is True
    assert second["resource_owner"]["owner_id"] == job_id
    job_progress.fail("test cleanup")
    assert resource_ownership.snapshot()["active"] is False


def test_cancel_state() -> None:
    job_id = job_progress.begin("cancel-test", "client-cancel-job")
    assert not job_progress.is_cancel_requested(job_id)
    cancelled = job_progress.request_cancel(job_id)
    assert cancelled is not None
    assert cancelled["state"] == "cancelling"
    assert job_progress.is_cancel_requested(job_id)
    job_progress.fail("cancelled")


def test_heavyweight_resource_rejects_parallel_owner_without_stealing_lease() -> None:
    first = resource_ownership.acquire("owner-a", "3d-generate", blocking=False)
    assert first["owner_id"] == "owner-a"
    try:
        try:
            resource_ownership.acquire("owner-b", "component-install", blocking=False)
            raise AssertionError("parallel heavyweight ownership should be rejected")
        except resource_ownership.ResourceBusyError:
            pass
        current = resource_ownership.snapshot()
        assert current["active"] is True
        assert current["owner_id"] == "owner-a"
    finally:
        assert resource_ownership.release("owner-a") is True
    assert resource_ownership.snapshot()["active"] is False


def test_component_mutation_cannot_interrupt_active_inference() -> None:
    resource_ownership.acquire("inference-owner", "3d-generate", blocking=False)
    original_install = model_manager._v105_install_component
    original_release = model_router.release_all_models
    called = {"install": 0, "release": 0}
    try:
        model_manager._v105_install_component = lambda *_args, **_kwargs: called.__setitem__("install", called["install"] + 1) or {"installed": True}
        model_router.release_all_models = lambda **_kwargs: called.__setitem__("release", called["release"] + 1)
        try:
            model_manager.install_component("triposr")
            raise AssertionError("component install should be rejected while inference owns the runtime")
        except resource_ownership.ResourceBusyError:
            pass
        assert called == {"install": 0, "release": 0}
        owner = resource_ownership.snapshot()
        assert owner["active"] is True
        assert owner["owner_id"] == "inference-owner"
    finally:
        model_manager._v105_install_component = original_install
        model_router.release_all_models = original_release
        resource_ownership.release("inference-owner")


def test_model_release_refuses_foreign_owner() -> None:
    resource_ownership.acquire("inference-release-owner", "3d-generate", blocking=False)
    try:
        try:
            model_router.release_all_models()
            raise AssertionError("model release must not tear down an active inference owner")
        except resource_ownership.ResourceBusyError:
            pass
        owner = resource_ownership.snapshot()
        assert owner["owner_id"] == "inference-release-owner"
    finally:
        resource_ownership.release("inference-release-owner")


def test_component_owner_is_visible_and_released_after_error() -> None:
    original_install = model_manager._v105_install_component
    original_release = model_router.release_all_models
    observed: dict = {}
    try:
        def fake_release(*, allow_owner_id=None):
            current = resource_ownership.snapshot()
            assert current["active"] is True
            assert current["owner_id"] == allow_owner_id
            observed["kind"] = current["kind"]

        def broken_install(*_args, **_kwargs):
            raise RuntimeError("simulated component failure")

        model_router.release_all_models = fake_release
        model_manager._v105_install_component = broken_install
        try:
            model_manager.install_component("triposr")
            raise AssertionError("simulated component failure should escape")
        except RuntimeError as exc:
            assert "simulated component failure" in str(exc)
        assert observed["kind"] == "component-install"
        assert resource_ownership.snapshot()["active"] is False
    finally:
        model_manager._v105_install_component = original_install
        model_router.release_all_models = original_release
        owner = resource_ownership.snapshot()
        if owner["active"]:
            resource_ownership.release(owner["owner_id"])


def test_component_update_remove_and_repair_use_shared_owner() -> None:
    original_update = model_manager._v105_update_component
    original_install = model_manager._v105_install_component
    original_uninstall = model_manager._legacy_uninstall_component
    original_release = model_router.release_all_models
    observed: list[str] = []
    try:
        def fake_release(*, allow_owner_id=None):
            current = resource_ownership.snapshot()
            assert current["owner_id"] == allow_owner_id
            observed.append(current["kind"])
        model_router.release_all_models = fake_release
        model_manager._v105_update_component = lambda _cid: {"updated": True}
        model_manager._v105_install_component = lambda _cid, _update=False: {"installed": True}
        model_manager._legacy_uninstall_component = lambda _cid: {"installed": False}

        assert model_manager.update_component("triposr")["operation_kind"] == "component-update"
        assert model_manager.repair_component("triposr")["operation_kind"] == "component-repair"
        assert model_manager.uninstall_component("triposr")["operation_kind"] == "component-remove"
        assert observed == ["component-update", "component-repair", "component-remove"]
        assert resource_ownership.snapshot()["active"] is False
    finally:
        model_manager._v105_update_component = original_update
        model_manager._v105_install_component = original_install
        model_manager._legacy_uninstall_component = original_uninstall
        model_router.release_all_models = original_release
        owner = resource_ownership.snapshot()
        if owner["active"]:
            resource_ownership.release(owner["owner_id"])


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
        assert resource_ownership.snapshot()["active"] is False

        successful_3d = job_progress.begin("3d-generate", "qualification-success-3d")
        job_progress.report("loading_model", "fallback selected", 20, "triposr")
        job_progress.complete("mesh saved and verified", "sf3d")
        assert job_progress.get(successful_3d)["provider"] == "sf3d"
        assert resource_ownership.snapshot()["active"] is False

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
        assert resource_ownership.snapshot()["active"] is False
    finally:
        job_progress._record_completed_inference = original


if __name__ == "__main__":
    test_job_identity_and_event_history()
    test_stage_c_context_is_retained_and_snapshot_isolated()
    test_cancel_state()
    test_heavyweight_resource_rejects_parallel_owner_without_stealing_lease()
    test_component_mutation_cannot_interrupt_active_inference()
    test_model_release_refuses_foreign_owner()
    test_component_owner_is_visible_and_released_after_error()
    test_component_update_remove_and_repair_use_shared_owner()
    test_only_verified_3d_completion_records_qualification()
    test_qualification_recording_failure_cannot_fail_completed_job()
    print("job_progress_tests: PASS")
