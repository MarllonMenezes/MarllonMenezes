"""Headless validation for Alba World authored Blender assets."""

from __future__ import annotations

import argparse
import math
import sys
from collections.abc import Iterable

import bpy
from mathutils import Vector


REQUIRED_HUMANOID_BONES = {
    "Root", "Hips", "Spine", "Chest", "Neck", "Head",
    "UpperArm.L", "LowerArm.L", "Hand.L", "UpperArm.R", "LowerArm.R", "Hand.R",
    "UpperLeg.L", "LowerLeg.L", "Foot.L", "UpperLeg.R", "LowerLeg.R", "Foot.R",
}

CHARACTER_COLLECTIONS = ("BodyGirl", "BodyBoy")
MIN_CHARACTER_TRIANGLES = 4_000
MAX_CHARACTER_TRIANGLES = 12_000
MIN_CHARACTER_HEIGHT = 1.03
MAX_CHARACTER_HEIGHT = 1.07
MAX_MESH_OBJECTS_PER_CHARACTER = 30
PLACEHOLDER_PREFIXES = ("cube", "sphere", "cylinder", "cone", "plane", "primitive", "placeholder")


def _mesh_objects() -> list[bpy.types.Object]:
    return [obj for obj in bpy.context.scene.objects if obj.type == "MESH"]


def _triangle_count(meshes: Iterable[bpy.types.Object]) -> int:
    return sum(len(polygon.vertices) - 2 for obj in meshes for polygon in obj.data.polygons)


def _collection_height(collection_name: str) -> float | None:
    collection = bpy.data.collections.get(collection_name)
    if collection is None:
        return None
    meshes = [obj for obj in collection.all_objects if obj.type == "MESH"]
    if not meshes:
        return None
    world_corners = [obj.matrix_world @ Vector(corner) for obj in meshes for corner in obj.bound_box]
    return max(corner.z for corner in world_corners) - min(corner.z for corner in world_corners)


def _is_unit_scale(obj: bpy.types.Object) -> bool:
    return all(math.isclose(component, 1.0, abs_tol=1e-5) for component in obj.scale)


def _has_zero_rotation(obj: bpy.types.Object) -> bool:
    return all(math.isclose(component, 0.0, abs_tol=1e-5) for component in obj.rotation_euler)


def _unweighted_vertex_count(obj: bpy.types.Object, bone_names: set[str]) -> int:
    valid_group_indices = {
        group.index for group in obj.vertex_groups if group.name in bone_names
    }
    return sum(
        1
        for vertex in obj.data.vertices
        if not any(group.group in valid_group_indices and group.weight > 0.0 for group in vertex.groups)
    )


def validate_character() -> list[str]:
    errors: list[str] = []
    bpy.context.view_layer.update()
    armatures = [obj for obj in bpy.context.scene.objects if obj.type == "ARMATURE"]
    if len(armatures) != 1 or armatures[0].name != "AlbaHumanoidRig":
        errors.append("character requires exactly one AlbaHumanoidRig armature")
        return errors

    rig = armatures[0]
    actual_bones = set(rig.data.bones.keys())
    missing = REQUIRED_HUMANOID_BONES - actual_bones
    unexpected = actual_bones - REQUIRED_HUMANOID_BONES
    if missing:
        errors.append("missing bones: " + ", ".join(sorted(missing)))
    if unexpected:
        errors.append("unexpected bones: " + ", ".join(sorted(unexpected)))
    if not _is_unit_scale(rig) or not _has_zero_rotation(rig):
        errors.append("armature AlbaHumanoidRig has unapplied transform")

    meshes = _mesh_objects()
    triangles = _triangle_count(meshes)
    if not MIN_CHARACTER_TRIANGLES <= triangles <= MAX_CHARACTER_TRIANGLES:
        errors.append(
            f"character triangle count {triangles} outside "
            f"{MIN_CHARACTER_TRIANGLES}..{MAX_CHARACTER_TRIANGLES}"
        )

    for collection_name in CHARACTER_COLLECTIONS:
        collection = bpy.data.collections.get(collection_name)
        collection_meshes = (
            [obj for obj in collection.all_objects if obj.type == "MESH"]
            if collection is not None
            else []
        )
        if len(collection_meshes) > MAX_MESH_OBJECTS_PER_CHARACTER:
            errors.append(
                f"{collection_name} has excessive mesh fragmentation "
                f"({len(collection_meshes)} > {MAX_MESH_OBJECTS_PER_CHARACTER})"
            )
        height = _collection_height(collection_name)
        if height is None:
            errors.append(f"missing character collection {collection_name}")
        elif not MIN_CHARACTER_HEIGHT <= height <= MAX_CHARACTER_HEIGHT:
            errors.append(
                f"{collection_name} height {height:.4f}m outside "
                f"{MIN_CHARACTER_HEIGHT:.2f}..{MAX_CHARACTER_HEIGHT:.2f}m"
            )

    for obj in meshes:
        armature_modifiers = [modifier for modifier in obj.modifiers if modifier.type == "ARMATURE"]
        if len(armature_modifiers) != 1 or armature_modifiers[0].object != rig:
            errors.append(f"mesh {obj.name} is not bound to AlbaHumanoidRig")
        unweighted = _unweighted_vertex_count(obj, actual_bones)
        if unweighted:
            errors.append(f"mesh {obj.name} has unweighted vertices ({unweighted})")
        if not _is_unit_scale(obj) or not _has_zero_rotation(obj):
            errors.append(f"mesh {obj.name} has unapplied transform")
        lowered_name = obj.name.lower()
        if lowered_name.startswith(PLACEHOLDER_PREFIXES):
            errors.append(f"placeholder mesh name {obj.name}")
        if len(obj.data.vertices) <= 8 or len(obj.data.polygons) <= 6:
            errors.append(f"mesh {obj.name} is a primitive/block placeholder")
        if obj.data.polygons:
            smooth_ratio = sum(1 for polygon in obj.data.polygons if polygon.use_smooth) / len(obj.data.polygons)
            if smooth_ratio < 0.75:
                errors.append(f"mesh {obj.name} is not predominantly smooth shaded")

    return errors


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--profile", required=True, choices=["character", "pet", "furniture"])
    args = parser.parse_args(sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else [])
    errors = validate_character() if args.profile == "character" else []
    for error in errors:
        print("ERROR:", error)
    if not errors:
        print(f"VALID: {args.profile} asset passed")
    return 1 if errors else 0


if __name__ == "__main__":
    raise SystemExit(main())
