"""Headless Blender checks for the Alba character validator.

Run with Blender, not the system Python:
  blender -b --factory-startup --python Tools/Blender/tests/test_validate_alba_asset.py
"""

from __future__ import annotations

import importlib
import sys
from pathlib import Path

import bpy


TOOLS_DIR = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(TOOLS_DIR))
validator = importlib.import_module("validate_alba_asset")


REQUIRED_BONES = sorted(validator.REQUIRED_HUMANOID_BONES)


def reset_scene() -> None:
    bpy.ops.object.mode_set(mode="OBJECT") if bpy.context.object and bpy.context.object.mode != "OBJECT" else None
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete(use_global=False)
    for collection in list(bpy.data.collections):
        if collection.name != "Collection":
            bpy.data.collections.remove(collection)


def create_rig(*, extra_bone: bool = False) -> bpy.types.Object:
    armature_data = bpy.data.armatures.new("AlbaHumanoidRig")
    rig = bpy.data.objects.new("AlbaHumanoidRig", armature_data)
    bpy.context.scene.collection.objects.link(rig)
    bpy.context.view_layer.objects.active = rig
    rig.select_set(True)
    bpy.ops.object.mode_set(mode="EDIT")
    for index, bone_name in enumerate(REQUIRED_BONES + (["Unexpected"] if extra_bone else [])):
        bone = armature_data.edit_bones.new(bone_name)
        bone.head = (0.0, 0.0, 0.02 + index * 0.001)
        bone.tail = (0.0, 0.0, 0.03 + index * 0.001)
    bpy.ops.object.mode_set(mode="OBJECT")
    return rig


def create_character_mesh(name: str, collection_name: str, rig: bpy.types.Object) -> bpy.types.Object:
    collection = bpy.data.collections.new(collection_name)
    bpy.context.scene.collection.children.link(collection)
    bpy.ops.mesh.primitive_uv_sphere_add(segments=32, ring_count=34, location=(0.0, 0.0, 0.525))
    mesh = bpy.context.object
    mesh.name = name
    mesh.data.name = f"{name}OrganicMesh"
    mesh.scale = (0.18, 0.14, 0.525)
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    for owner in list(mesh.users_collection):
        owner.objects.unlink(mesh)
    collection.objects.link(mesh)
    modifier = mesh.modifiers.new("AlbaRigBinding", "ARMATURE")
    modifier.object = rig
    group = mesh.vertex_groups.new(name="Root")
    group.add(range(len(mesh.data.vertices)), 1.0, "REPLACE")
    side_groups = {
        "L": ["UpperArm.L", "LowerArm.L", "Hand.L", "UpperLeg.L", "LowerLeg.L", "Foot.L"],
        "R": ["UpperArm.R", "LowerArm.R", "Hand.R", "UpperLeg.R", "LowerLeg.R", "Foot.R"],
    }
    for suffix, names in side_groups.items():
        candidates = [
            vertex.index for vertex in mesh.data.vertices
            if (vertex.co.x > 0.04 if suffix == "L" else vertex.co.x < -0.04)
        ]
        for index, bone_name in enumerate(names):
            weighted = mesh.vertex_groups.new(name=bone_name)
            weighted.add([candidates[index % len(candidates)]], 0.5, "REPLACE")
    hips = mesh.vertex_groups.new(name="Hips")
    hips.add([min(mesh.data.vertices, key=lambda vertex: abs(vertex.co.x)).index], 0.5, "REPLACE")
    for polygon in mesh.data.polygons:
        polygon.use_smooth = True
    return mesh


def create_valid_scene() -> tuple[bpy.types.Object, bpy.types.Object, bpy.types.Object]:
    reset_scene()
    rig = create_rig()
    girl = create_character_mesh("GirlOrganicBody", "BodyGirl", rig)
    boy = create_character_mesh("BoyOrganicBody", "BodyBoy", rig)
    return rig, girl, boy


def assert_contains(errors: list[str], fragment: str) -> None:
    assert any(fragment in error for error in errors), (fragment, errors)


def test_valid_character_scene_passes() -> None:
    create_valid_scene()
    assert validator.validate_character() == []


def test_requires_one_exact_rig_and_exact_bone_set() -> None:
    reset_scene()
    assert_contains(validator.validate_character(), "exactly one AlbaHumanoidRig")
    create_valid_scene()
    rig = bpy.data.objects["AlbaHumanoidRig"]
    rig.data.bones["Head"].name = "WrongHead"
    errors = validator.validate_character()
    assert_contains(errors, "missing bones: Head")
    assert_contains(errors, "unexpected bones: WrongHead")


def test_rejects_unbound_mesh_and_missing_weights() -> None:
    _, girl, boy = create_valid_scene()
    girl.modifiers.clear()
    boy.vertex_groups.clear()
    errors = validator.validate_character()
    assert_contains(errors, "mesh GirlOrganicBody is not bound")
    assert_contains(errors, "mesh BoyOrganicBody has unweighted vertices")


def test_rejects_wrong_height_unapplied_scale_and_placeholder_geometry() -> None:
    _, girl, _ = create_valid_scene()
    girl.name = "CubePlaceholder"
    girl.scale.z = 1.2
    errors = validator.validate_character()
    assert_contains(errors, "placeholder mesh name CubePlaceholder")
    assert_contains(errors, "unapplied transform")
    assert_contains(errors, "BodyGirl height")


def test_rejects_excessive_mesh_fragmentation() -> None:
    _, girl, _ = create_valid_scene()
    collection = bpy.data.collections["BodyGirl"]
    for index in range(30):
        fragment = girl.copy()
        fragment.data = girl.data.copy()
        fragment.name = f"GirlMicroFragment{index:02d}"
        collection.objects.link(fragment)

    assert_contains(validator.validate_character(), "excessive mesh fragmentation")


def test_rejects_right_bone_weight_on_left_side_vertex() -> None:
    _, girl, _ = create_valid_scene()
    left_vertex = max(girl.data.vertices, key=lambda vertex: vertex.co.x)
    crossed = girl.vertex_groups["UpperArm.R"]
    crossed.add([left_vertex.index], 0.75, "REPLACE")

    assert_contains(validator.validate_character(), "cross-side weight")


def test_rejects_side_bone_weight_on_centerline_vertex() -> None:
    _, girl, _ = create_valid_scene()
    center_vertex = min(girl.data.vertices, key=lambda vertex: abs(vertex.co.x))
    crossed = girl.vertex_groups["UpperArm.L"]
    crossed.add([center_vertex.index], 0.75, "REPLACE")

    assert_contains(validator.validate_character(), "centerline side weight")


def test_rejects_explosive_evaluated_mesh_during_walk_and_photo_poses() -> None:
    _, girl, _ = create_valid_scene()
    explosive = girl.modifiers.new("ExplosivePoseDeform", "DISPLACE")
    explosive.direction = "X"
    explosive.mid_level = 0.0
    explosive.strength = 10.0

    errors = validator.validate_character()
    assert_contains(errors, "deformation pose walk")
    assert_contains(errors, "deformation pose photo")


def test_requires_all_walk_and_photo_joint_weight_groups_per_character() -> None:
    _, girl, _ = create_valid_scene()
    girl.vertex_groups.remove(girl.vertex_groups["Hand.L"])

    assert_contains(validator.validate_character(), "BodyGirl missing deformation weight groups: Hand.L")


def main() -> None:
    tests = [value for name, value in sorted(globals().items()) if name.startswith("test_")]
    failures: list[str] = []
    for test in tests:
        try:
            test()
            print(f"PASS: {test.__name__}")
        except Exception as exception:
            failures.append(f"{test.__name__}: {exception}")
            print(f"FAIL: {test.__name__}: {exception}")
    reset_scene()
    if failures:
        raise SystemExit("\n".join(failures))


if __name__ == "__main__":
    main()
