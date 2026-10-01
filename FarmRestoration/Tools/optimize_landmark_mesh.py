"""Texture-aware GLB landmark decimation after convert_glb_landmark.py.

Requires PyMeshLab (install outside Unity Assets). Example:
  py Tools/optimize_landmark_mesh.py full.obj Assets/.../MountainA.obj --faces 45000
"""

import argparse
import uuid
from pathlib import Path

import pymeshlab


def optimize(source: Path, destination: Path, target_faces: int) -> None:
    scene = pymeshlab.MeshSet()
    scene.load_new_mesh(str(source))
    original = scene.current_mesh()
    if original.face_number() == 0 or not original.has_wedge_tex_coord():
        raise ValueError("Input must be a textured triangle OBJ with wedge UV coordinates")
    original_faces = original.face_number()
    scene.meshing_decimation_quadric_edge_collapse_with_texture(
        targetfacenum=target_faces,
        preserveboundary=False,
        preservenormal=True,
        optimalplacement=True,
        extratcoordw=0.5,
    )
    result = scene.current_mesh()
    if result.face_number() == 0 or not result.has_wedge_tex_coord():
        raise RuntimeError("Mesh simplification lost the model or its UVs")
    destination.parent.mkdir(parents=True, exist_ok=True)
    meshlab_output = Path(__file__).parent / f"landmark_{uuid.uuid4().hex}.obj"
    try:
        scene.save_current_mesh(str(meshlab_output))
        # Unity receives a URP material from ImportedLandmarksSetup, not MeshLab's MTL.
        with meshlab_output.open("r", encoding="ascii") as source_file, destination.open(
            "w", encoding="ascii", newline="\n"
        ) as target_file:
            for line in source_file:
                if not line.startswith(("mtllib ", "usemtl ")):
                    target_file.write(line)
    finally:
        meshlab_output.unlink(missing_ok=True)
        Path(str(meshlab_output) + ".mtl").unlink(missing_ok=True)
    print(
        f"{destination.name}: {original_faces:,} -> {result.face_number():,} faces, "
        f"{result.vertex_number():,} vertices, UV preserved"
    )


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("source", type=Path)
    parser.add_argument("destination", type=Path)
    parser.add_argument("--faces", type=int, required=True)
    arguments = parser.parse_args()
    optimize(arguments.source, arguments.destination, arguments.faces)
