"""Deterministic minimal skinning and Blender-side checks for the protagonist.

All joint coordinates and weighting fields use canonical Unity meters. Bone local
axes are intentionally not a runtime contract; drivers should use rest directions.
"""
import math
import json

import bpy
from mathutils import Matrix, Quaternion, Vector

SEMANTIC_GROUPS = ("Head", "Hair", "LeftBoot", "RightBoot", "RightHipBag")
SEMANTIC_NODES = SEMANTIC_GROUPS + ("Jacket", "Shirt", "LeftWristband", "LeftChestTab",
                                    "StrapFront", "StrapBack", "Nose")


def canonical(point):
    return Vector((point.x, point.z, point.y))


def bone_specs():
    # name, parent, canonical head, canonical tail
    specs = [
        ("Root", None, (0, 0, 0), (0, .1, 0)),
        ("Pelvis", "Root", (0, .91, 0), (0, 1.08, 0)),
        ("Spine", "Pelvis", (0, 1.08, 0), (0, 1.27, 0)),
        ("Chest", "Spine", (0, 1.27, 0), (0, 1.43, 0)),
        ("Neck", "Chest", (0, 1.43, 0), (0, 1.52, 0)),
        ("Head", "Neck", (0, 1.52, 0), (0, 1.73, 0)),
    ]
    for side, sign in (("Left", -1), ("Right", 1)):
        def p(x, y, z):
            return (sign * x, y, z)
        specs.extend([
            (side + "UpperArm", "Chest", p(.207, 1.425, 0), p(.296, 1.185, .011)),
            (side + "Forearm", side + "UpperArm", p(.296, 1.185, .011), p(.358, .915, .023)),
            (side + "Hand", side + "Forearm", p(.358, .915, .023), p(.374, .835, .03)),
            (side + "Fingers", side + "Hand", p(.374, .835, .03), p(.374, .786, .046)),
            (side + "Thumb", side + "Hand", p(.337, .875, .026), p(.331, .83, .062)),
            (side + "Thigh", "Pelvis", p(.10, .91, 0), p(.117, .48, .018)),
            (side + "Shin", side + "Thigh", p(.117, .48, .018), p(.12, .12, -.009)),
            (side + "Foot", side + "Shin", p(.12, .12, -.009), p(.12, .045, .13)),
            (side + "Toe", side + "Foot", p(.12, .045, .13), p(.12, .045, .192)),
        ])
    specs.append(("Bag", "Pelvis", (.267, .95, .005), (.267, .8, .005)))
    return [("Rig_" + n, "Rig_" + parent if parent else None, h, t) for n, parent, h, t in specs]


def smooth(a, b, value):
    t = max(0.0, min(1.0, (value - a) / (b - a)))
    return t * t * (3 - 2 * t)


def blend(a, b, amount):
    weights = {name: weight * (1 - amount) for name, weight in a.items()}
    for name, weight in b.items():
        weights[name] = weights.get(name, 0) + weight * amount
    return weights


def hip_fraction(p):
    lateral = smooth(.015, .075, abs(p.x))
    return (1 - smooth(.83, .97, p.y)) * lateral + (1 - smooth(.78, .84, p.y)) * (1 - lateral)


def shoulder_fraction(p):
    return smooth(0, .11, abs(p.x) - .16 + .45 * (1.43 - p.y))


def torso_weights(p, shoulders=True):
    weights = blend({"Pelvis": 1}, {"Spine": 1}, smooth(.99, 1.14, p.y))
    weights = blend(weights, {"Chest": 1}, smooth(1.16, 1.32, p.y))
    side = "Left" if p.x < 0 else "Right"
    if shoulders:
        amount = shoulder_fraction(p) * smooth(1.30, 1.36, p.y)
        weights = blend(weights, {side + "UpperArm": 1}, amount)
    return blend(weights, {side + "Thigh": 1}, hip_fraction(p))


def skin_weights(name, p, is_head=False):
    if is_head:
        return {"Head": 1}
    if name == "LeftWristband":
        return {"LeftForearm": 1}
    if name == "LeftChestTab":
        return {"Chest": 1}
    if name.startswith("Satchel"):
        return {"Bag": 1}
    if name.startswith("Strap"):
        return blend(torso_weights(p, shoulders=False), {"Bag": 1}, 1 - smooth(.997, 1.12, p.y))
    if name == "Neck":
        weights = blend({"Chest": 1}, {"Neck": 1}, smooth(1.425, 1.47, p.y))
        return blend(weights, {"Head": 1}, smooth(1.49, 1.535, p.y))
    if name == "TrouserSeat":
        side = "Left" if p.x < 0 else "Right"
        return blend({"Pelvis": 1}, {side + "Thigh": 1}, hip_fraction(p))
    for side in ("Left", "Right"):
        if not name.startswith(side):
            continue
        part = name[len(side):]
        if part in ("Sleeve", "Cuff", "ElbowFold"):
            weights = blend({side + "Forearm": 1}, {side + "UpperArm": 1}, smooth(1.12, 1.25, p.y))
            chest = (1 - shoulder_fraction(p)) * smooth(1.30, 1.36, p.y)
            return blend(weights, {"Chest": 1}, chest)
        if part == "Wrist":
            return blend({side + "Hand": 1}, {side + "Forearm": 1}, smooth(.895, .94, p.y))
        if part == "Palm" or part.startswith("Finger"):
            return blend({side + "Fingers": 1}, {side + "Hand": 1}, smooth(.815, .852, p.y))
        if part == "Thumb":
            return blend({side + "Thumb": 1}, {side + "Hand": 1}, smooth(.85, .884, p.y))
        if part == "TrouserLeg":
            weights = blend({side + "Shin": 1}, {side + "Thigh": 1}, smooth(.415, .545, p.y))
            return blend({"Pelvis": 1}, weights, hip_fraction(p))
        if part.startswith("Boot") or part.startswith("Lace"):
            weights = blend({side + "Foot": 1}, {side + "Toe": 1},
                            smooth(.085, .16, p.z) * (1 - smooth(.08, .15, p.y)))
            return blend(weights, {side + "Shin": 1}, smooth(.13, .22, p.y))
    return torso_weights(p)


def bind_rig(root, authoring):
    bpy.context.view_layer.update()
    meshes = [obj for obj in bpy.context.scene.objects if obj.type == 'MESH']
    world_matrices = {obj.name: obj.matrix_world.copy() for obj in meshes}
    head_parts = set()
    semantic_parents = {}
    for obj in meshes:
        parent = obj.parent
        while parent:
            if parent.name in SEMANTIC_GROUPS:
                semantic_parents[obj.name] = parent.name
            if parent.name in ("Head", "Hair"):
                head_parts.add(obj.name)
            parent = parent.parent
    data = bpy.data.armatures.new("ProtagonistSkeleton")
    rig = bpy.data.objects.new("ProtagonistArmature", data)
    bpy.context.collection.objects.link(rig)
    rig.parent = root
    rig.show_in_front = True
    data.display_type = 'STICK'
    bpy.context.view_layer.objects.active = rig
    rig.select_set(True)
    bpy.ops.object.mode_set(mode='EDIT')
    for name, parent, head, tail in bone_specs():
        bone = data.edit_bones.new(name)
        bone.head, bone.tail = authoring(head), authoring(tail)
        bone.use_deform = True
        if parent:
            bone.parent = data.edit_bones[parent]
    bpy.ops.object.mode_set(mode='OBJECT')
    for name in SEMANTIC_GROUPS:
        group = bpy.data.objects[name]
        group.parent = rig
        group.matrix_world = Matrix.Identity(4)
    bpy.context.view_layer.update()
    for obj in meshes:
        bpy.context.view_layer.objects.active = obj
        # Bake only existing geometry modifiers, before skinning. The saved mesh
        # remains editable and the procedural source can regenerate its modifiers.
        for modifier in list(obj.modifiers):
            bpy.ops.object.modifier_apply(modifier=modifier.name)
        obj.data.transform(world_matrices[obj.name])
        obj.parent = bpy.data.objects[semantic_parents[obj.name]] if obj.name in semantic_parents else rig
        obj.matrix_world = Matrix.Identity(4)
        groups = {}
        for vertex in obj.data.vertices:
            weights = skin_weights(obj.name, canonical(vertex.co), obj.name in head_parts)
            weights = sorted((("Rig_" + n, w) for n, w in weights.items() if w >= .001),
                             key=lambda item: (-item[1], item[0]))
            assert 0 < len(weights) <= 4, (obj.name, vertex.index, weights)
            total = sum(w for _, w in weights)
            for name, weight in weights:
                if name not in groups:
                    groups[name] = obj.vertex_groups.new(name=name)
                groups[name].add([vertex.index], weight / total, 'REPLACE')
        modifier = obj.modifiers.new("Protagonist skin", 'ARMATURE')
        modifier.object = rig
        modifier.use_deform_preserve_volume = False  # Match Unity linear blend skinning.
        modifier.use_vertex_groups = True
    for obj in list(bpy.context.scene.objects):
        if obj.type == 'EMPTY' and obj != root and obj.name not in SEMANTIC_GROUPS:
            bpy.data.objects.remove(obj, do_unlink=True)
    bpy.context.view_layer.update()
    return rig


def inspect_rig(rig):
    specs = bone_specs()
    assert len(rig.data.bones) == 25
    assert {bone.name for bone in rig.data.bones} == {s[0] for s in specs}
    bones = []
    for name, parent, head, tail in specs:
        bone = rig.data.bones[name]
        assert (bone.parent.name if bone.parent else None) == parent
        assert (canonical(bone.head_local) - Vector(head)).length < 1e-6
        bones.append({"name": name, "parent": parent, "head": list(canonical(bone.head_local)),
                      "tail": list(canonical(bone.tail_local))})
    error, minimum, maximum, count, multi = 0, 1, 0, 0, 0
    part_stats = []
    meshes = [o for o in bpy.context.scene.objects if o.type == 'MESH']
    usage = {bone.name: 0 for bone in rig.data.bones}
    for obj in meshes:
        assert (obj.parent == rig or obj.parent.name in SEMANTIC_GROUPS) and len(obj.modifiers) == 1
        assert obj.modifiers[0].type == 'ARMATURE' and obj.modifiers[0].object == rig
        assert (obj.scale - Vector((1, 1, 1))).length < 1e-6
        blended = 0
        for vertex in obj.data.vertices:
            assert 1 <= len(vertex.groups) <= 4
            total = sum(g.weight for g in vertex.groups)
            error = max(error, abs(total - 1))
            maximum = max(maximum, len(vertex.groups))
            multi += len(vertex.groups) > 1
            blended += len(vertex.groups) > 1
            count += 1
            for group in vertex.groups:
                name = obj.vertex_groups[group.group].name
                assert name in usage and math.isfinite(group.weight) and 0 < group.weight <= 1
                minimum = min(minimum, group.weight)
                usage[name] += 1
        part_stats.append({"name": obj.name, "vertices": len(obj.data.vertices), "blendedVertices": blended,
                           "groups": sorted(g.name for g in obj.vertex_groups)})
    assert error < 1e-6
    assert all(count > 0 for name, count in usage.items() if name != 'Rig_Root')
    for name, bone in (("LeftWristband", "Rig_LeftForearm"), ("LeftChestTab", "Rig_Chest"),
                       ("SatchelBody", "Rig_Bag")):
        obj = bpy.data.objects[name]
        assert all(len(v.groups) == 1 and obj.vertex_groups[v.groups[0].group].name == bone
                   for v in obj.data.vertices)
    assert all(o.scale.x > 0 and o.scale.y > 0 and o.scale.z > 0 for o in bpy.context.scene.objects)
    assert not any(o.name.endswith("Pivot") for o in bpy.context.scene.objects)
    assert all(name in bpy.context.scene.objects for name in SEMANTIC_NODES)
    assert multi >= 100
    assert not bpy.data.actions
    for name in ("LeftEye", "RightEye"):
        assert all(canonical(p.normal).z > .8 for p in bpy.data.objects[name].data.polygons)
    for side in ("Left", "Right"):
        for part in ("Sleeve", "TrouserLeg"):
            obj = bpy.data.objects[side + part]
            neighbors = {v.index: set() for v in obj.data.vertices}
            for edge in obj.data.edges:
                a, b = edge.vertices
                neighbors[a].add(b)
                neighbors[b].add(a)
            visited, pending = set(), [0]
            while pending:
                index = pending.pop()
                if index not in visited:
                    visited.add(index)
                    pending.extend(neighbors[index] - visited)
            assert len(visited) == len(neighbors), "Joint surface must be connected"
    return {"armature": rig.name, "boneCount": len(bones), "skinnedMeshCount": len(meshes),
            "bones": bones, "weightedVertices": count, "blendedVertices": multi,
            "maxInfluences": maximum, "maxWeightSumError": error, "minimumWeight": minimum,
            "boneVertexUsage": usage, "parts": part_stats, "semanticNodes": list(SEMANTIC_NODES), "clips": 0}


def evaluated_points(obj, deps):
    evaluated = obj.evaluated_get(deps)
    mesh = evaluated.to_mesh()
    points = [evaluated.matrix_world @ v.co for v in mesh.vertices]
    evaluated.to_mesh_clear()
    return points


def set_rotation(rig, name, canonical_axis, degrees, authoring):
    bone = rig.pose.bones["Rig_" + name]
    # The (x,z,y) basis is a reflection: axial vectors need its determinant -1.
    local_axis = bone.bone.matrix_local.to_3x3().inverted() @ -authoring(canonical_axis)
    bone.rotation_mode = 'QUATERNION'
    bone.rotation_quaternion = Quaternion(local_axis, math.radians(degrees))


def reset_pose(rig):
    for bone in rig.pose.bones:
        bone.matrix_basis = Matrix.Identity(4)
    bpy.context.view_layer.update()


def render_check(source, name, camera_position, target=(0, .90, 0)):
    # Temporary workbench evidence only; cameras/lights never enter source or FBX.
    scene = bpy.context.scene
    old_engine = scene.render.engine
    scene.render.engine = 'BLENDER_WORKBENCH'
    scene.render.resolution_x, scene.render.resolution_y = 640, 960
    scene.render.resolution_percentage = 100
    scene.render.image_settings.file_format = 'PNG'
    scene.display.shading.light = 'STUDIO'
    scene.display.shading.color_type = 'MATERIAL'
    scene.display.shading.show_shadows = True
    scene.display.shading.show_cavity = True
    scene.display.shading.background_type = 'WORLD'
    scene.world.color = (.11, .12, .13)
    data = bpy.data.cameras.new("RigCheckCamera")
    camera = bpy.data.objects.new("RigCheckCamera", data)
    bpy.context.collection.objects.link(camera)
    position = Vector((camera_position[0], camera_position[2], camera_position[1]))
    look_at = Vector((target[0], target[2], target[1]))
    camera.location = position
    camera.rotation_euler = (look_at - position).to_track_quat('-Z', 'Y').to_euler()
    data.type, data.ortho_scale = 'ORTHO', 2.05
    scene.camera = camera
    scene.render.filepath = str(source / (name + '.png'))
    bpy.ops.render.render(write_still=True)
    scene.camera = None
    bpy.data.objects.remove(camera, do_unlink=True)
    bpy.data.cameras.remove(data)
    scene.render.engine = old_engine


def validate_deformation(rig, authoring, source):
    meshes = [o for o in bpy.context.scene.objects if o.type == 'MESH']
    deps = bpy.context.evaluated_depsgraph_get()
    rest = {o.name: evaluated_points(o, deps) for o in meshes}
    rest_error = max((point - obj.data.vertices[i].co).length
                     for obj in meshes for i, point in enumerate(rest[obj.name]))
    assert rest_error < 1e-6
    render_check(source, "rig-rest-front", (0, .92, 4))
    render_check(source, "rig-rest-back", (0, .92, -4))
    cases = [
        ("elbow90", [("LeftForearm", (1, 0, 0), -90)], "LeftSleeve", .10),
        ("knee70", [("LeftShin", (1, 0, 0), 70)], "LeftTrouserLeg", .10),
        ("shoulder45", [("LeftUpperArm", (0, 0, 1), -45)], "LeftSleeve", .10),
        ("hip30", [("RightThigh", (1, 0, 0), -30)], "RightTrouserLeg", .10),
        ("neck25", [("Neck", (0, 1, 0), 25)], "FaceVolume", .02),
        ("hand25", [("LeftHand", (1, 0, 0), -25)], "LeftPalm", .01),
        ("finger30", [("LeftFingers", (1, 0, 0), -30)], "LeftFinger1", .01),
        ("thumb20", [("LeftThumb", (0, 1, 0), 20)], "LeftThumb", .005),
        ("toe20", [("LeftToe", (1, 0, 0), -20)], "LeftBootSole", .005),
        ("bag6", [("Bag", (0, 0, 1), 6)], "SatchelBody", .01),
        ("torso15", [("Spine", (1, 0, 0), -15)], "Jacket", .03),
    ]
    results = []
    for name, rotations, affected, threshold in cases:
        reset_pose(rig)
        for bone, axis, angle in rotations:
            set_rotation(rig, bone, axis, angle, authoring)
        bpy.context.view_layer.update()
        matrices = {b.name: b.matrix @ b.bone.matrix_local.inverted() for b in rig.pose.bones}
        error, displacement, points = 0, 0, []
        for obj in meshes:
            evaluated = evaluated_points(obj, deps)
            assert len(evaluated) == len(obj.data.vertices)
            for i, p in enumerate(evaluated):
                assert all(math.isfinite(v) for v in p)
                vertex = obj.data.vertices[i]
                expected = sum((matrices[obj.vertex_groups[g.group].name] @ vertex.co * g.weight
                                for g in vertex.groups), Vector())
                error = max(error, (p - expected).length)
                if obj.name == affected:
                    displacement = max(displacement, (p - rest[obj.name][i]).length)
            points.extend(canonical(p) for p in evaluated)
        assert error < 1e-5, (name, error)
        assert displacement > threshold, (name, displacement)
        results.append({"name": name, "affectedMesh": affected, "maxDisplacement": displacement,
                        "linearSkinMaxError": error, "boundsMin": [min(p[i] for p in points) for i in range(3)],
                        "boundsMax": [max(p[i] for p in points) for i in range(3)]})
    reset_pose(rig)
    # A simultaneous reach / leg-bend stress pose, not an animation clip.
    for bone, axis, angle in [("LeftUpperArm", (1, 0, 0), -35), ("LeftForearm", (1, 0, 0), -80),
                              ("RightThigh", (1, 0, 0), -25), ("RightShin", (1, 0, 0), 65),
                              ("LeftFingers", (1, 0, 0), -25), ("Bag", (0, 0, 1), 6)]:
        set_rotation(rig, bone, axis, angle, authoring)
    bpy.context.view_layer.update()
    render_check(source, "rig-bend-front", (0, .92, 4))
    render_check(source, "rig-bend-side", (-4, .92, 0))
    reset_pose(rig)
    reset_error = max((p - rest[obj.name][i]).length for obj in meshes
                      for i, p in enumerate(evaluated_points(obj, deps)))
    assert reset_error < 1e-6
    return {"restMaxError": rest_error, "poseResetMaxError": reset_error, "cases": results,
            "scope": "Blender linear skin deformation; not Unity import or motion acceptance"}


def validate_fbx(path, source, source_metrics):
    """Check the actual exported file in a clean scene, after saving editable source."""
    bpy.ops.object.select_all(action='SELECT')
    bpy.ops.object.delete(use_global=False)
    bpy.ops.import_scene.fbx(filepath=str(path), use_anim=False, ignore_leaf_bones=False)
    rigs = [o for o in bpy.context.scene.objects if o.type == 'ARMATURE']
    assert all(name in bpy.context.scene.objects for name in SEMANTIC_NODES)
    assert len(rigs) == 1 and len(rigs[0].data.bones) == 25
    rig = rigs[0]
    joint_error = 0
    for name, parent, head, _ in bone_specs():
        bone = rig.data.bones[name]
        assert (bone.parent.name if bone.parent else None) == parent
        joint_error = max(joint_error, (canonical(rig.matrix_world @ bone.head_local) - Vector(head)).length)
    assert joint_error < 1e-5
    meshes = [o for o in bpy.context.scene.objects if o.type == 'MESH']
    assert {o.name for o in meshes} == {p['name'] for p in source_metrics['parts']}
    points, triangles, vertices, error, maximum = [], 0, 0, 0, 0
    deps = bpy.context.evaluated_depsgraph_get()
    for obj in meshes:
        assert any(m.type == 'ARMATURE' and m.object == rig for m in obj.modifiers)
        for vertex in obj.data.vertices:
            assert 1 <= len(vertex.groups) <= 4
            assert all(obj.vertex_groups[g.group].name in rig.data.bones for g in vertex.groups)
            error = max(error, abs(sum(g.weight for g in vertex.groups) - 1))
            maximum = max(maximum, len(vertex.groups))
        evaluated = obj.evaluated_get(deps)
        mesh = evaluated.to_mesh()
        mesh.calc_loop_triangles()
        triangles += len(mesh.loop_triangles)
        vertices += len(mesh.vertices)
        points.extend(canonical(evaluated.matrix_world @ v.co) for v in mesh.vertices)
        evaluated.to_mesh_clear()
    low = [min(p[i] for p in points) for i in range(3)]
    high = [max(p[i] for p in points) for i in range(3)]
    bounds_error = max(abs(a - b) for key, value in (("boundsMin", low), ("boundsMax", high))
                       for a, b in zip(source_metrics[key], value))
    assert triangles == source_metrics['triangles'] and bounds_error < 1e-5 and error < 1e-6
    assert all(min(obj.scale) > 0 for obj in bpy.context.scene.objects)
    report = {"scope": "Blender FBX export/import roundtrip; Unity native import remains independent",
              "armatureCount": len(rigs), "boneCount": len(rig.data.bones), "skinnedMeshCount": len(meshes),
              "vertices": vertices, "triangles": triangles, "boundsMin": low, "boundsMax": high,
              "maxJointPositionError": joint_error, "maxBoundsError": bounds_error,
              "maxWeightSumError": error, "maxInfluences": maximum, "allObjectScalesPositive": True,
              "semanticNodes": list(SEMANTIC_NODES)}
    (source / 'fbx-roundtrip-metrics.json').write_text(json.dumps(report, indent=2) + '\n', encoding='utf-8')
    print("PROTAGONIST_FBX_ROUNDTRIP " + json.dumps(report))
