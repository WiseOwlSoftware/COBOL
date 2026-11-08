#!/usr/bin/env python3
"""Self-test for audit_batch.py (kb/Work PB4886): planted catalogs, a planted 1985 transcription and a FAKE Message Batches
client (fake_batches.py). It makes no API call and opens no real key file. The stamp line it plants is built from pieces, so
no tracked file carries one.
Run: python scripts/spec/test_audit_batch.py   (every gate and CI run it through scripts/self_tests.py.)

Cases: the map plan's candidates and its write-once plan; a full submit refused before a canary; the canary, with a crash
between `create` and the state write adopted on the next run; a text-only request; collect before and after the batch ends,
an answer outside the pass's shape kept in raw/ and retried; the full submit priced by the canary's measured cost and refused
past --max-usd; an answer never overwritten; the cost so far counting every billed reply (a retried item's refused or
cut-off first reply too, and each reply once when a collect is repeated); the verify plan from the SAME answers, and the
dispositions; the drafts plan; the claims plan from the 1985 text and its citation check; a stamp in a prompt refused before any batch; the key never
printed or stored, and the account recorded.
"""
import contextlib
import io
import json
import pathlib
import sys
import tempfile

sys.path.insert(0, str(pathlib.Path(__file__).resolve().parent))
import audit_batch as A  # noqa: E402
import rule_catalog_1985  # noqa: E402  (THE reader of a level-2 box)
import spec_pdf  # noqa: E402
from fake_batches import Crash, FakeBatches, client, succeeded  # noqa: E402  (THE fake client)
from message_batches import BatchRun, usd  # noqa: E402
sys.path.append(str(pathlib.Path(__file__).resolve().parents[1]))  # scripts/, for standard_streams
from standard_streams import ensure_utf8  # noqa: E402

KEY = "sk-planted-audit-key"
#: A planted stamp line, built from pieces: the repository's own stamp audit would refuse the whole shape in this file.
STAMP = "store order " + chr(35) + " X_" + "9999999"
failures: list[str] = []


def check(name: str, cond: bool) -> None:
    print(f"  {'ok  ' if cond else 'FAIL'} {name}")
    if not cond:
        failures.append(name)


RULES_1985 = [
    {"id": "1985-SR-VI.5.8.3-1", "citation": "§1985:VI.5.8.3 SR1", "module": "Nucleus", "level": 1, "box": "none",
     "subject": "THE OCCURS CLAUSE", "kind": "SR",
     "text": "The OCCURS clause must not be specified in a data description entry that has a level-number of 01, 66, 77, or 88."},
    {"id": "1985-GR-VI.6.18.4-2", "citation": "§1985:VI.6.18.4 GR2", "module": "Nucleus", "level": 1, "box": "none",
     "subject": "THE INSPECT STATEMENT", "kind": "GR",
     "text": "Inspection of identifier-1 proceeds character by character from the leftmost character position."},
    {"id": "1985-GR-XV.3.2.4-1", "citation": "§1985:XV.3.2.4 GR1", "module": "Debug", "level": 1, "box": "none",
     "subject": "THE USE FOR DEBUGGING STATEMENT", "kind": "GR",
     "text": "Debugging procedures are executed when the debugging declarative applies to the named procedure."},
]
RULES_2023 = [
    {"id": "SR-13.18.38.3-1", "section": "13.18.38.3", "kind": "SR", "subject": "OCCURS clause",
     "text": "The OCCURS clause shall not be specified in a data description entry that has a level-number of 01, 66, 77, or 88."},
    {"id": "GR-14.9.21.4-2", "section": "14.9.21.4", "kind": "GR", "subject": "INSPECT statement",
     "text": "Inspection of identifier-1 proceeds character position by character position from the leftmost position."},
    {"id": "GR-14.9.1.4-1", "section": "14.9.1.4", "kind": "GR", "subject": "ACCEPT statement",
     "text": "The ACCEPT statement causes data to be transferred from the hardware device."},
]

PLANTED_1985 = """# SECTION VI: NUCLEUS MODULE

## Page 258

## 5.8 THE OCCURS CLAUSE

### 5.8.3 Syntax Rules

(1) The OCCURS clause must not be specified in a data description entry that has a level-number of 01, 66, 77, or 88.

VI-26
"""


def run(argv: list[str], fake: FakeBatches) -> tuple[object, str]:
    out = io.StringIO()
    code: object = None
    with contextlib.redirect_stdout(out):
        try:
            code = A.main(argv, client_factory=lambda key: client(fake) if key == KEY else None)
        except SystemExit as e:
            code = e.code
        except Crash:
            code = "crash"
    return code, out.getvalue()


def answer(fake: FakeBatches, bid: str, replies: dict[str, str]) -> None:
    """End batch `bid` with a reply per custom id (text keyed by the item id the batch's request names)."""
    fake.ended.add(bid)
    batch = next(b for b in fake.created if b["id"] == bid)
    fake.results_by_id[bid] = [succeeded(cid, replies[cid], input_tokens=3000, output_tokens=800) for cid in batch["ids"]]


def main() -> int:
    with tempfile.TemporaryDirectory() as tmp:
        root = pathlib.Path(tmp)
        cat85, cat23 = root / "c85.json", root / "c23.json"
        cat85.write_text(json.dumps({"rules": RULES_1985}), encoding="utf-8", newline="\n")
        cat23.write_text(json.dumps({"rules": RULES_2023}), encoding="utf-8", newline="\n")
        keyfile = root / "key.txt"
        keyfile.write_text(KEY + chr(10), encoding="utf-8", newline=chr(10))
        state = root / "run"
        base = ["--state", str(state)]
        key = ["--api-key-file", str(keyfile)]
        fake = FakeBatches()
        cid = {r["id"]: A.custom_id("map", r["id"]) for r in RULES_1985}

        print("the map plan")
        near = A.candidates(RULES_1985, RULES_2023, 2)
        check("the closest 2023 rule by heading and wording is the first candidate",
              near["1985-SR-VI.5.8.3-1"][0] == "SR-13.18.38.3-1" and near["1985-GR-VI.6.18.4-2"][0] == "GR-14.9.21.4-2")
        plan = ["plan", "--pass", "map", *base, "--catalog", str(cat85), "--catalog-2023", str(cat23), "--candidates", "2"]
        code, out = run(plan, fake)
        items = json.loads((state / "map" / "items.json").read_text(encoding="utf-8"))
        check("the plan holds one item per 1985 rule, keyed by a custom id the API accepts",
              code == 0 and len(items) == 3 and all(len(c) <= 64 and c.replace("-", "").isalnum() for c in items))
        code, out = run(plan, fake)
        check("re-planning the same items keeps the plan", code == 0 and "unchanged" in out)
        other = root / "c23b.json"
        other.write_text(json.dumps({"rules": RULES_2023[1:]}), encoding="utf-8", newline="\n")
        code, out = run(["plan", "--pass", "map", *base, "--catalog", str(cat85), "--catalog-2023", str(other)], fake)
        check("another plan in the same run is refused", code not in (0, None) and "another plan" in str(code))

        print("submit: --max-usd, the canary first")
        code, out = run(["submit", "--pass", "map", *base, *key], fake)
        check("a submit without --max-usd is refused", code not in (0, None) and not fake.created)
        code, out = run(["submit", "--pass", "map", *base, *key, "--max-usd", "50"], fake)
        check("a full submit before any canary is refused", code not in (0, None) and "canary" in str(code) and not fake.created)
        fake.crash_after_create = True
        code, out = run(["submit", "--pass", "map", *base, *key, "--max-usd", "50", "--canary", "2"], fake)
        st = json.loads((state / "map" / "state.json").read_text(encoding="utf-8"))
        check("the canary's intent is recorded before create", code == "crash" and st["pending"]["canary"] is True
              and len(st["pending"]["items"]) == 2 and st["batches"] == [])
        code, out = run(["status", "--pass", "map", *base, *key], fake)
        st = json.loads((state / "map" / "state.json").read_text(encoding="utf-8"))
        check("the next run adopts the server's batch, still a canary", len(fake.created) == 1 and st["pending"] is None
              and st["batches"][0]["id"] == "msgbatch_1" and st["batches"][0]["canary"] is True)
        req = fake.created[0]["requests"][0]
        content = req["params"]["messages"][0]["content"]
        check("a request is text only, Opus, its rule and its candidates' ids in the prompt",
              [c["type"] for c in content] == ["text"] and req["params"]["model"] == "claude-opus-5-5"
              and "§1985:VI.5.8.3 SR1" in content[0]["text"] and "[SR-13.18.38.3-1]" in content[0]["text"])

        print("collect: before and after the canary ends")
        run(["collect", "--pass", "map", *base, *key], fake)
        check("nothing is stored while the batch runs", not (state / "map" / "answers").exists())
        canary_ids = fake.created[0]["ids"]
        good = json.dumps({"verdict": "SAME", "rules_2023": ["SR-13.18.38.3-1"], "differs": {}, "reason": "same"})
        stray = json.dumps({"verdict": "SAME", "rules_2023": ["GR-99.9-1"], "differs": {}, "reason": "no such candidate"})
        answer(fake, "msgbatch_1", {canary_ids[0]: "```json\n" + good + "\n```", canary_ids[1]: stray})
        code, out = run(["collect", "--pass", "map", *base, *key], fake)
        check("a valid answer is stored once, a fenced reply read", (state / "map" / "answers" / f"{canary_ids[0]}.json").exists())
        check("an answer naming a rule that is no candidate is kept in raw/ and not stored",
              (state / "map" / "raw" / f"{canary_ids[1]}.txt").exists()
              and not (state / "map" / "answers" / f"{canary_ids[1]}.json").exists())

        print("the full submit, priced by the canary")
        code, out = run(["submit", "--pass", "map", *base, *key, "--max-usd", "0.001"], fake)
        check("the estimate uses the canary's measured cost and --max-usd refuses it",
              code not in (0, None) and "measured over 2" in out and len(fake.created) == 1)
        code, out = run(["submit", "--pass", "map", *base, *key, "--max-usd", "50"], fake)
        check("the full submit sends only the item no batch holds", code == 0 and len(fake.created) == 2
              and fake.created[1]["ids"] == [c for c in items if c not in canary_ids])
        code, out = run(["retry", "--pass", "map", *base, *key, "--max-usd", "50"], fake)
        check("retry resubmits the item whose answer was not valid", code == 0 and len(fake.created) == 3
              and fake.created[2]["ids"] == [canary_ids[1]])
        later = {c: v for c, v in zip(fake.created[1]["ids"], [json.dumps({"verdict": "NONE", "rules_2023": [], "differs": {},
                                                                           "reason": "no counterpart"})])}
        answer(fake, "msgbatch_2", later)
        answer(fake, "msgbatch_3", {canary_ids[1]: json.dumps({"verdict": "DIFFERS", "rules_2023": ["GR-14.9.21.4-2"],
                                                               "differs": {"1985": "character", "2023": "position"},
                                                               "reason": "differs"})})
        fake.results_by_id["msgbatch_1"] = [succeeded(canary_ids[0], json.dumps({"verdict": "NONE", "rules_2023": [],
                                                                                 "differs": {}, "reason": "changed"}))]
        code, out = run(["collect", "--pass", "map", *base, *key], fake)
        first = json.loads((state / "map" / "answers" / f"{canary_ids[0]}.json").read_text(encoding="utf-8"))
        check("every item is answered and a stored answer is never overwritten",
              "answered 3" in out and first["answer"]["verdict"] == "SAME")
        st = json.loads((state / "map" / "state.json").read_text(encoding="utf-8"))
        billed = 4 * usd("claude-opus-5-5", 3000, 800)   # the canary's 2 replies, the full submit's 1, the retry's 1
        check("the cost so far counts every billed reply, the retried item's refused first reply too",
              len(st["usage"]) == 4 and abs(A.Pass(state, "map").batches.cost_so_far(st) - billed) < 1e-9)
        check("the key is never printed or stored, the account recorded as the one unlabeled key",
              KEY not in out and KEY not in (state / "map" / "state.json").read_text(encoding="utf-8")
              and "(one unlabeled key)" in out)

        print("verify, the dispositions and the drafts")
        code, out = run(["plan", "--pass", "drafts", *base], fake)
        check("the drafts plan waits for the verify pass", code not in (0, None) and "disposition" in str(code))
        code, out = run(["plan", "--pass", "verify", *base], fake)
        vitems = json.loads((state / "verify" / "items.json").read_text(encoding="utf-8"))
        same_rule = first["id"]
        check("the verify plan holds the SAME answers only, with the 2023 rules they named",
              code == 0 and [v["id"] for v in vitems.values()] == [same_rule]
              and [c["id"] for c in next(iter(vitems.values()))["candidates"]] == ["SR-13.18.38.3-1"])
        run(["submit", "--pass", "verify", *base, *key, "--max-usd", "50", "--canary", "5"], fake)
        answer(fake, "msgbatch_4", {next(iter(vitems)): json.dumps({"verdict": "REFUTED", "reason": "77 differs",
                                                                    "differs": {"1985": "77", "2023": "77"}})})
        run(["collect", "--pass", "verify", *base, *key], fake)
        code, out = run(["results", *base], fake)
        results = json.loads((state / "results.json").read_text(encoding="utf-8"))
        check("a refuted SAME is DISPUTED, a DIFFERS and a NONE are rows of their own",
              results[same_rule]["disposition"] == "DISPUTED" and results[same_rule]["verify"] == "REFUTED"
              and sorted(d["disposition"] for d in results.values()) == ["DISPUTED", "OWN-ROW", "OWN-ROW"])
        code, out = run(["plan", "--pass", "drafts", *base], fake)
        ditems = json.loads((state / "drafts" / "items.json").read_text(encoding="utf-8"))
        check("the drafts plan holds the rows of their own only", code == 0 and len(ditems) == 2
              and same_rule not in [d["id"] for d in ditems.values()])
        dcid = next(iter(ditems))
        draft, why = A.parse_answer("drafts", ditems[dcid], json.dumps(
            {"program": "       IDENTIFICATION DIVISION.", "expected": {"outcome": "output", "output": ["A"]}, "basis": "GR2"}))
        check("a draft answer is a program and an expected outcome", draft is not None and why == "")
        check("a draft with no outcome is refused", A.parse_answer("drafts", ditems[dcid], '{"program": "x"}')[0] is None)

        print("claims: the 1985 text and the citation check")
        planted = root / "repo"
        md = spec_pdf.markdown(1985, planted)
        md.parent.mkdir(parents=True)
        md.write_text(PLANTED_1985, encoding="utf-8", newline=chr(10))
        claims = root / "claims.json"
        claims.write_text(json.dumps([{"id": "VCR-1", "claim": "OCCURS may not be written at level 01.",
                                       "locator": "VI.5.8.3"}]), encoding="utf-8", newline="\n")
        citems = A.plan_claims(claims, planted)
        check("a claim carries the 1985 text of the locator it names",
              len(citems) == 1 and "must not be specified" in citems[0]["text"])
        ok, _ = A.parse_answer("claims", citems[0], json.dumps({"verdict": "CONFIRMED", "citation": "§1985:VI.5.8.3 SR1",
                                                                "quote": "must not", "reason": "says so"}))
        bad, why = A.parse_answer("claims", citems[0], json.dumps({"verdict": "REFUTED", "citation": "VI.5.8.3",
                                                                   "quote": "x", "reason": "y"}))
        check("a CONFIRMED claim with a 1985 citation is an answer; a citation not in the format is refused",
              ok is not None and bad is None and why.startswith("citation"))
        missing = root / "missing.json"
        missing.write_text(json.dumps([{"id": "VCR-2", "claim": "x", "locator": "VI.9.9"}]), encoding="utf-8", newline="\n")
        try:
            A.plan_claims(missing, planted)
            refused = False
        except SystemExit as e:
            refused = "VI.9.9" in str(e)
        check("a claim naming a locator the 1985 text lacks is refused", refused)

        print("the spend of a retried reply that was cut off")
        run_ = BatchRun(root / "cut" / "state.json", "items", str, str, "test")
        cst, fake3 = run_.load(), FakeBatches()
        req = {"custom_id": "map-a", "params": {}}
        run_.submit(cst, client(fake3), [req])
        fake3.ended.add("msgbatch_1")
        fake3.results_by_id["msgbatch_1"] = [succeeded("map-a", "x", stop="max_tokens", input_tokens=3000, output_tokens=8000)]
        run_.collect(cst, client(fake3), lambda m, msg, b: None)
        run_.submit(cst, client(fake3), [req])
        fake3.ended.add("msgbatch_2")
        fake3.results_by_id["msgbatch_2"] = [succeeded("map-a", "{}", input_tokens=3000, output_tokens=800)]
        run_.collect(cst, client(fake3), lambda m, msg, b: None)
        both = usd("claude-opus-5-5", 3000, 8000) + usd("claude-opus-5-5", 3000, 800)
        check("a cut-off reply and its retry are both in the cost so far",
              abs(run_.cost_so_far(cst) - both) < 1e-9 and sorted(cst["usage"]) == ["msgbatch_1/map-a", "msgbatch_2/map-a"])
        cst["batches"][1]["collected"] = False   # a crash after the usage, before the batch was marked collected
        run_.collect(cst, client(fake3), lambda m, msg, b: None)
        check("a collect repeated after a crash records each reply once", abs(run_.cost_so_far(cst) - both) < 1e-9)

        print("the level-2 box: the preamble names the transcription's own markup")
        side = spec_pdf.BOX_SIDE
        boxed = dict(RULES_1985[0], box="whole", level=2, text=f"{side}(1) A planted rule boxed whole.{side}")
        texts = {p: A.prompt_text(p, {"rule": boxed, "candidates": RULES_2023[:1], "id": "c1", "claim": "planted",
                                      "locator": "VI.5.8.3", "text": boxed["text"]}) for p in A.PROMPTS}
        check("every pass's preamble defines the box side the catalog marks a level-2 box with",
              all(f"between two {side} marks" in t and "<ins>" not in t for t in texts.values())
              and rule_catalog_1985.box_of(boxed["text"], "SR") == "whole")
        check("every pass quotes the boxed text with its box sides", all(boxed["text"] in t for t in texts.values()))

        print("public text only")
        dirty = root / "dirty.json"
        dirty.write_text(json.dumps({"rules": [dict(RULES_1985[0], text=RULES_1985[0]["text"] + " " + STAMP)]}),
                         encoding="utf-8", newline="\n")
        state2 = root / "run2"
        fake2 = FakeBatches()
        run(["plan", "--pass", "map", "--state", str(state2), "--catalog", str(dirty), "--catalog-2023", str(cat23)], fake2)
        code, out = run(["submit", "--pass", "map", "--state", str(state2), *key, "--max-usd", "50", "--canary", "1"], fake2)
        check("a prompt carrying a stamp shape is refused before any batch",
              code not in (0, None) and "stamp" in str(code) and not fake2.created)
        code, out = run(["status", "--pass", "map", *base], fake)
        check("status needs no key", code == 0 and "items 3" in out)
    print(f"=== AUDIT_BATCH SELF-TEST: {'GREEN' if not failures else 'RED: ' + '; '.join(failures)} ===")
    return 1 if failures else 0


if __name__ == "__main__":
    ensure_utf8()
    sys.exit(main())
