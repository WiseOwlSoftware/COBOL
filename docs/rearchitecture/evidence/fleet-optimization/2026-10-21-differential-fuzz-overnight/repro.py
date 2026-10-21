"""Compile and run fuzz programs on the fresh main build, with timeouts. usage: repro.py <results-dir> <id>..."""
import subprocess, sys, os, shutil, time
from pathlib import Path

COBOL = r"E:\COBOL\src\Cobol.Net.Cli\bin\Debug\net10.0\cobol.exe"
res = Path(sys.argv[1])
work = Path(r"E:\Temp\claude\E--COBOL\3b7f980b-9825-4d6d-8b96-9cfd8422deb8\scratchpad\work")
work.mkdir(exist_ok=True)
env = dict(os.environ, COBOL_USER_LOCALE="INVARIANT", COBOL_SYSTEM_LOCALE="INVARIANT")
show = "--show" in sys.argv
for pid in [a for a in sys.argv[2:] if not a.startswith("--")]:
    src = res / "programs" / f"{pid}.cob"
    d = work / pid
    shutil.rmtree(d, ignore_errors=True)
    d.mkdir()
    shutil.copy(src, d / f"{pid}.cob")
    print(f"\n##### {pid}")
    if show:
        print(src.read_text(encoding="utf-8", errors="replace"))
    t = time.time()
    try:
        c = subprocess.run([COBOL, f"{pid}.cob", "--std", "2014", "--source-format", "free", "-o", f"{pid}.dll"],
                           cwd=d, capture_output=True, text=True, timeout=120, env=env)
    except subprocess.TimeoutExpired:
        print(f"COMPILE HUNG (>120 s)")
        continue
    print(f"compile rc={c.returncode} in {time.time()-t:.1f}s")
    if c.returncode != 0:
        print((c.stdout + c.stderr)[:1500])
        continue
    t = time.time()
    try:
        r = subprocess.run(["dotnet", f"{pid}.dll"], cwd=d, capture_output=True, text=True, timeout=60, env=env)
        print(f"run rc={r.returncode} ({r.returncode & 0xFFFFFFFF:#x}) in {time.time()-t:.1f}s")
        print(r.stdout[:2500])
        if r.stderr:
            print("STDERR:", r.stderr[:2500])
    except subprocess.TimeoutExpired as e:
        print(f"RUN HUNG (>60 s); partial stdout: {(e.stdout or b'')[:800]!r}")
