"""Build the original Alba Chibi Pop premium-toy shared character bases.

The deterministic source favors continuous, weighted silhouettes over collections
of rigid primitives.  It produces the source blend, atlas, FBXs and review renders.
"""

from __future__ import annotations

import math
import sys
from pathlib import Path

import bpy
import numpy as np
from mathutils import Vector


ROOT = Path(__file__).resolve().parents[2]
BLEND_PATH = ROOT / "Art/Blender/Characters/alba-character-bases.blend"
ATLAS_PATH = ROOT / "Assets/Art3D/Characters/Textures/character-skin-atlas.png"
GIRL_FBX_PATH = ROOT / "Assets/Art3D/Characters/Models/body-girl.fbx"
BOY_FBX_PATH = ROOT / "Assets/Art3D/Characters/Models/body-boy.fbx"
REVIEW_DIR = ROOT / "Art/Reviews/Task5"


BONES = {
    "Root": ((0.0, 0.0, 0.00), (0.0, 0.0, 0.05), None),
    "Hips": ((0.0, 0.0, 0.44), (0.0, 0.0, 0.51), "Root"),
    "Spine": ((0.0, 0.0, 0.51), (0.0, 0.0, 0.61), "Hips"),
    "Chest": ((0.0, 0.0, 0.61), (0.0, 0.0, 0.69), "Spine"),
    "Neck": ((0.0, 0.0, 0.69), (0.0, 0.0, 0.735), "Chest"),
    "Head": ((0.0, 0.0, 0.735), (0.0, 0.0, 0.94), "Neck"),
    "UpperArm.L": ((0.105, 0.0, 0.675), (0.205, 0.0, 0.60), "Chest"),
    "LowerArm.L": ((0.205, 0.0, 0.60), (0.285, 0.0, 0.515), "UpperArm.L"),
    "Hand.L": ((0.285, 0.0, 0.515), (0.35, 0.0, 0.455), "LowerArm.L"),
    "UpperArm.R": ((-0.105, 0.0, 0.675), (-0.205, 0.0, 0.60), "Chest"),
    "LowerArm.R": ((-0.205, 0.0, 0.60), (-0.285, 0.0, 0.515), "UpperArm.R"),
    "Hand.R": ((-0.285, 0.0, 0.515), (-0.35, 0.0, 0.455), "LowerArm.R"),
    "UpperLeg.L": ((0.065, 0.0, 0.45), (0.07, 0.0, 0.255), "Hips"),
    "LowerLeg.L": ((0.07, 0.0, 0.255), (0.07, 0.0, 0.075), "UpperLeg.L"),
    "Foot.L": ((0.07, 0.0, 0.075), (0.07, -0.095, 0.04), "LowerLeg.L"),
    "UpperLeg.R": ((-0.065, 0.0, 0.45), (-0.07, 0.0, 0.255), "Hips"),
    "LowerLeg.R": ((-0.07, 0.0, 0.255), (-0.07, 0.0, 0.075), "UpperLeg.R"),
    "Foot.R": ((-0.07, 0.0, 0.075), (-0.07, -0.095, 0.04), "LowerLeg.R"),
}


def reset_scene() -> None:
    if bpy.context.object and bpy.context.object.mode != "OBJECT":
        bpy.ops.object.mode_set(mode="OBJECT")
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete(use_global=False)
    for collection in list(bpy.data.collections):
        bpy.data.collections.remove(collection)
    for datablocks in (
        bpy.data.meshes, bpy.data.curves, bpy.data.armatures, bpy.data.materials,
        bpy.data.cameras, bpy.data.lights,
    ):
        for datablock in list(datablocks):
            datablocks.remove(datablock)


def create_material(name: str, color: tuple[float, float, float, float], roughness: float) -> bpy.types.Material:
    material = bpy.data.materials.new(name)
    material.use_nodes = True
    material.diffuse_color = color
    shader = material.node_tree.nodes.get("Principled BSDF")
    shader.inputs["Base Color"].default_value = color
    shader.inputs["Roughness"].default_value = roughness
    shader.inputs["Metallic"].default_value = 0.0
    if "Coat Weight" in shader.inputs:
        shader.inputs["Coat Weight"].default_value = 0.12
        shader.inputs["Coat Roughness"].default_value = 0.22
    return material


def create_skin_atlas() -> bpy.types.Image:
    ATLAS_PATH.parent.mkdir(parents=True, exist_ok=True)
    swatches = np.array([
        (0.96, 0.72, 0.56, 1.0), (0.86, 0.56, 0.38, 1.0),
        (0.70, 0.39, 0.23, 1.0), (0.52, 0.27, 0.16, 1.0),
        (0.35, 0.17, 0.10, 1.0), (0.20, 0.09, 0.055, 1.0),
    ], dtype=np.float32)
    pixels = np.ones((1024, 1024, 4), dtype=np.float32)
    tile_width, tile_height = 1024 // 3, 1024 // 2
    for index, color in enumerate(swatches):
        column, row = index % 3, index // 3
        x0, x1 = column * tile_width, 1024 if column == 2 else (column + 1) * tile_width
        y0, y1 = row * tile_height, (row + 1) * tile_height
        pixels[y0:y1, x0:x1] = color
    image = bpy.data.images.new("character-skin-atlas", width=1024, height=1024, alpha=True)
    image.pixels.foreach_set(pixels.ravel())
    image.filepath_raw = str(ATLAS_PATH)
    image.file_format = "PNG"
    image.save()
    return image


def link_only(obj: bpy.types.Object, collection: bpy.types.Collection) -> None:
    for owner in list(obj.users_collection):
        owner.objects.unlink(obj)
    collection.objects.link(obj)


def finish_object(
    obj: bpy.types.Object,
    collection: bpy.types.Collection,
    material: bpy.types.Material,
    rig: bpy.types.Object,
    weights: str | list[dict[str, float]],
) -> bpy.types.Object:
    bpy.context.view_layer.objects.active = obj
    obj.select_set(True)
    bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
    for polygon in obj.data.polygons:
        polygon.use_smooth = True
        polygon.material_index = 0
    obj.data.materials.clear()
    obj.data.materials.append(material)
    link_only(obj, collection)
    modifier = obj.modifiers.new("AlbaRigBinding", "ARMATURE")
    modifier.object = rig
    if isinstance(weights, str):
        weights = [{weights: 1.0} for _ in obj.data.vertices]
    groups: dict[str, bpy.types.VertexGroup] = {}
    for vertex_index, influences in enumerate(weights):
        total = sum(influences.values()) or 1.0
        for bone_name, value in influences.items():
            group = groups.setdefault(bone_name, obj.vertex_groups.get(bone_name) or obj.vertex_groups.new(name=bone_name))
            group.add([vertex_index], value / total, "REPLACE")
    obj["alba_authored_form"] = "continuous-organic"
    obj.select_set(False)
    return obj


def ellipsoid(
    name: str,
    location: tuple[float, float, float],
    scale: tuple[float, float, float],
    material: bpy.types.Material,
    bone_name: str,
    collection: bpy.types.Collection,
    rig: bpy.types.Object,
    *,
    rotation: tuple[float, float, float] = (0.0, 0.0, 0.0),
    segments: int = 16,
    rings: int = 8,
) -> bpy.types.Object:
    bpy.ops.mesh.primitive_uv_sphere_add(
        segments=segments, ring_count=rings, radius=1.0,
        location=location, rotation=rotation,
    )
    obj = bpy.context.object
    obj.name = name
    obj.data.name = f"{name}OrganicMesh"
    obj.scale = scale
    return finish_object(obj, collection, material, rig, bone_name)


def join_objects(name: str, objects: list[bpy.types.Object], material: bpy.types.Material) -> bpy.types.Object:
    bpy.ops.object.select_all(action="DESELECT")
    active = objects[0]
    for obj in objects:
        obj.select_set(True)
    bpy.context.view_layer.objects.active = active
    bpy.ops.object.join()
    active.name = name
    active.data.name = f"{name}OrganicMesh"
    active.data.materials.clear()
    active.data.materials.append(material)
    for polygon in active.data.polygons:
        polygon.material_index = 0
        polygon.use_smooth = True
    active.select_set(False)
    return active


def organic_lathe(
    name: str,
    rings: list[tuple[float, float, float]],
    ring_weights: list[dict[str, float]],
    segments: int,
    material: bpy.types.Material,
    collection: bpy.types.Collection,
    rig: bpy.types.Object,
) -> bpy.types.Object:
    vertices: list[tuple[float, float, float]] = []
    weights: list[dict[str, float]] = []
    for (z, radius_x, radius_y), influences in zip(rings, ring_weights):
        for index in range(segments):
            angle = 2.0 * math.pi * index / segments
            vertices.append((math.cos(angle) * radius_x, math.sin(angle) * radius_y, z))
            weights.append(influences)
    faces: list[tuple[int, ...]] = []
    for ring_index in range(len(rings) - 1):
        base, next_base = ring_index * segments, (ring_index + 1) * segments
        for index in range(segments):
            next_index = (index + 1) % segments
            faces.append((base + index, base + next_index, next_base + next_index, next_base + index))
    bottom_index = len(vertices)
    vertices.append((0.0, 0.0, rings[0][0] - 0.018))
    weights.append(ring_weights[0])
    top_index = len(vertices)
    vertices.append((0.0, 0.0, rings[-1][0] + 0.012))
    weights.append(ring_weights[-1])
    for index in range(segments):
        next_index = (index + 1) % segments
        faces.append((bottom_index, next_index, index))
        last = (len(rings) - 1) * segments
        faces.append((top_index, last + index, last + next_index))
    mesh = bpy.data.meshes.new(f"{name}OrganicMesh")
    mesh.from_pydata(vertices, [], faces)
    mesh.update()
    obj = bpy.data.objects.new(name, mesh)
    collection.objects.link(obj)
    return finish_object(obj, collection, material, rig, weights)


def organic_tube(
    name: str,
    points: list[tuple[float, float, float]],
    radii: list[tuple[float, float]],
    ring_weights: list[dict[str, float]],
    segments: int,
    reference_axis: tuple[float, float, float],
    material: bpy.types.Material,
    collection: bpy.types.Collection,
    rig: bpy.types.Object,
) -> bpy.types.Object:
    path = [Vector(point) for point in points]
    reference = Vector(reference_axis)
    vertices: list[tuple[float, float, float]] = []
    weights: list[dict[str, float]] = []
    for ring_index, (point, radius) in enumerate(zip(path, radii)):
        if ring_index == 0:
            tangent = (path[1] - path[0]).normalized()
        elif ring_index == len(path) - 1:
            tangent = (path[-1] - path[-2]).normalized()
        else:
            tangent = ((path[ring_index] - path[ring_index - 1]).normalized() +
                       (path[ring_index + 1] - path[ring_index]).normalized()).normalized()
        axis_u = (reference - tangent * reference.dot(tangent)).normalized()
        axis_v = tangent.cross(axis_u).normalized()
        for index in range(segments):
            angle = 2.0 * math.pi * index / segments
            vertex = point + axis_u * (math.cos(angle) * radius[0]) + axis_v * (math.sin(angle) * radius[1])
            vertices.append(tuple(vertex))
            weights.append(ring_weights[ring_index])
    faces: list[tuple[int, ...]] = []
    for ring_index in range(len(path) - 1):
        base, next_base = ring_index * segments, (ring_index + 1) * segments
        for index in range(segments):
            next_index = (index + 1) % segments
            faces.append((base + index, base + next_index, next_base + next_index, next_base + index))
    first_center, last_center = len(vertices), len(vertices) + 1
    vertices.extend((tuple(path[0]), tuple(path[-1])))
    weights.extend((ring_weights[0], ring_weights[-1]))
    for index in range(segments):
        next_index = (index + 1) % segments
        faces.append((first_center, next_index, index))
        last = (len(path) - 1) * segments
        faces.append((last_center, last + index, last + next_index))
    mesh = bpy.data.meshes.new(f"{name}OrganicMesh")
    mesh.from_pydata(vertices, [], faces)
    mesh.update()
    obj = bpy.data.objects.new(name, mesh)
    collection.objects.link(obj)
    return finish_object(obj, collection, material, rig, weights)


def oval_patch(
    name: str,
    center: tuple[float, float, float],
    radius_x: float,
    radius_z: float,
    material: bpy.types.Material,
    collection: bpy.types.Collection,
    rig: bpy.types.Object,
    *,
    segments: int = 20,
    rotation: float = 0.0,
    dome: float = 0.003,
) -> bpy.types.Object:
    cx, cy, cz = center
    vertices = [(cx, cy - dome, cz)]
    for index in range(segments):
        angle = 2.0 * math.pi * index / segments
        local_x, local_z = math.cos(angle) * radius_x, math.sin(angle) * radius_z
        x = cx + local_x * math.cos(rotation) - local_z * math.sin(rotation)
        z = cz + local_x * math.sin(rotation) + local_z * math.cos(rotation)
        vertices.append((x, cy, z))
    faces = [(0, index + 1, (index + 1) % segments + 1) for index in range(segments)]
    mesh = bpy.data.meshes.new(f"{name}OrganicMesh")
    mesh.from_pydata(vertices, [], faces)
    mesh.update()
    obj = bpy.data.objects.new(name, mesh)
    collection.objects.link(obj)
    obj.visible_shadow = False
    return finish_object(obj, collection, material, rig, "Head")


def hair_cap(
    name: str,
    material: bpy.types.Material,
    collection: bpy.types.Collection,
    rig: bpy.types.Object,
    *,
    segments: int = 26,
    rings: int = 8,
    tousled: bool = False,
) -> bpy.types.Object:
    """Create a smooth open hair shell with a deliberate chibi hairline."""
    center = Vector((0.0, 0.026, 0.865))
    radius = Vector((0.172, 0.143, 0.185))
    vertices: list[tuple[float, float, float]] = [tuple(center + Vector((0.0, 0.0, radius.z)))]
    for ring_index in range(1, rings + 1):
        progress = ring_index / rings
        for index in range(segments):
            phi = 2.0 * math.pi * index / segments
            front = max(0.0, -math.sin(phi))
            back = max(0.0, math.sin(phi))
            theta_max = 1.76 - 0.46 * front + 0.38 * back
            theta = progress * theta_max
            flow = 1.0 + (0.035 * math.cos(phi * 5.0 + 0.45) * progress * progress if tousled else 0.0)
            vertices.append((
                center.x + radius.x * flow * math.sin(theta) * math.cos(phi),
                center.y + radius.y * flow * math.sin(theta) * math.sin(phi),
                center.z + radius.z * math.cos(theta),
            ))
    faces: list[tuple[int, ...]] = []
    for index in range(segments):
        faces.append((0, 1 + index, 1 + (index + 1) % segments))
    for ring_index in range(rings - 1):
        current = 1 + ring_index * segments
        following = current + segments
        for index in range(segments):
            next_index = (index + 1) % segments
            faces.append((current + index, following + index, following + next_index, current + next_index))
    mesh = bpy.data.meshes.new(f"{name}OrganicMesh")
    mesh.from_pydata(vertices, [], faces)
    mesh.update()
    obj = bpy.data.objects.new(name, mesh)
    collection.objects.link(obj)
    return finish_object(obj, collection, material, rig, "Head")


def directed_hair_lock(
    name: str,
    points: list[tuple[float, float, float]],
    radii: list[tuple[float, float]],
    material: bpy.types.Material,
    collection: bpy.types.Collection,
    rig: bpy.types.Object,
) -> bpy.types.Object:
    return organic_tube(
        name, points, radii, [{"Head": 1.0} for _ in points], 10,
        (0.0, 1.0, 0.0), material, collection, rig,
    )


def smile_curve(name: str, material: bpy.types.Material, collection: bpy.types.Collection, rig: bpy.types.Object) -> bpy.types.Object:
    curve = bpy.data.curves.new(f"{name}OrganicCurve", "CURVE")
    curve.dimensions = "3D"
    curve.resolution_u = 4
    curve.bevel_depth = 0.0032
    curve.bevel_resolution = 3
    spline = curve.splines.new("BEZIER")
    spline.bezier_points.add(2)
    for point, coordinate in zip(
        spline.bezier_points,
        [(-0.040, -0.166, 0.789), (0.0, -0.170, 0.779), (0.040, -0.166, 0.789)],
    ):
        point.co = coordinate
        point.handle_left_type = "AUTO"
        point.handle_right_type = "AUTO"
    obj = bpy.data.objects.new(name, curve)
    collection.objects.link(obj)
    obj.visible_shadow = False
    bpy.context.view_layer.objects.active = obj
    obj.select_set(True)
    bpy.ops.object.convert(target="MESH")
    return finish_object(bpy.context.object, collection, material, rig, "Head")


def create_rig(girl: bpy.types.Collection, boy: bpy.types.Collection) -> bpy.types.Object:
    armature_data = bpy.data.armatures.new("AlbaHumanoidRig")
    rig = bpy.data.objects.new("AlbaHumanoidRig", armature_data)
    girl.objects.link(rig)
    boy.objects.link(rig)
    rig.show_in_front = True
    rig.display_type = "WIRE"
    rig["alba_rest_pose"] = "shared-a-pose-v1"
    bpy.context.view_layer.objects.active = rig
    rig.select_set(True)
    bpy.ops.object.mode_set(mode="EDIT")
    edit_bones: dict[str, bpy.types.EditBone] = {}
    for name, (head, tail, parent_name) in BONES.items():
        bone = armature_data.edit_bones.new(name)
        bone.head, bone.tail, bone.roll, bone.use_connect = head, tail, 0.0, False
        if parent_name:
            bone.parent = edit_bones[parent_name]
        edit_bones[name] = bone
    bpy.ops.object.mode_set(mode="OBJECT")
    rig.select_set(False)
    return rig


def add_body(prefix: str, collection: bpy.types.Collection, rig: bpy.types.Object, materials: dict[str, bpy.types.Material], *, girl: bool) -> None:
    suit = materials["girl_suit" if girl else "boy_suit"]
    hip_width = 1.03 if girl else 0.98
    shoulder_width = 0.98 if girl else 1.03
    organic_lathe(
        f"{prefix}BodySurface",
        [(0.414, 0.088 * hip_width, 0.073), (0.438, 0.121 * hip_width, 0.091),
         (0.470, 0.133 * hip_width, 0.095), (0.510, 0.119 * hip_width, 0.089),
         (0.550, 0.106, 0.083), (0.610, 0.121 * shoulder_width, 0.089),
         (0.650, 0.112 * shoulder_width, 0.082), (0.660, 0.078, 0.061)],
        [{"Hips": 1.0}, {"Hips": 1.0}, {"Hips": 1.0},
         {"Hips": 0.55, "Spine": 0.45}, {"Spine": 1.0},
         {"Spine": 0.35, "Chest": 0.65}, {"Chest": 1.0}, {"Chest": 1.0}],
        24, suit, collection, rig,
    )
    ellipsoid(f"{prefix}Neck", (0.0, 0.004, 0.697), (0.041, 0.038, 0.054), materials["skin"], "Neck", collection, rig, segments=14, rings=7)
    ellipsoid(f"{prefix}Head", (0.0, 0.008, 0.842), (0.174, 0.149, 0.181), materials["skin"], "Head", collection, rig, segments=26, rings=13)

    for side, sign in (("Left", 1.0), ("Right", -1.0)):
        suffix = "L" if sign > 0 else "R"
        upper, lower, hand = f"UpperArm.{suffix}", f"LowerArm.{suffix}", f"Hand.{suffix}"
        arm = organic_tube(
            f"{prefix}Arm{side}",
            [(0.102 * sign, 0.008, 0.657), (0.145 * sign, 0.002, 0.638),
             (0.205 * sign, -0.004, 0.600), (0.247 * sign, -0.010, 0.560),
             (0.282 * sign, -0.010, 0.519), (0.300 * sign, -0.009, 0.502)],
            [(0.034, 0.037), (0.037, 0.040), (0.034, 0.036),
             (0.032, 0.034), (0.028, 0.030), (0.020, 0.022)],
            [{upper: 1.0}, {upper: 1.0}, {upper: 0.55, lower: 0.45},
             {lower: 1.0}, {lower: 0.65, hand: 0.35}, {hand: 1.0}],
            18, (0.0, 1.0, 0.0), materials["skin"], collection, rig,
        )
        palm = ellipsoid(
            f"{prefix}Palm{side}", (0.323 * sign, -0.012, 0.476), (0.052, 0.032, 0.039),
            materials["skin"], hand, collection, rig,
            rotation=(0.0, 0.50 * sign, 0.0), segments=16, rings=8,
        )
        thumb = ellipsoid(
            f"{prefix}Thumb{side}", (0.301 * sign, -0.041, 0.480), (0.018, 0.021, 0.031),
            materials["skin"], hand, collection, rig,
            rotation=(0.0, 0.48 * sign, 0.12 * sign), segments=14, rings=7,
        )
        join_objects(f"{prefix}Arm{side}", [arm, palm, thumb], materials["skin"])

        upper_leg, lower_leg, foot = f"UpperLeg.{suffix}", f"LowerLeg.{suffix}", f"Foot.{suffix}"
        leg = organic_tube(
            f"{prefix}Leg{side}",
            [(0.062 * sign, 0.0, 0.447), (0.068 * sign, 0.0, 0.350),
             (0.070 * sign, -0.004, 0.255), (0.070 * sign, -0.002, 0.202),
             (0.070 * sign, 0.002, 0.132), (0.070 * sign, 0.0, 0.080),
             (0.070 * sign, -0.030, 0.055)],
            [(0.059, 0.057), (0.056, 0.054), (0.043, 0.044), (0.052, 0.050),
             (0.050, 0.048), (0.039, 0.040), (0.017, 0.018)],
            [{upper_leg: 1.0}, {upper_leg: 1.0}, {upper_leg: 0.50, lower_leg: 0.50},
             {lower_leg: 1.0}, {lower_leg: 1.0}, {lower_leg: 0.65, foot: 0.35}, {foot: 1.0}],
            20, (1.0, 0.0, 0.0), materials["skin"], collection, rig,
        )
        foot_mesh = ellipsoid(
            f"{prefix}Foot{side}", (0.070 * sign, -0.075, 0.045), (0.065, 0.094, 0.045),
            materials["skin"], foot, collection, rig,
            rotation=(0.0, 0.0, 0.0), segments=16, rings=8,
        )
        join_objects(f"{prefix}Leg{side}", [leg, foot_mesh], materials["skin"])

    add_face(prefix, collection, rig, materials, girl=girl)


def add_face(prefix: str, collection: bpy.types.Collection, rig: bpy.types.Object, materials: dict[str, bpy.types.Material], *, girl: bool) -> None:
    ears = [
        ellipsoid(f"{prefix}EarLeft", (0.171, 0.006, 0.842), (0.030, 0.021, 0.049), materials["skin"], "Head", collection, rig, segments=14, rings=7),
        ellipsoid(f"{prefix}EarRight", (-0.171, 0.006, 0.842), (0.030, 0.021, 0.049), materials["skin"], "Head", collection, rig, segments=14, rings=7),
    ]
    join_objects(f"{prefix}Ears", ears, materials["skin"])
    ellipsoid(f"{prefix}Nose", (0.0, -0.149, 0.816), (0.018, 0.014, 0.016), materials["skin_highlight"], "Head", collection, rig, segments=12, rings=6)

    feature_groups: dict[str, list[bpy.types.Object]] = {key: [] for key in ("whites", "irises", "pupils", "glints", "brows", "cheeks")}
    for side, x in (("Left", 0.064), ("Right", -0.064)):
        feature_groups["whites"].append(oval_patch(f"{prefix}EyeWhite{side}", (x, -0.143, 0.858), 0.041, 0.050, materials["eye_white"], collection, rig, segments=22, dome=0.005))
        feature_groups["irises"].append(oval_patch(f"{prefix}Iris{side}", (x, -0.150, 0.855), 0.029, 0.037, materials["iris"], collection, rig, segments=20, dome=0.003))
        feature_groups["pupils"].append(oval_patch(f"{prefix}Pupil{side}", (x, -0.155, 0.854), 0.014, 0.022, materials["pupil"], collection, rig, segments=16, dome=0.002))
        feature_groups["glints"].append(oval_patch(f"{prefix}EyeGlint{side}", (x - 0.006, -0.158, 0.866), 0.0055, 0.0075, materials["eye_white"], collection, rig, segments=10, dome=0.001))
        brow_rotation = -0.08 if x > 0 else 0.08
        feature_groups["brows"].append(oval_patch(f"{prefix}Brow{side}", (x, -0.145, 0.922), 0.037 if girl else 0.040, 0.006, materials["hair"], collection, rig, segments=12, rotation=brow_rotation, dome=0.002))
        feature_groups["cheeks"].append(oval_patch(f"{prefix}Cheek{side}", (x * 1.50, -0.137, 0.801), 0.021, 0.009, materials["blush"], collection, rig, segments=14, dome=0.002))
    feature_materials = {
        "whites": "eye_white", "irises": "iris", "pupils": "pupil",
        "glints": "eye_white", "brows": "hair", "cheeks": "blush",
    }
    for group_name, objects in feature_groups.items():
        join_objects(f"{prefix}{group_name.title()}", objects, materials[feature_materials[group_name]])
    smile_curve(f"{prefix}Smile", materials["mouth"], collection, rig)


def add_girl_hair(collection: bpy.types.Collection, rig: bpy.types.Object, materials: dict[str, bpy.types.Material]) -> None:
    hair = materials["hair"]
    parts = [
        hair_cap("GirlHairCap", hair, collection, rig),
        ellipsoid("GirlHairSweep", (0.040, -0.120, 0.958), (0.116, 0.023, 0.044), hair, "Head", collection, rig, rotation=(0.0, 0.0, -0.29), segments=16, rings=8),
        ellipsoid("GirlHairSide", (-0.114, -0.091, 0.920), (0.046, 0.025, 0.057), hair, "Head", collection, rig, rotation=(0.0, 0.0, 0.40), segments=14, rings=7),
        ellipsoid("GirlHairBun", (0.0, 0.137, 0.985), (0.064, 0.074, 0.065), hair, "Head", collection, rig, segments=16, rings=8),
        ellipsoid("GirlHairLockLeft", (0.145, -0.050, 0.810), (0.015, 0.015, 0.080), hair, "Head", collection, rig, rotation=(0.0, 0.15, 0.0), segments=12, rings=6),
        ellipsoid("GirlHairLockRight", (-0.145, -0.050, 0.810), (0.015, 0.015, 0.080), hair, "Head", collection, rig, rotation=(0.0, -0.15, 0.0), segments=12, rings=6),
    ]
    join_objects("GirlHair", parts, hair)
    bow_parts = [
        ellipsoid("GirlBowLeft", (0.046, -0.045, 1.010), (0.055, 0.018, 0.027), materials["girl_bow"], "Head", collection, rig, rotation=(0.0, 0.0, -0.40), segments=12, rings=6),
        ellipsoid("GirlBowRight", (-0.046, -0.045, 1.010), (0.055, 0.018, 0.027), materials["girl_bow"], "Head", collection, rig, rotation=(0.0, 0.0, 0.40), segments=12, rings=6),
        ellipsoid("GirlBowKnot", (0.0, -0.052, 1.007), (0.021, 0.019, 0.024), materials["girl_bow"], "Head", collection, rig, segments=12, rings=6),
    ]
    join_objects("GirlHairBow", bow_parts, materials["girl_bow"])


def add_boy_hair(collection: bpy.types.Collection, rig: bpy.types.Object, materials: dict[str, bpy.types.Material]) -> None:
    hair = materials["hair"]
    parts = [hair_cap("BoyHairCap", hair, collection, rig, tousled=True)]
    locks = [
        ([(-0.010, -0.075, 1.018), (0.043, -0.113, 0.993), (0.112, -0.126, 0.952)], [(0.032, 0.034), (0.027, 0.029), (0.006, 0.008)]),
        ([(0.010, -0.078, 1.016), (-0.043, -0.115, 0.990), (-0.110, -0.125, 0.950)], [(0.032, 0.034), (0.027, 0.029), (0.006, 0.008)]),
        ([(0.075, -0.050, 0.987), (0.130, -0.070, 0.950), (0.165, -0.056, 0.892)], [(0.029, 0.031), (0.024, 0.026), (0.005, 0.007)]),
        ([(-0.075, -0.050, 0.985), (-0.130, -0.070, 0.948), (-0.165, -0.054, 0.890)], [(0.029, 0.031), (0.024, 0.026), (0.005, 0.007)]),
        ([(-0.030, 0.005, 1.018), (0.030, -0.004, 1.031), (0.096, -0.018, 1.010)], [(0.025, 0.027), (0.024, 0.026), (0.005, 0.007)]),
        ([(0.025, 0.012, 1.016), (-0.030, 0.004, 1.034), (-0.088, -0.012, 1.014)], [(0.024, 0.026), (0.023, 0.025), (0.005, 0.007)]),
    ]
    for index, (points, radii) in enumerate(locks):
        parts.append(directed_hair_lock(f"BoyHairLock{index}", points, radii, hair, collection, rig))
    join_objects("BoyHair", parts, hair)


def setup_studio() -> bpy.types.Object:
    scene = bpy.context.scene
    scene.render.engine = "BLENDER_EEVEE_NEXT"
    scene.render.resolution_x = scene.render.resolution_y = 1024
    scene.render.resolution_percentage = 100
    scene.render.image_settings.file_format = "PNG"
    scene.render.image_settings.color_mode = "RGBA"
    scene.view_settings.look = "AgX - Medium High Contrast"
    scene.world.use_nodes = True
    background = scene.world.node_tree.nodes.get("Background")
    background.inputs["Color"].default_value = (0.28, 0.34, 0.46, 1.0)
    background.inputs["Strength"].default_value = 0.65
    for name, location, energy, size, color in [
        ("KeyLight", (-2.4, -3.2, 3.0), 720.0, 2.8, (1.0, 0.82, 0.72)),
        ("FillLight", (2.6, -2.0, 1.8), 470.0, 2.5, (0.70, 0.82, 1.0)),
        ("RimLight", (0.0, 2.6, 2.7), 680.0, 2.2, (0.88, 0.76, 1.0)),
    ]:
        light_data = bpy.data.lights.new(name, "AREA")
        light_data.energy, light_data.shape, light_data.size, light_data.color = energy, "DISK", size, color
        light = bpy.data.objects.new(name, light_data)
        scene.collection.objects.link(light)
        light.location = location
        light.rotation_euler = (Vector((0.0, 0.0, 0.56)) - light.location).to_track_quat("-Z", "Y").to_euler()
    camera_data = bpy.data.cameras.new("Task5ReviewCamera")
    camera = bpy.data.objects.new("Task5ReviewCamera", camera_data)
    scene.collection.objects.link(camera)
    scene.camera = camera
    return camera


def collection_meshes(name: str) -> list[bpy.types.Object]:
    return [obj for obj in bpy.data.collections[name].all_objects if obj.type == "MESH"]


def render_review(camera: bpy.types.Object, filename: str, camera_location: tuple[float, float, float], target: tuple[float, float, float], offsets: tuple[Vector, Vector], *, orthographic: bool) -> None:
    girl_meshes, boy_meshes = collection_meshes("BodyGirl"), collection_meshes("BodyBoy")
    original_locations = {obj: obj.location.copy() for obj in girl_meshes + boy_meshes}
    try:
        for obj in girl_meshes:
            obj.location += offsets[0]
        for obj in boy_meshes:
            obj.location += offsets[1]
        camera.location = camera_location
        camera.rotation_euler = (Vector(target) - camera.location).to_track_quat("-Z", "Y").to_euler()
        camera.data.type = "ORTHO" if orthographic else "PERSP"
        camera.data.ortho_scale, camera.data.lens = 1.48, 67
        bpy.context.scene.render.filepath = str(REVIEW_DIR / filename)
        bpy.context.view_layer.update()
        bpy.ops.render.render(write_still=True)
    finally:
        for obj, location in original_locations.items():
            obj.location = location
        bpy.context.view_layer.update()


def build() -> None:
    reset_scene()
    bpy.context.scene.unit_settings.system = "METRIC"
    bpy.context.scene.unit_settings.scale_length = 1.0
    bpy.context.scene["alba_character_style"] = "Alba Chibi Pop premium-toy realism"
    bpy.context.scene["alba_approved_reference_girl"] = "Art/Concepts/character-girl-turnaround.png"
    bpy.context.scene["alba_approved_reference_boy"] = "Art/Concepts/character-boy-turnaround.png"
    girl_collection, boy_collection = bpy.data.collections.new("BodyGirl"), bpy.data.collections.new("BodyBoy")
    bpy.context.scene.collection.children.link(girl_collection)
    bpy.context.scene.collection.children.link(boy_collection)
    rig = create_rig(girl_collection, boy_collection)
    create_skin_atlas()
    materials = {
        "skin": create_material("CharacterSkin", (0.78, 0.36, 0.18, 1.0), 0.38),
        "skin_highlight": create_material("SkinHighlight", (0.92, 0.52, 0.31, 1.0), 0.34),
        "eye_white": create_material("EyeWhite", (0.92, 0.90, 0.82, 1.0), 0.24),
        "iris": create_material("WarmBrownIris", (0.18, 0.055, 0.018, 1.0), 0.24),
        "pupil": create_material("SoftBlackPupil", (0.006, 0.004, 0.007, 1.0), 0.18),
        "hair": create_material("CocoaHair", (0.075, 0.018, 0.008, 1.0), 0.32),
        "blush": create_material("SoftBlush", (0.86, 0.20, 0.24, 1.0), 0.48),
        "mouth": create_material("WarmSmile", (0.38, 0.025, 0.040, 1.0), 0.40),
        "girl_suit": create_material("GirlBaseSuit", (0.50, 0.30, 0.72, 1.0), 0.46),
        "boy_suit": create_material("BoyBaseSuit", (0.20, 0.60, 0.49, 1.0), 0.46),
        "girl_bow": create_material("GirlHairBow", (0.45, 0.20, 0.70, 1.0), 0.42),
    }
    add_body("Girl", girl_collection, rig, materials, girl=True)
    add_body("Boy", boy_collection, rig, materials, girl=False)
    add_girl_hair(girl_collection, rig, materials)
    add_boy_hair(boy_collection, rig, materials)
    camera = setup_studio()
    BLEND_PATH.parent.mkdir(parents=True, exist_ok=True)
    REVIEW_DIR.mkdir(parents=True, exist_ok=True)
    bpy.ops.wm.save_as_mainfile(filepath=str(BLEND_PATH), check_existing=False)
    render_review(camera, "character-bases-front.png", (0.0, -3.2, 0.54), (0.0, 0.0, 0.54), (Vector((-0.37, 0.0, 0.0)), Vector((0.37, 0.0, 0.0))), orthographic=True)
    render_review(camera, "character-bases-side.png", (3.2, 0.0, 0.54), (0.0, 0.0, 0.54), (Vector((0.0, -0.34, 0.0)), Vector((0.0, 0.34, 0.0))), orthographic=True)
    render_review(camera, "character-bases-back.png", (0.0, 3.2, 0.54), (0.0, 0.0, 0.54), (Vector((0.37, 0.0, 0.0)), Vector((-0.37, 0.0, 0.0))), orthographic=True)
    render_review(camera, "character-bases-neutral.png", (1.32, -2.25, 1.18), (0.0, 0.0, 0.53), (Vector((-0.34, 0.0, 0.0)), Vector((0.34, 0.0, 0.0))), orthographic=False)
    bpy.ops.wm.save_as_mainfile(filepath=str(BLEND_PATH), check_existing=False)
    sys.path.insert(0, str(Path(__file__).resolve().parent))
    from export_alba_asset import export_collection
    export_collection("BodyGirl", str(GIRL_FBX_PATH))
    export_collection("BodyBoy", str(BOY_FBX_PATH))
    bpy.ops.wm.save_as_mainfile(filepath=str(BLEND_PATH), check_existing=False)
    print(f"BUILT: {BLEND_PATH}")


if __name__ == "__main__":
    build()
