#!/usr/bin/env python3
"""Recreate the Unity OBJ from the repository's original GLB sample.

Run before opening the Unity project after a fresh clone. The OBJ exceeds
GitHub's 100 MB per-file limit, so Git intentionally does not track it.
"""
from pathlib import Path
import sys

try:
    import trimesh
except ImportError as error:
    raise SystemExit("Install dependencies first: python3 -m pip install -r scripts/requirements.txt") from error

root = Path(__file__).resolve().parents[1]
source = root / "sample-assets" / "result.glb"
output = root / "UnityProject" / "Assets" / "Models" / "UserModel"
if not source.is_file():
    raise SystemExit(f"Missing source model: {source}")
output.mkdir(parents=True, exist_ok=True)
scene = trimesh.load(source, force="scene")
if len(scene.geometry) != 1:
    raise SystemExit(f"Expected one mesh in sample GLB; found {len(scene.geometry)}")
mesh = next(iter(scene.geometry.values()))
obj, textures = trimesh.exchange.obj.export_obj(
    mesh, return_texture=True, mtl_name="result.mtl"
)
(output / "result.obj").write_text(obj, encoding="utf-8")
for name, data in textures.items():
    if Path(name).name != name:
        raise SystemExit(f"Unsafe output filename: {name}")
    (output / name).write_bytes(data)
print(f"Generated {output / 'result.obj'} ({len(mesh.vertices)} vertices, {len(mesh.faces)} triangles)")
print("Keep result.obj.meta next to the generated OBJ to preserve Unity scene references.")
