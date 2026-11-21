#!/usr/bin/env python3
"""THE IMPORT OF ADJUDICATED DISPOSITIONS into a content-keyed manifest (kb/Work PB5452; decision PB5448 point 3).

A disposition campaign (kb/Work PB5450 for 2023, PB5452 for 1985) writes, per slice of an edition's units of unnumbered
text, a READER's records (`<slice>.json`: one record per unit, keyed by clause, unit label, digest and order, its
disposition SR/GR/AR/RV/DOC, `restates` with `rules`, `not-a-rule` with `class`, or `pending` with a `repair` or a
`hold`) and an independent REFUTER's (`<slice>-refute.jsonl`, or `.json`: the attempt, `outcome` UPHELD or OVERTURNED,
an OVERTURNED one's `corrected` disposition). This is the ONE writer that turns those records into the manifest's
`paragraph-blocks` entries the RULE CATALOGS audit reads (`text_units.audit`) and commits the refuter records the
entries name (`refuted`, JSON lines in the tree), and that refutes the manifest's `not-rules` blocks the same way
(`text_units.block_refusals`). It decides nothing: every disposition it writes is a record's.

THE DISPOSITION OF A UNIT is its reader record's, the record of the LATEST slice in `--slices` holding its key winning
(a re-keyed or re-decided slice after the first), corrected by that slice's refuter: an OVERTURNED record's `corrected`
disposition wins ("SR", "GR (definitional)", "restates <ids>", "not-a-rule <class>", "repair", "hold"). Refused, each a
failure of the run (nothing is written):
  - a record whose key is no unit of the tree, unless `--skip` lists it (a key a re-segmentation retired);
  - a `--skip` key that IS a unit of the tree (skipping it would hide a unit), or that no record holds;
  - a unit of the tree, pending or not, with no record (every unit is disposed, decision PB5448 point 3);
  - a `restates` or `not-a-rule` whose deciding slice holds no UPHELD refuter record of its digest: an OVERTURNED
    record correcting a row to `restates` is itself refuted in a later slice, or the unit stays out of nothing;
  - two refuter records of one unit in one slice that disagree.
A `repair` or a `hold` leaves its unit pending (decided again on the repaired text or the corrected segmentation).

THE BLOCKS (`--blocks <slice>`): the refuter's records of the manifest's `not-rules` blocks, keyed `unit` = the clause.
An UPHELD one stands: the block takes `refuted` and the `digest` the content-keyed scan reads its body under now (the
record's own `digest` is rewritten to it, the one it read kept as `digest_read`); an OVERTURNED one makes the block
`pending`, outside the count until its rows are decided (`pending-clauses`). The run sets `not-rules-refuted`, so a
`not-rules` block without its record is refused from then on.

The committed refuter files are `<--into>/<slice>-refute.jsonl`, one record per line, a record's `unit` the unit's label
on the tree now (the label it was read under kept as `unit_read` when a re-segmentation relabelled it).

    python scripts/spec/import_dispositions.py --edition 1985 --records <dir> --slices 85u-01,...,85x-02 \\
        --skip <skip.json> --blocks 85b-01 [--write]
    Its cases: scripts/selftests/spec/import_dispositions/test_import_dispositions.py
"""
from __future__ import annotations

import argparse
import dataclasses
import json
import pathlib
import re
import sys

sys.path.insert(0, str(pathlib.Path(__file__).resolve().parent))
import text_units  # noqa: E402
sys.path.append(str(pathlib.Path(__file__).resolve().parents[1]))  # scripts/, for standard_streams
from standard_streams import ensure_utf8  # noqa: E402

REPO = pathlib.Path(__file__).resolve().parents[2]
#: Where an edition's committed refuter records live: beside its content-keyed manifest (kb/Work PB5452).
INTO = {1985: "docs/rearchitecture/spec-dispositions-1985"}
#: A row id a `restates` correction names.
ROW_ID = re.compile(r"\b(?:\d{4}-)?(?:SR|GR|AR|RV|DOC|FMT)-[\w.]+(?:-[\w.]+)?")

Key = tuple[str, str, int]   # (clause, digest, order): a unit's key on the tree (decision PB5448 point 3)


@dataclasses.dataclass
class Outcome:
    """What an import decides: the entries by clause and key, the refuter records to commit by slice, and the
    refusals (any one fails the run)."""
    entries: dict[str, dict[tuple[str, int], dict]]
    records: dict[str, list[dict]]
    refusals: list[str]


def load(path: pathlib.Path) -> list[dict]:
    """The records of a slice file: a JSON array, or JSON lines (a refuter writes either)."""
    text = path.read_text(encoding="utf-8")
    try:
        data = json.loads(text)
        return data if isinstance(data, list) else [data]
    except json.JSONDecodeError:
        return [json.loads(ln) for ln in text.splitlines() if ln.strip()]


def corrected(rec: dict) -> dict:
    """The disposition an OVERTURNED refuter record's `corrected` field names, as entry fields."""
    c = str(rec.get("corrected", "")).strip()
    head = c.split()[0] if c else ""
    if head in text_units.ROW_KINDS:
        return {"disposition": head, **({"definitional": True} if "definitional" in c else {})}
    if head == text_units.RESTATES:
        return {"disposition": text_units.RESTATES, "rules": ROW_ID.findall(c)}
    if c.startswith(text_units.NOT_A_RULE):
        cls = c[len(text_units.NOT_A_RULE):].strip(" /")
        return {"disposition": text_units.NOT_A_RULE, "class": cls}
    if head in ("repair", "hold"):
        return {"disposition": text_units.PENDING, head: True}
    raise ValueError(f"an OVERTURNED record's correction {c!r} names no disposition")


def decide(units: dict[str, list[tuple[str, str, int]]], slices: list[tuple[str, list[dict], list[dict]]],
           skip: list[dict], path_of, row_id=None) -> Outcome:
    """THE IMPORT, pure: `units` is the tree's units by clause, each (label, digest, order); `slices` the campaign's
    slices in precedence order, each (name, reader records, refuter records); `skip` the keys never imported, each
    {clause, unit, digest, order, file}; `path_of(slice)` the committed refuter file's repository path; `row_id(kind,
    clause, label)` spells the edition's row id, which resolves a `restates` chain (`_resolve_chains`)."""
    out = Outcome({}, {}, [])
    label_of = {(c, d, o): lab for c, us in units.items() for lab, d, o in us}
    skipped = {(s["clause"], s["unit"], s["digest"], s["order"], s.get("file")) for s in skip}
    final: dict[Key, tuple[str, dict]] = {}
    refuted: dict[tuple[str, str, str], dict] = {}   # (slice, clause, digest) -> its last refuter record
    held = set()
    for name, readers, refuters in slices:
        for r in readers:
            k = (r["clause"], r["digest"], r.get("order", 0))
            if (r["clause"], r["unit"], r["digest"], r.get("order", 0), f"{name}.json") in skipped:
                # A skip hides no unit: one no other record decides is refused below as a unit with no record.
                held.add((r["clause"], r["unit"], r["digest"], r.get("order", 0), f"{name}.json"))
                continue
            if k not in label_of:
                out.refusals.append(f"{r['clause']} {r['unit']} ({name}): no unit of the tree has this key (digest "
                                    f"{r['digest']}, order {r.get('order', 0)}): a stale record, or skip it")
                continue
            final[k] = (name, r)
        for rec in refuters:
            k = (name, rec["clause"], rec["digest"])
            if (prev := refuted.get(k)) is not None and (prev["outcome"], prev.get("corrected")) != (
                    rec["outcome"], rec.get("corrected")):
                out.refusals.append(f"{rec['clause']} {rec['unit']} ({name}): two refuter records disagree")
            refuted[k] = rec
    out.refusals += [f"{c} {u} ({f}): a skip no record holds" for c, u, d, o, f in skipped - held]
    for clause, us in units.items():
        for label, dg, order in us:
            k = (clause, dg, order)
            if k not in final:
                out.refusals.append(f"{clause} {label}: a unit of the tree with no record (digest {dg})")
                continue
            name, r = final[k]
            rec = refuted.get((name, clause, dg))
            if r.get("repair") or r.get("hold"):
                e = {"disposition": text_units.PENDING}
            elif rec is not None and rec.get("outcome") == "OVERTURNED":
                try:
                    e = corrected(rec)
                except ValueError as why:
                    out.refusals.append(f"{clause} {label} ({name}): {why}")
                    continue
                e = {"disposition": text_units.PENDING} if e["disposition"] == text_units.PENDING else e
            else:
                e = {"disposition": r["disposition"]}
                if r["disposition"] == text_units.RESTATES:
                    e["rules"] = list(r.get("rules") or [])
                elif r["disposition"] == text_units.NOT_A_RULE:
                    e["class"] = r.get("class", "")
                elif r.get("definitional"):
                    e["definitional"] = True
            if e["disposition"] in (text_units.RESTATES, text_units.NOT_A_RULE) and (
                    rec is None or rec.get("outcome") != text_units.UPHELD):
                out.refusals.append(f"{clause} {label} ({name}): {e['disposition']} with no UPHELD refuter record of "
                                    f"digest {dg} in its slice"
                                    + (" (its record OVERTURNED it: refute the correction in a later slice)"
                                       if rec is not None else ""))
                continue
            entry = {"unit": label, "digest": dg, "order": order, **e}
            if rec is not None:
                entry["refuted"] = path_of(name)
            out.entries.setdefault(clause, {})[dg, order] = entry
    if row_id is not None:
        _resolve_chains(out, row_id)
    for (name, clause, dg), rec in refuted.items():
        label = next((lab for (c, d, _o), lab in label_of.items() if c == clause and d == dg), None)
        if label is None:
            continue   # a record of a retired key: the slice file is committed whole only for live units
        row = {**rec, "unit": label}
        if label != rec["unit"]:
            row["unit_read"] = rec["unit"]
        out.records.setdefault(name, []).append(row)
    return out


def _resolve_chains(out: Outcome, row_id) -> None:
    """A `restates` naming the row of a unit this import disposes: when that unit is a row of the named kind, the id
    stands; when the unit is itself `restates`, its rules stand in its place (`restates` read transitively: the unit
    states the same requirement as one that states the same requirement as they do, kb/Work PB5452), the replaced id
    kept as `restated_via`; any other disposition of the named unit is refused."""
    by_id = {row_id(k, clause, e["unit"]): (k, e) for clause, es in out.entries.items() for e in es.values()
             for k in text_units.ROW_KINDS}
    for clause, es in out.entries.items():
        for e in es.values():
            if e["disposition"] != text_units.RESTATES:
                continue
            rules, via = list(e["rules"]), []
            changed = True
            while changed:
                changed, nxt = False, []
                for r in rules:
                    kind, t = by_id.get(r, (None, None))
                    if t is None or t["disposition"] == kind:
                        nxt.append(r)
                    elif t["disposition"] == text_units.RESTATES and r not in via:
                        via.append(r)
                        nxt += [x for x in t["rules"] if x not in nxt]
                        changed = True
                    else:
                        out.refusals.append(f"{clause} {e['unit']}: restates {r}, whose unit is {t['disposition']}")
                        nxt.append(r)
                rules = nxt
            if via:
                e["rules"], e["restated_via"] = rules, via


def refute_blocks(declared: dict[str, dict], measured: dict[str, dict], records: list[dict], path: str
                  ) -> tuple[dict[str, dict], list[dict], list[str]]:
    """The `not-rules` blocks after their refuter's records (module docstring, THE BLOCKS): the new `blocks` map, the
    records as committed, and the refusals (a record of no `not-rules` block, a block with no record or no measured
    body)."""
    blocks = {c: dict(v) for c, v in declared.items()}
    committed, refusals = [], []
    by_clause = {r["unit"]: r for r in records}
    for clause, info in blocks.items():
        if info.get("disposition") != "not-rules":
            continue
        rec = by_clause.get(clause)
        now = measured.get(clause, {}).get("digest")
        if rec is None or now is None:
            refusals.append(f"{clause}: a not-rules block " + ("with no refuter record" if rec is None
                                                               else "the content-keyed scan does not measure"))
            continue
        committed.append({**rec, "digest": now, **({"digest_read": rec["digest"]} if rec["digest"] != now else {})})
        if rec["outcome"] == text_units.UPHELD:
            info |= {"digest": now, "refuted": path}
        else:
            info |= {"disposition": "pending", "digest": now, "refuted": path,
                     "why": f"PENDING (kb/Work PB5452): its not-rules disposition was OVERTURNED by its refuter "
                            f"({rec.get('refuter')}, {rec.get('date')}), corrected to {rec.get('corrected')}: "
                            f"{rec.get('reason', '')}"}
    refusals += [f"{c}: a refuter record of no not-rules block" for c in by_clause.keys() - {
        c for c, v in declared.items() if v.get("disposition") == "not-rules"}]
    return blocks, committed, refusals


def _write_jsonl(path: pathlib.Path, records: list[dict]) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text("".join(json.dumps(r, ensure_ascii=False) + "\n" for r in records), encoding="utf-8", newline="\n")


def main() -> int:
    ensure_utf8()
    ap = argparse.ArgumentParser(description=__doc__.split("\n\n")[0])
    ap.add_argument("--edition", type=int, required=True, choices=sorted(INTO))
    ap.add_argument("--records", type=pathlib.Path, required=True, help="the campaign's records directory")
    ap.add_argument("--slices", required=True, help="the slices, comma-separated, in precedence order (later wins)")
    ap.add_argument("--skip", type=pathlib.Path, help="JSON list of the record keys never imported")
    ap.add_argument("--blocks", help="the slice of the not-rules blocks' refuter records")
    ap.add_argument("--write", action="store_true", help="write the manifest and the refuter files (else: report)")
    args = ap.parse_args()
    import extract_rule_catalog as erc  # noqa: PLC0415
    import spec_pdf  # noqa: PLC0415
    lines = spec_pdf.markdown(args.edition).read_text(encoding="utf-8").splitlines()
    cat, units, _after, _margin = erc.units_1985(lines)
    tree = {c: [(u.label, u.digest, o) for u, o in text_units.keyed(us)] for c, us in units.items()}
    slices = []
    for name in args.slices.split(","):
        reader = args.records / f"{name}.json"
        refs = [p for p in (args.records / f"{name}-refute.json", args.records / f"{name}-refute.jsonl") if p.exists()]
        seen, refuters = set(), []
        for p in refs:
            for r in load(p):
                if (sig := json.dumps(r, sort_keys=True)) not in seen:
                    seen.add(sig)
                    refuters.append(r)
        slices.append((name, load(reader), refuters))
    skip = json.loads(args.skip.read_text(encoding="utf-8")) if args.skip else []
    into = INTO[args.edition]
    import rule_catalog_1985  # noqa: PLC0415
    outcome = decide(tree, slices, skip, lambda name: f"{into}/{name}-refute.jsonl", rule_catalog_1985.unit_rule_id)
    manifest = erc.UNHARVESTED_OF[args.edition]
    raw = json.loads(manifest.read_text(encoding="utf-8"))
    block_records: list[dict] = []
    if args.blocks:
        raw["blocks"], block_records, refused = refute_blocks(
            raw["blocks"], cat.unharvested, load(args.records / f"{args.blocks}-refute.jsonl"),
            f"{into}/{args.blocks}-refute.jsonl")
        outcome.refusals += refused
    counts: dict[str, int] = {}
    for es in outcome.entries.values():
        for e in es.values():
            tag = e["disposition"] + (f" ({e['class']})" if e.get("class") else "")
            counts[tag] = counts.get(tag, 0) + 1
    print(f"{sum(len(es) for es in outcome.entries.values())} units disposed: "
          + ", ".join(f"{k} {v}" for k, v in sorted(counts.items())))
    print(f"blocks: {sum(r['outcome'] == text_units.UPHELD for r in block_records)} not-rules upheld, "
          f"{sum(r['outcome'] != text_units.UPHELD for r in block_records)} overturned (pending)")
    for why in outcome.refusals:
        print(f"  REFUSED {why}")
    if outcome.refusals:
        print(f"⛔ {len(outcome.refusals)} refusal(s): nothing written")
        return 1
    if not args.write:
        return 0
    pb = raw[erc.PARAGRAPH_BLOCKS]
    for clause, es in outcome.entries.items():
        pb[clause]["units"] = [es[(e["digest"], e.get("order", 0))] for e in pb[clause]["units"]]
    if args.blocks:
        raw[erc.NOT_RULES_REFUTED] = True
    manifest.write_text(json.dumps(raw, indent=1, ensure_ascii=False) + "\n", encoding="utf-8", newline="\n")
    for name, recs in outcome.records.items():
        _write_jsonl(REPO / into / f"{name}-refute.jsonl", recs)
    if args.blocks:
        _write_jsonl(REPO / into / f"{args.blocks}-refute.jsonl", block_records)
    print(f"wrote {manifest.relative_to(REPO)} and {len(outcome.records) + bool(args.blocks)} refuter file(s) in {into}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
