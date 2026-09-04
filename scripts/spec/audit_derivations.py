#!/usr/bin/env python3
"""The gate over the `derivation` evidence kind — §1.1's owner-signed alternative to a spec-derived test.

    python scripts/spec/audit_derivations.py              # report every live derivation and any finding
    python scripts/spec/audit_derivations.py --check      # exit 1 on any finding (the gate)
    python scripts/spec/audit_derivations.py --parity     # evaluate the cross-language parity fixture
    python scripts/spec/audit_derivations.py --json       # one machine-readable line for the C# gate
    python scripts/spec/audit_derivations.py --self-test  # prove every check here can FAIL

WHY THIS EXISTS. `kb/Work/PB386` (owner, 2026-09-03) admits a determination in `docs/CONFORMANCE.md` §8 in place
of §1(c)'s covering test, for a rule that carries NO OBSERVABLE OBLIGATION. Its second stated cost is that the
determination be **checkable, not a free-text escape hatch, or the GAP metric becomes cheaper to move than the
work it stands for** — and its first is that `Schema.state_for` and its C# twin `DerivedState` learn the evidence
kind in the SAME change set, because `kb/Work/PB315` is the note recording what happens when they do not.

This script is both of those costs, on the Python side:

  · `--check` runs the ONE predicate (`inventory_schema.Derivation.refusals`) over every live inventory row that
    claims a derivation, so the A.2 arm is re-resolved against the standard and the argument arms are re-checked
    for shape as the register, the catalog and the spec change underneath a determination recorded sessions ago.
    It also asserts its own POPULATION — a run that evaluated nothing would report "no findings" about nothing,
    and a MISSING observation is not a NEGATIVE one (`feedback_verdict_evidence_invariant`).
  · `--parity` evaluates `tests/version-matrix/derivation-parity-cases.json`, the fixture the C# gate evaluates
    too. Both engines must produce the same `state` AND the same refusal CODES for every case; comparing codes
    rather than a boolean is the point, because two evaluators refusing one row for two different reasons look
    identical under "it was refused" — which is exactly what PB315's disagreement hid behind.
"""
from __future__ import annotations

import argparse
import json
import pathlib
import sys

from inventory_schema import (DerivationRow, INVENTORY_PATH, load_inventory, load_schema)

REPO = pathlib.Path(__file__).resolve().parents[2]
FIXTURE = REPO / "tests" / "version-matrix" / "derivation-parity-cases.json"
FIXTURE_REL = "tests/version-matrix/derivation-parity-cases.json"


def load_fixture(path: pathlib.Path = FIXTURE) -> dict:
    if not path.exists():
        raise SystemExit(f"parity fixture not found: {path}")
    return json.loads(path.read_text(encoding="utf-8"))


def schema_for(world: dict):
    """A schema whose derivation reads the FABRICATED world instead of the live artifacts."""
    schema = load_schema()
    d = schema.derivation
    if d is None:
        raise SystemExit("inventory-schema.json declares no `derivation` — there is nothing to evaluate")
    d._register = {r["key"]: DerivationRow(r["key"], r["arm"], r["names"], r.get("argument", ""),
                                           r["signature"]) for r in world["register"]}
    d._undefined = {int(k): set(v) for k, v in world["undefined"].items() if not k.startswith("$")}
    schema._rule_ids = set(world["catalog-rule-ids"])
    schema._register_items = set(world["register-items"])
    return schema


def evaluate_fixture(fixture: dict) -> list[dict]:
    """`[{name, state, refusals}]` — this engine's answer for every case, in fixture order."""
    out = []
    for case in fixture["cases"]:
        world = {**fixture["world"], **case.get("world-overrides", {})}
        schema = schema_for(world)
        row = dict(case["row"])
        out.append({
            "name": case["name"],
            "state": schema.state_for(row),
            "refusals": sorted(r.code for r in schema.derivation.refusals(row, schema)),
        })
    return out


def parity_findings(fixture: dict, answers: list[dict]) -> list[str]:
    """Where THIS engine disagrees with the fixture's recorded expectation."""
    bad = []
    for case, got in zip(fixture["cases"], answers, strict=True):
        want_state, want_ref = case["state"], sorted(case["refusals"])
        if got["state"] != want_state:
            bad.append(f'parity case "{case["name"]}": state {got["state"]}, fixture says {want_state}')
        if got["refusals"] != want_ref:
            bad.append(f'parity case "{case["name"]}": refusals {got["refusals"]}, fixture says {want_ref}')
    return bad


def live_findings(schema, rows: list[dict]) -> tuple[list[str], list[str]]:
    """Every live row that CLAIMS a derivation, and every reason one of them does not stand."""
    claimed, findings = [], []
    for row in rows:
        if not (row.get(schema.derivation.field) or "").strip():
            continue
        claimed.append(row["rule-id"])
        findings += [f'{row["rule-id"]}: [{r.code}] {r.message}' for r in schema.derivation.refusals(row, schema)]
    return claimed, findings


# ── self-test ────────────────────────────────────────────────────────────────────────────────────────

def self_test() -> int:
    fixture = load_fixture()
    cases: list[tuple[str, bool, str]] = []

    def check(name: str, ok: bool, detail: str = "") -> None:
        cases.append((name, ok, detail))

    answers = evaluate_fixture(fixture)
    check("the parity fixture is evaluated with NO disagreement",
          not parity_findings(fixture, answers), "; ".join(parity_findings(fixture, answers)))
    check("the fixture drives BOTH outcomes, so it is not one-sided",
          {a["state"] for a in answers} == {"OK", "GAP"}, f"got {sorted({a['state'] for a in answers})}")
    codes = {c for a in answers for c in a["refusals"]}
    want_codes = {"a2-does-not-cover", "a2-no-such-item", "bad-signature", "unknown-arm", "no-register-row",
                  "not-computed-anchor", "names-shape", "self-indistinguishable", "unknown-rule",
                  "has-spec-derived-test", "verdict-does-not-resolve", "determination-not-stated"}
    check("EVERY refusal code the evaluator can emit is driven by a case",
          codes == want_codes, f"missing {sorted(want_codes - codes)}, extra {sorted(codes - want_codes)}")

    # ⛔ THE COMPARISON MUST BE ABLE TO FAIL. A `parity_findings` that returned [] unconditionally would satisfy
    # the first case above and measure nothing (feedback_green_gates_arent_evidence).
    corrupted = json.loads(json.dumps(fixture))
    corrupted["cases"][0]["state"] = "GAP" if corrupted["cases"][0]["state"] == "OK" else "OK"
    check("a fixture whose expected STATE is wrong is caught",
          bool(parity_findings(corrupted, evaluate_fixture(corrupted))))
    corrupted2 = json.loads(json.dumps(fixture))
    corrupted2["cases"][1]["refusals"] = []
    check("a fixture whose expected REFUSAL SET is wrong is caught",
          bool(parity_findings(corrupted2, evaluate_fixture(corrupted2))))
    # …and the world must be load-bearing: emptying the fabricated A.2 resolution must break the A.2 arm.
    blinded = json.loads(json.dumps(fixture))
    blinded["world"]["undefined"] = {}
    check("emptying the fabricated Annex A.2 list breaks the A.2 arm (the world is load-bearing)",
          bool(parity_findings(blinded, evaluate_fixture(blinded))))

    for name, ok, detail in cases:
        print(f"  {'PASS' if ok else 'FAIL'}  {name}" + (f"  — {detail}" if not ok and detail else ""))
    bad = sum(1 for _, ok, _ in cases if not ok)
    print(f"\n{len(cases) - bad}/{len(cases)} self-test case(s) passed")
    return 1 if bad else 0


def main() -> int:
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("--check", action="store_true", help="exit 1 on any finding")
    ap.add_argument("--parity", action="store_true", help="evaluate the cross-language parity fixture")
    ap.add_argument("--json", action="store_true", help="emit one machine-readable JSON line")
    ap.add_argument("--self-test", action="store_true", help="prove every check here can fail")
    args = ap.parse_args()
    try:
        sys.stdout.reconfigure(encoding="utf-8")
    except Exception:  # noqa: BLE001
        pass

    if args.self_test:
        return self_test()

    findings: list[str] = []
    answers: list[dict] = []
    claimed: list[str] = []

    if args.parity:
        fixture = load_fixture()
        answers = evaluate_fixture(fixture)
        findings += parity_findings(fixture, answers)
        print(f"parity cases  : {len(answers)} from {FIXTURE_REL}")
    else:
        schema = load_schema()
        if schema.derivation is None:
            print("⛔ inventory-schema.json declares no `derivation` — §1.1's evidence kind is gone")
            return 1
        rows = load_inventory()
        claimed, live = live_findings(schema, rows)
        findings += live
        closed = [r["rule-id"] for r in rows
                  if (r.get(schema.derivation.field) or "").strip() and r["state"] == "OK"]
        print(f"inventory     : {len(rows)} rows from {INVENTORY_PATH.name}")
        print(f"  claiming a derivation : {len(claimed)}")
        print(f"  closed on one         : {len(closed)}  {sorted(closed)}")
        # ⛔ POPULATION BEFORE VERDICT. "No findings" over zero rows is a statement about nothing.
        if args.check and not claimed:
            findings.append("no inventory row claims a derivation — this gate measured NOTHING, so the "
                            "inventory or the schema is what changed, not the register")

    if args.json:
        print("JSON " + json.dumps({"findings": findings, "claimed": claimed, "parity": answers},
                                   ensure_ascii=False))

    if findings:
        print(f"\n⛔ {len(findings)} finding(s):")
        for f in findings:
            print(f"   {f}")
        return 1 if args.check else 0
    print("\nno findings.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
