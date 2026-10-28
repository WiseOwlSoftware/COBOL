#!/usr/bin/env python3
"""test_fuzz_history.py — the self-test of scripts/fuzz_history.py and `fuzz_triage.py history` (kb/Work PB4006;
docs/rearchitecture/DESIGN-differential-fuzz.md §6, §7).

    python scripts/test_fuzz_history.py          # no build, no git, no network

It proves, on a FABRICATED run directory in the shapes scripts/differential_fuzz.py writes:
  STATS     a run's numbers come from run.json, report.md's headline and survivor line, and results.jsonl's classes; a
            report whose headline cannot be read is REFUSED, never recorded as zeros;
  TALLY     signatures and programs per verdict;
  NOTES     the night's notes are found by the run id in their `source` (defect and adjudication notes only); each note's
            kind is read from the register (compiler bug, test-harness bug, settled question, retired), a defect's from
            its declared sites (product code or not), a site-less one only from the closed legacy record, and a defect
            nothing classifies is REFUSED; a GnuCOBOL record counts only when the fuzzing produced it and it is not retired;
  UPSERT    a re-recorded night replaces its own record and the nights stay in order;
  PAGE      every night is rendered, the page says it is machine-generated, a status is shown as the register has it, only
            compiler bugs reach the totals, a finding from a run that is not a recorded night is counted and listed by
            its date, the facts it states come from the records, and a tally reaches the page;
  MARKDOWN  FUZZ-LEDGER.md carries the same content, links each note, names its chart files, and each chart file is a
            standalone SVG;
  SYNC      the page and FUZZ-LEDGER.md are written by one writer with one ledger version, which names the content and not
            the time; mark-published refuses a page that is not main's version and records the one it is; status names
            current, PUBLISH OWED and REGENERATE OWED;
  HISTORY   `fuzz_triage.py history` refuses an unfinished triage, a note id the register lacks and a defect nothing
            classifies, numbers a new night
            after the last, keeps a re-run's own number and its stored notes, and writes nothing without --write;
  VOCAB     the page's verdict list equals the triage tool's vocabulary, so no verdict reaches the page without a colour.
"""
from __future__ import annotations

import json
import pathlib
import sys
import tempfile
import types
import unittest.mock

HERE = pathlib.Path(__file__).resolve().parent
sys.path.insert(0, str(HERE))
import fuzz_history as fh  # noqa: E402
import fuzz_triage as ft  # noqa: E402
from standard_streams import ensure_utf8  # noqa: E402

FAILS: list[str] = []


def expect(what: str, ok: bool, got=None) -> None:
    print(("PASS  " if ok else "FAIL  ") + what + ("" if ok else f"  (got {got!r})"))
    if not ok:
        FAILS.append(what)


def fabricated_run(root: pathlib.Path, run_id: str, headline: bool = True) -> pathlib.Path:
    run = root / run_id
    run.mkdir()
    (run / "run.json").write_text(json.dumps({"model": "m", "seed": 7, "started": "2026-10-28T04:00:00+00:00",
                                              "hours": 8.0, "edition": "2014", "seeds": 211}), encoding="utf-8", newline="\n")
    head = ("Programs: 5 · judged: 5 · usable: 3 (60% of judged) · usable on the first attempt: 2 (40% of judged) · "
            "GnuCOBOL alone refused (THEY_REJECT leads): 1") if headline else "Programs: none"
    (run / "report.md").write_text(f"# Differential fuzz — triage report\n\n{head}\n\n## Survivors: 3 programs in 2 signatures\n",
                                   encoding="utf-8", newline="\n")
    rows = [{"verdict": v} for v in ("AGREE", "AGREE", "OUTPUT_DIFFERS", "WE_REJECT", "THEY_REJECT")]
    (run / "results.jsonl").write_text("\n".join(json.dumps(r) for r in rows) + "\n", encoding="utf-8", newline="\n")
    return run


RUN = "fuzz-20261028-200000"
OLD = "fuzz-20261020-000000"
SHORT = "fuzz-20261022-160216"
ITEMS = [
    {"id": "PB9001", "kind": "defect", "status": "open", "area": "binding", "title": "PB9001 — a bug", "source": f"{RUN} slice D1",
     "_body": "**Sites:** `src/WiseOwl.Cobol.Compiler/Binding/DataBinder.cs#Bind`"},
    {"id": "PB9002", "kind": "upstream", "status": "open", "title": "x", "source": f"the triage of {RUN}",
     "upstream_project": "GnuCOBOL", "filed": "2026-10-28"},
    {"id": "PB9003", "kind": "defect", "status": "landed", "area": "codegen", "title": "PB9003 — Defect: older", "source": OLD,
     "_body": "**Sites:** `src/WiseOwl.Cobol.Compiler/CodeGen/Verbs/MoveEmitter.cs`"},
    {"id": "PB9004", "kind": "defect", "status": "landed", "area": "tooling/fuzz", "title": "PB9004 — harness", "source": OLD,
     "_body": "**Sites:** none (a harness setting, no code file)"},
    {"id": "PB9005", "kind": "adjudication", "status": "landed", "area": "grammar", "title": "PB9005 — settled", "source": OLD},
    {"id": "PB9006", "kind": "defect", "status": "retired", "area": "arithmetic", "title": "PB9006 — RETIRED — x", "source": OLD},
    {"id": "PB9007", "kind": "upstream", "status": "open", "title": "y", "source": "the owner decision PB1",
     "upstream_project": "GnuCOBOL", "filed": "2026-10-27"},
    # a harness bug filed under tests/: its declared sites, never its area, decide (kb/Work PB4010 R2-1)
    {"id": "PB9008", "kind": "defect", "status": "landed", "area": "tests/conformance", "title": "PB9008 — harness",
     "source": OLD, "_body": "**Sites:** `scripts/differential_fuzz.py#classify`"},
    # a diagnostic defect no user program sees (process_only) is still a compiler bug when its sites are the product's
    {"id": "PB9009", "kind": "defect", "status": "landed", "area": "frontend/diagnostics", "process_only": True,
     "title": "PB9009 — message", "source": OLD, "_body": "**Sites:** `src/WiseOwl.Cobol.Frontend/Parsing/CobolErrorStrategy.cs`"},
    # a note from before sites were required: only the closed LEGACY_KINDS record classifies it (patched in below)
    {"id": "PB9010", "kind": "defect", "status": "landed", "area": "codegen", "title": "PB9010 — legacy", "source": OLD},
    # a compiler bug from a run the history holds no night for (kb/Work PB4010 R2-2)
    {"id": "PB9011", "kind": "defect", "status": "landed", "area": "codegen", "title": "PB9011 — short run",
     "source": f"the aborted run {SHORT}", "_body": "**Sites:** `src/WiseOwl.Cobol.Compiler/CodeGen/Verbs/ArithmeticEmitter.cs`"},
]
#: A site block naming only a member declares no path (review R1-1): nothing classifies it, so it is refused, never guessed.
MEMBER_ONLY = {"id": "PB9012", "kind": "defect", "status": "open", "area": "binding", "title": "PB9012 — members",
               "source": "filed by hand",
               "_body": "**Sites:** `DataBinder.Bind`"}
LEGACY = {"PB9010": "harness"}


def main() -> int:
    by = {i["id"]: i for i in ITEMS}
    with unittest.mock.patch.object(fh, "LEGACY_KINDS", LEGACY), tempfile.TemporaryDirectory() as tmp:
        root = pathlib.Path(tmp)
        run = fabricated_run(root, RUN)
        s = fh.run_stats(run)
        expect("STATS headline", (s["programs"], s["judged"], s["usable"], s["usable_first"], s["they_reject"]) == (5, 5, 3, 2, 1), s)
        expect("STATS survivors", (s["survivors"], s["survivor_signatures"]) == (3, 2), s)
        expect("STATS classes", s["classes"] == {"AGREE": 2, "OUTPUT_DIFFERS": 1, "WE_REJECT": 1, "THEY_REJECT": 1}, s["classes"])
        expect("STATS run meta", (s["run_id"], s["model"], s["seed"], s["sections"]) == (RUN, "m", 7, 211), s)
        bad = fabricated_run(root, "fuzz-20261028-210000", headline=False)
        try:
            fh.run_stats(bad)
            expect("STATS refuses an unreadable headline", False, "recorded")
        except SystemExit as e:
            expect("STATS refuses an unreadable headline", "programs" in str(e), str(e))

        verdicts = [{"verdict": "WISEOWL-CORRECT", "count": 4}, {"verdict": "WISEOWL-CORRECT", "count": 1},
                    {"verdict": "GNUCOBOL-SIDE", "count": 2}]
        tally = fh.verdict_tally(verdicts)
        expect("TALLY", tally == {"WISEOWL-CORRECT": {"signatures": 2, "programs": 5},
                                  "GNUCOBOL-SIDE": {"signatures": 1, "programs": 2}}, tally)

        expect("NOTES by run id, defect and adjudication only", fh.defects_of(ITEMS, RUN) == ["PB9001"], fh.defects_of(ITEMS, RUN))
        kinds = {d: fh.finding_kind(by[d]) for d in ("PB9001", "PB9004", "PB9005", "PB9006", "PB9008", "PB9009", "PB9010")}
        expect("NOTES kinds: declared sites decide, a site-less note only by the closed legacy record", kinds == {
            "PB9001": "compiler", "PB9004": "harness", "PB9005": "settled", "PB9006": "retired", "PB9008": "harness",
            "PB9009": "compiler", "PB9010": "harness"}, kinds)
        try:
            fh.finding_kind(MEMBER_ONLY)
            expect("NOTES refuses a defect nothing classifies (a block naming only members)", False, "classified")
        except SystemExit as e:
            expect("NOTES refuses a defect nothing classifies (a block naming only members)", "PB9012" in str(e), str(e))
        expect("NOTES a GnuCOBOL record counts only from the fuzzing", [i["id"] for i in ITEMS if fh.from_fuzzing(i)] == ["PB9002"])

        rec = fh.night_record(run, 2, verdicts, ["PB9001"], "abc1234")
        older = {**rec, "night": 1, "run_id": OLD, "defects": ["PB9003", "PB9004", "PB9005", "PB9006", "PB9008", "PB9009",
                                                                "PB9010"], "verdicts": None, "hours": 8.0}
        hist = fh.upsert(fh.upsert({"artifact": "https://example.invalid/a", "nights": []}, rec), older)
        expect("UPSERT orders nights", [n["night"] for n in hist["nights"]] == [1, 2], hist["nights"])
        again = fh.upsert(hist, {**rec, "judged": 99})
        expect("UPSERT replaces its own record", len(again["nights"]) == 2 and again["nights"][1]["judged"] == 99, again["nights"])

        ctx = fh.context(hist, ITEMS)
        page = fh.render_html(ctx)
        expect("PAGE renders every night", "Night 1" in page and "Night 2" in page)
        expect("PAGE states it is machine-generated", "machine-generated" in page and "fuzz_history.py" in page)
        expect("PAGE shows the register's status", '<span class="pill bad">open</span>' in page
               and '<span class="pill ok">landed</span>' in page and '<span class="pill muted">retired</span>' in page)
        tiles = dict((c, v) for v, c in ctx["tiles"])
        expect("PAGE totals count compiler bugs only, a run that is not a night included", (
            tiles["bugs found in WiseOwl"], tiles["of those bugs fixed"], tiles["bugs recorded in GnuCOBOL"]) == ("4", "3", "1"),
            tiles)
        expect("PAGE lists a run that is not a night by its date", "Run of 2026-10-22" in page and "PB9011" in page)
        expect("PAGE keeps the per-night chart to the nights", "Run of" not in fh.charts(ctx)["bugs"])
        expect("PAGE states facts from the records", "For 8 hours" in page and "about 211 sections" in page
               and "Night 1 was reviewed before" in page, page[page.find("<ol"):][:300])
        expect("PAGE shows the tally", "2 / 5" in page, page[page.find("Decisions"):][:400])

        md, files = fh.render_markdown(ctx, hist["artifact"])
        expect("MARKDOWN is marked machine-generated", md.startswith("<!-- MACHINE-GENERATED") and "machine-generated" in md)
        expect("MARKDOWN links each note", "[PB9001](kb/Work/PB9001.md)" in md and "[PB9006](kb/Work/PB9006.md)" in md)
        expect("MARKDOWN names its charts and the page", all(f"docs/fuzz-ledger/{n}" in md for n in files)
               and "https://example.invalid/a" in md, sorted(files))
        expect("MARKDOWN charts are standalone SVG", sorted(files) == ["bugs.svg", "programs.svg", "who.svg"] and all(
            t.startswith('<svg xmlns="http://www.w3.org/2000/svg"') and "<style>" in t and "prefers-color-scheme" in t
            for t in files.values()))
        written = fh.write_markdown(ctx, hist["artifact"], root=root)
        expect("MARKDOWN writes the ledger and its charts", (root / "FUZZ-LEDGER.md").read_text(encoding="utf-8") == md
               and len(written) == 4)

        # SYNC: the Artifact and FUZZ-LEDGER.md are one rendering with one ledger version (owner 2026-10-28, PB4010)
        art = hist["artifact"]
        version = fh.ledger_version(ctx, art)
        expect("SYNC equal content renders equal bytes (the tracked file carries no time)",
               fh.rendered_markdown(ctx, art) == fh.rendered_markdown(fh.context(hist, ITEMS), art)
               and version in fh.rendered_markdown(ctx, art))
        moved = fh.upsert(hist, {**rec, "judged": 6})
        expect("SYNC a change of content changes the version", fh.ledger_version(fh.context(moved, ITEMS), art) != version)
        sync = root / "sync"
        got, paths = fh.write_ledger(ctx, art, sync / "page.html", root=sync)
        ledger = (sync / "FUZZ-LEDGER.md").read_text(encoding="utf-8")
        expect("SYNC one writer stamps both renderings with one version", got == version and len(paths) == 5
               and fh.version_of((sync / "page.html").read_text(encoding="utf-8")) == version
               and fh.version_of(ledger) == version, (got, version, len(paths)))
        expect("SYNC the written FUZZ-LEDGER.md is the rendered one", ledger == fh.rendered_markdown(ctx, art))
        rc = fh.mark_published(sync / "page.html", art, "Ledger version 000000000000.", coord_override=str(root))
        expect("SYNC mark-published refuses a page that is not main's version", rc == 1
               and not (root / fh.PUBLISHED).exists())
        rc = fh.mark_published(sync / "page.html", art, ledger, coord_override=str(root))
        record = fh.published_record(str(root))
        expect("SYNC mark-published records main's version", rc == 0 and record.get("version") == version
               and record.get("url") == art, record)
        moved_md = fh.rendered_markdown(fh.context(moved, ITEMS), art)
        status = {"current": fh.sync_status(ledger, record, art, ledger),
                  "publish": fh.sync_status(ledger, {}, art, ledger),
                  "elsewhere": fh.sync_status(ledger, {**record, "url": "https://example.invalid/other"}, art, ledger),
                  "regenerate": fh.sync_status(ledger, record, art, moved_md),
                  "hand-edit": fh.sync_status(ledger.replace("Night 1", "Night one", 1), record, art, ledger),
                  "unversioned": fh.sync_status("no stamp", record, art, ledger),
                  "behind": fh.sync_status(ledger, record, art, None)}
        expect("SYNC status names each state", "current" in status["current"] and "PUBLISH OWED" in status["publish"]
               and "PUBLISH OWED" in status["elsewhere"] and "another Artifact" in status["elsewhere"]
               and "REGENERATE OWED" in status["regenerate"] and "REGENERATE OWED" in status["hand-edit"]
               and "REGENERATE OWED" in status["unversioned"] and "UNCHECKED" in status["behind"], status)

        # HISTORY: the subcommand, on a history file of its own
        hfile = root / "fuzz-history.json"
        fh.write_history({"artifact": None, "nights": [{**older, "night": 1}]}, hfile)
        t = types.SimpleNamespace(run=run, run_id=RUN, coord=str(root))
        done = types.SimpleNamespace(verdicts=[("A1", v) for v in verdicts], problems=[], undecided={})
        args = types.SimpleNamespace(defect=[], judge_commit="abc1234", out=str(root / "page.html"), write=False)
        fake_work = types.SimpleNamespace(load=lambda _d: [*ITEMS, MEMBER_ONLY], note_sites=fh.register().note_sites)
        with unittest.mock.patch.object(fh, "HISTORY", hfile), unittest.mock.patch.object(fh, "register", lambda: fake_work):
            rc = ft.history(t, types.SimpleNamespace(verdicts=[], problems=["x"], undecided={}), root, args)
            expect("HISTORY refuses an unfinished triage", rc == 1)
            rc = ft.history(t, done, root, types.SimpleNamespace(**{**vars(args), "defect": ["PB0000"]}))
            expect("HISTORY refuses a note the register lacks", rc == 1)
            rc = ft.history(t, done, root, types.SimpleNamespace(**{**vars(args), "defect": ["PB9012"]}))
            expect("HISTORY refuses a defect whose site block names no path, glob or `none`", rc == 1)
            rc = ft.history(t, done, root, types.SimpleNamespace(**{**vars(args), "defect": ["PB9002"]}))
            expect("HISTORY refuses a note that is no finding (an upstream record)", rc == 1)
            before = hfile.read_text(encoding="utf-8")
            with unittest.mock.patch.object(fh, "register", lambda: types.SimpleNamespace(
                    load=lambda _d: [*ITEMS, {**MEMBER_ONLY, "source": f"the aborted run {SHORT}"}],
                    note_sites=fake_work.note_sites)):
                rc = ft.history(t, done, root, types.SimpleNamespace(**{**vars(args), "write": True}))
            expect("HISTORY refuses an unclassifiable note from a run with no night, writing nothing", rc == 1
                   and hfile.read_text(encoding="utf-8") == before)
            rc = ft.history(t, done, root, args)
            expect("HISTORY writes nothing without --write", rc == 0 and len(fh.load_history(hfile)["nights"]) == 1)
            with unittest.mock.patch.object(fh, "write_markdown", lambda *_a, **_k: []):
                ft.history(t, done, root, types.SimpleNamespace(**{**vars(args), "write": True}))
                h1 = fh.load_history(hfile)
                expect("HISTORY numbers a new night after the last", [n["night"] for n in h1["nights"]] == [1, 2]
                       and h1["nights"][1]["defects"] == ["PB9001"], h1["nights"])
                stored = h1["nights"][1]
                fh.write_history({**h1, "nights": [h1["nights"][0], {**stored, "defects": ["PB9001", "PB9003"]}]}, hfile)
                ft.history(t, done, root, types.SimpleNamespace(**{**vars(args), "write": True, "judge_commit": None}))
                h2 = fh.load_history(hfile)
                expect("HISTORY keeps a re-run's number, its stored notes and its judge", len(h2["nights"]) == 2
                       and h2["nights"][1]["night"] == 2 and h2["nights"][1]["defects"] == ["PB9001", "PB9003"]
                       and h2["nights"][1]["judge"] == "abc1234", h2["nights"][1])
                expect("HISTORY writes the page", (root / "page.html").exists())

    vocab = {v for v, _c, _n in fh.VERDICTS}
    expect("VOCAB equals the triage tool's verdicts", vocab == set(ft.VERDICTS), sorted(vocab ^ set(ft.VERDICTS)))
    print(f"=== test_fuzz_history: {'GREEN' if not FAILS else 'RED'} ({len(FAILS)} failure(s)) ===")
    return 1 if FAILS else 0


if __name__ == "__main__":
    ensure_utf8()
    raise SystemExit(main())
