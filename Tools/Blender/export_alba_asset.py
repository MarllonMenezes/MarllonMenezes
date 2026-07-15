"""Deterministic selected-collection FBX export for Alba World assets."""

from __future__ import annotations

import argparse
import sys
from pathlib import Path

import bpy


def export_collection(collection_name: str, output_path: str) -> None:
    collection = bpy.data.collections.get(collection_name)
    if collection is None:
        raise ValueError(f"collection {collection_name!r} does not exist")

    export_objects = [
        obj for obj in collection.all_objects if obj.type in {"MESH", "ARMATURE"}
    ]
    if not export_objects:
        raise ValueError(f"collection {collection_name!r} has no mesh or armature objects")
    if sum(obj.type == "ARMATURE" for obj in export_objects) != 1:
        raise ValueError(f"collection {collection_name!r} must contain exactly one armature")

    destination = Path(output_path).resolve()
    destination.parent.mkdir(parents=True, exist_ok=True)
    previous_active = bpy.context.view_layer.objects.active
    previous_selection = {obj for obj in bpy.context.selected_objects}
    parent_state = {
        obj: (obj.parent, obj.matrix_parent_inverse.copy(), obj.matrix_world.copy())
        for obj in export_objects
    }
    root = bpy.data.objects.new(collection_name, None)
    collection.objects.link(root)

    try:
        for obj in export_objects:
            world_matrix = obj.matrix_world.copy()
            obj.parent = root
            obj.matrix_world = world_matrix
        bpy.ops.object.select_all(action="DESELECT")
        root.select_set(True)
        for obj in export_objects:
            obj.select_set(True)
        bpy.context.view_layer.objects.active = root
        bpy.context.view_layer.update()

        bpy.ops.export_scene.fbx(
            filepath=str(destination),
            check_existing=False,
            use_selection=True,
            object_types={"EMPTY", "ARMATURE", "MESH"},
            use_mesh_modifiers=True,
            global_scale=1.0,
            apply_unit_scale=True,
            apply_scale_options="FBX_SCALE_UNITS",
            axis_forward="-Z",
            axis_up="Y",
            add_leaf_bones=False,
            use_armature_deform_only=False,
            bake_anim=False,
            path_mode="AUTO",
            embed_textures=False,
        )
    finally:
        for obj, (parent, parent_inverse, world_matrix) in parent_state.items():
            obj.parent = parent
            obj.matrix_parent_inverse = parent_inverse
            obj.matrix_world = world_matrix
        bpy.data.objects.remove(root, do_unlink=True)
        bpy.ops.object.select_all(action="DESELECT")
        for obj in previous_selection:
            if obj.name in bpy.data.objects:
                obj.select_set(True)
        if previous_active and previous_active.name in bpy.data.objects:
            bpy.context.view_layer.objects.active = previous_active
        bpy.context.view_layer.update()

    print(f"EXPORTED: {collection_name} -> {destination}")


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--collection", required=True)
    parser.add_argument("--output", required=True)
    args = parser.parse_args(sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else [])
    export_collection(args.collection, args.output)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
