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
import inventory_schema  # noqa: E402  (the 1985 edition and the change record, kept-batch)
import lineage  # noqa: E402  (THE validator of a change record)
import rule_catalog_1985  # noqa: E402  (THE reader of a level-2 box)
import spec_pdf  # noqa: E402
from fake_batches import Crash, FakeBatches, client, succeeded  # noqa: E402  (THE fake client)
from message_batches import BatchRun, usage_of, usd  # noqa: E402
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
    {"id": "1985-SR-VI.5.8.3-1", "document": "X3.23-1985", "locator": "VI.5.8.3", "citation": "§1985:VI.5.8.3 SR1", "module": "Nucleus", "level": 1, "box": "none",
     "subject": "THE OCCURS CLAUSE", "kind": "SR",
     "text": "The OCCURS clause must not be specified in a data description entry that has a level-number of 01, 66, 77, or 88."},
    {"id": "1985-GR-VI.6.18.4-2", "document": "X3.23-1985", "locator": "VI.6.18.4", "citation": "§1985:VI.6.18.4 GR2", "module": "Nucleus", "level": 1, "box": "none",
     "subject": "THE INSPECT STATEMENT", "kind": "GR",
     "text": "Inspection of identifier-1 proceeds character by character from the leftmost character position."},
    {"id": "1985-GR-XV.3.2.4-1", "document": "X3.23-1985", "locator": "XV.3.2.4", "citation": "§1985:XV.3.2.4 GR1", "module": "Debug", "level": 1, "box": "none",
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
    {"id": "GR-14.9.21.4-7", "section": "14.9.21.4", "kind": "GR", "subject": "INSPECT statement",
     "text": "Tallying counts the occurrences in the tally."},
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


def unrouted(state: pathlib.Path, cat85: pathlib.Path, cat23: pathlib.Path, fake: FakeBatches) -> None:
    """Plan `state`'s route pass and answer every clause NONE, as `collect` stores answers, so its map pass can be planned."""
    run(["plan", "--pass", "route", "--state", str(state), "--catalog", str(cat85), "--catalog-2023", str(cat23)], fake)
    (state / "route" / "answers").mkdir(parents=True, exist_ok=True)
    for c, it in json.loads((state / "route" / "items.json").read_text(encoding="utf-8")).items():
        (state / "route" / "answers" / f"{c}.json").write_text(json.dumps(
            {"id": it["id"], "answer": {"verdict": "NONE", "clauses_2023": [], "reason": "planted"}}), encoding="utf-8", newline="\n")


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

        print("the route plan (kb/Work PB4971)")
        (root / "c23b.json").write_text(json.dumps({"rules": RULES_2023[:2] + RULES_2023[3:]}), encoding="utf-8", newline="\n")
        route = ["plan", "--pass", "route", *base, "--catalog", str(cat85), "--catalog-2023", str(cat23)]
        plan = ["plan", "--pass", "map", *base, "--catalog", str(cat85), "--catalog-2023", str(cat23), "--candidates", "2"]
        code, out = run(plan, fake)
        check("the map plan waits for the route pass", code not in (0, None) and "the route pass has no plan" in str(code))
        code, out = run(route, fake)
        ritems = json.loads((state / "route" / "items.json").read_text(encoding="utf-8"))
        rby = {it["id"]: (c, it) for c, it in ritems.items()}
        check("one route item per 1985 clause, each carrying every 2023 clause once",
              code == 0 and sorted(rby) == ["X3.23-1985 VI.5.8", "X3.23-1985 VI.6.18", "X3.23-1985 XV.3.2"]
              and [c["id"] for c in next(iter(ritems.values()))["clauses_2023"]] == ["13.18.38", "14.9.21", "14.9.1"])
        rtext = A.prompt_text("route", rby["X3.23-1985 VI.6.18"][1])
        check("a route prompt quotes the clause's rules and lists the 2023 clauses by number and subject",
              "§1985:VI.6.18.4 GR2" in rtext and "14.9.21 INSPECT statement" in rtext and "13.18.38 OCCURS clause" in rtext)
        est = A.Pass(root / "run7", "route")
        A.write_plan(est, [it for _c, it in rby.values()])
        per_cached, _how = A.per_request_usd(est, {"model": "claude-opus-5-5"}, list(est.items()))
        whole = sum(len(A.prompt_text("route", it)) for it in est.items().values()) / len(est.items())
        check("the estimate before a canary prices the cached system a write once and a read after (review "
              "w20261922c R1-4): it differs from pricing the whole prompt as input",
              abs(per_cached - usd("claude-opus-5-5", int(whole / A.CHARS_PER_TOKEN), A.EST_OUTPUT_TOKENS["route"])) > 1e-9)
        layers = {k: A.prompt_parts("route", it) for k, (_c, it) in rby.items()}
        check("every route request shares one system text, the 2023 clause list, and its own part is its clause",
              len({sh for sh, _g, _o in layers.values()}) == 1
              and "14.9.21 INSPECT statement" in layers["X3.23-1985 VI.6.18"][0]
              and "§1985:VI.6.18.4 GR2" in layers["X3.23-1985 VI.6.18"][2]
              and "14.9.21 INSPECT" not in layers["X3.23-1985 VI.6.18"][2] and layers["X3.23-1985 VI.6.18"][1] == "")
        check("a cache write costs twice the input price and a read a twentieth, at the batch discount",
              abs(usd("claude-opus-5-5", 0, 0, 1_000_000, 0) - 4.0) < 1e-9
              and abs(usd("claude-opus-5-5", 0, 0, 0, 1_000_000) - 0.1) < 1e-9
              and abs(usd("claude-opus-5-5", 1_000_000, 0) - 2.0) < 1e-9)
        from types import SimpleNamespace  # noqa: PLC0415
        cached_reply = SimpleNamespace(usage=SimpleNamespace(input_tokens=700, output_tokens=900,
                                                             cache_creation_input_tokens=7000, cache_read_input_tokens=0))
        later_reply = SimpleNamespace(usage=SimpleNamespace(input_tokens=700, output_tokens=900,
                                                            cache_creation_input_tokens=None, cache_read_input_tokens=7000))
        check("a reply's usage records its cache write and read (kb/Work PB4998), a missing count as none",
              usage_of(cached_reply) == [700, 900, 7000, 0] and usage_of(later_reply) == [700, 900, 0, 7000])
        inspect_item = rby["X3.23-1985 VI.6.18"][1]
        check("a route answer names only listed clauses, none for NONE, at most MAX_ROUTES",
              A.parse_answer("route", inspect_item, json.dumps({"verdict": "ROUTED", "clauses_2023": ["14.9.21"], "reason": "r"}))[0]
              is not None
              and A.parse_answer("route", inspect_item, json.dumps({"verdict": "ROUTED", "clauses_2023": ["99.1"], "reason": "r"}))[0]
              is None
              and A.parse_answer("route", inspect_item, json.dumps({"verdict": "NONE", "clauses_2023": ["14.9.21"], "reason": "r"}))[0]
              is None
              and A.parse_answer("route", inspect_item, json.dumps({"verdict": "ROUTED", "clauses_2023": [], "reason": "r"}))[0]
              is None)
        wide = dict(inspect_item, clauses_2023=[{"id": f"1.{n}", "subject": "s"} for n in range(A.MAX_ROUTES + 1)])
        check("a route answer naming more than MAX_ROUTES clauses is refused, and MAX_ROUTES is accepted",
              A.parse_answer("route", wide, json.dumps({"verdict": "ROUTED", "clauses_2023": [c["id"] for c in
                             wide["clauses_2023"]], "reason": "r"}))[0] is None
              and A.parse_answer("route", wide, json.dumps({"verdict": "ROUTED", "clauses_2023": [c["id"] for c in
                                 wide["clauses_2023"][:A.MAX_ROUTES]], "reason": "r"}))[0] is not None)
        code, out = run(plan, fake)
        check("the map plan refuses an unanswered route pass", code not in (0, None) and "route item(s) are unanswered" in str(code))
        (state / "route" / "answers").mkdir()
        routed_to = {"X3.23-1985 VI.5.8": ("NONE", []), "X3.23-1985 VI.6.18": ("ROUTED", ["14.9.21"]),
                     "X3.23-1985 XV.3.2": ("ROUTED", ["14.9.1"])}
        for key_, (verdict, to) in routed_to.items():
            c_, _it = rby[key_]
            (state / "route" / "answers" / f"{c_}.json").write_text(json.dumps(
                {"id": key_, "answer": {"verdict": verdict, "clauses_2023": to, "reason": "planted"}}), encoding="utf-8", newline="\n")

        print("the map plan")
        near = A.candidates(RULES_1985, RULES_2023, 1, A.routes(state))
        check("a routed clause's rules of the same kind are its clause part, in catalog order, beyond the wording's k",
              near["1985-GR-VI.6.18.4-2"].clause == ["GR-14.9.21.4-2", "GR-14.9.21.4-7"])
        check("a clause that shares a heading is a route too (THE OCCURS CLAUSE, OCCURS clause), with no route answer",
              near["1985-SR-VI.5.8.3-1"].clause == ["SR-13.18.38.3-1"])
        check("a route finds the counterpart the wording does not",
              near["1985-GR-XV.3.2.4-1"].all[0] == "GR-14.9.1.4-1")
        check("a rule named by its clause is not named again by its wording",
              all(not set(v.clause) & set(v.worded) for v in near.values()))
        twin = dict(RULES_1985[1], id="1993-GR-VI.6.18.4-2", document="X3.23b-1993")
        both = A.candidates([RULES_1985[1], twin], RULES_2023, 0,
                            {"X3.23-1985 VI.6.18": ["14.9.21", "14.9.1"], "X3.23b-1993 VI.6.18": ["14.9.21"]})
        check("the clause key carries the document: the same locator in another document has its own route",
              both["1985-GR-VI.6.18.4-2"].clause[-1] == "GR-14.9.1.4-1"
              and "GR-14.9.1.4-1" not in both["1993-GR-VI.6.18.4-2"].clause)
        check("several routed clauses keep the route's closest-first order (14.9.21, then 14.9.1), not their numbers' "
              "order (review w20261922b R2-1)",
              both["1985-GR-VI.6.18.4-2"].clause == ["GR-14.9.21.4-2", "GR-14.9.21.4-7", "GR-14.9.1.4-1"])
        extra = root / "c85x.json"
        extra.write_text(json.dumps({"rules": RULES_1985 + [dict(RULES_1985[0], id="1985-SR-VI.5.9.3-1",
                                                                 locator="VI.5.9.3", citation="§1985:VI.5.9.3 SR1")]}),
                         encoding="utf-8", newline="\n")
        code, out = run(["plan", "--pass", "map", *base, "--catalog", str(extra), "--catalog-2023", str(cat23)], fake)
        check("a map plan from a 1985 catalog with a clause the route pass never saw is refused (review R1-1)",
              code not in (0, None) and "have no route item" in str(code) and not (state / "map").exists())
        code, out = run(["plan", "--pass", "map", *base, "--catalog", str(cat85), "--catalog-2023", str(root / "c23b.json")], fake)
        check("a map plan from a 2023 catalog that lacks a routed clause is refused",
              code not in (0, None) and "lacks" in str(code) and not (state / "map").exists())
        code, out = run(plan, fake)
        items = json.loads((state / "map" / "items.json").read_text(encoding="utf-8"))
        check("the plan holds one item per 1985 rule, keyed by a custom id the API accepts",
              code == 0 and len(items) == 3 and all(len(c) <= 64 and c.replace("-", "").isalnum() for c in items))
        code, out = run(plan, fake)
        check("re-planning the same items keeps the plan", code == 0 and "unchanged" in out)
        other = root / "c23b.json"
        other.write_text(json.dumps({"rules": RULES_2023[1:]}), encoding="utf-8", newline="\n")
        code, out = run(["plan", "--pass", "map", *base, "--catalog", str(cat85), "--catalog-2023", str(other)], fake)  # noqa
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
              all(c["type"] == "text" for c in content) and req["params"]["model"] == "claude-opus-5-5"
              and "§1985:VI.5.8.3 SR1" in content[-1]["text"]
              and any("[SR-13.18.38.3-1]" in c["text"] for c in content))
        ttl = {"type": "ephemeral", "ttl": "1h"}
        check("a request's pass instructions are its cached system block, an hour's TTL (Anthropic notice 2026-11-08)",
              req["params"]["system"] == [{"type": "text", "text": A.PROMPTS["map"], "cache_control": ttl}])
        check("a map request's clause candidates are its first user block, before the rule; a block fewer than "
              "CACHE_MIN_SHARERS requests share (each planted rule has a clause of its own) is sent uncached",
              len(content) == 2 and "HOLD THE 1985 RULE'S FEATURE" in content[0]["text"]
              and "cache_control" not in content[0] and "[SR-13.18.38.3-1]" in content[0]["text"]
              and "cache_control" not in content[1] and "§1985:VI.5.8.3 SR1" not in content[0]["text"])
        c_a, c_b = RULES_2023[0], RULES_2023[1]
        word_item = {"id": "1985-GR-VI.6.18.4-9", "rule": dict(RULES_1985[1], id="1985-GR-VI.6.18.4-9"),
                     "clause_candidates": [c_a], "word_candidates": [c_b]}
        same_on_word = json.dumps({"verdict": "SAME", "rules_2023": [c_b["id"]], "differs": {}, "reason": "by wording"})
        check("a SAME answer naming a wording candidate is accepted (review w20261922c R1-1)",
              A.parse_answer("map", word_item, same_on_word)[0] is not None
              and A.parse_answer("map", dict(word_item, word_candidates=[]), same_on_word)[0] is None)
        state6 = root / "run6"
        A.write_plan(A.Pass(state6, "map"), [word_item])
        (state6 / "map" / "answers").mkdir(parents=True)
        (state6 / "map" / "answers" / f"{A.custom_id('map', word_item['id'])}.json").write_text(
            json.dumps({"id": word_item["id"], "answer": json.loads(same_on_word)}), encoding="utf-8", newline="\n")
        check("the verify plan carries a SAME answer's wording candidate (review w20261922c R1-1)",
              [c["id"] for v in A.plan_verify(state6) for c in v["candidates"]] == [c_b["id"]])
        trio = [dict(word_item, id=f"1985-GR-VI.6.18.4-{n}") for n in (11, 12, 13)]
        shared = A.shared_groups("map", trio)
        req3 = A.request("map", "c3", trio[0], "claude-opus-5-5", [], shared)
        check("a clause block CACHE_MIN_SHARERS requests share is cached, and one fewer share is not",
              len(shared) == 1 and req3["params"]["messages"][0]["content"][0].get("cache_control") == ttl
              and A.shared_groups("map", trio[:2]) == set())

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
        kept = root / "kept.json"
        code, out = run(["kept-batch", *base, "--out", str(kept)], fake)
        check("kept-batch writes no record while no SAME is upheld (kb/Work PB4993)",
              code == 0 and json.loads(kept.read_text(encoding="utf-8"))["records"] == [])
        planted = {"1985-SR-VI.5.8.3-1": {"disposition": A.CARRIES, "rules_2023": ["SR-13.18.38.3-1"]},
                   "1985-SR-VI.5.8.3-2": {"disposition": A.DISPUTED, "rules_2023": ["SR-13.18.38.3-1"]},
                   "1985-GR-VI.5.8.4-1": {"disposition": A.OWN_ROW, "rules_2023": []}}
        recs = A.kept_records(planted, "audit-1985/planted")
        schema = inventory_schema.load_schema()
        targets = {"SR-13.18.38.3-1": {"rule-id": "SR-13.18.38.3-1", "state": "OK", "editions": "85,2002,2014,2023"}}
        check("the kept records are the CARRIES rules only, each a legal chained change record with no reason",
              [r["rule-id"] for r in recs] == ["1985-SR-VI.5.8.3-1"] and "reason" not in recs[0]["next"]
              and lineage.next_errors({**recs[0], "verdict": ""}, inventory_schema.edition(1985), schema, targets) == []
              and lineage.chained(recs[0], schema))
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
        def claim(**fields: object) -> tuple[object, str]:
            return A.parse_answer("claims", citems[0], json.dumps({"verdict": "CONFIRMED", "quote": "q", "reason": "r",
                                                                   **fields}))
        ok, _ = claim(locator="VI.5.8.3", rule="SR1")
        check("a CONFIRMED claim citing a locator and its rule is an answer, its citation rendered in the one format",
              ok is not None and ok["citation"] == "§1985:VI.5.8.3 SR1")
        # The claims canary of 2026-11-07: three right answers, each citing a Function paragraph (kb/Work PB4886).
        function, why = claim(locator="VII.3.5.1", rule=None)
        check("a paragraph with no numbered rule (a Function) is cited by its locator alone, the rule null",
              function is not None and function["citation"] == "§1985:VII.3.5.1" and why == "")
        check("the prompt asks for the locator and the rule as fields of their own, in CITATION_FIELDS' words",
              A.CITATION_FIELDS in A.PROMPTS["claims"] and '"locator"' in A.CITATION_FIELDS)
        refused = {"a heading word in the rule": claim(locator="VII.3.5.1", rule="Function"),
                   "a heading word in the locator": claim(locator="VII.3.5.1 Function"),
                   "the edition in the locator": claim(locator="§1985:VII.3.5.1"),
                   "no locator": claim(citation="§1985:VII.3.5.1"),
                   "a locator the 1985 text lacks": claim(locator="VII.99.9"),
                   "a paragraph outside its section": claim(locator="I.5.8.3")}
        check("a citation outside the shape, or naming nothing in the 1985 text, is a reply to retry: "
              + ", ".join(refused), all(a is None and w.startswith("citation") for a, w in refused.values()))
        na, _ = A.parse_answer("claims", citems[0], json.dumps({"verdict": "NOT-ADDRESSED", "reason": "silent"}))
        check("a NOT-ADDRESSED answer needs no locator", na is not None)
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
        texts = {p: A.prompt_text(p, {"rule": boxed, "candidates": RULES_2023[:1], "clause_candidates": RULES_2023[:1],
                                      "word_candidates": [], "id": "c1", "claim": "planted",
                                      "locator": "VI.5.8.3", "text": boxed["text"],
                                      "clause": {"locator": "VI.5.8", "document": "X3.23-1985", "module": "Nucleus",
                                                 "subject": "THE OCCURS CLAUSE", "rules": 1, "sample": [boxed["text"]]},
                                      "clauses_2023": A.clauses_2023(RULES_2023)}) for p in A.PROMPTS}
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
        code, out = run(["plan", "--pass", "route", "--state", str(state2), "--catalog", str(dirty), "--catalog-2023",
                         str(cat23)], fake2)
        code, out = run(["submit", "--pass", "route", "--state", str(state2), *key, "--max-usd", "50", "--canary", "1"], fake2)
        check("a route prompt carrying a stamp shape is refused before any batch",
              code not in (0, None) and "stamp" in str(code) and not fake2.created)
        unrouted(state2, dirty, cat23, fake2)
        run(["plan", "--pass", "map", "--state", str(state2), "--catalog", str(dirty), "--catalog-2023", str(cat23)], fake2)
        code, out = run(["submit", "--pass", "map", "--state", str(state2), *key, "--max-usd", "50", "--canary", "1"], fake2)
        check("a prompt carrying a stamp shape is refused before any batch",
              code not in (0, None) and "stamp" in str(code) and not fake2.created)
        code, out = run(["status", "--pass", "map", *base], fake)
        check("status needs no key", code == 0 and "items 3" in out)

        print("retry after a canary whose every reply failed (kb/Work PB4930)")
        state3, fake4 = root / "run3", FakeBatches()
        b3 = ["--state", str(state3)]
        unrouted(state3, cat85, cat23, fake4)
        run(["plan", "--pass", "map", *b3, "--catalog", str(cat85), "--catalog-2023", str(cat23), "--candidates", "2"], fake4)
        code, out = run(["retry", "--pass", "map", *b3, *key, "--max-usd", "50"], fake4)
        check("a retry before any canary is refused", code not in (0, None) and "canary" in str(code) and not fake4.created)
        run(["submit", "--pass", "map", *b3, *key, "--max-usd", "50", "--canary", "2"], fake4)
        tried = fake4.created[0]["ids"]
        answer(fake4, "msgbatch_1", {c: "no JSON here" for c in tried})
        run(["collect", "--pass", "map", *b3, *key], fake4)
        code, out = run(["retry", "--pass", "map", *b3, *key, "--max-usd", "50"], fake4)
        st3 = json.loads((state3 / "map" / "state.json").read_text(encoding="utf-8"))
        check("the retry resubmits the failed canary's items, as the canary, and no never-submitted item",
              code == 0 and len(fake4.created) == 2 and fake4.created[1]["ids"] == tried and st3["batches"][1]["canary"])
        code, out = run(["submit", "--pass", "map", *b3, *key, "--max-usd", "50"], fake4)
        check("the full submit stays refused while the retried canary runs", code not in (0, None) and len(fake4.created) == 2)
        none = json.dumps({"verdict": "NONE", "rules_2023": [], "differs": {}, "reason": "no counterpart"})
        answer(fake4, "msgbatch_2", {tried[0]: none, tried[1]: "no JSON here"})
        run(["collect", "--pass", "map", *b3, *key], fake4)
        code, out = run(["submit", "--pass", "map", *b3, *key, "--max-usd", "50"], fake4)
        rest = [c for c in json.loads((state3 / "map" / "items.json").read_text(encoding="utf-8")) if c not in tried]
        check("the retried canary's valid answer proves the pass: the full submit sends the never-submitted item",
              code == 0 and len(fake4.created) == 3 and fake4.created[2]["ids"] == rest)

        print("the catalog's citations: every pass quotes them")
        ar = {"id": "1989-AR-A.2.5.3-1", "document": "X3.23a-1989", "locator": "A.2.5.3", "citation": "§1989:A.2.5.3 AR1", "module": "Intrinsic Function", "level": 1,
              "box": "none", "subject": "ACOS", "kind": "AR", "text": "Argument-1 must be class numeric."}
        good_cat, bad_cat = root / "c85ar.json", root / "c85bad.json"
        good_cat.write_text(json.dumps({"rules": RULES_1985 + [ar]}), encoding="utf-8", newline="\n")
        bad_cat.write_text(json.dumps({"rules": [dict(ar, citation="§1985:VII.3.5.1 Function")]}), encoding="utf-8",
                           newline="\n")
        state5 = root / "run5"
        unrouted(state5, good_cat, cat23, FakeBatches())
        check("a catalog row of every rule kind, an argument rule too, is quoted by its citation",
              [i["rule"]["citation"] for i in A.plan_map(good_cat, cat23, 1, state5)][-1] == "§1989:A.2.5.3 AR1")
        try:
            A.plan_map(bad_cat, cat23, 1, state5)
            refused_plan = False
        except SystemExit as e:
            refused_plan = "is not a rule designator" in str(e)
        check("a catalog row whose citation is none refuses the plan", refused_plan)
    print(f"=== AUDIT_BATCH SELF-TEST: {'GREEN' if not failures else 'RED: ' + '; '.join(failures)} ===")
    return 1 if failures else 0


if __name__ == "__main__":
    ensure_utf8()
    sys.exit(main())
