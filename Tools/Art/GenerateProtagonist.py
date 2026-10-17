"""Rebuild the editable protagonist first pass with Blender 4.2 LTS.

Run Blender --background --factory-startup --python Tools/Art/GenerateProtagonist.py.
Shape parameters below are canonical Unity meters: +Y up, +Z front, +X anatomical
right. Source meshes are converted into Blender's right-handed Z-up authoring space.
Separate mesh parts share a genuine, minimally skinned armature; no clips are authored.
"""
import json
import math
import sys
from pathlib import Path

import bmesh
import bpy
from mathutils import Vector

sys.path.insert(0, str(Path(__file__).resolve().parent))
sys.dont_write_bytecode = True
from ProtagonistRig import bind_rig, inspect_rig, validate_deformation, validate_fbx

ROOT = Path(__file__).resolve().parents[2]
SOURCE = ROOT / "SourceArt/Characters/Protagonist"
MODEL = ROOT / "Assets/_Project/Art/Characters/Protagonist/Protagonist.fbx"
PALETTE = {
    "JacketSage": (0.55, 0.59, 0.55, 1),
    "JacketSeam": (0.42, 0.47, 0.43, 1),
    "JacketWear": (0.61, 0.64, 0.59, 1),
    "ShirtCharcoal": (0.20, 0.23, 0.22, 1),
    "TrousersCharcoal": (0.19, 0.20, 0.21, 1),
    "TrouserSeam": (0.15, 0.16, 0.17, 1),
    "Skin": (0.74, 0.60, 0.50, 1),
    "SkinShade": (0.59, 0.43, 0.35, 1),
    "Lip": (0.47, 0.32, 0.28, 1),
    "HairCharcoal": (0.085, 0.077, 0.070, 1),
    "HairHighlight": (0.135, 0.12, 0.10, 1),
    "Eye": (0.10, 0.09, 0.08, 1),
    "BootBrown": (0.29, 0.22, 0.16, 1),
    "BootSole": (0.14, 0.12, 0.10, 1),
    "BootSeam": (0.39, 0.31, 0.23, 1),
    "CanvasOchre": (0.50, 0.39, 0.24, 1),
    "CanvasWear": (0.59, 0.46, 0.28, 1),
    "Buckle": (0.30, 0.27, 0.21, 1),
    "HospitalIvory": (0.88, 0.88, 0.83, 1),
    "ChestFadedRed": (0.54, 0.30, 0.24, 1),
}
MATERIALS = {}


def authoring(point):
    x, y, z = point
    return Vector((x, z, y))


def srgb_to_linear(value):
    return value / 12.92 if value <= 0.04045 else ((value + 0.055) / 1.055) ** 2.4


def empty(name, parent=None, position=(0, 0, 0)):
    obj = bpy.data.objects.new(name, None)
    bpy.context.collection.objects.link(obj)
    obj.empty_display_size = 0.025
    obj.parent = parent
    obj.location = authoring(position)
    return obj


def mesh_object(name, vertices, faces, material, parent, origin=(0, 0, 0), smooth=False):
    origin = Vector(origin)
    mesh = bpy.data.meshes.new(name + "Mesh")
    # The canonical-to-authoring basis changes handedness; reverse face winding.
    mesh.from_pydata([authoring(Vector(v) - origin) for v in vertices], [], [list(reversed(face)) for face in faces])
    mesh.validate(verbose=False)
    mesh.update()
    bm = bmesh.new()
    bm.from_mesh(mesh)
    bmesh.ops.recalc_face_normals(bm, faces=list(bm.faces))
    bm.to_mesh(mesh)
    bm.free()
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(obj)
    obj.parent = parent
    obj.location = Vector((0, 0, 0))
    obj.data.materials.append(MATERIALS[material])
    for polygon in mesh.polygons:
        polygon.use_smooth = smooth
    return obj


def subdivide_sections(sections, max_step):
    result = []
    for a, b in zip(sections, sections[1:]):
        steps = max(1, math.ceil(abs(b[0] - a[0]) / max_step))
        result.extend(tuple(x + (y - x) * i / steps for x, y in zip(a, b)) for i in range(steps))
    return result + [sections[-1]]


def loft(name, sections, material, parent, origin=(0, 0, 0), segments=16, power=1.0, smooth=True,
         max_step=None):
    if max_step:
        sections = subdivide_sections(sections, max_step)
    vertices = []
    for y, cx, cz, rx, rz in sections:
        for i in range(segments):
            theta = i * math.tau / segments
            sine, cosine = math.sin(theta), math.cos(theta)
            vertices.append((cx + math.copysign(abs(sine) ** power, sine) * rx,
                             y, cz + math.copysign(abs(cosine) ** power, cosine) * rz))
    faces = []
    for row in range(len(sections) - 1):
        for i in range(segments):
            n = (i + 1) % segments
            faces.append((row * segments + i, row * segments + n, (row + 1) * segments + n, (row + 1) * segments + i))
    faces.append(tuple(reversed(range(segments))))
    faces.append(tuple((len(sections) - 1) * segments + i for i in range(segments)))
    return mesh_object(name, vertices, faces, material, parent, origin, smooth)


def box(name, center, size, material, parent, origin=(0, 0, 0), bevel=0.004):
    c, s = Vector(center), Vector(size) * 0.5
    vertices = [c + Vector((x * s.x, y * s.y, z * s.z)) for x, y, z in
                [(-1, -1, -1), (1, -1, -1), (1, 1, -1), (-1, 1, -1),
                 (-1, -1, 1), (1, -1, 1), (1, 1, 1), (-1, 1, 1)]]
    faces = [(0, 3, 2, 1), (4, 5, 6, 7), (0, 4, 7, 3), (1, 2, 6, 5), (3, 7, 6, 2), (0, 1, 5, 4)]
    obj = mesh_object(name, vertices, faces, material, parent, origin)
    if bevel:
        modifier = obj.modifiers.new("Soft sewn edges", 'BEVEL')
        modifier.width = bevel
        modifier.segments = 2
        modifier.affect = 'EDGES'
    return obj


def tube(name, points, widths, depths, material, parent, origin=(0, 0, 0), segments=8, smooth=True):
    points = [Vector(point) for point in points]
    vertices = []
    for i, point in enumerate(points):
        tangent = (points[min(i + 1, len(points) - 1)] - points[max(0, i - 1)]).normalized()
        side = Vector((1, 0, 0))
        if abs(tangent.dot(side)) > 0.9:
            side = Vector((0, 0, 1))
        side = (side - tangent * tangent.dot(side)).normalized()
        normal = tangent.cross(side).normalized()
        for j in range(segments):
            angle = math.tau * j / segments
            vertices.append(point + side * math.cos(angle) * widths[i] + normal * math.sin(angle) * depths[i])
    faces = []
    for i in range(len(points) - 1):
        for j in range(segments):
            n = (j + 1) % segments
            faces.append((i * segments + j, i * segments + n, (i + 1) * segments + n, (i + 1) * segments + j))
    faces += [tuple(reversed(range(segments))), tuple((len(points) - 1) * segments + j for j in range(segments))]
    return mesh_object(name, vertices, faces, material, parent, origin, smooth)


def ribbon(name, points, width, thickness, material, parent, face_sign=1):
    # More longitudinal edges let the strap follow torso bend and bag sway.
    dense = []
    for a, b in zip(points, points[1:]):
        steps = max(1, math.ceil((Vector(b) - Vector(a)).length / .035))
        dense.extend(Vector(a).lerp(Vector(b), i / steps) for i in range(steps))
    points = dense + [Vector(points[-1])]
    vertices = []
    for i, point in enumerate(points):
        previous = Vector(points[max(0, i - 1)])
        following = Vector(points[min(i + 1, len(points) - 1)])
        tangent = (following - previous).normalized()
        normal = Vector((0, 0, face_sign))
        side = tangent.cross(normal).normalized() * width * 0.5
        center = Vector(point)
        vertices.extend([center - side - normal * thickness * 0.5, center + side - normal * thickness * 0.5,
                         center + side + normal * thickness * 0.5, center - side + normal * thickness * 0.5])
    faces = []
    for i in range(len(points) - 1):
        for j in range(4):
            n = (j + 1) % 4
            faces.append((i * 4 + j, i * 4 + n, (i + 1) * 4 + n, (i + 1) * 4 + j))
    faces.extend([(3, 2, 1, 0), tuple((len(points) - 1) * 4 + j for j in range(4))])
    return mesh_object(name, vertices, faces, material, parent)


def jacket_shell(parent):
    # Open-front garment, rather than a painted zipper on a solid torso.
    rings = [(.945, .19, .108, .18), (.98, .193, .113, .19), (1.1, .185, .116, .20),
             (1.23, .19, .12, .21), (1.37, .215, .115, .32), (1.435, .215, .10, .55)]
    rings = subdivide_sections(rings, .035)
    count = 25
    vertices = []
    for y, rx, rz, gap in rings:
        for i in range(count):
            a = gap + (math.tau - 2 * gap) * i / (count - 1)
            vertices.append((math.sin(a) * rx, y, math.cos(a) * rz))
    faces = [(r * count + i, r * count + i + 1, (r + 1) * count + i + 1, (r + 1) * count + i)
             for r in range(len(rings) - 1) for i in range(count - 1)]
    shell = mesh_object("Jacket", vertices, faces, "JacketSage", parent, smooth=True)
    solid = shell.modifiers.new("Cloth thickness", 'SOLIDIFY')
    solid.thickness = .008
    for side in (-1, 1):
        tube("JacketOpeningTrim" + str(side),
             [(side * math.sin(gap) * rx, y, math.cos(gap) * rz + .003) for y, rx, rz, gap in rings],
             [.005] * len(rings), [.004] * len(rings), "JacketSeam", parent)
        x = side * .12
        box("FrontPocket" + str(side), (x, 1.055, .121), (.108, .13, .016), "JacketSage", parent, bevel=.007)
        box("PocketFlap" + str(side), (x, 1.111, .135), (.111, .025, .008), "JacketSeam", parent, bevel=.004)
        tube("PocketStitch" + str(side), [(x - .045, 1.108, .141), (x + .045, 1.108, .141)],
             [.0015, .0015], [.0015, .0015], "JacketWear", parent, segments=6)
    # An open standing collar and a clean, single back yoke.
    for side in (-1, 1):
        points = [(side * .071, 1.433, .091), (side * .105, 1.478, .035),
                  (side * .082, 1.49, -.045), (side * .025, 1.48, -.080)]
        tube("Collar" + str(side), points, [.021, .019, .018, .017], [.011] * 4, "JacketSage", parent)
    tube("BackYokeSeam", [(-.18, 1.363, -.093), (-.09, 1.366, -.117), (0, 1.366, -.122),
                          (.09, 1.366, -.117), (.18, 1.363, -.093)],
         [.002] * 5, [.002] * 5, "JacketSeam", parent, segments=6)
    box("LeftChestTab", (-.146, 1.328, .115), (.057, .021, .008), "ChestFadedRed", parent, bevel=.002)


def arm(parent, side):
    label = "Right" if side > 0 else "Left"
    origin = (side * .207, 1.425, 0)
    pivot = empty(label + "ArmPivot", parent, origin)
    loft(label + "Sleeve", [(1.44, side * .214, -.006, .068, .078),
         (1.36, side * .245, -.002, .069, .073), (1.245, side * .280, .003, .064, .062),
         (1.185, side * .296, .011, .058, .062), (1.155, side * .302, .009, .063, .065),
         (1.08, side * .322, .014, .052, .054), (.98, side * .343, .019, .052, .045),
         (.948, side * .35, .020, .049, .042)], "JacketSage", pivot, origin, segments=14, power=.88, max_step=.025)
    loft(label + "Cuff", [(.968, side * .346, .019, .053, .047),
         (.935, side * .354, .021, .049, .043)], "JacketSeam", pivot, origin, segments=14, power=.85)
    loft(label + "Wrist", [(.95, side * .354, .021, .033, .033),
         (.883, side * .365, .025, .034, .028)], "Skin", pivot, origin, segments=12, max_step=.02)
    loft(label + "Palm", [(.897, side * .366, .025, .035, .026),
         (.855, side * .371, .027, .041, .025), (.823, side * .374, .030, .034, .021)],
         "Skin", pivot, origin, segments=12, power=.8)
    # Four contiguous fingers share one minimal finger bone; the thumb has its own.
    for i in range(4):
        x = side * (.349 + i * .015)
        length = [.041, .051, .047, .036][i]
        tube(label + "Finger" + str(i), [(x, .837, .027), (x + side * .003, .813, .041),
             (x + side * .001, .837 - length, .046)], [.010, .0085, .0065], [.011, .008, .006],
             "Skin", pivot, origin, segments=8)
    tube(label + "Thumb", [(side * .337, .875, .026), (side * .325, .85, .051),
         (side * .331, .83, .062)], [.014, .011, .009], [.012, .01, .008], "Skin", pivot, origin, segments=10)
    if side < 0:
        loft("LeftWristband", [(.934, -.356, .021, .037, .036), (.908, -.361, .023, .037, .035)],
             "HospitalIvory", pivot, origin, segments=16)
    tube(label + "ElbowFold", [(side * .25, 1.18, .047), (side * .3, 1.175, .076),
         (side * .34, 1.18, .052)], [.0025] * 3, [.002] * 3, "JacketSeam", pivot, origin, segments=6)


def leg(parent, side):
    label = "Right" if side > 0 else "Left"
    origin = (side * .10, .91, 0)
    pivot = empty(label + "LegPivot", parent, origin)
    loft(label + "TrouserLeg", [(.94, side * .095, -.008, .091, .094), (.82, side * .103, -.008, .093, .090),
         (.69, side * .109, -.006, .085, .083), (.52, side * .115, .012, .071, .077),
         (.48, side * .117, .018, .075, .080), (.445, side * .118, .013, .068, .070),
         (.33, side * .12, -.002, .067, .065), (.22, side * .12, -.003, .062, .061),
         (.19, side * .12, -.001, .066, .066), (.175, side * .12, .001, .065, .063)],
         "TrousersCharcoal", pivot, origin, segments=14, power=.88, max_step=.025)
    boot_group = empty(label + "Boot", pivot)
    loft(label + "BootUpper", [(.042, side * .12, .052, .071, .133), (.075, side * .12, .052, .075, .139),
         (.10, side * .12, .040, .072, .127), (.132, side * .12, .018, .065, .088),
         (.185, side * .12, -.005, .060, .067), (.218, side * .12, -.009, .060, .061)],
         "BootBrown", boot_group, origin, segments=16, power=.78, max_step=.025)
    loft(label + "BootSole", [(.0, side * .12, .052, .073, .136), (.015, side * .12, .052, .078, .143),
         (.038, side * .12, .052, .078, .142), (.046, side * .12, .052, .074, .138)],
         "BootSole", boot_group, origin, segments=16, power=.8)
    for i in range(3):
        y = .118 + i * .023
        z = .124 - i * .026
        tube(label + "Lace" + str(i), [(side * .12 - .037, y, z), (side * .12, y + .005, z + .004),
             (side * .12 + .037, y, z)], [.0027] * 3, [.0027] * 3, "BootSeam", boot_group, origin, segments=6)


def face(parent):
    head = empty("Head", parent)
    loft("FaceVolume", [(1.485, 0, .018, .038, .035), (1.502, 0, .014, .053, .055),
         (1.535, 0, .006, .065, .071), (1.577, 0, -.002, .079, .079),
         (1.62, 0, -.009, .083, .082), (1.658, 0, -.012, .080, .082),
         (1.697, 0, -.015, .077, .077), (1.727, 0, -.015, .061, .058),
         (1.745, 0, -.015, .028, .026)], "Skin", head, segments=24)
    nose_vertices = [(-.012, 1.64, .070), (.012, 1.64, .070), (-.014, 1.585, .075),
                     (.014, 1.585, .075), (-.010, 1.596, .108), (.010, 1.596, .108), (0, 1.626, .088)]
    mesh_object("Nose", nose_vertices, [(0, 1, 6), (0, 6, 4, 2), (6, 1, 3, 5),
        (4, 6, 5), (2, 4, 5, 3), (0, 2, 3, 1)], "Skin", head, smooth=True)
    for side in (-1, 1):
        label = "Right" if side > 0 else "Left"
        loft(label + "Ear", [(1.58, side * .086, -.012, .012, .015),
             (1.607, side * .093, -.013, .016, .022), (1.637, side * .088, -.014, .013, .018)],
             "Skin", head, segments=12)
        tube(label + "EarFold", [(side * .099, 1.592, -.004), (side * .104, 1.615, -.004),
             (side * .097, 1.632, -.007)], [.003] * 3, [.003] * 3, "SkinShade", head, segments=6)
        eye_x = side * .034
        # Almond-shaped eye slit, not a white sphere stuck on the face.
        vertices = [(eye_x - .017, 1.625, .066), (eye_x - .006, 1.630, .070),
                    (eye_x + .007, 1.629, .070), (eye_x + .017, 1.625, .064),
                    (eye_x + .006, 1.621, .069), (eye_x - .007, 1.621, .070)]
        mesh_object(label + "Eye", vertices, [(5, 4, 3, 2, 1, 0)], "Eye", head)
        tube(label + "UpperLid", [vertices[0], vertices[1], vertices[2], vertices[3]],
             [.0015] * 4, [.0015] * 4, "SkinShade", head, segments=6)
        tube(label + "Eyebrow", [(eye_x - .017, 1.645, .067), (eye_x, 1.649, .071),
             (eye_x + .017, 1.644, .064)], [.0035, .004, .002], [.002] * 3, "HairCharcoal", head, segments=6)
    tube("Mouth", [(-.021, 1.557, .073), (-.009, 1.558, .079), (0, 1.556, .080),
         (.010, 1.558, .079), (.021, 1.557, .073)], [.002] * 5, [.002] * 5, "Lip", head, segments=6)
    return head


def hair(parent):
    root = empty("Hair", parent)
    vertices = []
    segments, rings = 28, 7
    for row in range(rings):
        for i in range(segments):
            theta = math.tau * i / segments
            # Higher forehead, cropped sides and a longer back/nape.
            edge_y = 1.65 + .022 * math.cos(theta) - .030 * max(0, -math.cos(theta))
            polar_max = math.acos((edge_y - 1.65) / .12)
            phi = .055 + (polar_max - .055) * row / (rings - 1)
            radius = 1 + .025 * math.sin(theta * 5 + row * .6)
            vertices.append((.097 * math.sin(phi) * math.sin(theta) * radius,
                             1.65 + .12 * math.cos(phi),
                             -.018 + .086 * math.sin(phi) * math.cos(theta) * radius))
    faces = [(r * segments + i, r * segments + (i + 1) % segments,
              (r + 1) * segments + (i + 1) % segments, (r + 1) * segments + i)
             for r in range(rings - 1) for i in range(segments)]
    faces.append(tuple(reversed(range(segments))))
    mesh_object("HairCap", vertices, faces, "HairCharcoal", root, smooth=True)
    for i in range(8):
        start_x = -.062 + i * .016
        end_x = start_x + .025
        end_y = 1.635 + .045 * (i / 7)
        tube("FringeLock" + str(i), [(start_x - .014, 1.744, -.003), (start_x, 1.752, .033),
             (end_x + .006, 1.718, .078), (end_x, 1.678, .081), (end_x - .015, end_y, .078)],
             [.012, .015, .014, .009, .0015], [.007, .010, .008, .006, .0015],
             "HairHighlight" if i in (2, 5) else "HairCharcoal", root, segments=8)
    for side in (-1, 1):
        for i in range(4):
            z = -.065 + i * .03
            tube("SideLock" + str(side) + "_" + str(i), [(side * .063, 1.739, z),
                 (side * .097, 1.698, z + .012), (side * .093, 1.653, z + .018),
                 (side * .082, 1.625 + .01 * i, z + .024)],
                 [.010, .012, .009, .001], [.005, .007, .005, .001], "HairCharcoal", root, segments=8)
    for i in range(7):
        x = -.065 + i * .0215
        tube("BackLock" + str(i), [(x, 1.743, -.045), (x * 1.15, 1.69, -.103),
             (x * 1.08, 1.637, -.100), (x * .93, 1.605 + .008 * abs(i - 3), -.086)],
             [.012, .014, .010, .001], [.005, .007, .005, .001],
             "HairHighlight" if i == 3 else "HairCharcoal", root, segments=8)


def bag_and_strap(parent):
    bag = empty("RightHipBag", parent)
    box("SatchelBody", (.267, .875, .005), (.13, .25, .20), "CanvasOchre", bag, bevel=.025)
    box("SatchelFlap", (.341, .963, .012), (.018, .10, .203), "CanvasWear", bag, bevel=.012)
    box("SatchelLowerPatch", (.337, .818, .015), (.012, .082, .16), "CanvasWear", bag, bevel=.008)
    box("SatchelFastener", (.355, .917, .012), (.011, .065, .021), "BootBrown", bag, bevel=.003)
    box("SatchelBuckle", (.362, .933, .012), (.008, .022, .029), "Buckle", bag, bevel=.004)
    front = [(-.17, 1.475, .095), (-.11, 1.37, .134), (-.025, 1.245, .137),
             (.07, 1.125, .131), (.17, 1.025, .121), (.269, .997, .092)]
    back = [(-.17, 1.475, -.078), (-.11, 1.37, -.122), (-.025, 1.245, -.128),
            (.07, 1.125, -.121), (.17, 1.025, -.117), (.269, .997, -.082)]
    ribbon("StrapFront", front, .036, .008, "CanvasOchre", parent)
    ribbon("StrapBack", back, .036, .008, "CanvasOchre", parent, face_sign=-1)
    tube("StrapShoulderBridge", [front[0], (-.177, 1.49, .048), (-.177, 1.494, -.028), back[0]],
         [.018] * 4, [.004] * 4, "CanvasOchre", parent, segments=8)


def build():
    bpy.ops.object.select_all(action='SELECT')
    bpy.ops.object.delete(use_global=False)
    bpy.context.scene.unit_settings.system = 'METRIC'
    bpy.context.scene.unit_settings.scale_length = 1.0
    bpy.context.scene.render.engine = 'BLENDER_EEVEE_NEXT'
    for name, color in PALETTE.items():
        material = bpy.data.materials.new(name)
        material.diffuse_color = (*[srgb_to_linear(v) for v in color[:3]], 1)
        material.use_nodes = True
        shader = material.node_tree.nodes.get("Principled BSDF")
        shader.inputs['Base Color'].default_value = material.diffuse_color
        shader.inputs['Roughness'].default_value = .82
        shader.inputs['Metallic'].default_value = 0
        MATERIALS[name] = material
    root = empty("ProtagonistModel")
    motion = empty("MotionRoot", root)
    loft("Shirt", [(.95, 0, 0, .17, .10), (1.25, 0, 0, .17, .108),
         (1.42, 0, 0, .183, .095), (1.455, 0, 0, .080, .068)], "ShirtCharcoal", motion, segments=20, max_step=.035)
    loft("Neck", [(1.425, 0, -.008, .052, .048), (1.51, 0, -.007, .046, .044),
         (1.54, 0, -.008, .052, .047)], "Skin", motion, segments=16, max_step=.02)
    loft("TrouserSeat", [(.855, 0, -.01, .17, .099), (.93, 0, -.01, .175, .10),
         (.99, 0, -.007, .17, .095)], "TrousersCharcoal", motion, segments=18, max_step=.025)
    jacket_shell(motion)
    for side in (-1, 1):
        arm(motion, side)
        leg(motion, side)
    face(motion)
    hair(motion)
    bag_and_strap(motion)
    rig = bind_rig(root, authoring)

    # Persist actual evaluated export measurements in canonical meters.
    deps = bpy.context.evaluated_depsgraph_get()
    metrics = {"version": 2, "colorSpace": "srgb", "canonicalAxes": "+Y up, +Z front, +X anatomical right",
               "authoringAxes": "Blender +Z up, +Y front, +X anatomical right; (x,y,z) -> (x,z,y)",
               "materials": [{"name": name, "color": list(color)} for name, color in PALETTE.items()],
               "parts": [], "triangles": 0, "vertices": 0}
    points = []
    for obj in bpy.context.scene.objects:
        if obj.type != 'MESH':
            continue
        evaluated = obj.evaluated_get(deps)
        mesh = evaluated.to_mesh()
        mesh.calc_loop_triangles()
        canonical = []
        for vertex in mesh.vertices:
            p = evaluated.matrix_world @ vertex.co
            canonical.append((p.x, p.z, p.y))
        points.extend(canonical)
        metrics['vertices'] += len(mesh.vertices)
        metrics['triangles'] += len(mesh.loop_triangles)
        metrics['parts'].append({"name": obj.name, "triangles": len(mesh.loop_triangles),
                                 "materials": [mat.name for mat in mesh.materials]})
        evaluated.to_mesh_clear()
    metrics['boundsMin'] = [min(p[i] for p in points) for i in range(3)]
    metrics['boundsMax'] = [max(p[i] for p in points) for i in range(3)]
    SOURCE.mkdir(parents=True, exist_ok=True)
    MODEL.parent.mkdir(parents=True, exist_ok=True)
    metrics['rig'] = inspect_rig(rig)
    metrics['deformationChecks'] = validate_deformation(rig, authoring, SOURCE)
    assert abs(metrics['boundsMin'][1]) < 1e-6, "Soles must rest at canonical Y=0"
    assert 1.76 < metrics['boundsMax'][1] < 1.78, "Unexpected height"
    (SOURCE / 'model-metrics.json').write_text(json.dumps(metrics, indent=2) + '\n', encoding='utf-8')
    bpy.context.scene.world.color = (.22, .24, .25)
    bpy.context.preferences.filepaths.save_version = 0
    bpy.ops.wm.save_as_mainfile(filepath=str(SOURCE / 'Protagonist.blend'), compress=True)
    bpy.ops.export_scene.fbx(filepath=str(MODEL), use_selection=False, object_types={'MESH', 'EMPTY', 'ARMATURE'},
        global_scale=1.0, apply_unit_scale=True, apply_scale_options='FBX_SCALE_UNITS',
        use_space_transform=True, bake_space_transform=False, axis_forward='-Z', axis_up='Y',
        use_mesh_modifiers=True, mesh_smooth_type='FACE', add_leaf_bones=False, bake_anim=False,
        use_custom_props=False, path_mode='AUTO', use_armature_deform_only=False)
    print("PROTAGONIST_EXPORT " + json.dumps({key: metrics[key] for key in ('vertices', 'triangles', 'boundsMin', 'boundsMax')}))
    validate_fbx(MODEL, SOURCE, metrics)


if __name__ == '__main__':
    build()
