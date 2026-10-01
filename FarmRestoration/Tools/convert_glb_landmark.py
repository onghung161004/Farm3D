"""Extract one GLB as a full UV-preserving OBJ and PNG textures for simplification.

The original GLB stays outside Assets. Run with Python 3, no extra packages:
  py Tools/convert_glb_landmark.py source.glb full.obj output_color.png output_normal.png
"""

import argparse
import array
import json
import math
import struct
from pathlib import Path


def read_glb(path):
    with path.open("rb") as source:
        magic, version, length = struct.unpack("<4sII", source.read(12))
        if magic != b"glTF" or version != 2:
            raise ValueError(f"Not a glTF 2.0 binary file: {path}")
        json_size, json_type = struct.unpack("<I4s", source.read(8))
        if json_type != b"JSON":
            raise ValueError("GLB JSON chunk is missing")
        document = json.loads(source.read(json_size).decode("utf-8"))
        bin_size, bin_type = struct.unpack("<I4s", source.read(8))
        if bin_type != b"BIN\0":
            raise ValueError("GLB binary chunk is missing")
        binary = source.read(bin_size)
        if len(binary) != bin_size or length != path.stat().st_size:
            raise ValueError("GLB is incomplete")
    return document, binary


def accessor(document, binary, number, typecode, components):
    description = document["accessors"][number]
    view = document["bufferViews"][description["bufferView"]]
    if view.get("byteStride"):
        raise ValueError("Interleaved accessors are not supported by this converter")
    offset = view.get("byteOffset", 0) + description.get("byteOffset", 0)
    size = description["count"] * components * array.array(typecode).itemsize
    values = array.array(typecode)
    values.frombytes(binary[offset : offset + size])
    if len(values) != description["count"] * components:
        raise ValueError("Accessor length is invalid")
    return values, description


def image_bytes(document, binary, texture_info):
    texture = document["textures"][texture_info["index"]]
    image = document["images"][texture["source"]]
    if image.get("mimeType") != "image/png":
        raise ValueError("This converter expects embedded PNG textures")
    view = document["bufferViews"][image["bufferView"]]
    offset = view.get("byteOffset", 0)
    return binary[offset : offset + view["byteLength"]]


def rotate(point, quaternion):
    x, y, z = point
    qx, qy, qz, qw = quaternion
    # Quaternion rotation, preserving the node's authored GLB orientation.
    tx = 2 * (qy * z - qz * y)
    ty = 2 * (qz * x - qx * z)
    tz = 2 * (qx * y - qy * x)
    return (
        x + qw * tx + qy * tz - qz * ty,
        y + qw * ty + qz * tx - qx * tz,
        z + qw * tz + qx * ty - qy * tx,
    )


def convert(source, output_obj, output_color, output_normal, cells, uv_bins):
    document, binary = read_glb(source)
    if len(document.get("meshes", [])) != 1 or len(document["meshes"][0]["primitives"]) != 1:
        raise ValueError("Expected one mesh with one triangle primitive")
    primitive = document["meshes"][0]["primitives"][0]
    if primitive.get("mode", 4) != 4:
        raise ValueError("Expected a triangle mesh")
    attributes = primitive["attributes"]
    positions, position_info = accessor(document, binary, attributes["POSITION"], "f", 3)
    texcoords, _ = accessor(document, binary, attributes["TEXCOORD_0"], "f", 2)
    indices, _ = accessor(document, binary, primitive["indices"], "I", 1)
    count = position_info["count"]
    if len(texcoords) != count * 2:
        raise ValueError("Position and UV counts differ")

    if cells == 0:
        quaternion = document.get("nodes", [{}])[0].get("rotation", [0, 0, 0, 1])
        output_obj.parent.mkdir(parents=True, exist_ok=True)
        with output_obj.open("w", encoding="ascii", newline="\n") as output:
            output.write(f"# Full source mesh: {count} vertices\no {output_obj.stem}\n")
            for vertex in range(count):
                p = vertex * 3
                x, y, z = rotate(positions[p : p + 3], quaternion)
                output.write(f"v {x:.7f} {y:.7f} {z:.7f}\n")
            for vertex in range(count):
                t = vertex * 2
                output.write(f"vt {texcoords[t]:.7f} {1 - texcoords[t + 1]:.7f}\n")
            output.write("s 1\n")
            for triangle in range(0, len(indices), 3):
                a, b, c = (indices[triangle + offset] + 1 for offset in range(3))
                output.write(f"f {a}/{a} {b}/{b} {c}/{c}\n")
        material = document["materials"][primitive["material"]]
        output_color.parent.mkdir(parents=True, exist_ok=True)
        output_normal.parent.mkdir(parents=True, exist_ok=True)
        output_color.write_bytes(image_bytes(document, binary, material["pbrMetallicRoughness"]["baseColorTexture"]))
        output_normal.write_bytes(image_bytes(document, binary, material["normalTexture"]))
        print(f"{output_obj.stem}: full source exported with {count:,} vertices and {len(indices) // 3:,} triangles")
        return

    low = position_info["min"]
    high = position_info["max"]
    span = [max(high[axis] - low[axis], 1e-8) for axis in range(3)]
    mapping = array.array("I", [0]) * count
    cluster_index = {}
    clusters = []
    for vertex in range(count):
        p = vertex * 3
        t = vertex * 2
        key = (
            round((positions[p] - low[0]) / span[0] * cells),
            round((positions[p + 1] - low[1]) / span[1] * cells),
            round((positions[p + 2] - low[2]) / span[2] * cells),
            math.floor(texcoords[t] * uv_bins),
            math.floor(texcoords[t + 1] * uv_bins),
        )
        cluster = cluster_index.get(key)
        if cluster is None:
            cluster = len(clusters)
            cluster_index[key] = cluster
            clusters.append([0.0, 0.0, 0.0, 0.0, 0.0, 0])
        mapping[vertex] = cluster
        data = clusters[cluster]
        data[0] += positions[p]
        data[1] += positions[p + 1]
        data[2] += positions[p + 2]
        data[3] += texcoords[t]
        data[4] += texcoords[t + 1]
        data[5] += 1

    quaternion = document.get("nodes", [{}])[0].get("rotation", [0, 0, 0, 1])
    points = []
    uvs = []
    for x, y, z, u, v, n in clusters:
        points.append(rotate((x / n, y / n, z / n), quaternion))
        uvs.append((u / n, 1 - v / n))  # glTF image UV origin to OBJ/Unity.

    output_obj.parent.mkdir(parents=True, exist_ok=True)
    seen = set()
    kept = 0
    with output_obj.open("w", encoding="ascii", newline="\n") as output:
        output.write(f"# Optimized from source GLB; {count} source vertices\n")
        output.write(f"o {output_obj.stem}\n")
        for x, y, z in points:
            output.write(f"v {x:.7f} {y:.7f} {z:.7f}\n")
        for u, v in uvs:
            output.write(f"vt {u:.7f} {v:.7f}\n")
        output.write("s 1\n")
        for triangle in range(0, len(indices), 3):
            a, b, c = (mapping[indices[triangle + offset]] for offset in range(3))
            if a == b or b == c or c == a:
                continue
            signature = tuple(sorted((a, b, c)))
            if signature in seen:
                continue
            seen.add(signature)
            # Most microscopic faces vanished during clustering. Keep faces with real area.
            pa, pb, pc = points[a], points[b], points[c]
            ab = (pb[0] - pa[0], pb[1] - pa[1], pb[2] - pa[2])
            ac = (pc[0] - pa[0], pc[1] - pa[1], pc[2] - pa[2])
            cross = (
                ab[1] * ac[2] - ab[2] * ac[1],
                ab[2] * ac[0] - ab[0] * ac[2],
                ab[0] * ac[1] - ab[1] * ac[0],
            )
            if sum(value * value for value in cross) < 1e-14:
                continue
            output.write(f"f {a + 1}/{a + 1} {b + 1}/{b + 1} {c + 1}/{c + 1}\n")
            kept += 1

    material = document["materials"][primitive["material"]]
    color = image_bytes(document, binary, material["pbrMetallicRoughness"]["baseColorTexture"])
    normal = image_bytes(document, binary, material["normalTexture"])
    output_color.parent.mkdir(parents=True, exist_ok=True)
    output_normal.parent.mkdir(parents=True, exist_ok=True)
    output_color.write_bytes(color)
    output_normal.write_bytes(normal)
    print(f"{output_obj.stem}: {count:,} vertices / {len(indices) // 3:,} triangles -> {len(points):,} vertices / {kept:,} triangles")


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("source", type=Path)
    parser.add_argument("output_obj", type=Path)
    parser.add_argument("output_color", type=Path)
    parser.add_argument("output_normal", type=Path)
    parser.add_argument("--cells", type=int, default=0, help="0 exports the full mesh for texture-aware decimation")
    parser.add_argument("--uv-bins", type=int, default=8)
    args = parser.parse_args()
    convert(args.source, args.output_obj, args.output_color, args.output_normal, args.cells, args.uv_bins)
