"""Headless smoke tests for deterministic Alba FBX export."""

from __future__ import annotations

import importlib
import sys
import tempfile
from pathlib import Path

import bpy


TOOLS_DIR = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(TOOLS_DIR))
exporter = importlib.import_module("export_alba_asset")


def reset_scene() -> None:
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete(use_global=False)
    for collection in list(bpy.data.collections):
        if collection.name != "Collection":
            bpy.data.collections.remove(collection)


def create_export_fixture() -> None:
    reset_scene()
    collection = bpy.data.collections.new("BodyGirl")
    bpy.context.scene.collection.children.link(collection)
    armature_data = bpy.data.armatures.new("AlbaHumanoidRig")
    rig = bpy.data.objects.new("AlbaHumanoidRig", armature_data)
    collection.objects.link(rig)
    bpy.ops.mesh.primitive_uv_sphere_add(segments=12, ring_count=6)
    mesh = bpy.context.object
    mesh.name = "OrganicBody"
    for owner in list(mesh.users_collection):
        owner.objects.unlink(mesh)
    collection.objects.link(mesh)
    mesh.modifiers.new("AlbaRigBinding", "ARMATURE").object = rig


def test_missing_collection_is_rejected() -> None:
    reset_scene()
    try:
        exporter.export_collection("Missing", "missing.fbx")
    except ValueError as exception:
        assert "Missing" in str(exception)
    else:
        raise AssertionError("missing collection must fail")


def test_export_collection_writes_nonempty_fbx_without_changing_scene() -> None:
    create_export_fixture()
    original_objects = set(bpy.data.objects.keys())
    with tempfile.TemporaryDirectory() as temporary_directory:
        output = Path(temporary_directory) / "body-girl.fbx"
        exporter.export_collection("BodyGirl", str(output))
        assert output.exists()
        assert output.stat().st_size > 1_000
    assert set(bpy.data.objects.keys()) == original_objects
    assert bpy.data.objects.get("BodyGirl") is None


def main() -> None:
    failures: list[str] = []
    for name, test in sorted(globals().items()):
        if not name.startswith("test_"):
            continue
        try:
            test()
            print(f"PASS: {name}")
        except Exception as exception:
            failures.append(f"{name}: {exception}")
            print(f"FAIL: {name}: {exception}")
    reset_scene()
    if failures:
        raise SystemExit("\n".join(failures))


if __name__ == "__main__":
    main()
