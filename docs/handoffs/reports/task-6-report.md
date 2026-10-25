# Task 6: bundled climate data and ClimateModel

Status: **implemented and validated**, 2026-10-25. This completes the full Task 6, including the smaller Task 6a handoff.

- Added the Python converter, local dependency list, and nine tests for row-major output, shape/type/class validation, cached archives, north/south orientation, and invalid georeferencing.
- Downloaded the pinned Figshare archive (130,618,411 bytes), checked published MD5 `7fc2f5a15d4f5fe0ce59c9a9b502aa09`, and extracted the 1991–2020 0.1-degree member.
- Verified `(1800, 3600)` uint8 data, pixel scale `(0.1, 0.1, 0)`, and tie point `(-180, 90)`. Generated exactly 6,480,000 bytes. A cached rebuild produced the same SHA-256: `533e3f2f20ac758ce87014aeb3151e7552432f1146e4bb3022cff9012fb6b047`.
- Bundled the grid with CC BY 4.0 attribution and the fixed dataset provenance model. World remains free of Unity references and independent of Fantasy.
- Added three synthetic model cases and five real-data cases: Phoenix, Singapore, London, Utqiaġvik, and the mid-Atlantic no-data fallback.

Validation:

```bash
ar-game/Tools/climate/.cache/venv/bin/pytest -q ar-game/Tools/climate
ar-game/Tools/climate/.cache/venv/bin/python ar-game/Tools/climate/build_koppen_bytes.py
unity command script recompile --project-path "$PROJ" --format json
unity command script recompile_status --project-path "$PROJ" --format json
unity command test run --mode EditMode --filter FantasyEarth --project-path "$PROJ" --format json
```

RED: Python collection failed because the converter module was absent; Unity compilation failed with CS0246 because `ClimateModel` was absent. GREEN: **9 Python tests passed; 55 Unity EditMode cases passed, 0 failed/skipped; compilation completed without errors**. Unity commands used approved host access on the running 6000.3.27f1 Editor. Detailed JSON is stored locally in the plan's ignored `.superpowers/sdd/` directory.

No scene or device setup was required. The user-visible attribution screen remains a Plan 3 deliverable.

Final review follow-up: a new regression case demonstrated that a cache hit accepted an archive with the wrong checksum and overwrote the existing output. The converter now verifies cached archives against the same pinned MD5 before decoding or writing. The expanded Python suite passes **10/10**; regenerating from the real, verified cache still produces the exact SHA-256 above. Synthetic archive fixtures supply their own expected checksum for orientation/georeferencing tests.
