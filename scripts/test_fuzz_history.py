#!/usr/bin/env python3
"""test_fuzz_history.py — the self-test of scripts/fuzz_history.py (kb/Work PB4006; DESIGN-differential-fuzz.md §6, §7).

    python scripts/test_fuzz_history.py          # no build, no git, no network

It proves, on a FABRICATED run directory in the shapes scripts/differential_fuzz.py writes:
  STATS     a run's numbers come from run.json, report.md's headline and survivor line, and results.jsonl's classes;
  TALLY     signatures and programs per verdict;
  DEFECTS   the night's WiseOwl notes are found by the run id in their `source`, and only defect or adjudication notes;
  UPSERT    a re-recorded night replaces its own record and the nights stay in order;
  PAGE      every night is rendered, the page says it is machine-generated, a status is shown as the register has it, and
            every value of a verdict tally reaches the page;
  VOCAB     the page's verdict list equals the triage tool's vocabulary, so no verdict reaches the page without a colour.
"""
from __future__ import annotations

import json
import pathlib
import sys
import tempfile

HERE = pathlib.Path(__file__).resolve().parent
sys.path.insert(0, str(HERE))
import fuzz_history as fh  # noqa: E402
import fuzz_triage as ft  # noqa: E402

FAILS: list[str] = []


def expect(what: str, ok: bool, got=None) -> None:
    print(("PASS  " if ok else "FAIL  ") + what + ("" if ok else f"  (got {got!r})"))
    if not ok:
        FAILS.append(what)


def fabricated_run(root: pathlib.Path, run_id: str) -> pathlib.Path:
    run = root / run_id
    run.mkdir()
    (run / "run.json").write_text(json.dumps({"model": "m", "seed": 7, "started": "2026-10-28T04:00:00+00:00",
                                              "hours": 8.0, "edition": "2014"}), encoding="utf-8")
    (run / "report.md").write_text("# Differential fuzz — triage report\n\nPrograms: 5 · judged: 5 · usable: 3 (60% of judged)"
                                   " · usable on the first attempt: 2 (40% of judged) · GnuCOBOL alone refused (THEY_REJECT"
                                   " leads): 1\n\n## Survivors: 3 programs in 2 signatures\n", encoding="utf-8")
    rows = [{"verdict": v} for v in ("AGREE", "AGREE", "OUTPUT_DIFFERS", "WE_REJECT", "THEY_REJECT")]
    (run / "results.jsonl").write_text("\n".join(json.dumps(r) for r in rows) + "\n", encoding="utf-8")
    return run


def main() -> int:
    with tempfile.TemporaryDirectory() as tmp:
        run = fabricated_run(pathlib.Path(tmp), "fuzz-20261028-200000")
        s = fh.run_stats(run)
        expect("STATS headline", (s["programs"], s["judged"], s["usable"], s["usable_first"], s["they_reject"]) == (5, 5, 3, 2, 1), s)
        expect("STATS survivors", (s["survivors"], s["survivor_signatures"]) == (3, 2), s)
        expect("STATS classes", s["classes"] == {"AGREE": 2, "OUTPUT_DIFFERS": 1, "WE_REJECT": 1, "THEY_REJECT": 1}, s["classes"])
        expect("STATS run meta", (s["run_id"], s["model"], s["seed"]) == ("fuzz-20261028-200000", "m", 7), s)

        verdicts = [{"verdict": "WISEOWL-CORRECT", "count": 4}, {"verdict": "WISEOWL-CORRECT", "count": 1},
                    {"verdict": "GNUCOBOL-SIDE", "count": 2}]
        tally = fh.verdict_tally(verdicts)
        expect("TALLY", tally == {"WISEOWL-CORRECT": {"signatures": 2, "programs": 5},
                                  "GNUCOBOL-SIDE": {"signatures": 1, "programs": 2}}, tally)

        items = [{"id": "PB9001", "kind": "defect", "status": "open", "title": "PB9001 — a bug", "source": "fuzz-20261028-200000 slice D1"},
                 {"id": "PB9002", "kind": "upstream", "status": "open", "title": "x", "source": "fuzz-20261028-200000",
                  "upstream_project": "GnuCOBOL", "filed": "2026-10-28"},
                 {"id": "PB9003", "kind": "defect", "status": "landed", "title": "PB9003 — Defect: older", "source": "fuzz-20261020-000000"}]
        expect("DEFECTS by run id, defect kinds only", fh.defects_of(items, "fuzz-20261028-200000") == ["PB9001"],
               fh.defects_of(items, "fuzz-20261028-200000"))

        rec = fh.night_record(run, 2, verdicts, ["PB9001"], "abc1234")
        older = {**rec, "night": 1, "run_id": "fuzz-20261020-000000", "defects": ["PB9003"], "verdicts": None}
        hist = fh.upsert(fh.upsert({"artifact": None, "nights": []}, rec), older)
        expect("UPSERT orders nights", [n["night"] for n in hist["nights"]] == [1, 2], hist["nights"])
        again = fh.upsert(hist, {**rec, "judged": 99})
        expect("UPSERT replaces its own record", len(again["nights"]) == 2 and again["nights"][1]["judged"] == 99, again["nights"])

        page = fh.render(hist, items, now="2026-10-28 12:00 PDT")
        expect("PAGE renders every night", "Night 1" in page and "Night 2" in page)
        expect("PAGE states it is machine-generated", "machine-generated" in page and "fuzz_history.py" in page)
        expect("PAGE shows the register's status", "PB9001" in page and '<span class="pill bad">open</span>' in page
               and '<span class="pill ok">landed</span>' in page)
        expect("PAGE shows the tally", ">2<span class=sub> / 5</span>" in page, page[page.find("Decisions"):][:400])
        expect("PAGE counts the GnuCOBOL record", "<td>2026-10-28</td><td class=num>1</td>" in page)

    vocab = {v for v, _c, _n in fh.VERDICTS}
    expect("VOCAB equals the triage tool's verdicts", vocab == set(ft.VERDICTS), sorted(vocab ^ set(ft.VERDICTS)))
    print(f"=== test_fuzz_history: {'GREEN' if not FAILS else 'RED'} ({len(FAILS)} failure(s)) ===")
    return 1 if FAILS else 0


if __name__ == "__main__":
    from standard_streams import ensure_utf8

    ensure_utf8()
    raise SystemExit(main())
