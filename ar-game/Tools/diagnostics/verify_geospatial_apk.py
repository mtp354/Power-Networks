"""Verify that an APK defines the Java classes ARCore Geospatial loads at runtime."""

import argparse
from pathlib import Path
import re
import subprocess
import tempfile
import zipfile


REQUIRED = {
    "Lcom/google/android/gms/location/FusedLocationProviderClient;",
    "Lcom/google/android/gms/auth/GoogleAuthUtil;",
}


def verify(apk, dexdump):
    found = set()
    with zipfile.ZipFile(apk) as archive, tempfile.TemporaryDirectory() as temporary:
        for name in archive.namelist():
            if not re.fullmatch(r"classes\d*\.dex", name):
                continue
            path = Path(temporary) / name
            path.write_bytes(archive.read(name))
            result = subprocess.run([str(dexdump), str(path)], capture_output=True,
                                    encoding="utf-8", errors="replace", check=True)
            # Class definitions, rather than string references or .properties markers.
            defined = set(re.findall(r"Class descriptor\s*:\s*'([^']+)'", result.stdout))
            found.update(defined & REQUIRED)
            if found == REQUIRED:
                break
    missing = REQUIRED - found
    if missing:
        raise ValueError("APK is missing required class definitions: " + ", ".join(sorted(missing)))
    return sorted(found)


def find_dexdump():
    project = Path(__file__).resolve().parents[2]
    settings = (project / "ProjectSettings/ProjectVersion.txt").read_text()
    version = re.search(r"^m_EditorVersion: (\S+)", settings, re.MULTILINE).group(1)
    sdk = Path.home() / "Unity/Hub/Editor" / version / "Editor/Data/PlaybackEngines/AndroidPlayer/SDK"
    candidates = sorted(sdk.glob("build-tools/*/dexdump"), reverse=True)
    if not candidates:
        raise ValueError("Supply --dexdump from an installed Android SDK build-tools directory")
    return candidates[0]


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("apk", type=Path)
    parser.add_argument("--dexdump", type=Path)
    args = parser.parse_args()
    for name in verify(args.apk, args.dexdump or find_dexdump()):
        print("Verified class definition:", name)
