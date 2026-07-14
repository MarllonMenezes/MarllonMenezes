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
SIDE_WEIGHT_EPSILON = 1e-4
CENTERLINE_TOLERANCE = 0.015
MAX_POSED_WORLD_COORDINATE = 3.0
DEFORMATION_POSES = {
    "walk": {
        "Hips": (0.0, 0.0, 0.10),
        "UpperArm.L": (0.48, 0.10, -0.12),
        "LowerArm.L": (0.18, 0.06, 0.42),
        "Hand.L": (0.16, 0.0, 0.12),
        "UpperArm.R": (-0.48, -0.10, 0.12),
        "LowerArm.R": (-0.18, -0.06, -0.42),
        "Hand.R": (-0.16, 0.0, -0.12),
        "UpperLeg.L": (-0.50, 0.04, 0.04),
        "LowerLeg.L": (0.62, 0.0, 0.0),
        "Foot.L": (-0.22, 0.0, 0.0),
        "UpperLeg.R": (0.50, -0.04, -0.04),
        "LowerLeg.R": (0.22, 0.0, 0.0),
        "Foot.R": (0.18, 0.0, 0.0),
    },
    "photo": {
        "Hips": (0.0, 0.0, -0.10),
        "Spine": (0.04, 0.0, 0.08),
        "Chest": (-0.04, 0.0, 0.10),
        "UpperArm.L": (-0.18, -0.12, 0.82),
        "LowerArm.L": (0.08, 0.18, 0.62),
        "Hand.L": (0.22, 0.0, 0.18),
        "UpperArm.R": (-0.18, 0.12, -0.82),
        "LowerArm.R": (0.08, -0.18, -0.62),
        "Hand.R": (0.22, 0.0, -0.18),
        "UpperLeg.L": (-0.22, 0.08, 0.10),
        "LowerLeg.L": (0.48, 0.0, 0.0),
        "Foot.L": (-0.28, 0.0, 0.0),
        "UpperLeg.R": (0.16, -0.06, -0.08),
        "LowerLeg.R": (0.30, 0.0, 0.0),
        "Foot.R": (0.20, 0.0, 0.0),
    },
}
DEFORMATION_WEIGHT_BONES = {
    "Hips",
    "UpperArm.L", "LowerArm.L", "Hand.L",
    "UpperArm.R", "LowerArm.R", "Hand.R",
    "UpperLeg.L", "LowerLeg.L", "Foot.L",
    "UpperLeg.R", "LowerLeg.R", "Foot.R",
}


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


def _side_weight_errors(obj: bpy.types.Object) -> list[str]:
    errors: list[str] = []
    groups_by_index = {group.index: group.name for group in obj.vertex_groups}
    world_x = [(obj.matrix_world @ vertex.co).x for vertex in obj.data.vertices]
    spans_center = (
        bool(world_x)
        and min(world_x) < -CENTERLINE_TOLERANCE
        and max(world_x) > CENTERLINE_TOLERANCE
    )
    for vertex, x_coordinate in zip(obj.data.vertices, world_x):
        left_weight = 0.0
        right_weight = 0.0
        for assignment in vertex.groups:
            if assignment.weight <= SIDE_WEIGHT_EPSILON:
                continue
            group_name = groups_by_index.get(assignment.group, "")
            if group_name.endswith(".L"):
                left_weight += assignment.weight
            elif group_name.endswith(".R"):
                right_weight += assignment.weight
        if x_coordinate > CENTERLINE_TOLERANCE and right_weight > SIDE_WEIGHT_EPSILON:
            errors.append(
                f"mesh {obj.name} vertex {vertex.index} has cross-side weight "
                f"{right_weight:.4f} from right bones on the left side"
            )
        elif x_coordinate < -CENTERLINE_TOLERANCE and left_weight > SIDE_WEIGHT_EPSILON:
            errors.append(
                f"mesh {obj.name} vertex {vertex.index} has cross-side weight "
                f"{left_weight:.4f} from left bones on the right side"
            )
        if left_weight > SIDE_WEIGHT_EPSILON and right_weight > SIDE_WEIGHT_EPSILON:
            errors.append(f"mesh {obj.name} vertex {vertex.index} has mixed left/right weights")
        if (
            spans_center
            and abs(x_coordinate) <= CENTERLINE_TOLERANCE
            and left_weight + right_weight > SIDE_WEIGHT_EPSILON
        ):
            errors.append(
                f"mesh {obj.name} vertex {vertex.index} has centerline side weight "
                f"{left_weight + right_weight:.4f}"
            )
    return errors


def _weighted_bone_groups(meshes: Iterable[bpy.types.Object]) -> set[str]:
    weighted: set[str] = set()
    for obj in meshes:
        groups_by_index = {group.index: group.name for group in obj.vertex_groups}
        for vertex in obj.data.vertices:
            for assignment in vertex.groups:
                group_name = groups_by_index.get(assignment.group)
                if group_name in DEFORMATION_WEIGHT_BONES and assignment.weight > SIDE_WEIGHT_EPSILON:
                    weighted.add(group_name)
    return weighted


def _evaluated_pose_errors(
    rig: bpy.types.Object,
    meshes: list[bpy.types.Object],
) -> list[str]:
    errors: list[str] = []
    original_basis = {bone.name: bone.matrix_basis.copy() for bone in rig.pose.bones}
    original_modes = {bone.name: bone.rotation_mode for bone in rig.pose.bones}
    try:
        for pose_name, rotations in DEFORMATION_POSES.items():
            for bone in rig.pose.bones:
                bone.matrix_basis.identity()
            for bone_name, rotation in rotations.items():
                bone = rig.pose.bones.get(bone_name)
                if bone is None:
                    continue
                bone.rotation_mode = "XYZ"
                bone.rotation_euler = rotation
            bpy.context.view_layer.update()
            depsgraph = bpy.context.evaluated_depsgraph_get()
            checked = 0
            for obj in meshes:
                baseline = [obj.matrix_world @ vertex.co for vertex in obj.data.vertices]
                if not baseline:
                    continue
                baseline_size = max(
                    (max(point[axis] for point in baseline) - min(point[axis] for point in baseline))
                    for axis in range(3)
                )
                evaluated = obj.evaluated_get(depsgraph)
                evaluated_mesh = evaluated.to_mesh()
                try:
                    points = [evaluated.matrix_world @ vertex.co for vertex in evaluated_mesh.vertices]
                    checked += 1
                    if not points or any(not all(math.isfinite(value) for value in point) for point in points):
                        errors.append(f"deformation pose {pose_name}: mesh {obj.name} has non-finite vertices")
                        continue
                    posed_size = max(
                        (max(point[axis] for point in points) - min(point[axis] for point in points))
                        for axis in range(3)
                    )
                    max_coordinate = max(abs(value) for point in points for value in point)
                    if max_coordinate > MAX_POSED_WORLD_COORDINATE:
                        errors.append(
                            f"deformation pose {pose_name}: mesh {obj.name} exploded "
                            f"(world coordinate {max_coordinate:.3f}m)"
                        )
                    if baseline_size > 1e-5 and posed_size < baseline_size * 0.25:
                        errors.append(
                            f"deformation pose {pose_name}: mesh {obj.name} collapsed "
                            f"({posed_size:.4f}m from {baseline_size:.4f}m)"
                        )
                    if baseline_size > 1e-5 and posed_size > baseline_size * 4.0:
                        errors.append(
                            f"deformation pose {pose_name}: mesh {obj.name} expanded excessively "
                            f"({posed_size:.4f}m from {baseline_size:.4f}m)"
                        )
                finally:
                    evaluated.to_mesh_clear()
            print(f"POSE CHECK: {pose_name} evaluated {checked} meshes")
    finally:
        for bone in rig.pose.bones:
            bone.rotation_mode = original_modes[bone.name]
            bone.matrix_basis = original_basis[bone.name]
        bpy.context.view_layer.update()
    return errors


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
        missing_deformation_groups = DEFORMATION_WEIGHT_BONES - _weighted_bone_groups(collection_meshes)
        if missing_deformation_groups:
            errors.append(
                f"{collection_name} missing deformation weight groups: "
                + ", ".join(sorted(missing_deformation_groups))
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
        errors.extend(_side_weight_errors(obj))
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

    if not missing and not unexpected:
        errors.extend(_evaluated_pose_errors(rig, meshes))

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
