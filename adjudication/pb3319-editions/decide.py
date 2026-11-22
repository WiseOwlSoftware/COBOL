"""PB3319 -- the screen-handling rows' editions, decided from the standard (kb/Work PB3173's evidence shape).

THE FACILITY. Annex A.4.2 (ACCEPT and DISPLAY screen handling: the SCREEN SECTION, the screen description entry, the
ACCEPT/DISPLAY/SET screen statements, CRT STATUS, CURSOR) is the optional module of this implementation's docs/conformance
register, and every rule that hangs off it carries a DOCUMENTED-NON-SUPPORT verdict whose notes name `A.4.2`. The facility is
a COBOL-2002 introduction, so none of its rules can start at 85. Evidence, each piece checked before a batch is written:
  - `reserved-words.json`: SCREEN is first reserved at 2002;
  - `constructs/end-accept-2002.json`: introducedIn 2002, whose description quotes §2002:F.2 item 1 ("The ACCEPT
    statement is extended to allow a screen item specified in the screen section to be used for operator input at a
    terminal");
  - `cite.py --check`: the screen clauses' own general rules name the ACCEPT/DISPLAY screen statements;
  - 23 sibling rows of the same module already carry 2002 (PB3173's DOC-A.1-11 / -41 / -83 / -172 and GR-14.9.1.4-13..25).

THE SELECTION is mechanical, so a row cannot be missed or over-taken: the view's rows with verdict DOCUMENTED-NON-SUPPORT,
an `editions` field that starts at 85, and notes naming `A.4.2`. The 13 DOCUMENTED-NON-SUPPORT rows that start at 85 and do
NOT name A.4.2 (DOC-A.1 items on alignment/non-COBOL calls, the STANDARD-1 tape rules, the READ/WRITE reel swap, the
CURRENT-DATE and WHEN-COMPILED return values) are other facilities and stay at 85; the 13 CONFORMS rows that name A.4.2
(SIGN, GLOBAL, entry-name, level-number, LINE/COLUMN formats shared with data and report items) are multi-facility rules
and stay at 85.

    python adjudication/pb3319-editions/decide.py <out-batch.json>

Run on the tree BEFORE the batch is applied (it refuses a row that no longer starts at 85).
"""
import json
import pathlib
import re
import subprocess
import sys

sys.path.insert(0, "scripts/spec")
import inventory_view as iv  # noqa: E402

BAND = "2002,2014,2023"
REASON = ("the screen-handling facility (Annex A.4.2: the SCREEN SECTION, the screen description entry and the ACCEPT/"
          "DISPLAY/SET screen statements) is a COBOL-2002 introduction (SCREEN first reserved at 2002; "
          "§2002:F.2 item 1: the ACCEPT statement is extended to allow a screen item specified in the screen section "
          "to be used for operator input at a terminal), so a rule hanging off it cannot start at 85")


def check_evidence() -> int:
    n = 0
    words = json.loads(pathlib.Path("tests/version-matrix/reserved-words.json").read_text(encoding="utf-8"))
    entry = next(w for w in _walk(words) if isinstance(w, dict) and w.get("word") == "SCREEN")
    assert entry["r85"] is False and entry["r2002"] is True, "SCREEN is not first reserved at 2002"
    n += 1
    c = json.loads(pathlib.Path("tests/version-matrix/constructs/end-accept-2002.json").read_text(encoding="utf-8"))
    assert c["introducedIn"] == 2002 and "§2002:F.2 item 1" in c["citation"], "end-accept-2002 moved"
    n += 1
    for clause, text in (("13.18.25.1", "The FROM clause specifies the source of data for an ACCEPT screen statement"),
                         ("13.18.56.1", "The TO clause identifies the destination of the data in an ACCEPT screen statement")):
        out = subprocess.run([sys.executable, "scripts/spec/cite.py", "--check", f"§2023:{clause}", text],
                             capture_output=True, text=True, encoding="utf-8").stdout
        assert out.startswith("OK"), f"cite.py --check §2023:{clause}: {out}"
        n += 1
    rows = {r["rule-id"]: r for r in iv.inventory(2023)}
    for rid in ("DOC-A.1-11", "DOC-A.1-41", "DOC-A.1-83", "DOC-A.1-172", "GR-14.9.1.4-24"):
        assert rows[rid]["editions"] == BAND, f"{rid} is not already {BAND}"
        n += 1
    return n


def _walk(o):
    if isinstance(o, dict):
        yield o
        for v in o.values():
            yield from _walk(v)
    elif isinstance(o, list):
        for v in o:
            yield from _walk(v)


def main() -> int:
    out = pathlib.Path(sys.argv[1])
    checked = check_evidence()
    records = []
    for r in iv.inventory(2023):
        if (r["verdict"] == "DOCUMENTED-NON-SUPPORT" and r["editions"].startswith("85,")
                and re.search(r"\bA\.4\.2\b", r["notes"])):
            note = f"editions {BAND} (kb/Work PB3319; was {r['editions']}): {REASON}"
            records.append({"rule-id": r["rule-id"], "verdict": r["verdict"], "editions": BAND,
                            "notes": r["notes"].rstrip() + " || " + note})
    out.write_text(json.dumps({"batch": "pb3319-screen-editions", "records": records}, indent=1, ensure_ascii=False)
                   + "\n", encoding="utf-8")
    print(f"{len(records)} records, {checked} evidence items checked, all OK -> {out}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
