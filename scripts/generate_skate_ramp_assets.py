#!/usr/bin/env python3
"""SPDX-License-Identifier: Apache-2.0

Author the original CC0 skate park model pack with pinned Blender 5.2.0.
Run with Blender, passing --output and optionally --preview-dir after --.
"""

import argparse
import hashlib
import json
import math
import os
import struct
import sys
from pathlib import Path

import bmesh
import bpy
from mathutils import Vector


NAMES = (
    "skate_bank", "skate_kicker", "skate_quarterpipe", "skate_ledge_low",
    "skate_ledge_high", "skate_long_ledge", "skate_funbox", "skate_halfpipe",
    "skate_qp_round_mini", "skate_qp_round", "skate_qp_round_large", "skate_mega_halfpipe",
)
LOWER_RUN = 3 * math.cos(math.radians(15))
LOWER_RISE = 3 * math.sin(math.radians(15))
TRANSITION_RUN = LOWER_RUN + 3 * math.cos(math.radians(35))
TRANSITION_RISE = LOWER_RISE + 3 * math.sin(math.radians(35))
LOWER_SLOPE = math.tan(math.radians(15))
UPPER_SLOPE = math.tan(math.radians(35))
ENTRY_BLEND = 0.25
MIDDLE_HALF_BLEND = 0.10
EXIT_BLEND = 0.20
BLEND_STEPS = 16
SKIN = 0.14
COPING_RADIUS = 0.03
SOURCE = "scripts/generate_skate_ramp_assets.py"


def point(right, forward, height):
    return (right, -forward, height)


def material(name, color, metallic=0.0, roughness=0.6):
    mat = bpy.data.materials.new(name)
    mat.diffuse_color = (*color, 1.0)
    mat.use_nodes = True
    shader = mat.node_tree.nodes["Principled BSDF"]
    shader.inputs["Base Color"].default_value = (*color, 1.0)
    shader.inputs["Metallic"].default_value = metallic
    shader.inputs["Roughness"].default_value = roughness
    return mat


def clear():
    for obj in list(bpy.data.objects):
        bpy.data.objects.remove(obj, do_unlink=True)
    for collection in list(bpy.data.collections):
        bpy.data.collections.remove(collection)
    for data in (bpy.data.meshes, bpy.data.materials, bpy.data.cameras, bpy.data.lights):
        for item in list(data):
            if item.users == 0:
                data.remove(item)
    scene = bpy.context.scene
    scene.unit_settings.system = "METRIC"
    scene.unit_settings.scale_length = 1.0
    for name in ("Visual", "CollisionProxies"):
        collection = bpy.data.collections.new(name)
        scene.collection.children.link(collection)
    return {
        "concrete": material("Warm concrete", (0.48, 0.51, 0.49), roughness=0.82),
        "wood": material("Birch ride panels", (0.53, 0.32, 0.14), roughness=0.66),
        "wood_light": material("Plywood edge", (0.72, 0.50, 0.26), roughness=0.74),
        "wood_dark": material("Panel alternate", (0.39, 0.22, 0.10), roughness=0.72),
        "metal": material("Brushed steel", (0.36, 0.43, 0.47), metallic=0.88, roughness=0.32),
        "frame": material("Painted teal frame", (0.035, 0.23, 0.25), metallic=0.55, roughness=0.5),
        "dark": material("Steel underside", (0.045, 0.065, 0.075), metallic=0.7, roughness=0.52),
        "mark": material("Safety ochre", (0.90, 0.48, 0.065), roughness=0.64),
    }


def finish(obj, name, mat, collision=False):
    obj.name = ("COLLISION__" if collision else "VISUAL__") + name
    obj["role"] = "collision" if collision else "visual"
    obj["collision_kind"] = "static_concave" if collision else "none"
    obj["license"] = "CC0-1.0"
    for collection in list(obj.users_collection):
        collection.objects.unlink(obj)
    bpy.data.collections["CollisionProxies" if collision else "Visual"].objects.link(obj)
    if mat is not None:
        obj.data.materials.append(mat)
    return obj


def mesh(name, vertices, faces, mat, collision=False, smooth_faces=()):
    data = bpy.data.meshes.new(name)
    data.from_pydata(vertices, [], faces)
    data.update()
    bm = bmesh.new()
    bm.from_mesh(data)
    bmesh.ops.recalc_face_normals(bm, faces=list(bm.faces))
    if bm.calc_volume(signed=True) < 0:
        bmesh.ops.reverse_faces(bm, faces=list(bm.faces))
    bm.to_mesh(data)
    bm.free()
    obj = bpy.data.objects.new(name, data)
    bpy.context.scene.collection.objects.link(obj)
    finish(obj, name, mat, collision)
    for index in smooth_faces:
        data.polygons[index].use_smooth = True
    return obj


def box(name, center, size, mat, bevel=0.0, collision=False):
    bpy.ops.mesh.primitive_cube_add(size=1.0, location=point(*center))
    obj = bpy.context.object
    obj.scale = (size[0], size[1], size[2])
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    if bevel:
        modifier = obj.modifiers.new("Edge radius", "BEVEL")
        modifier.width = min(bevel, min(size) / 4)
        modifier.segments = 3
        bpy.ops.object.modifier_apply(modifier=modifier.name)
    return finish(obj, name, mat, collision)


def cylinder(name, start, end, radius, mat, collision=False, sides=20):
    a, b = Vector(point(*start)), Vector(point(*end))
    direction = b - a
    bpy.ops.mesh.primitive_cylinder_add(vertices=sides, radius=radius,
        depth=direction.length, location=(a + b) / 2)
    obj = bpy.context.object
    obj.rotation_mode = "QUATERNION"
    obj.rotation_quaternion = direction.to_track_quat("Z", "Y")
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    for polygon in obj.data.polygons:
        polygon.use_smooth = len(polygon.vertices) == 4
    return finish(obj, name, mat, collision)


def solid_profile(name, profile, width, bottom, mat, collision=False, smooth=False,
        sectioned=False):
    # Each strip is closed independently; a whole-bowl convex hull would fill its opening.
    if sectioned:
        # Side n-gons can tessellate nearly collinear curve knots into slivers.
        # Keep each station connected to its own base edge instead.
        count = len(profile)
        vertices = [point(side, forward, height if top else bottom)
            for top in (True, False) for side in (-width / 2, width / 2)
            for forward, height in profile]
        faces = []
        for i in range(count - 1):
            j = i + 1
            faces.extend(((i, j, count + j, count + i),
                (2 * count + i, 3 * count + i, 3 * count + j, 2 * count + j),
                (i, 2 * count + i, 2 * count + j, j),
                (count + i, count + j, 3 * count + j, 3 * count + i)))
        faces.extend(((0, count, 3 * count, 2 * count),
            (count - 1, 2 * count - 1, 4 * count - 1, 3 * count - 1)))
        top_faces = range(0, 4 * (count - 1), 4) if smooth else ()
        return mesh(name, vertices, faces, mat, collision, top_faces)
    outline = profile + [(profile[-1][0], bottom), (profile[0][0], bottom)]
    count = len(outline)
    vertices = [point(side, forward, height)
        for side in (-width / 2, width / 2) for forward, height in outline]
    faces = [tuple(range(count - 1, -1, -1)), tuple(range(count, 2 * count))]
    for i in range(count):
        j = (i + 1) % count
        faces.append((i, j, count + j, count + i))
    top = range(2, 2 + len(profile) - 1) if smooth else ()
    return mesh(name, vertices, faces, mat, collision, top)


def strip(name, profile, width, thickness, mat, smooth=False, top_slopes=None):
    count = len(profile)
    vertices = [point(x, forward, height - depth)
        for depth in (0.0, thickness) for x in (-width / 2, width / 2)
        for forward, height in profile]
    faces = []
    for i in range(count - 1):
        j = i + 1
        faces.extend(((i, j, count + j, count + i),
            (2 * count + i, 3 * count + i, 3 * count + j, 2 * count + j),
            (i, 2 * count + i, 2 * count + j, j),
            (count + i, count + j, 3 * count + j, 3 * count + i)))
    faces.extend(((0, count, 3 * count, 2 * count),
        (count - 1, 2 * count - 1, 4 * count - 1, 3 * count - 1)))
    obj = mesh(name, vertices, faces, mat, smooth_faces=range(0, 4 * (count - 1), 4) if smooth else ())
    if top_slopes is not None:
        if not smooth or len(top_slopes) != count:
            raise ValueError(name + ": top slopes require a matching smooth strip")
        data = obj.data
        normals = [(0.0, 0.0, 0.0)] * len(data.loops)
        for index in range(0, 4 * (count - 1), 4):
            polygon = data.polygons[index]
            for loop_index in polygon.loop_indices:
                slope = top_slopes[data.loops[loop_index].vertex_index % count]
                length = math.sqrt(1 + slope * slope)
                normals[loop_index] = (0.0, slope / length, 1 / length)
        data.normals_split_custom_set(normals)
    obj["ride_surface"] = True
    return obj


def cladding(name, profile, width, mats, sectioned=False):
    for side in (-1, 1):
        obj = solid_profile(name + ("_left" if side < 0 else "_right"), profile,
            0.10, -SKIN, mats["wood_light"], sectioned=sectioned)
        obj.location.x = side * (width / 2 + 0.05)
    for forward, height in profile[::max(1, len(profile) // 8)]:
        if height > 0.12:
            cylinder(name + "_rivet", (-width / 2 - 0.106, forward, height * 0.55),
                (-width / 2 - 0.098, forward, height * 0.55), 0.012, mats["metal"], sides=12)
            cylinder(name + "_rivet", (width / 2 + 0.098, forward, height * 0.55),
                (width / 2 + 0.106, forward, height * 0.55), 0.012, mats["metal"], sides=12)


def coping(name, width, forward, height, mats):
    start, end = (-width / 2, forward, height), (width / 2, forward, height)
    cylinder(name, start, end, COPING_RADIUS, mats["metal"])
    cylinder(name, start, end, COPING_RADIUS, None, collision=True, sides=16)


def deck(name, width, start, end, height, mats):
    length = abs(end - start)
    forward = (start + end) / 2
    box(name + "_solid", (0, forward, height - (SKIN + 0.014) / 2),
        (width, length, SKIN - 0.014), mats["dark"])
    box(name, (0, forward, height - SKIN / 2), (width, length, SKIN), None, collision=True)
    panels = max(1, round(length / 0.3))
    for i in range(panels):
        f = min(start, end) + length * (i + 0.5) / panels
        plank = box(name + "_plank", (0, f, height - 0.007), (width, length / panels - 0.004, 0.014),
            mats["wood"] if i % 3 else mats["wood_dark"])
        plank["ride_surface"] = True
    for right in (-width / 2 + 0.14, width / 2 - 0.14):
        box(name + "_support", (right, forward, (height - SKIN) / 2),
            (0.16, max(0.05, length - 0.08), max(0.01, height - SKIN)), mats["frame"], bevel=0.008)


def backrail(name, width, forward, height, mats):
    for x in (-width / 2 + 0.08, 0.0, width / 2 - 0.08):
        box(name + "_post", (x, forward, height + 0.43), (0.09, 0.09, 0.86), mats["wood_light"], bevel=0.008)
        box(name + "_post", (x, forward, height + 0.43), (0.09, 0.09, 0.86), None, collision=True)
    for level in (0.35, 0.79):
        box(name + "_rail", (0, forward, height + level), (width, 0.085, 0.12), mats["wood"], bevel=0.012)
        box(name + "_rail", (0, forward, height + level), (width, 0.085, 0.12), None, collision=True)


def transition_height(along, flat_top):
    if along <= ENTRY_BLEND:
        u = along / ENTRY_BLEND
        return (LOWER_SLOPE * ENTRY_BLEND * (2 * u * u - u * u * u),
            LOWER_SLOPE * (4 * u - 3 * u * u))
    if along < LOWER_RUN - MIDDLE_HALF_BLEND:
        return LOWER_SLOPE * along, LOWER_SLOPE
    if along <= LOWER_RUN + MIDDLE_HALF_BLEND:
        t = along - (LOWER_RUN - MIDDLE_HALF_BLEND)
        delta = UPPER_SLOPE - LOWER_SLOPE
        return (LOWER_RISE - LOWER_SLOPE * MIDDLE_HALF_BLEND + LOWER_SLOPE * t +
            delta * t * t / (4 * MIDDLE_HALF_BLEND),
            LOWER_SLOPE + delta * t / (2 * MIDDLE_HALF_BLEND))
    if not flat_top or along < TRANSITION_RUN - EXIT_BLEND:
        return LOWER_RISE + UPPER_SLOPE * (along - LOWER_RUN), UPPER_SLOPE
    u = (along - (TRANSITION_RUN - EXIT_BLEND)) / EXIT_BLEND
    return (TRANSITION_RISE - UPPER_SLOPE * EXIT_BLEND +
        UPPER_SLOPE * EXIT_BLEND * (u + u * u - u * u * u),
        UPPER_SLOPE * (1 + 2 * u - 3 * u * u))


def transition_profile(start, direction, flat_top):
    stations = [0.0]

    def span(end, count):
        first = stations[-1]
        stations.extend(first + (end - first) * i / count for i in range(1, count + 1))
        stations[-1] = end

    span(ENTRY_BLEND, BLEND_STEPS)
    span(LOWER_RUN - MIDDLE_HALF_BLEND, 1)
    span(LOWER_RUN + MIDDLE_HALF_BLEND, BLEND_STEPS)
    if flat_top:
        span(TRANSITION_RUN - EXIT_BLEND, 1)
        span(TRANSITION_RUN, BLEND_STEPS)
    else:
        span(TRANSITION_RUN, 1)
    samples = []
    for along in stations:
        height, slope = transition_height(along, flat_top)
        samples.append((start + direction * along, height, direction * slope))
    if direction < 0:
        samples.reverse()
    return [(f, h) for f, h, _ in samples], [slope for _, _, slope in samples]


def transition(name, width, start, direction, mats, flat_top=False):
    profile, slopes = transition_profile(start, direction, flat_top)
    solid_profile(name + "_structure", [(f, z - 0.025) for f, z in profile],
        width, -SKIN, mats["dark"], sectioned=True)
    solid_profile(name, profile, width, -SKIN, None, collision=True, sectioned=True)
    strip(name + "_ride", profile, width, 0.025, mats["metal"], smooth=True, top_slopes=slopes)
    cladding(name + "_sides", profile, width, mats, sectioned=True)
    end = start + direction * TRANSITION_RUN
    coping(name + "_coping", width, end, TRANSITION_RISE, mats)
    seam_profile = [(f - 0.0005 * slope / math.sqrt(1 + slope * slope),
        h + 0.0005 / math.sqrt(1 + slope * slope)) for (f, h), slope in zip(profile, slopes)]
    for fraction in (0.25, 0.5, 0.75):
        x = (fraction - 0.5) * width
        edge = strip(name + "_panel_seam", seam_profile, 0.006, 0.026, mats["dark"],
            smooth=True, top_slopes=slopes)
        edge.location.x = x


def ramp(mats, angle, name):
    radians = math.radians(angle)
    run, rise, width = 3 * math.cos(radians), 3 * math.sin(radians), 3.0
    profile = [(0.0, 0.0), (run, rise)]
    solid_profile(name + "_structure", [(f, z - 0.025) for f, z in profile],
        width, -SKIN, mats["dark"])
    solid_profile(name, profile, width, -SKIN, None, collision=True)
    strip(name + "_ride", profile, width, 0.025, mats["wood"])
    cladding(name + "_sides", profile, width, mats)
    coping(name + "_lip", width, run, rise, mats)
    box(name + "_entry", (0, 0.04, -0.012), (width, 0.08, 0.018), mats["metal"])
    return {"kind": name, "pivot": "low-entry midpoint on riding plane", "ride_width": width,
        "slope_degrees": angle, "rise": rise, "slope_run": run, "ride_length": run,
        "platform_length": 0.0, "profile_reference": "nominal native floor centre plane",
        "native_anchor_offset": {"right": 0.0, "up": 0.0, "forward": -run / 2}}


def ledge(mats, width, height, kind):
    depth = 3.0
    body = box("concrete_body", (0, 0, height / 2), (width, depth, height), mats["concrete"], bevel=0.016)
    body["ride_surface"] = True
    box("ride", (0, 0, height / 2), (width, depth, height), None, collision=True)
    for f in (-depth / 2, depth / 2):
        box("grind_edge", (0, f, height - 0.015), (width, 0.055, 0.030), mats["metal"], bevel=0.004)
    for x in (-width / 2, width / 2):
        box("grind_edge", (x, 0, height - 0.015), (0.055, depth - 0.10, 0.030), mats["metal"], bevel=0.004)
    for x in (-width / 2 + 0.2, width / 2 - 0.2):
        box("height_marker", (x, -depth / 2 - 0.001, height * 0.45),
            (0.06, 0.004, height * 0.60), mats["mark"])
    return {"kind": kind, "pivot": "base centre", "ride_width": width,
        "ride_length": depth, "rise": height,
        "profile_reference": "nominal native floor centre plane",
        "native_anchor_offset": {"right": 0.0, "up": 0.0, "forward": 0.0}}


def quarterpipe(mats):
    width = 3.0
    transition("quarter", width, 0.0, 1, mats)
    return {"kind": "quarterpipe", "pivot": "low-entry midpoint on riding plane", "ride_width": width,
        "rise": TRANSITION_RISE, "transition_run": TRANSITION_RUN, "ride_length": TRANSITION_RUN,
        "platform_length": 0.0, "native_slopes_degrees": [15, 35], "profile_knots": 35,
        "profile_reference": "nominal native floor centre plane", "profile_continuity": "C1",
        "maximum_profile_deviation": (UPPER_SLOPE - LOWER_SLOPE) * MIDDLE_HALF_BLEND / 4,
        "native_anchor_offset": {"right": 0.0, "up": 0.0, "forward": -LOWER_RUN / 2}}


def funbox(mats):
    width, height, half = 3.0, 3 * math.sin(math.radians(15)), 1.5
    run = 3 * math.cos(math.radians(15))
    box("core", (0, 0, (height - 0.02) / 2), (width, 3.0, height - 0.02), mats["frame"])
    box("core", (0, 0, height / 2), (width, 3.0, height), None, collision=True)
    for direction in (-1, 1):
        profile = [(half, height), (half + run, 0)]
        if direction < 0:
            profile = [(-forward, z) for forward, z in profile][::-1]
        solid_profile("bank", [(f, z - 0.025) for f, z in profile], width, -SKIN, mats["dark"])
        solid_profile("bank", profile, width, -SKIN, None, collision=True)
        strip("bank_ride", profile, width, 0.025, mats["wood"])
        cladding("bank_sides", profile, width, mats)
        coping("deck_edge", width, direction * half, height, mats)
    top = box("top", (0, 0, height - 0.010), (width, 3.0, 0.020), mats["wood"])
    top["ride_surface"] = True
    for f in (-0.9, -0.3, 0.3, 0.9):
        box("deck_seam", (0, f, height - 0.001), (width, 0.007, 0.003), mats["wood_dark"])
    return {"kind": "funbox", "pivot": "base centre below flat deck", "ride_width": width,
        "rise": height, "flat_length": 3.0, "ride_length": 3.0 + 2 * run, "slope_degrees": 15,
        "profile_reference": "nominal native floor centre plane",
        "native_anchor_offset": {"right": 0.0, "up": 0.0, "forward": 0.0}}


def halfpipe(mats):
    width, half, platform = 6.0, 3.0, 3.0
    flat = box("flat", (0, 0, -SKIN / 2), (width, 6.0, SKIN), mats["concrete"])
    flat["ride_surface"] = True
    box("flat", (0, 0, -SKIN / 2), (width, 6.0, SKIN), None, collision=True)
    for direction in (-1, 1):
        name = "front" if direction > 0 else "back"
        transition(name, width, direction * half, direction, mats, flat_top=True)
        edge = direction * (half + TRANSITION_RUN)
        end = edge + direction * platform
        deck(name + "_deck", width, edge, end, TRANSITION_RISE, mats)
        backrail(name + "_rail", width, end - direction * 0.05, TRANSITION_RISE, mats)
    box("flat_joint", (0, 0, -0.0005), (width, 0.006, 0.002), mats["dark"])
    for direction in (-1, 1):
        box("flat_joint", (0, direction * (half + 0.003) / 2, -0.0005),
            (0.006, half - 0.003, 0.002), mats["dark"])
    return {"kind": "halfpipe", "pivot": "centre of the 6 x 6 m flat riding plane",
        "ride_width": width, "flat_width": 6.0, "flat_length": 6.0,
        "rise": TRANSITION_RISE, "transition_run": TRANSITION_RUN, "platform_length": platform,
        "ride_length": 2 * half + 2 * TRANSITION_RUN + 2 * platform,
        "native_slopes_degrees": [15, 35], "transition_profile_knots": 51,
        "profile_reference": "nominal native floor centre plane", "profile_continuity": "C1",
        "maximum_profile_deviation": 4 * UPPER_SLOPE * EXIT_BLEND / 27,
        "native_anchor_offset": {"right": 1.5, "up": 0.0, "forward": 1.5}}



# Circular transitions are independent assets; legacy faceted transition helpers above stay intact.
def round_profile(radius, rise, start=0.0, direction=1, vertical=0.0, max_step_degrees=0.9):
    if not (math.isfinite(radius) and math.isfinite(rise) and radius > 0 and 0 < rise <= radius and
            direction in (-1, 1) and 0 <= vertical <= 0.5 and 0 < max_step_degrees <= 3):
        raise ValueError("Invalid circular transition parameters")
    angle = math.acos(1 - rise / radius)
    steps = max(1, math.ceil(math.degrees(angle) / max_step_degrees))
    samples = []
    for i in range(steps + 1):
        theta = angle * i / steps
        forward = start + direction * radius * math.sin(theta)
        height = radius * (1 - math.cos(theta))
        # Normal in Blender coordinates: (right, -forward, height).
        samples.append((forward, height, direction * math.sin(theta), math.cos(theta), theta))
    if vertical:
        if abs(angle - math.pi / 2) > 1e-9:
            raise ValueError("A vertical extension requires a true 90-degree quarter circle")
        f, h, ny, nz, theta = samples[-1]
        samples.append((f, h + vertical, ny, nz, theta))
    if direction < 0:
        samples.reverse()
    return samples


def round_shell(name, samples, width, thickness, mat, collision=False):
    # Top and underside follow the same arc, offset along its exact inward normal.
    # A collision object is ONE independently closed narrow strip per chord, never a hull of a bowl.
    vertices = []
    for f, h, ny, nz, theta in samples:
        for x in (-width / 2, width / 2):
            vertices.append(point(x, f, h))
        for x in (-width / 2, width / 2):
            vertices.append(point(x, f + thickness * ny, h - thickness * nz))
    faces = []
    for i in range(len(samples) - 1):
        a, b = 4 * i, 4 * (i + 1)
        faces.extend(((a, b, b + 1, a + 1), (a + 2, a + 3, b + 3, b + 2),
            (a, a + 2, b + 2, b), (a + 1, b + 1, b + 3, a + 3)))
    faces.extend(((0, 1, 3, 2), (len(vertices) - 4, len(vertices) - 2, len(vertices) - 1, len(vertices) - 3)))
    obj = mesh(name, vertices, faces, mat, collision, range(0, 4 * (len(samples) - 1), 4) if not collision else ())
    if not collision:
        normals = [(0.0, 0.0, 0.0)] * len(obj.data.loops)
        for index in range(0, 4 * (len(samples) - 1), 4):
            for li in obj.data.polygons[index].loop_indices:
                sample = samples[obj.data.loops[li].vertex_index // 4]
                normals[li] = (0.0, sample[2], sample[3])
        obj.data.normals_split_custom_set(normals)
        obj["ride_surface"] = True
    obj["circular_arc"] = True
    obj["skin_thickness"] = thickness
    return obj


def round_coping(name, width, lip, height, direction, mats):
    # Top is 5 mm above the platform; the tube projects exactly 10 mm into the riding side.
    # Relative to the lip line the forward-most riding point is -10 mm.
    centre_forward = lip + direction * (COPING_RADIUS - 0.01)
    centre_height = height - COPING_RADIUS + 0.005
    start = (-width / 2, centre_forward, centre_height)
    end = (width / 2, centre_forward, centre_height)
    obj = cylinder(name, start, end, COPING_RADIUS, mats["metal"], sides=24)
    obj["coping"] = True
    obj["lip_height"] = height
    obj["protrusion_above_deck"] = 0.005
    obj["riding_side_overhang"] = 0.01
    obj = cylinder(name, start, end, COPING_RADIUS, None, collision=True, sides=16)
    obj["coping"] = True


def round_transition(name, radius, rise, width, start, direction, mats, vertical=0.0):
    samples = round_profile(radius, rise, start, direction, vertical)
    round_shell(name + "_ride", samples, width, .012, mats["wood"])
    for i in range(len(samples) - 1):
        slab = round_shell(name + "_arc_%02d" % i, samples[i:i + 2], width, SKIN, None, collision=True)
        slab["arc_segment"] = i
        slab["radius"] = radius
        slab["analytic_theta_start"] = samples[i][4]
        slab["analytic_theta_end"] = samples[i + 1][4]
    # Closed plywood side cladding; all construction is original, with no hidden collision hull.
    def unique_forward(points, offset):
        result = []
        for f, h, _, _, _ in points:
            if result and abs(result[-1][0] - f) < 1e-9:
                result[-1] = (f, max(h - offset, result[-1][1]))
            else:
                result.append((f, h - offset))
        return result
    profile = unique_forward(samples, .012)
    # At the very first station the side cladding enters the structural base below y=0.
    for side in (-1, 1):
        side_obj = solid_profile(name + ("_side_left" if side < 0 else "_side_right"),
            profile, .065, -SKIN, mats["wood_light"])
        side_obj.location.x = side * (width / 2 + .0325)
        side_proxy = solid_profile(name + ("_sidewall_left" if side < 0 else "_sidewall_right"),
            profile, .065, -SKIN, None, collision=True)
        side_proxy.location.x = side_obj.location.x
    # Repeated closed wooden ribs, <=0.4m centres across the full width. Their lower outline
    # remains deliberately coarse, while the riding shell and collision arc stay <=3 degrees.
    rib_intervals = math.ceil(width / .4)
    rib_samples = samples[::10]
    if rib_samples[-1] != samples[-1]: rib_samples.append(samples[-1])
    # A vertical-only lowering leaks ribs through steep skin. Offset them along the actual
    # normal by 32mm, including clearance for their coarser 9-degree structural top chords.
    rib_samples = [(f+.032*ny,h-.032*nz,ny,nz,theta) for f,h,ny,nz,theta in rib_samples]
    rib_profile = unique_forward(rib_samples, 0)
    for i in range(rib_intervals + 1):
        x = -width / 2 + width * i / rib_intervals
        rib = solid_profile(name + "_wood_rib_%02d" % i, rib_profile, .035, -SKIN, mats["wood"], sectioned=True)
        rib.location.x = x
        rib["wood_rib"] = True
    lip = start + direction * radius * math.sin(math.acos(1 - rise / radius))
    round_coping(name + "_coping", width, lip, rise + vertical, direction, mats)
    return samples


def round_backrail(name, width, forward, height, mats):
    for x in (-width / 2 + .08, 0, width / 2 - .08):
        box(name + "_post", (x, forward, height+.43), (.075,.075,.86), mats["metal"])
        box(name + "_post", (x, forward, height+.43), (.075,.075,.86), None, collision=True)
    for level in (.35,.79):
        cylinder(name + "_rail", (-width/2,forward,height+level), (width/2,forward,height+level), .035,mats["metal"],sides=12)
        cylinder(name + "_rail", (-width/2,forward,height+level), (width/2,forward,height+level), .035,None,collision=True,sides=12)


def round_back_cladding(name, width, start, end, height, mats):
    direction = 1 if end > start else -1
    for side in (-1, 1):
        centre = (side * (width/2 + .0325), (start+end)/2, (height-SKIN)/2)
        size = (.065, abs(end-start), height+SKIN)
        box(name + ("_deck_side_left" if side < 0 else "_deck_side_right"), centre, size, mats["wood_light"])
        box(name + ("_deck_sidewall_left" if side < 0 else "_deck_sidewall_right"), centre, size, None, collision=True)
    box(name + "_closed_back", (0, end, (height - SKIN) / 2), (width + .13, .055, height + SKIN), mats["wood_dark"])
    box(name + "_closed_back", (0, end, (height - SKIN) / 2), (width + .13, .055, height + SKIN), None, collision=True)
    count = math.ceil(width / .4)
    for i in range(count + 1):
        x = -width / 2 + width * i / count
        # Exposed solid wood rib ends / battens show the real 0.4m-centre construction on the back.
        box(name + "_back_rib_%02d" % i, (x, end + direction*.032, (height - SKIN) / 2),
            (.045, .035, height + SKIN), mats["wood_light"])



def round_side_frame(name, width, start, lip, end, radius, rise, direction, mats):
    # Closed contrasting timber lies OUTSIDE the closed plywood side panels. Its original
    # triangular stock section keeps the side frame visible within the strict quarter budget.
    count = math.ceil(abs(lip-start)/.4)
    for side in (-1,1):
        inner = side*(width/2+.065)
        outer = side*(width/2+.083)
        box(name + ("_base_timber_left" if side<0 else "_base_timber_right"),
            (side*(width/2+.074),(start+end)/2,.015),(.018,abs(end-start),.03),mats["wood_dark"])
        for i in range(1,count+1):
            along=abs(lip-start)*i/count
            forward=start+direction*along
            # The whole stud top stays below the neighbouring curved panel edge.
            lower_along=max(0,along-.018)
            top=radius-math.sqrt(max(0,radius*radius-lower_along*lower_along))-.014
            if top<=.035: continue
            cross_section=[(inner,forward-.018),(inner,forward+.018),(outer,forward)]
            vertices=[point(x,f,h) for h in (.03,top) for x,f in cross_section]
            faces=[(0,2,1),(3,4,5),(0,1,4,3),(1,2,5,4),(2,0,3,5)]
            stud=mesh(name+"_exposed_side_stud_"+str(side)+"_%02d"%i,vertices,faces,mats["wood_dark"])
            stud["visible_wood_frame"]=True
            stud["max_stud_spacing"]=.4


def round_quarterpipe(mats, radius, rise, width, platform):
    samples = round_transition("round", radius, rise, width, 0, 1, mats)
    run = radius * math.sin(math.acos(1 - rise / radius))
    deck("round_deck", width, run, run + platform, rise, mats)
    round_back_cladding("round", width, run, run + platform, rise, mats)
    round_backrail("round_backrail", width, run+platform-.05, rise, mats)
    round_side_frame("round",width,0,run,run+platform,radius,rise,1,mats)
    return {"kind": "quarterpipe_round", "pivot": "low-entry midpoint on riding plane", "ride_width": width,
        "radius": radius, "rise": rise, "transition_angle_degrees": math.degrees(math.acos(1 - rise / radius)), "vert_degrees": math.degrees(math.acos(1-rise/radius)),
        "transition_run": run, "platform_length": platform, "ride_length": run + platform,
        "profile_formula": "forward=R*sin(theta), height=R*(1-cos(theta))", "arc_max_step_degrees": .9,
        "profile_knots": len(samples), "collision_skin_thickness": SKIN, "collision_max_profile_error": radius * (1 - math.cos(math.acos(1-rise/radius)/(len(samples)-1)/2)),
        "profile_reference": "circular riding surface; horizontal tangent at entry", "native_anchor_offset": {"right": 0, "up": 0, "forward": 0},
        "coping_radius": COPING_RADIUS, "coping_above_deck": .005, "coping_overhang": .01, "wood_rib_max_spacing": .4}


def mega_halfpipe(mats):
    radius, rise, vertical, width, flat, platform = 3.5, 3.5, .1, 9.0, 6.0, 3.0
    slab = box("mega_flat", (0, 0, -SKIN/2), (width, flat, SKIN), mats["wood"]); slab["ride_surface"] = True
    box("mega_flat", (0, 0, -SKIN/2), (width, flat, SKIN), None, collision=True)
    for direction in (-1, 1):
        name = "mega_front" if direction > 0 else "mega_back"
        round_transition(name, radius, rise, width, direction*flat/2, direction, mats, vertical)
        lip = direction*(flat/2+radius); end = lip+direction*platform
        deck(name+"_deck", width, lip, end, rise+vertical, mats)
        round_back_cladding(name, width, lip, end, rise+vertical, mats)
        round_backrail(name+"_rail", width, end-direction*.05, rise+vertical, mats)
        round_side_frame(name,width,direction*flat/2,lip,end,radius,rise,direction,mats)
    return {"kind": "halfpipe_round", "pivot": "centre of 6m flat riding plane", "ride_width": width,
        "radius": radius, "rise": rise+vertical, "arc_rise": rise, "vertical_extension": vertical,
        "transition_angle_degrees": 90, "vert_degrees": 90, "transition_run": radius, "flat_length": flat,
        "platform_length": platform, "ride_length": flat+2*radius+2*platform,
        "arc_max_step_degrees": .9, "collision_skin_thickness": SKIN,
        "collision_max_profile_error": radius*(1-math.cos(math.radians(.45))),
        "profile_reference": "mirrored circular riding surfaces with 0.1m vertical extensions",
        "native_anchor_offset": {"right": 0, "up": 0, "forward": 0},
        "coping_radius": COPING_RADIUS, "coping_above_deck": .005, "coping_overhang": .01, "wood_rib_max_spacing": .4}


def round_hard_checks(name, dimensions, visual, collision):
    radius = dimensions['radius']; rise = dimensions.get('arc_rise', dimensions['rise'])
    angle = math.acos(1-rise/radius); run = radius*math.sin(angle)
    mega = name == 'skate_mega_halfpipe'
    result = {'pass': True, 'checks': 0, 'worst': {}, 'scope': 'Actual generated collision slab vertices and visual loop normals; before GLB export.'}
    def check(key, value, limit):
        result['checks'] += 1
        previous = result['worst'].get(key)
        if previous is None or value > previous['value']:
            result['worst'][key] = {'value': value, 'limit': limit}
        if not math.isfinite(value) or value > limit + 1e-9:
            raise ValueError(name + ': ' + key + '=' + str(value) + ' exceeds ' + str(limit))
    groups = {}
    for obj in collision:
        if 'arc_segment' not in obj: continue
        direction = -1 if 'mega_back' in obj.name else 1
        start = direction*3 if mega else 0
        group = groups.setdefault(direction, [])
        points = []
        for index in (0, 4):
            p = obj.matrix_world @ obj.data.vertices[index].co
            points.append((direction*(-p.y-start),p.z))
        points.sort()
        if points[0][1] <= rise+1e-6 and points[1][1] <= rise+1e-6:
            group.append(tuple(points))
        for a,b in ((0,2),(1,3),(4,6),(5,7)):
            check('proxy_normal_thickness_error_m', abs((obj.data.vertices[a].co-obj.data.vertices[b].co).length-SKIN), 1e-6)
    expected = {-1,1} if mega else {1}
    if set(groups) != expected: raise ValueError(name+': missing circular collision walls')
    def nearest(p, segment):
        a,b=segment; dx=b[0]-a[0];dy=b[1]-a[1]
        t=max(0,min(1,((p[0]-a[0])*dx+(p[1]-a[1])*dy)/(dx*dx+dy*dy)))
        q=(a[0]+t*dx,a[1]+t*dy)
        return math.hypot(p[0]-q[0],p[1]-q[1]),q
    for direction, segments in groups.items():
        segments.sort()
        check('entry_position_error_m',math.hypot(*segments[0][0]),1e-6)
        check('arc_lip_error_m',math.hypot(segments[-1][1][0]-run,segments[-1][1][1]-rise),1e-6)
        def observed(s):
            if s >= run-1e-7: return rise, math.degrees(math.atan2(segments[-1][1][1]-segments[-1][0][1],segments[-1][1][0]-segments[-1][0][0]))
            for a,b in segments:
                if a[0]-1e-7 <= s <= b[0]+1e-7:
                    t=max(0,min(1,(s-a[0])/(b[0]-a[0])))
                    return a[1]+t*(b[1]-a[1]), math.degrees(math.atan2(b[1]-a[1],b[0]-a[0]))
            raise ValueError(name+': gap in collision arc')
        for i in range(10):
            s=run*i/9; expected_h=radius-math.sqrt(max(0,radius*radius-s*s)); expected_angle=math.degrees(math.asin(min(1,s/radius)))
            h,slope=observed(s)
            check('ten_point_height_error_m',abs(h-expected_h),.003)
            check('ten_point_slope_error_degrees',abs(slope-expected_angle),.5)
        check('entry_slope_error_degrees',abs(observed(0)[1]),.5)
        check('lip_slope_error_degrees',abs(observed(run)[1]-math.degrees(angle)),.5)
        for segment in segments:
            a,b=segment
            first=math.atan2(a[0],radius-a[1]);last=math.atan2(b[0],radius-b[1])
            check('maximum_arc_segment_degrees',abs(math.degrees(last-first)),3)
            for point in segment: check('circle_vertex_radius_error_m',abs(math.hypot(point[0],point[1]-radius)-radius),1e-6)
        for i in range(1001):
            theta=angle*i/1000;p=(radius*math.sin(theta),radius*(1-math.cos(theta)))
            check('actual_arc_proxy_distance_m',min(nearest(p,segment)[0] for segment in segments),.005)
        half_angle=math.asin(.49/(2*radius))
        for i in range(501):
            centre=half_angle+(angle-2*half_angle)*i/500
            endpoints=[(radius*math.sin(t),radius*(1-math.cos(t))) for t in (centre-half_angle,centre+half_angle)]
            projected=[];errors=[]
            for p in endpoints:
                gap,q=min((nearest(p,seg) for seg in segments),key=lambda hit:hit[0]);projected.append(q);errors.append(gap)
            check('wheelbase_049_axle_surface_error_m',max(errors),.004)
            check('wheelbase_049_chord_length_error_m',abs(math.dist(projected[0],projected[1])-.49),.002)
    # Check actual coarse rib tops throughout each segment: they must stay behind the skin,
    # even close to a vertical lip where a vertical-only offset would be ineffective.
    minimum_rib_recess = float("inf")
    for obj in visual:
        if not obj.get("wood_rib"): continue
        direction = -1 if 'mega_back' in obj.name else 1
        start = direction*3 if mega else 0
        count = len(obj.data.vertices)//4
        row = [obj.matrix_world@obj.data.vertices[i].co for i in range(count)]
        for a,b in zip(row,row[1:]):
            for k in range(17):
                v=a.lerp(b,k/16);along=direction*(-v.y-start)
                recess=math.hypot(along,v.z-radius)-radius-.012
                minimum_rib_recess=min(minimum_rib_recess,recess)
                check('rib_top_skin_clearance_violation_m',max(0,.001-recess),0)
    result['minimum_rib_under_skin_recess_m'] = minimum_rib_recess
    # Check the actual custom corner normals on every smooth ride strip top face.
    for obj in visual:
        if not obj.get('circular_arc'):continue
        direction=-1 if 'mega_back' in obj.name else 1
        for polygon in obj.data.polygons:
            if not polygon.use_smooth:continue
            for li in polygon.loop_indices:
                v=obj.data.vertices[obj.data.loops[li].vertex_index].co
                theta=math.acos(max(-1,min(1,1-min(v.z,rise)/radius)))
                expected_normal=Vector((0,direction*math.sin(theta),math.cos(theta)))
                actual=obj.data.corner_normals[li].vector
                check('analytic_visual_normal_error_degrees',math.degrees(actual.angle(expected_normal)),.05)
    if mega:
        left=sorted([p for segment in groups[-1] for p in segment]);right=sorted([p for segment in groups[1] for p in segment])
        check('mega_mirror_error_m',max(math.dist(a,b) for a,b in zip(left,right)),1e-6)
        flat=[o for o in collision if o.name=='COLLISION__mega_flat'][0]
        points=[flat.matrix_world@v.co for v in flat.data.vertices]
        check('mega_flat_length_error_m',abs(max(v.y for v in points)-min(v.y for v in points)-6),1e-6)
        check('mega_flat_top_error_m',abs(max(v.z for v in points)),1e-6)
    coping=[o for o in visual if o.get('coping')]
    heights=[]
    for obj in coping:
        y=max((obj.matrix_world@v.co).z for v in obj.data.vertices); heights.append(y)
        check('coping_flush_above_deck_error_m',max(0,dimensions['rise']-y,y-dimensions['rise']-.01),0)
        direction=-1 if 'mega_back' in obj.name else 1
        f=[-(obj.matrix_world@v.co).y for v in obj.data.vertices]
        inner=min(f) if direction>0 else max(f);lip=direction*((3 if mega else 0)+run)
        check('coping_overhang_error_m',abs(direction*(lip-inner)-.01),1e-6)
    if mega:check('mega_coping_height_difference_m',max(heights)-min(heights),.002)
    print('ROUND_CHECK',name,'PASS',result['checks'],json.dumps(result['worst'],sort_keys=True),flush=True)
    return result


GENERATORS = {
    "skate_bank": lambda mats: ramp(mats, 12, "bank"),
    "skate_kicker": lambda mats: ramp(mats, 25, "kicker"),
    "skate_quarterpipe": quarterpipe,
    "skate_ledge_low": lambda mats: ledge(mats, 3.0, 0.4, "ledge-low"),
    "skate_ledge_high": lambda mats: ledge(mats, 3.0, 0.8, "ledge-high"),
    "skate_long_ledge": lambda mats: ledge(mats, 9.0, 0.4, "long-ledge"),
    "skate_funbox": funbox,
    "skate_halfpipe": halfpipe,
    "skate_qp_round_mini": lambda mats: round_quarterpipe(mats, 1.5, 1.0, 3.0, 1.0),
    "skate_qp_round": lambda mats: round_quarterpipe(mats, 2.5, 2.0, 3.0, 1.5),
    "skate_qp_round_large": lambda mats: round_quarterpipe(mats, 3.0, 3.0, 4.5, 2.0),
    "skate_mega_halfpipe": mega_halfpipe,
}


def mesh_report(objects):
    total_vertices = total_triangles = 0
    points = []
    for obj in objects:
        data = obj.data
        data.calc_loop_triangles()
        if any(not math.isfinite(value) for vertex in data.vertices for value in vertex.co):
            raise ValueError(obj.name + ": nonfinite vertex")
        if any(triangle.area <= 1e-12 for triangle in data.loop_triangles):
            raise ValueError(obj.name + ": degenerate triangle")
        bm = bmesh.new()
        bm.from_mesh(data)
        if any(not edge.is_manifold for edge in bm.edges) or bm.calc_volume(signed=True) <= 1e-12:
            bm.free()
            raise ValueError(obj.name + ": not a closed outward solid")
        bm.free()
        if abs(obj.matrix_world.determinant()) < 1e-8 or obj.matrix_world.determinant() < 0:
            raise ValueError(obj.name + ": invalid mirrored transform")
        total_vertices += len(data.vertices)
        total_triangles += len(data.loop_triangles)
        points.extend(obj.matrix_world @ vertex.co for vertex in data.vertices)
    if not points:
        raise ValueError("No meshes generated")
    lo = [min(p[i] for p in points) for i in range(3)]
    hi = [max(p[i] for p in points) for i in range(3)]
    return {"vertices": total_vertices, "triangles": total_triangles, "bounds": [lo, hi]}


def separate_scenes(path):
    raw = path.read_bytes()
    magic, version, length = struct.unpack_from("<III", raw)
    if magic != 0x46546C67 or version != 2 or length != len(raw):
        raise ValueError("Invalid Blender GLB export")
    json_size, chunk_type = struct.unpack_from("<II", raw, 12)
    if chunk_type != 0x4E4F534A:
        raise ValueError("Missing GLB JSON chunk")
    data = json.loads(raw[20:20 + json_size])
    roots = data["scenes"][data.get("scene", 0)]["nodes"]
    collision = [i for i in roots if data["nodes"][i].get("extras", {}).get("role") == "collision"]
    visual = [i for i in roots if i not in collision]
    if not collision or not visual or any("children" in data["nodes"][i] for i in roots):
        raise ValueError("Unexpected GLB root hierarchy")
    data["scenes"] = [{"name": "Visual", "nodes": visual},
        {"name": "CollisionProxies", "nodes": collision}]
    data["scene"] = 0
    encoded = json.dumps(data, separators=(",", ":"), ensure_ascii=True).encode("utf-8")
    encoded += b" " * ((-len(encoded)) % 4)
    tail = raw[20 + json_size:]
    output = struct.pack("<III", magic, version, 20 + len(encoded) + len(tail))
    output += struct.pack("<II", len(encoded), chunk_type) + encoded + tail
    path.write_bytes(output)



def round_glb_readback(path):
    raw = path.read_bytes()
    size, chunk_type = struct.unpack_from("<II", raw, 12)
    data = json.loads(raw[20:20+size])
    if chunk_type != 0x4E4F534A or len(data.get("buffers", [])) != 1 or "uri" in data["buffers"][0]:
        raise ValueError(path.name + ": external or missing binary buffer")
    if [scene["name"] for scene in data.get("scenes", [])] != ["Visual", "CollisionProxies"] or data.get("scene",0) != 0:
        raise ValueError(path.name + ": wrong scenes or default")
    if any(data.get(key) for key in ("images","textures","cameras","animations","skins")) or data.get("extensions",{}).get("KHR_lights_punctual"):
        raise ValueError(path.name + ": unwanted external/viewer data")
    for obj in data.get("nodes", []):
        if "matrix" in obj or any(value <= 0 for value in obj.get("scale", [1,1,1])):
            raise ValueError(path.name + ": invalid node transform")
    counts = {}
    for scene in data["scenes"]:
        total = 0
        for node_index in scene["nodes"]:
            node = data["nodes"][node_index]
            role = "visual" if scene["name"] == "Visual" else "collision"
            if node.get("extras",{}).get("role") != role or "children" in node:
                raise ValueError(path.name + ": scene role mismatch")
            mesh = data["meshes"][node["mesh"]]
            if len(mesh["primitives"]) != 1:
                raise ValueError(path.name + ": multiple primitives")
            primitive = mesh["primitives"][0]
            if primitive.get("mode",4) != 4 or not all(key in primitive["attributes"] for key in ("POSITION","NORMAL")):
                raise ValueError(path.name + ": unsupported primitive")
            if role == "collision" and "material" in primitive:
                raise ValueError(path.name + ": collision material is not implicit opaque")
            total += data["accessors"][primitive["indices"]]["count"]//3
        counts[scene["name"]] = total
    return {"pass":True,"sha256":hashlib.sha256(raw).hexdigest(),"bytes":len(raw),"triangles":counts,"scenes":[s["name"] for s in data["scenes"]],"default":"Visual","one_embedded_buffer":True,"one_primitive_per_mesh":True,"no_images_cameras_lights_skins_animations":True}


def prepare_preview(visual):
    scene = bpy.context.scene
    report = mesh_report(visual)
    lo, hi = (Vector(bound) for bound in report["bounds"])
    centre = (lo + hi) / 2
    radius = max((hi - lo).length / 2, 1.0)
    scene.render.engine = "CYCLES"
    scene.cycles.samples = 24
    scene.cycles.use_denoising = True
    scene.world.use_nodes = True
    scene.world.node_tree.nodes["Background"].inputs[0].default_value = (0.16, 0.20, 0.24, 1.0)
    scene.world.node_tree.nodes["Background"].inputs[1].default_value = 0.7
    for offset, power, size in (((-3, -4, 5), 500, 4), ((3, -1, 2), 180, 3), ((0, 4, 5), 550, 3)):
        bpy.ops.object.light_add(type="AREA", location=centre + Vector(offset) * radius)
        light = bpy.context.object
        light.data.energy = power * radius * radius
        light.data.shape = "DISK"
        light.data.size = size * radius
        light.rotation_euler = (centre - light.location).to_track_quat("-Z", "Y").to_euler()
    bpy.ops.object.camera_add(location=centre + Vector((1.8, 2.7, 1.9)) * radius)
    camera = bpy.context.object
    camera.rotation_euler = (centre - camera.location).to_track_quat("-Z", "Y").to_euler()
    camera.data.type = "ORTHO"
    rotation = camera.rotation_euler.to_quaternion()
    right, up = rotation @ Vector((1, 0, 0)), rotation @ Vector((0, 1, 0))
    vertices = [obj.matrix_world @ v.co - centre for obj in visual for v in obj.data.vertices]
    horizontal = [v.dot(right) for v in vertices]
    vertical = [v.dot(up) for v in vertices]
    screen_centre = centre + right * ((min(horizontal) + max(horizontal)) / 2)
    screen_centre += up * ((min(vertical) + max(vertical)) / 2)
    camera.location += screen_centre - centre
    camera.data.ortho_scale = max(max(horizontal) - min(horizontal),
        (max(vertical) - min(vertical)) * (1000 / 760)) * 1.24
    scene.camera = camera
    scene.render.resolution_x, scene.render.resolution_y = 1000, 760
    scene.render.resolution_percentage = 100
    scene.render.image_settings.file_format = "PNG"
    scene.render.film_transparent = True
    scene.view_settings.view_transform = "AgX"
    return scene



def merge_manifest_entries(existing, regenerated):
    # A seeded manifest may interleave non-park assets. Update owned entries in their
    # existing slots, preserving every other object and the complete existing order.
    replacements = {entry["name"]: entry for entry in regenerated}
    merged = []
    seen = set()
    for entry in existing:
        name = entry.get("name")
        if name in seen:
            raise ValueError("Existing asset manifest has duplicate names")
        seen.add(name)
        merged.append(replacements.get(name, entry))
    for entry in regenerated:
        if entry["name"] not in seen:
            merged.append(entry)
            seen.add(entry["name"])
    return merged


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--output", type=Path, required=True)
    parser.add_argument("--preview-dir", type=Path)
    parser.add_argument("--only", help="Comma-separated exact asset names. Use to preserve every unselected existing file.")
    args = parser.parse_args(sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else [])
    args.output.mkdir(parents=True, exist_ok=True)
    if args.preview_dir:
        args.preview_dir.mkdir(parents=True, exist_ok=True)
    bpy.context.preferences.filepaths.save_version = 0
    manifest_path = args.output / "manifest.json"
    manifest = json.loads(manifest_path.read_text()) if manifest_path.is_file() else {
        "schema": 1, "license": "CC0-1.0", "units": "meters", "source": SOURCE,
        "generator_blender": bpy.app.version_string, "assets": []}
    if not isinstance(manifest.get("assets"), list):
        raise ValueError("Existing asset manifest has no assets list")
    requested = tuple(args.only.split(",")) if args.only else NAMES
    if not requested or len(set(requested)) != len(requested) or any(name not in GENERATORS for name in requested):
        raise ValueError("--only requires distinct exact known asset names")
    entries = []
    for name in requested:
        mats = clear()
        dimensions = GENERATORS[name](mats)
        bpy.context.view_layer.update()
        visual = list(bpy.data.collections["Visual"].objects)
        collision = list(bpy.data.collections["CollisionProxies"].objects)
        visual_report, collision_report = mesh_report(visual), mesh_report(collision)
        if name in ("skate_qp_round_mini", "skate_qp_round", "skate_qp_round_large", "skate_mega_halfpipe"):
            limit = 20000 if name == "skate_mega_halfpipe" else 6000
            if visual_report["triangles"] + collision_report["triangles"] >= limit:
                raise ValueError(name + ": combined visual + collision triangle budget exceeded")
        elif visual_report["triangles"] > 12000 or collision_report["triangles"] > 3000:
            raise ValueError(name + ": mesh budget exceeded")
        if name in ("skate_qp_round_mini", "skate_qp_round", "skate_qp_round_large", "skate_mega_halfpipe"):
            check_report = round_hard_checks(name, dimensions, visual, collision)
            (args.output / (name + "-checks.json")).write_text(json.dumps(check_report, indent=2) + "\n")
        bpy.ops.object.select_all(action="DESELECT")
        for obj in visual + collision:
            obj.select_set(True)
        destination = args.output / (name + ".glb")
        bpy.ops.export_scene.gltf(filepath=str(destination.resolve()), export_format="GLB",
            use_selection=True, export_animations=False, export_skins=False, export_cameras=False,
            export_lights=False, export_extras=True)
        separate_scenes(destination)
        if name in ("skate_qp_round_mini", "skate_qp_round", "skate_qp_round_large", "skate_mega_halfpipe"):
            readback = round_glb_readback(destination)
            if readback["triangles"] != {"Visual": visual_report["triangles"], "CollisionProxies": collision_report["triangles"]}:
                raise ValueError(name + ": exported triangle counts changed")
            (args.output / (name + "-readback.json")).write_text(json.dumps(readback, indent=2) + "\n")
        for obj in collision:
            obj.hide_render = True
            obj.hide_set(True)
        scene = prepare_preview(visual)
        round_asset = name in ("skate_qp_round_mini", "skate_qp_round", "skate_qp_round_large", "skate_mega_halfpipe")
        if round_asset:
            scene.render.engine = "BLENDER_EEVEE"
        scene.render.filepath = "//" + name + "-preview.png"
        scene["asset_license"] = "CC0-1.0"
        scene["asset_source"] = SOURCE
        scene["coordinate_contract"] = "metres; Z up; forward -Y; export Y up, forward +Z"
        bpy.ops.wm.save_as_mainfile(filepath=str((args.output / (name + ".blend")).resolve()))
        if args.preview_dir:
            scene.render.filepath = str((args.preview_dir / (name + ".png")).resolve())
            bpy.ops.render.render(write_still=True)
            if round_asset:
                # Exact side view for the radius silhouette, with all geometry framed in daylight.
                report = mesh_report(visual)
                lo, hi = (Vector(bound) for bound in report["bounds"])
                centre = (lo+hi)/2
                camera = scene.camera
                camera.location = centre + Vector((max((hi-lo).length, 5), 0, 0))
                camera.rotation_euler = (centre-camera.location).to_track_quat("-Z","Y").to_euler()
                camera.data.ortho_scale = max(hi.y-lo.y,(hi.z-lo.z)*(1000/760))*1.15
                scene.render.filepath = str((args.preview_dir / (name + "-side.png")).resolve())
                bpy.ops.render.render(write_still=True)
        entry = {"name": name, "file": name + ".glb", **visual_report,
            "bounds_space": "Blender Z-up, forward -Y", "animations": [], "license": "CC0-1.0",
            "source": SOURCE, "generator_blender": bpy.app.version_string,
            "default_scene": "Visual", "collision_scene": "CollisionProxies",
            "collision": collision_report, "dimensions": dimensions,
            "sha256": hashlib.sha256(destination.read_bytes()).hexdigest(),
            "bytes": destination.stat().st_size}
        entries.append(entry)
        print("SKATE_ASSET", json.dumps(entry, sort_keys=True))
    manifest["assets"] = merge_manifest_entries(manifest["assets"], entries)
    manifest_path.write_text(json.dumps(manifest, indent=2) + "\n")
    print("Generated", len(requested), "selected original skate park assets:", args.output.resolve())


if __name__ == "__main__":
    try:
        main()
    except Exception:
        # Blender otherwise prints an uncaught script exception while returning exit code zero.
        # This is a dedicated background generator process; fail its calling build immediately.
        import traceback
        traceback.print_exc()
        sys.stdout.flush()
        sys.stderr.flush()
        os._exit(1)
