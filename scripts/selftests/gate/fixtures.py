#!/usr/bin/env python3
"""The gate's planted worlds (DESIGN-orchestrator-loop.md section 16.2, kb/Work PB5432): `world()` is a scratch directory and
the helpers a case runs a `gate.Gate` over a `gate_fake_host.FakeHost` with (`gate(name, **fake)` runs one gate in its own
directory of the world and clears the console sink; `said(text)` asks the sink), and `audit_shape_arms`, the ONE SHAPE audit on a planted tree. No case lives here."""
from __future__ import annotations

import tempfile
from pathlib import Path

import selftest_cases
from gate_fake_host import FakeHost
from scratch_repo import isolate, scratch_dir

gate_mod = selftest_cases.subject(__file__)   # scripts/gate.py
Gate = gate_mod.Gate
Outcome = gate_mod.Outcome
from gate_audits import audit_shape


def world():
    """(root, sink, gate, said): a scratch directory of this case's own, the console lines of the last gate run in it, and the helpers."""
    isolate()  # ⛔ hermetic: the cases make git repositories (scratch_repo.isolate, kb/Work PB1719)
    root = scratch_dir("gate-legs-selftest-")
    sink: list[str] = []

    def gate(name: str, **fake) -> tuple[Outcome, FakeHost]:
        d = root / name
        d.mkdir()
        host = FakeHost(d, d / "gate.lock", **fake)
        sink.clear()
        return Gate(host, d / "runs", say=sink.append).run(), host

    def said(text: str) -> bool:
        return any(text in line for line in sink)

    return root, sink, gate, said


def audit_shape_arms() -> str:
    """The audit on a planted tree: '' when every arm holds, else what failed."""
    with tempfile.TemporaryDirectory() as tmp:
        root = Path(tmp)
        planted = {"scripts/a.ps1": "pwsh build-local.ps1 -Mode " + "lander\n", "docs/b.md": "ok\nrun --mode " + "implementer now\n",
                   "README.md": "-Mode " + "implementer\n", "DEVLOG/1.md": "-Mode " + "lander\n",
                   "docs/rearchitecture/evidence/x.md": "-Mode " + "lander\n", "tests/T.cs": "-Modest lander\n",
                   "src/a.cs": "-Mode " + "lander\n"}
        for rel, text in planted.items():
            (root / rel).parent.mkdir(parents=True, exist_ok=True)
            (root / rel).write_text(text, encoding="utf-8", newline="\n")
        got = [b.split(":", 2)[0] + ":" + b.split(":", 2)[1] for b in audit_shape(root, sorted(planted))]
        want = ["README.md:1", "docs/b.md:2", "scripts/a.ps1:1"]
        return "" if got == want else f"got {got}, want {want}"
