#!/usr/bin/env python3
"""Watch the axis nothing else watches: a traceability-inventory row LOSING a witness (kb/Work PB959).

    python scripts/spec/audit_witness_loss.py --check [--base REF]      # the gate: working tree vs merge-base
    python scripts/spec/audit_witness_loss.py --history [--since REV]   # every loss in the inventory's git history
    python scripts/spec/audit_witness_loss.py --history --restore-batch OUT.json   # ...and a batch owing them back
    python scripts/spec/audit_witness_loss.py --self-test               # prove the check FAILS on a planted loss

⛔ WHY THIS EXISTS. `code-location` and `test-ref` are the EVIDENCE a row's verdict stands on, and the inventory is
the instrument that defines "done" for this project. Until PB959, `record_verdicts.py` assigned both fields by
blind overwrite, so a batch that re-typed two of a row's five witnesses silently deleted the other three, and one
that omitted the field emptied it. `SpecTraceabilityInventoryDriftTests.EveryTestRef_ResolvesToARealTest` checks
that each SURVIVING reference RESOLVES — deliberately not how many there are — so a subtraction of perfectly valid
evidence left every gate green and every derived artifact (the burn-down, the owner's ledger) unchanged. A row
whose witnesses were removed still reads CONFORMS; it simply no longer has the evidence that justified it.

WHAT COUNTS AS A LOSS is defined ONCE, in `inventory_schema.witness_losses`, and the writer asks the same function
at record time — so this auditor and `record_verdicts.py` cannot come to disagree about it. Three losses are
EXCUSED, and each is printed: a RETIREMENT (the writer records `retired-witness: <ref> (<reason>)` in the row's
`notes` when a batch retires a witness by name), a RE-SITE (a record that changes the verdict restates the
row's `code-location` — see `inventory_schema.WITNESS_FIELDS` for why), and a RE-SPELLING (the test's file was
MOVED and the row cites it under its new name: git's rename record across the change says which file became
which, `respellings`, so a move needs no map of its own; counted, not listed). Every other loss is RED.

THE SAME AXIS FOR THE STATEMENTS (kb/Work PB3795, PB3375). A row's `notes` is the only record of WHY its verdict
stands, and a record that restated a verdict without it used to write the absent field as empty. `--check` is RED on
a statement (`notes` prose, `derivation`, `verdict`, `editions`) that went from non-empty to empty
(`inventory_schema.statement_losses`: one definition, asked by the writer too); `--history` lists every erasure and
`--restore-batch` emits the full records that give the erased `notes` back.

WHAT IS NOT A WITNESS LOSS: a ROW that disappears. Rows are regenerated from the rule catalog by
`build_inventory.py`, which owns a row's existence; such rows are counted and printed, never gated.

WHETHER A RESTORED REFERENCE STILL RESOLVES is NOT decided here. That is the C# battery gate's job (the division
of labour in `inventory_schema.py`); a `--restore-batch` is applied through `record_verdicts.py` and then gated
by `SpecTraceabilityInventoryDriftTests`, which names every restored reference that points at nothing.
"""
from __future__ import annotations

import argparse
import json
import pathlib
import subprocess
import sys
from collections import Counter
from typing import Any, NamedTuple

from inventory_schema import (INVENTORY_PATH, REPO, RETIRED_WITNESS_MARK, SCHEMA_PATH, Schema, StatementLoss,
                              WitnessLoss, load_inventory, load_schema, statement_losses, statement_text,
                              witness_losses, witness_shape_error)
sys.path.append(str(pathlib.Path(__file__).resolve().parents[1]))  # scripts/, for standard_streams
from standard_streams import ensure_utf8  # noqa: E402

INVENTORY_REL = INVENTORY_PATH.relative_to(REPO).as_posix()
SCHEMA_REL = SCHEMA_PATH.relative_to(REPO).as_posix()


def _git(*args: str) -> str:
    return subprocess.run(["git", *args], cwd=REPO, check=True, capture_output=True, text=True,
                          encoding="utf-8").stdout


def _sep(field: str, schema) -> str:
    return schema.code_location_sep if field == "code-location" else schema.test_ref_sep


def rows_at(rev: str) -> list[dict[str, Any]] | None:
    """The inventory as committed at `rev`, or None when the file does not exist there."""
    r = subprocess.run(["git", "show", f"{rev}:{INVENTORY_REL}"], cwd=REPO, capture_output=True)
    return json.loads(r.stdout.decode("utf-8")) if r.returncode == 0 else None


def schema_at(rev: str) -> Schema | None:
    """The inventory schema as committed at `rev` (its test-ref forms name the files of THAT tree), or None."""
    r = subprocess.run(["git", "show", f"{rev}:{SCHEMA_REL}"], cwd=REPO, capture_output=True)
    if r.returncode != 0:
        return None
    try:
        return Schema(json.loads(r.stdout.decode("utf-8")))
    except (KeyError, TypeError):
        # A schema from before a shape change (PB3172 made `editions` an object) is not one this class can read.
        # The callers fall back to the current schema, which only loses the re-spelling of a moved test file.
        return None


def renamed_from_name_status(out: str) -> dict[str, str]:
    """`git diff -M --name-status -z` output → {old path: new path} for every RENAMED file (a modified, deleted or
    copied one moves nothing)."""
    fields = out.split("\0")
    found: dict[str, str] = {}
    i = 0
    while i < len(fields) and fields[i]:
        status = fields[i]
        if status[:1] in ("R", "C"):
            if status[0] == "R":
                found[fields[i + 1]] = fields[i + 2]
            i += 3
        else:
            i += 2
    return found


def respelled_from_name_status(out: str, before: Schema, after: Schema) -> dict[str, str]:
    """`git diff -M --name-status -z` output → {before test-ref: after test-ref} for every RENAMED file that a
    file-backed form names on both sides, each side spelled by its own tree's schema (`Schema.ref_for_path`)."""
    return {was: now for old, new in renamed_from_name_status(out).items()
            if (was := before.ref_for_path(old)) and (now := after.ref_for_path(new))}


def moves(before_rev: str, after_rev: str | None) -> dict[str, str]:
    """{old path: new path} for every file git records as RENAMED from `before_rev` to `after_rev` (None: the working
    tree), anywhere in the repository: a code-location may name any file (kb/Work PB3837)."""
    return renamed_from_name_status(_git("-c", "diff.renameLimit=0", "diff", "-M", "--name-status", "-z", before_rev,
                                         *([after_rev] if after_rev else [])))


def respellings(before_rev: str, after_rev: str | None, before: Schema, after: Schema) -> dict[str, str]:
    """{before test-ref: after test-ref} for every witness file git records as RENAMED from `before_rev` to
    `after_rev` (None: the working tree, whose renamed files must be tracked). Git's rename record IS the identity
    of a moved file, so a move (the suite's S2 move, a test re-filed under another clause folder) needs no
    hand-kept map, and the history walk reads the same record of every commit long after the move landed."""
    roots = sorted(set(before.ref_roots) | set(after.ref_roots))
    out = _git("-c", "diff.renameLimit=0", "diff", "-M", "--name-status", "-z", before_rev,
               *([after_rev] if after_rev else []), "--", *roots)
    return respelled_from_name_status(out, before, after)


def compare(before: list[dict[str, Any]], after: list[dict[str, Any]], schema,
            respelled: dict[str, str] | None = None,
            moved: dict[str, str] | None = None) -> tuple[list[WitnessLoss], list[str]]:
    """(every witness lost by a row present on both sides, the rule-ids of witnessed rows that vanished)."""
    by_after = {r["rule-id"]: r for r in after}
    losses: list[WitnessLoss] = []
    vanished: list[str] = []
    for row in before:
        now = by_after.get(row["rule-id"])
        if now is None:
            if row.get("code-location") or row.get("test-ref"):
                vanished.append(row["rule-id"])
            continue
        losses += witness_losses(row, now, schema, respelled, moved)
    return losses, vanished


def compare_revisions(before: list[dict[str, Any]], after: list[dict[str, Any]], before_rev: str,
                      after_rev: str | None, schema) -> tuple[list[WitnessLoss], list[str]]:
    """`compare`, with the re-spellings of the change from `before_rev` to `after_rev` (None: the working tree).
    Git's rename record is read only when a file-backed test-ref was lost, so a change that moved nothing pays
    nothing for it."""
    losses, vanished = compare(before, after, schema)
    before_schema = schema_at(before_rev) or schema
    refs = any(not l.excused and l.field == "test-ref" and before_schema.ref_path(l.ref) for l in losses)
    locations = any(not l.excused and l.field == "code-location" for l in losses)
    if refs or locations:
        after_schema = (schema_at(after_rev) or schema) if after_rev else schema
        losses, vanished = compare(before, after, schema,
                                   respellings(before_rev, after_rev, before_schema, after_schema) if refs else None,
                                   moves(before_rev, after_rev) if locations else None)
    return losses, vanished


def compare_statements(before: list[dict[str, Any]], after: list[dict[str, Any]], schema) -> list[StatementLoss]:
    """Every statement (`notes` prose, `derivation`, `verdict`, `editions`) a row present on both sides said
    something in and says nothing in now (`inventory_schema.statement_losses`, kb/Work PB3795). A hand-written
    edit, a merge and a record from before the writer refused it are all caught here; there is no excuse to write."""
    by_after = {r["rule-id"]: r for r in after}
    return [s for row in before if (now := by_after.get(row["rule-id"])) is not None
            for s in statement_losses(row, now, schema)]


class Erasure(NamedTuple):
    """One statement a commit erased: where, what, the text it held, and the verdict that text ARGUED."""
    label: str
    loss: StatementLoss
    text: str
    verdict: str   # the row's verdict BEFORE the erasing commit: the verdict the erased prose was written for


def default_base() -> str:
    for ref in ("origin/main", "main"):
        try:
            return _git("merge-base", "HEAD", ref).strip()
        except subprocess.CalledProcessError:
            continue
    raise SystemExit("⛔ no base: neither origin/main nor main is a ref here — pass --base REF")


def check(base: str, schema) -> int:
    before = rows_at(base)
    if before is None:
        print(f"⛔ {INVENTORY_REL} does not exist at {base} — nothing to compare against")
        return 2
    after = load_inventory()
    losses, vanished = compare_revisions(before, after, base, None, schema)
    erased = compare_statements(before, after, schema)
    unexcused = [l for l in losses if not l.excused]
    retired = [l for l in losses if l.retired]
    resited = [l for l in losses if l.resited and not l.retired]
    respelled = [l for l in losses if l.respelled and not (l.retired or l.resited)]
    for l in retired:
        print(f"  ⓘ RETIRED  {l.rule_id} {l.field}: {l.ref}")
    for l in resited:
        print(f"  ⓘ RE-SITED {l.rule_id} {l.field}: {l.ref}  (the verdict changed — the record restates it)")
    for l in unexcused:
        print(f"  ⛔ LOST     {l.rule_id} {l.field}: {l.ref}")
    for s in erased:
        print(f"  ⛔ ERASED   {s.rule_id} {s.field}: {s.chars} characters of statement now empty")
    if respelled:
        print(f"  ⓘ RE-SPELLED {len(respelled)} witness(es) on {len({l.rule_id for l in respelled})} row(s): the "
              "same files, moved (git's rename record), cited under their new names")
    if vanished:
        print(f"  ⓘ {len(vanished)} witnessed row(s) no longer in the inventory (catalog-owned, not gated): "
              + ", ".join(vanished[:10]) + (" …" if len(vanished) > 10 else ""))
    print(f"=== WITNESS LOSS vs {base[:12]}: {len(unexcused)} unexcused, {len(retired)} retired, "
          f"{len(resited)} re-sited, {len(respelled)} re-spelled, {len(erased)} statements erased — "
          f"{'RED' if unexcused or erased else 'GREEN'} ===")
    return 1 if unexcused or erased else 0


def history(since: str | None, schema) -> tuple[list[tuple[str, str, WitnessLoss]], Counter[str], list[Erasure]]:
    """Walk every commit that touched the inventory (oldest first) and diff it against its first parent.

    Each witness loss comes back as (commit label, the row's verdict AFTER that commit, the loss): the verdict is
    what decides whether a code-location loss is still owed back (`owed_back`). Each erased statement comes back as
    an `Erasure` (commit label, the loss, the text it erased, the verdict that text argued), which is what
    `owed_statements` restores (kb/Work PB3795).
    """
    rng = [f"{since}..HEAD"] if since else []
    shas = _git("log", "--reverse", "--first-parent", "--format=%H %ad %s", "--date=short", *rng, "--",
                INVENTORY_REL).splitlines()
    found: list[tuple[str, str, WitnessLoss]] = []
    vanished: Counter[str] = Counter()
    erased: list[Erasure] = []
    for line in shas:
        sha, date, *subject = line.split(" ", 2)
        after = rows_at(sha)
        before = rows_at(f"{sha}^")
        if after is None or before is None:
            continue
        losses, gone = compare_revisions(before, after, f"{sha}^", sha, schema)
        verdict_after = {r["rule-id"]: r.get("verdict", "") for r in after}
        label = f"{sha[:9]} {date} {(subject[0] if subject else '')[:70]}"
        found += [(label, verdict_after[l.rule_id], l) for l in losses]
        vanished[label] += len(gone)
        was = {r["rule-id"]: r for r in before}
        erased += [Erasure(label, s, statement_text(was[s.rule_id], s.field), was[s.rule_id].get("verdict", ""))
                   for s in compare_statements(before, after, schema)]
    return found, vanished, erased


def owed_back(found: list[tuple[str, str, WitnessLoss]], current: dict[str, dict[str, Any]],
              schema) -> dict[str, dict[str, set[str]]]:
    """The witnesses history removed that PB959's write rule would have KEPT, and that the row still lacks.

    That is the history REPLAYED under the current rule, never a blanket undo:
      · a retirement or a re-site was excused when it happened and stays excused;
      · a silently lost TEST-REF is owed back — a test that exercises the rule observes it whatever the verdict;
      · a silently lost CODE-LOCATION is owed back only while the row still carries the verdict it was lost
        under: a later re-adjudication would have re-sited it anyway;
      · a removal the SCHEMA compelled is not owed: a reference that breaks a shape rule
        (`witness_shape_error`), and a test-ref on a row now closed on a §1.1 derivation, which may carry none.
    Whether an owed reference still RESOLVES is the battery gate's question, not this one's.
    """
    owed: dict[str, dict[str, set[str]]] = {}
    for _, verdict_then, l in found:
        row = current.get(l.rule_id)
        if row is None or l.excused or witness_shape_error(l.field, l.ref, schema):
            continue   # a witness removed because it broke a SHAPE rule was a correction, not a loss
        if l.field == "code-location" and row.get("verdict", "") != verdict_then:
            continue
        if l.field == "test-ref" and (row.get("derivation") or "").strip():
            continue   # a row closed on a §1.1 DERIVATION may carry no spec-derived test (refusal 1): compelled
        if l.ref not in schema.split(row.get(l.field, "") or "", _sep(l.field, schema)):
            owed.setdefault(l.rule_id, {}).setdefault(l.field, set()).add(l.ref)
    return owed


def owed_statements(erased: list[Erasure], current: dict[str, dict[str, Any]]
                    ) -> tuple[dict[str, dict[str, str]], dict[str, str]]:
    """The statements history erased (`statement_losses`) that the row STILL says nothing in, with the text they
    held when they went (the latest erasure wins: it is the nearest to the present) — and, apart, the rule-ids
    whose erased prose is NOT owed back because it argued a verdict the row no longer carries.

    A row that vanished, or that has said something in the field since, owes nothing. ⛔ A restore puts the text
    back under the row's CURRENT verdict, so it is only sound while that is the verdict the text argued
    (`Erasure.verdict`): a PARTIAL row's defect prose put back under the CONFORMS that later closed it is exactly
    the state `record_verdicts.validate` refuses a verdict change to leave behind. Such a row is STRANDED: the
    prose of its present verdict has to be written by a person, and `run_history` says so (kb/Work PB3795).
    """
    owed: dict[str, dict[str, str]] = {}
    stranded: dict[str, str] = {}
    for e in erased:
        row = current.get(e.loss.rule_id)
        if row is None or statement_text(row, e.loss.field):
            continue
        if e.loss.field == "notes" and row.get("verdict", "") != e.verdict:
            stranded[e.loss.rule_id] = e.verdict
            owed.get(e.loss.rule_id, {}).pop("notes", None)
            continue
        if e.loss.field == "notes":
            stranded.pop(e.loss.rule_id, None)
        owed.setdefault(e.loss.rule_id, {})[e.loss.field] = e.text
    return owed, stranded


def restore_records(owed: dict[str, dict[str, set[str]]], owed_text: dict[str, dict[str, str]],
                    current: dict[str, dict[str, Any]], schema) -> list[dict[str, str]]:
    """The `record_verdicts` records that give history's losses back, one per row.

    A row owed only witnesses gets a WITNESS-ONLY record (`record_verdicts.is_witness_only`): the owed witnesses and
    nothing else, which the writer MERGES into what the row carries and which restate no verdict, editions or notes
    — so the batch is safe to re-apply on a merged tree, in any order relative to another train's re-adjudication.
    A row owed a STATEMENT gets a record that names it, so it also restates the row's verdict and editions (a
    record naming a statement is a full record); the verdict it restates is the row's own, so nothing is
    re-adjudicated. The restored `notes` keep the retired-witness marks the row carries now, which the writer
    only ever appends after the prose.
    """
    records = []
    for rid in sorted(set(owed) | set(owed_text)):
        rec: dict[str, str] = {"rule-id": rid}
        if rid in owed_text:
            row = current[rid]
            rec |= {"verdict": row.get("verdict", ""), "editions": row.get("editions", "")}
            for field, text in owed_text[rid].items():
                rec[field] = f"{text} {row.get(field, '')}".strip() if field == "notes" else text
        for field, refs in owed.get(rid, {}).items():
            rec[field] = _sep(field, schema).join(sorted(refs))
        records.append(rec)
    return records


def run_history(args, schema) -> int:
    found, vanished, erased = history(args.since, schema)
    current = {r["rule-id"]: r for r in load_inventory()}
    per_commit: Counter[str] = Counter(label for label, _, _ in found)
    owed = owed_back(found, current, schema)
    kinds = Counter(("retired" if l.retired else "re-sited" if l.resited else "re-spelled" if l.respelled
                     else "lost", l.field) for *_, l in found)
    print(f"commits that removed a witness : {len(per_commit)}")
    for label, n in per_commit.most_common():
        print(f"   {n:5d}  {label}")
    print(f"witness removals in history    : {len(found)}")
    for (kind, field), n in sorted(kinds.items()):
        print(f"   {n:5d}  {kind:<9s} {field}")
    by_field = {f: sum(len(o.get(f, ())) for o in owed.values()) for f in ("code-location", "test-ref")}
    print(f"rows OWED a witness back       : {len(owed)} ({sum(by_field.values())} reference(s): "
          + ", ".join(f"{f} {n}" for f, n in by_field.items()) + ")")
    owed_text, stranded = owed_statements(erased, current)
    print(f"statement erasures in history  : {len(erased)} on {len({e.loss.rule_id for e in erased})} row(s)")
    for label, n in Counter(e.label for e in erased).most_common():
        print(f"   {n:5d}  {label}")
    if stranded:
        print(f"rows whose erased notes argued ANOTHER verdict (not restorable, write the prose by hand): {len(stranded)}")
        for rid, was in sorted(stranded.items()):
            print(f"   {rid}: the erased prose argued {was or '(no verdict)'}, the row is {current[rid].get('verdict', '')}")
    print(f"rows OWED a statement back     : {len(owed_text)} ("
          + ", ".join(f"{f} {sum(f in o for o in owed_text.values())}" for f in ("notes", "derivation")) + ")")
    if sum(vanished.values()):
        print(f"witnessed rows that vanished   : {sum(vanished.values())} (catalog-owned, not counted above)")
    if args.restore_batch:
        records = restore_records(owed, owed_text, current, schema)
        pathlib.Path(args.restore_batch).write_text(
            json.dumps({"batch": "restore-owed-witnesses-and-statements", "records": records}, indent=1,
                       ensure_ascii=False) + "\n", encoding="utf-8", newline="\n")
        print(f"wrote {len(records)} restore record(s) → {args.restore_batch}")
    return 0


def self_test(schema) -> int:
    """Prove each arm FIRES on a planted defect, and stays silent on the shapes that are not one."""
    rows = load_inventory()
    row = next(r for r in rows if len(schema.split(r.get("test-ref", ""), schema.test_ref_sep)) >= 3
               and len(schema.split(r.get("code-location", ""), schema.code_location_sep)) >= 2
               and r.get("verdict"))
    refs = schema.split(row["test-ref"], schema.test_ref_sep)
    locs = schema.split(row["code-location"], schema.code_location_sep)
    fails = 0

    def verify(name: str, ok: bool, detail: str = "") -> None:
        nonlocal fails
        fails += not ok
        print(f"  {'PASS' if ok else 'FAIL'}  {name}{detail}")

    def expect(name: str, got: list[WitnessLoss], unexcused: int, excused: int) -> None:
        u, e = sum(not l.excused for l in got), sum(l.excused for l in got)
        verify(name, (u, e) == (unexcused, excused),
               f": {u} unexcused, {e} excused (want {unexcused}, {excused})")

    narrowed = {**row, "test-ref": refs[-1], "code-location": locs[0]}
    expect("planted overwrite (PB959's shape)", witness_losses(row, narrowed, schema),
           len(refs) - 1 + len(locs) - 1, 0)
    expect("field omitted → emptied", witness_losses(row, {**row, "test-ref": ""}, schema), len(refs), 0)
    retiring = {**row, "test-ref": schema.test_ref_sep.join(refs[1:]),
                "notes": (row.get("notes", "") + f" retired-witness: {refs[0]} (self-test)").strip()}
    expect("explicit retirement is excused", witness_losses(row, retiring, schema), 0, 1)
    resite = {**narrowed, "verdict": "PARTIAL" if row["verdict"] != "PARTIAL" else "CONFORMS"}
    expect("a re-adjudication re-sites code-location, never test-ref", witness_losses(row, resite, schema),
           len(refs) - 1, len(locs) - 1)
    widened = {**row, "test-ref": row["test-ref"] + schema.test_ref_sep + "conformance:cases/0-SELF-TEST/self_test_extra"}
    expect("a widening is not a loss", witness_losses(row, widened, schema), 0, 0)
    reordered = {**row, "test-ref": schema.test_ref_sep.join(reversed(refs))}
    expect("a reordering is not a loss", witness_losses(row, reordered, schema), 0, 0)
    losses, vanished = compare([row], [], schema)
    verify("a vanished row is reported apart, never as a witness loss",
           not losses and vanished == [row["rule-id"]])

    # A RE-SPELLING (kb/Work PB3177): the same test file, moved, cited under its new name.
    moved_to = "conformance:cases/0-SELF-TEST/self_test_moved"
    moved = {**row, "test-ref": schema.test_ref_sep.join([moved_to, *refs[1:]])}
    expect("a re-spelled witness (its file moved, the row cites the new name) is excused",
           witness_losses(row, moved, schema, {refs[0]: moved_to}), 0, 1)
    expect("a re-spelling the row does not cite is still a loss",
           witness_losses(row, {**row, "test-ref": schema.test_ref_sep.join(refs[1:])}, schema,
                          {refs[0]: moved_to}), 1, 0)
    expect("a re-spelling never excuses a code-location",
           witness_losses(row, {**row, "code-location": schema.code_location_sep.join(locs[1:])}, schema,
                          {locs[0]: locs[1]}), 1, 0)
    # A code-location whose FILE moved (kb/Work PB3837: the product rename moved every source file), cited at the new
    # path with its member kept, is the same code; a moved file the row does not cite at its new path is still a loss.
    path0, hash0, member0 = locs[0].partition("#")
    relocated_to = f"src/0-SELF-TEST/{path0.rpartition('/')[2]}"
    relocated_row = {**row, "code-location": schema.code_location_sep.join([relocated_to + hash0 + member0, *locs[1:]])}
    expect("a code-location whose file moved, cited at its new path with its member, is excused",
           witness_losses(row, relocated_row, schema, None, {path0: relocated_to}), 0, 1)
    expect("a moved file the row does not cite at its new path is still a loss",
           witness_losses(row, {**row, "code-location": schema.code_location_sep.join(locs[1:])}, schema, None,
                          {path0: relocated_to}), 1, 0)
    verify("git's rename record maps each RENAMED path to its new one (a modified, deleted or copied file moves nothing)",
           renamed_from_name_status("\0".join(["R098", "src/A/x.cs", "src/B/x.cs", "M", "src/C.cs", "D", "src/D.cs",
                                               "C075", "src/E.cs", "src/F.cs", ""])) == {"src/A/x.cs": "src/B/x.cs"})
    owed = owed_back([("t", row["verdict"], l) for l in witness_losses(row, moved, schema, {refs[0]: moved_to})],
                     {row["rule-id"]: moved}, schema).get(row["rule-id"], {})
    verify("history replay owes back no re-spelled witness", not owed, f": owed {owed}")
    raw = json.loads(SCHEMA_PATH.read_text(encoding="utf-8"))
    raw["test-ref"]["forms"]["conformance"]["path-template"] = "tests/conformance/{0}.cob"   # the tree before S2
    old = Schema(raw)
    z = "\0".join(["R087", "tests/conformance/85/foo.cob", "conformance-suite/cases/7.2.3-COPY/foo_85.cob",
                   "R100", "tests/conformance/85/lib.cpy", "conformance-suite/cases/7.2.3-COPY/lib.cpy",
                   "M", "tests/nist/programs/NC101A.cob", "D", "tests/conformance/85/gone.cob",
                   "C075", "tests/nist/programs/NC102A.cob", "tests/nist/programs/NC102B.cob", ""])
    got = respelled_from_name_status(z, old, schema)
    verify("git's rename record re-spells each renamed witness file, each side by its own tree's schema (a "
           "modified, deleted or copied file, or one no form names, re-spells nothing)",
           got == {"conformance:85/foo": "conformance:cases/7.2.3-COPY/foo_85"}, f": {got}")
    verify("a file-backed form's ref and its path are inverses; an xUnit form names no file",
           schema.ref_for_path(schema.ref_path(moved_to) or "") == moved_to
           and schema.ref_path("unit:SomeTests.SomeMethod") is None
           and "conformance-suite" in schema.ref_roots, f": {schema.ref_roots}")

    rid = row["rule-id"]
    owed = owed_back([("t", row["verdict"], l) for l in witness_losses(row, narrowed, schema)],
                     {rid: narrowed}, schema).get(rid, {})
    verify("history replay owes back every silently lost witness",
           owed.get("test-ref") == set(refs[:-1]) and owed.get("code-location") == set(locs[1:]))
    owed = owed_back([("t", resite["verdict"], l) for l in witness_losses(row, resite, schema)],
                     {rid: resite}, schema).get(rid, {})
    verify("history replay leaves a re-site re-sited", owed.get("test-ref") == set(refs[:-1])
           and "code-location" not in owed)
    later = {**narrowed, "verdict": resite["verdict"]}
    owed = owed_back([("t", row["verdict"], l) for l in witness_losses(row, narrowed, schema)],
                     {rid: later}, schema).get(rid, {})
    verify("a code-location lost under a verdict since re-adjudicated is not owed",
           "code-location" not in owed and owed.get("test-ref") == set(refs[:-1]))

    # THE WRITER, asked the same questions: `record_verdicts.merged` is the write rule, so each planted batch
    # shape must come out of it with exactly the losses the auditor above would excuse — and no others.
    from record_verdicts import merged
    stmt = {k: row[k] for k in ("rule-id", "verdict", "editions", "notes") if row.get(k)}

    def written(rec: dict[str, str]) -> dict[str, Any]:
        return {**row, **merged(row, rec, schema)}

    expect("writer: a batch re-typing ONE witness per field (PB959's batch) loses nothing",
           witness_losses(row, written({**stmt, "test-ref": refs[-1], "code-location": locs[0]}), schema), 0, 0)
    expect("writer: a batch OMITTING both witness fields loses nothing",
           witness_losses(row, written(stmt), schema), 0, 0)
    got = written({**stmt, "retire-witnesses": refs[0], "retire-reason": "self-test"})
    expect("writer: a named retirement is the only loss, and it is marked", witness_losses(row, got, schema), 0, 1)
    new_verdict = "PARTIAL" if row["verdict"] != "PARTIAL" else "CONFORMS"
    got = written({**stmt, "verdict": new_verdict, "code-location": locs[0]})
    expect("writer: a re-adjudication naming a site re-sites code-location only",
           witness_losses(row, got, schema), 0, len(locs) - 1)
    got = written({**stmt, "verdict": new_verdict})
    expect("writer: a re-adjudication naming NO site keeps the row's", witness_losses(row, got, schema), 0, 0)
    got = written({"rule-id": row["rule-id"], "test-ref": "conformance:cases/0-SELF-TEST/self_test_extra",
                   "code-location": locs[0]})
    verify("writer: a witness-only record adds its witness and restates nothing",
           not witness_losses(row, got, schema) and "conformance:cases/0-SELF-TEST/self_test_extra" in got["test-ref"]
           and all(got.get(k) == row.get(k) for k in ("verdict", "editions", "notes", "derivation")))

    # A RETIREMENT OBLIGES THE PROSE (kb/Work PB762): the writer's shape rule, asked of planted records.
    from record_verdicts import validate
    # The row is PLANTED, never borrowed from the live inventory: the rule needs a retired witness that carries a
    # symbol, a second witness naming a DIFFERENT symbol, and prose that narrates the first. A live row's first
    # witness may be a bare path (28 of 1313 qualifying rows are) or be re-sited, and either turns this arm RED
    # with no defect.
    symbol = "PlantedSymbol"
    planted_locs = ["src/Planted.cs#PlantedSymbol", "src/Other.cs#OtherSymbol"]
    planted = {**row, "code-location": schema.code_location_sep.join(planted_locs)}
    narrating = {**planted, "notes": f"The {symbol} method does the work today."}
    untouched = {**planted, "notes": "The work is done elsewhere."}
    planted_stmt = {k: planted[k] for k in ("rule-id", "verdict", "editions") if planted.get(k)}
    catalog = {rid: row.get("kind", "")}
    stale_msg = "leaves the row's notes naming"

    def stale(rec: dict[str, str], on: dict[str, Any]) -> bool:
        errs = validate([(pathlib.Path("self-test"), 0, {"rule-id": rid, **rec})], schema, catalog, {rid: on})
        return any(stale_msg in e for e in errs)

    retiring_loc = {"retire-witnesses": planted_locs[0], "retire-reason": "self-test"}
    verify("writer: a witness-only retirement of a symbol the prose names is refused",
           stale(retiring_loc, narrating))
    verify("writer: a full record restating that prose is refused too",
           stale({**planted_stmt, "notes": narrating["notes"], **retiring_loc}, narrating))
    verify("writer: an ERRATUM appended beside the stale sentence is refused (PB338's shape: the prose still names it)",
           stale({**planted_stmt, "notes": narrating["notes"] + " ERRATUM: renamed.", **retiring_loc}, narrating))
    verify("writer: prose rewritten so it no longer names the symbol is accepted",
           not stale({**planted_stmt, "notes": untouched["notes"] + " It is done elsewhere now.", **retiring_loc}, narrating))
    verify("writer: a symbol that MOVED (the record names it at another file) keeps its true prose",
           not stale({**planted_stmt, "notes": narrating["notes"], "code-location": f"src/Moved.cs#{symbol}",
                      **retiring_loc}, narrating))
    verify("writer: a retirement of a symbol the prose does not name needs no new prose",
           not stale(retiring_loc, untouched))
    verify("writer: a record retiring nothing is never asked about the prose", not stale({**planted_stmt}, narrating))

    # A STATEMENT IS NEVER ERASED (kb/Work PB3795, PB3375): `inventory_schema.statement_losses`, asked of planted rows,
    # of the writer's output and of a history restore. The row is PLANTED: a live row may carry no prose at all.
    prose = "The planted derivation, its cite.py --check and its goldens."
    said = {**row, "notes": prose, "derivation": "", "verdict": "CONFORMS"}
    said_stmt = {k: said[k] for k in ("rule-id", "verdict", "editions") if said.get(k)}
    mark = f"{RETIRED_WITNESS_MARK} src/Gone.cs#Gone (self-test)"

    def erased(after: dict[str, Any], before: dict[str, Any] = said) -> list[str]:
        return [l.field for l in statement_losses(before, after, schema)]

    verify("statements: notes emptied is a loss", erased({**said, "notes": ""}) == ["notes"])
    verify("statements: notes left as only the retirement marks is a loss (the PROSE went)",
           erased({**said, "notes": mark}) == ["notes"])
    verify("statements: notes rewritten, extended or given a mark is not a loss",
           not erased({**said, "notes": "Rewritten."}) and not erased({**said, "notes": f"{prose} {mark}"}))
    verify("statements: a verdict or editions emptied is a loss",
           erased({**said, "verdict": ""}) == ["verdict"] and erased({**said, "editions": ""}) == ["editions"])
    deriving = {**said, "notes": "", "derivation": "owner-signed", "test-ref": ""}
    spec_ref = next((r for r in refs if schema.is_spec_derived(r)), None)
    verify("statements: a derivation emptied is a loss, unless the row no longer stands on it",
           erased({**deriving, "derivation": ""}, deriving) == ["derivation"]
           and bool(spec_ref) and not erased({**deriving, "derivation": "", "test-ref": spec_ref}, deriving))
    verify("statements: ... and the excuse is a SPEC-DERIVED test, not any test-ref (a differential one is a net)",
           schema.derivation is None or erased({**deriving, "derivation": "", "test-ref": "nist:planted/golden"},
                                               deriving) == ["derivation"])
    verify("statements: ... or a verdict that stops resolving (a derivation never stands for the verdict)",
           not erased({**deriving, "derivation": "", "verdict": next(iter(schema.defective))}, deriving)
           and erased({**deriving, "derivation": "", "verdict": "CONFORMS"}, deriving) == ["derivation"])
    verify("statements: a row that had nothing to say loses nothing", not erased({**said, "notes": ""}, {**said, "notes": ""}))
    kept = {**said, **merged(said, {**said_stmt, "test-ref": refs[0]}, schema)}
    verify("writer: a full record that OMITS notes and derivation keeps the row's (PB3795)",
           kept["notes"] == prose and not erased(kept))
    verify("writer: a full record that NAMES notes restates them",
           merged(said, {**said_stmt, "notes": "Restated."}, schema)["notes"] == "Restated.")

    def refused(rec: dict[str, str], on: dict[str, Any]) -> list[str]:
        return validate([(pathlib.Path("self-test"), 0, {"rule-id": rid, **rec})], schema, catalog, {rid: on})

    verify("writer: writing notes EMPTY over prose is refused, naming the field",
           any("'notes' would go from" in e for e in refused({**said_stmt, "notes": ""}, said)))
    verify("writer: omitting notes is accepted, and so is a witness-only record",
           not any("notes" in e for e in refused({**said_stmt}, said))
           and not any("notes" in e for e in refused({"test-ref": refs[0]}, said)))
    other = "PARTIAL" if said["verdict"] != "PARTIAL" else "CONFORMS"
    verify("writer: a record changing the verdict must restate notes, whose prose argues the old one",
           any("keeps the row's notes" in e for e in refused({**said_stmt, "verdict": other}, said))
           and not any("keeps the row's notes" in e for e in refused({**said_stmt, "verdict": other, "notes": "New."}, said)))
    drow = next((r for r in rows if (r.get("derivation") or "").strip() and r.get("verdict") in schema.resolving), None)

    def defect_errors(extra: dict[str, str]) -> list[str]:
        """What the writer says about a derivation-closed row re-adjudicated to a defect verdict."""
        rec = {"rule-id": drow["rule-id"], "verdict": sorted(schema.defective)[0], "editions": drow["editions"],
               "notes": "Re-adjudicated.", **extra}
        errs = validate([(pathlib.Path("self-test"), 0, rec)], schema, {drow["rule-id"]: drow.get("kind", "")},
                        {drow["rule-id"]: drow})
        return [e for e in errs if "derivation" in e or "does-not-resolve" in e]

    verify("writer: a derivation-closed row re-adjudicated to a defect that OMITS derivation is refused, with the way out",
           drow is not None and any("[verdict-does-not-resolve]" in e and "write it empty" in e
                                    for e in defect_errors({})))
    verify("writer: ... and writing derivation empty withdraws it, accepted (R1-1)",
           drow is not None and not defect_errors({"derivation": ""}))
    with_mark = {**said, "notes": mark, "derivation": ""}
    prose_now = {**said, "notes": ""}
    def erasures(before: dict[str, Any], after: dict[str, Any]) -> list[Erasure]:
        return [Erasure("t", s, prose, before.get("verdict", "")) for s in statement_losses(before, after, schema)]

    got = restore_records({}, owed_statements(erasures(said, prose_now), {rid: with_mark})[0], {rid: with_mark}, schema)
    verify("history restore: owed notes come back BEFORE the marks the row carries, as a full record",
           got == [{"rule-id": rid, "verdict": said["verdict"], "editions": said["editions"], "notes": f"{prose} {mark}"}],
           f": {got}")
    from normalize_batch import normalize
    got = normalize({"records": [{**said_stmt}]}, "self-test", [], schema)["records"][0]
    verify("normalize_batch: a record that names no notes still names none (an empty one would erase the row's)",
           "notes" not in got, f": {got}")
    verify("history restore: a row that says something again owes nothing",
           not any(owed_statements(erasures(said, prose_now), {rid: said})))
    partial = {**said, "verdict": other}
    owed_p, stranded_p = owed_statements(erasures(partial, prose_now), {rid: {**prose_now}})
    verify("history restore: prose that argued ANOTHER verdict is stranded, never put back under the row's (R1-2)",
           not owed_p and stranded_p == {rid: other}, f": {owed_p} {stranded_p}")
    print(f"=== SELF-TEST: {'GREEN' if not fails else f'RED ({fails})'} ===")
    return 1 if fails else 0


def main() -> int:
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    mode = ap.add_mutually_exclusive_group(required=True)
    mode.add_argument("--check", action="store_true",
                      help="gate the working tree against --base (default: the merge-base with main)")
    mode.add_argument("--history", action="store_true", help="report every witness removal in git history")
    mode.add_argument("--self-test", action="store_true", help="prove the check fires on a planted loss")
    ap.add_argument("--base", help="the revision --check compares against")
    ap.add_argument("--since", help="--history: only commits after this revision")
    ap.add_argument("--restore-batch", help="--history: write a record_verdicts batch of every witness and statement owed back")
    args = ap.parse_args()
    schema = load_schema()
    if args.self_test:
        return self_test(schema)
    if args.history:
        return run_history(args, schema)
    return check(args.base or default_base(), schema)


if __name__ == "__main__":
    ensure_utf8()
    sys.exit(main())
