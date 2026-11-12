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
  · It reads EVERY held edition's rows (kb/Work PB5374), each against its own edition's catalog, and refuses a §8 row
    that no row of any held edition claims: a derivation on a row of an edition the checker did not open would be
    SKIPPED, and a skipped row is a row nobody checked. It checks the `signatures` map itself too
    (`Derivation.signature_findings`: a known signer each, and a decision note for the deciding session's keys).
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

from inventory_schema import DerivationRow, load_schema
from inventory_view import edition_views
sys.path.append(str(pathlib.Path(__file__).resolve().parents[1]))  # scripts/, for standard_streams
from standard_streams import ensure_utf8  # noqa: E402

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
                                           r["signature"], r.get("refuter", "")) for r in world["register"]}
    d._undefined = {int(k): set(v) for k, v in world["undefined"].items() if not k.startswith("$")}
    d._refuters = {path: dict(records) for path, records in world["refuter-records"].items() if not path.startswith("$")}
    schema._catalog = {rid: (int(year), text) for year, rules in world["catalogs"].items() if not year.startswith("$")
                       for rid, text in rules.items()}
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
    """Every live row that CLAIMS a derivation, and every reason one of them does not stand. `rows` are EVERY held
    edition's (kb/Work PB5374)."""
    claimed, findings = [], []
    for row in rows:
        if not (row.get(schema.derivation.field) or "").strip():
            continue
        claimed.append(row["rule-id"])
        findings += [f'{row["rule-id"]}: [{r.code}] {r.message}' for r in schema.derivation.refusals(row, schema)]
    return claimed, findings


def unclaimed_register_rows(schema, claimed: list[str]) -> list[str]:
    """Every §8 row no row of a held edition claims: its rule is in no edition this checker opened, or no row names it.
    Either way it was never checked, and an unchecked determination is refused, never skipped (kb/Work PB5374)."""
    d = schema.derivation
    keys = {d.key_for({"rule-id": rid}) for rid in claimed}
    return [f"{key}: docs/CONFORMANCE.md {d.heading.strip('# ')} carries this row, and no row of a held edition "
            f"({', '.join(str(e.year) for e in schema.all_editions if e.held)}) claims it, so nothing checked it"
            for key in sorted(set(d.register) - keys)]


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
                  "has-spec-derived-test", "verdict-does-not-resolve", "determination-not-stated",
                  "a2-lists-rule", "quote-not-in-rule", "no-refuter-record", "refuter-not-upheld"}
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
    # …the refuter records too: a world with none must break every claude-signed argued case that closes.
    unrefuted = json.loads(json.dumps(fixture))
    unrefuted["world"]["refuter-records"] = {}
    check("emptying the fabricated refuter records breaks the argued arms the deciding session signed",
          bool(parity_findings(unrefuted, evaluate_fixture(unrefuted))))
    # …and the catalogs: a world whose 1985 catalog is gone must break the undefined-in-rule arm.
    uncatalogued = json.loads(json.dumps(fixture))
    uncatalogued["world"]["catalogs"].pop("1985")
    check("dropping the fabricated 1985 catalog breaks the arms that read the row's own edition",
          bool(parity_findings(uncatalogued, evaluate_fixture(uncatalogued))))

    # The live gate's two population checks, against a fabricated register and inventory.
    schema = schema_for(fixture["world"])
    claimed = ["GR-14.9.5.4-11"]
    orphans = unclaimed_register_rows(schema, claimed)
    check("a §8 row no held edition's row claims is a finding (never skipped)",
          any(o.startswith("DRV-1985-GR-XIV.3.6.4-5:") for o in orphans) and not any(
              o.startswith("DRV-GR-14.9.5.4-11:") for o in orphans), "; ".join(orphans[:3]))
    d = schema.derivation
    saved = dict(d.signatures)
    d.signatures["nobody: 2026-11-12"] = "kb/Work PB5373"
    d.signatures["claude: 2026-11-13"] = "a decision recorded nowhere"
    d.signatures["claude: 2026-11-14"] = "kb/Work PB5374 — a defect note, not a decision"
    sig = d.signature_findings()
    d.signatures.clear()
    d.signatures.update(saved)
    check("a signature with an unknown signer, or a deciding-session key with no decision note, is a finding",
          len(sig) == 3 and not d.signature_findings(), "; ".join(sig))

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
        views = edition_views(schema)
        rows = [r for year_rows in views.values() for r in year_rows]
        claimed, live = live_findings(schema, rows)
        findings += live
        findings += unclaimed_register_rows(schema, claimed)
        findings += schema.derivation.signature_findings()
        closed = [r["rule-id"] for r in rows
                  if (r.get(schema.derivation.field) or "").strip() and r["state"] == "OK"]
        print(f"inventory     : {len(rows)} rows from the views of "
              + ", ".join(f"{year} ({len(v)})" for year, v in views.items()))
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
    ensure_utf8()
    sys.exit(main())
