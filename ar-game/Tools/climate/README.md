# Climate asset builder

From the repository root:

```bash
python3 -m venv ar-game/Tools/climate/.cache/venv
ar-game/Tools/climate/.cache/venv/bin/pip install -r ar-game/Tools/climate/requirements.txt
ar-game/Tools/climate/.cache/venv/bin/pytest -q ar-game/Tools/climate
ar-game/Tools/climate/.cache/venv/bin/python ar-game/Tools/climate/build_koppen_bytes.py
```

The default output is `ar-game/Assets/FantasyEarth/Data/koppen_1991_2020_0p1.bytes`, independent of the shell's working directory. `--cache-dir` and `--out-path` override the defaults. The public archive and virtual environment stay in the ignored `.cache/` directory.

The builder verifies the pinned archive checksum on downloads and cache hits, selects the 1991–2020 0.1-degree raster, validates its shape, class values, type, and georeferencing, and normalizes rows north to south. See `ar-game/Assets/FantasyEarth/Data/ATTRIBUTION.md` for the original citation, CC BY 4.0 license, archive checksum, and expected output hash. The `.bytes` asset is tracked through Git LFS.
