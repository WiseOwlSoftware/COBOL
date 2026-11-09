#!/usr/bin/env python3
"""Self-test for transcribe_pdf.py (kb/Work PB4816): a planted 2-page PDF and a FAKE Message Batches client. It makes no API
call and opens no licensed PDF. The stamp shapes it plants are built from pieces, so no tracked file carries one.
Run: python scripts/spec/test_transcribe_pdf.py   (every gate and CI run it through scripts/self_tests.py.)

Cases: submit; a crash between `create` and the state write, adopted on the next run; collect before and after the batch
ends; an errored page and a cut-off page retried; a written page never overwritten, and replaced only by `--replace` (the
old text kept); the stamp line dropped; the cost guard; the key never printed or stored; a key file of labeled keys, the
account chosen by --api-account, recorded on each batch and kept while a batch is uncollected (kb/Work PB4916), another
account once every batch is collected, and each account's spend (kb/Work PB4995); the local backend;
adoption of a run that already exists; a reply that declines or summarises the page recorded as failed and never written,
by its refusal shape or by word agreement with the text layer (kb/Work PB4917); the check numbers; the assembly and its
refusals.
"""
import base64
import contextlib
import io
import http.client
import json
import pathlib
import sys
import tempfile
import urllib.error

sys.path.insert(0, str(pathlib.Path(__file__).resolve().parent))
import transcribe_pdf as T  # noqa: E402
from fake_batches import Crash, FakeBatches, client, errored, succeeded  # noqa: E402  (THE fake client)
sys.path.append(str(pathlib.Path(__file__).resolve().parents[1]))  # scripts/, for standard_streams
from standard_streams import ensure_utf8  # noqa: E402

KEY = "sk-planted-secret-key"
#: kb/Work PB4916: the second account's key of a planted labeled pair (invented labels; no sign-in in a tracked file).
KEY2 = "sk-planted-second-account-key"
#: A planted stamp line, built from pieces: the repository's own stamp audit would refuse the whole shape in this file.
STAMP = "Copyright by the publisher. store order " + chr(35) + " X_" + "9999999. Downloaded today"
failures: list[str] = []

def check(name: str, cond: bool) -> None:
    print(f"  {'ok  ' if cond else 'FAIL'} {name}")
    if not cond:
        failures.append(name)

def page_ok(pno, text, stop="end_turn"):
    return succeeded(T.page_name(pno), text, stop)

def page_errored(pno):
    return errored(T.page_name(pno))

def plant_pdf(path: pathlib.Path, texts: list[str]) -> None:
    """A stand-in PDF: the three functions that open one (render, page count, text layer) are replaced, so the self-test
    needs no PDF library (CI's self-test jobs install none) and reads no licensed file."""
    path.write_bytes(b"planted, not a PDF")
    T.render_png = lambda pdf, pno: b"rendered page %d" % pno
    T.pdf_page_count = lambda pdf: len(texts)
    T.page_text = lambda pdf, pno: texts[pno - 1]

def run(argv, fake, pdf, parts=None, key_wanted=KEY):
    out = io.StringIO()
    code = None
    with contextlib.redirect_stdout(out):
        try:
            code = T.main(argv, client_factory=lambda key: client(fake) if key == key_wanted else None,
                          pdf_override=pdf, parts=parts)
        except SystemExit as e:
            code = e.code
        except Crash:
            code = "crash"
    return code, out.getvalue()

def main() -> int:
    parts = [(1, "Front matter"), (2, "SECTION A: SECOND")]
    with tempfile.TemporaryDirectory() as tmp:
        root = pathlib.Path(tmp)
        pdf = root / "planted.pdf"
        plant_pdf(pdf, ["alpha beta gamma", "delta epsilon zeta"])
        keyfile = root / "key.txt"
        keyfile.write_text(KEY + chr(10), encoding="utf-8", newline="\n")
        state = root / "run"
        base = ["--edition", "1985", "--state", str(state)]
        key = ["--api-key-file", str(keyfile)]
        fake = FakeBatches()

        print("submit, with a crash between create and the state write")
        fake.crash_after_create = True
        code, out = run(["submit", *base, *key], fake, pdf)
        check("the process died after the server accepted the batch", code == "crash" and len(fake.created) == 1)
        st = json.loads((state / "state.json").read_text(encoding="utf-8"))
        check("the intent was recorded before create", st["pending"] is not None and st["pending"]["pages"] == [1, 2] and st["batches"] == [])
        code, out = run(["submit", *base, *key], fake, pdf)
        st = json.loads((state / "state.json").read_text(encoding="utf-8"))
        check("the next run ADOPTS the server's batch (no second create)", len(fake.created) == 1 and [b["id"] for b in st["batches"]] == ["msgbatch_1"] and st["pending"] is None)
        check("the adopted run submits nothing more", "0 page(s) not yet submitted" in out or "adopted" in out)
        check("the request names the edition and carries no licence wording",
              "ANSI X3.23-1985" in fake.created[0]["requests"][0]["params"]["messages"][0]["content"][1]["text"]
              and "licen" not in T.prompt_for(1985).lower())
        check("the request is Opus at medium effort", fake.created[0]["requests"][0]["params"]["model"] == "claude-opus-5-5"
              and fake.created[0]["requests"][0]["params"]["output_config"] == {"effort": "medium"})

        print("collect before and after the batch ends")
        code, out = run(["collect", *base, *key], fake, pdf)
        check("nothing is written while the batch runs", not list((state / "pages").glob("*.md")))
        fake.ended.add("msgbatch_1")
        fake.results_by_id["msgbatch_1"] = [page_ok(1, "alpha beta gamma\n" + STAMP + "\n"), page_errored(2)]
        code, out = run(["collect", *base, *key], fake, pdf)
        page1 = (state / "pages" / "p0001.md").read_text(encoding="utf-8")
        check("an ended batch's good page is written, the stamp line dropped", page1 == "alpha beta gamma\n")
        check("the errored page is not written", not (state / "pages" / "p0002.md").exists())
        check("collect names the bad page and the cost", "p0002" in out and "pages written 1/2" in out)
        check("pages are LF", b"\r" not in (state / "pages" / "p0001.md").read_bytes())
        before = page1
        fake.results_by_id["msgbatch_1"] = [page_ok(1, "CHANGED"), page_errored(2)]
        run(["collect", *base, *key], fake, pdf)
        check("a collected page is never overwritten by a re-run", (state / "pages" / "p0001.md").read_text(encoding="utf-8") == before)

        print("retry: errored, then cut off")
        code, out = run(["retry", *base, *key], fake, pdf)
        check("retry resubmits only the missing page", len(fake.created) == 2 and fake.created[1]["ids"] == ["p0002"])
        fake.ended.add("msgbatch_2")
        fake.results_by_id["msgbatch_2"] = [page_ok(2, "cut off text", stop="max_tokens")]
        run(["collect", *base, *key], fake, pdf)
        check("a cut-off reply is not written", not (state / "pages" / "p0002.md").exists())
        code, out = run(["retry", *base, *key], fake, pdf)
        check("a cut-off page is retried", len(fake.created) == 3 and fake.created[2]["ids"] == ["p0002"])
        fake.ended.add("msgbatch_3")
        fake.results_by_id["msgbatch_3"] = [page_ok(2, "delta epsilon zeta\n")]
        run(["collect", *base, *key], fake, pdf)
        check("the retried page is written", (state / "pages" / "p0002.md").read_text(encoding="utf-8") == "delta epsilon zeta\n")
        code, out = run(["retry", *base, *key], fake, pdf)
        check("nothing to retry once every page is written", len(fake.created) == 3 and "retrying 0" in out)

        print("pages: named pages again")
        code, out = run(["pages", "1", *base, *key], fake, pdf)
        check("a written page is not resubmitted without --replace", len(fake.created) == 3)
        code, out = run(["pages", "1", "--replace", *base, *key], fake, pdf)
        check("--replace submits the named page", len(fake.created) == 4 and fake.created[3]["ids"] == ["p0001"])
        fake.ended.add("msgbatch_4")
        fake.results_by_id["msgbatch_4"] = [page_ok(1, "alpha beta gamma, corrected\n")]
        run(["collect", *base, *key], fake, pdf)
        check("the replaced page holds the new text", (state / "pages" / "p0001.md").read_text(encoding="utf-8") == "alpha beta gamma, corrected\n")
        check("the old text is kept in orig/", (state / "orig" / "p0001.md").read_text(encoding="utf-8") == before)

        print("the key and the cost guard")
        check("the key is never printed", KEY not in out and KEY not in "".join(p.read_text(encoding="utf-8") for p in state.glob("*.json")))
        fresh = root / "fresh"
        fake2 = FakeBatches()
        code, out = run(["submit", "--edition", "1985", "--state", str(fresh), *key, "--max-usd", "0.001"], fake2, pdf)
        check("--max-usd refuses a dearer estimate and submits nothing", code not in (0, None) and not fake2.created)
        code, out = run(["submit", "--edition", "1985", "--state", str(fresh), "--api-key-file", str(root / "none.txt")], fake2, pdf)
        check("a missing key file is refused naming the path", code not in (0, None) and not fake2.created)
        code, out = run(["submit", "--edition", "1985", "--state", str(fresh)], fake2, pdf)
        check("a network command without a key file is refused", code not in (0, None) and not fake2.created)
        print("a key file of labeled keys: --api-account (kb/Work PB4916)")
        pair = root / "pair.txt"
        pair.write_text(f"account1 (planted)\n{KEY}\n\naccount2 (planted)\n{KEY2}\n", encoding="utf-8", newline="\n")
        billed = root / "billed"
        fake4 = FakeBatches()
        acct = ["--edition", "1985", "--state", str(billed), "--api-key-file", str(pair)]
        code, out = run(["submit", *acct], fake4, pdf, key_wanted=KEY2)
        check("a labeled pair with no --api-account is refused before any batch", code not in (0, None) and not fake4.created)
        code, out = run(["submit", *acct, "--api-account", "account2"], fake4, pdf, key_wanted=KEY2)
        st = json.loads((billed / "state.json").read_text(encoding="utf-8"))
        check("--api-account bills its key and the batch records the account's NAME, never a key",
              code == 0 and len(fake4.created) == 1 and [b["api_account"] for b in st["batches"]] == ["account2"]
              and "api_account" not in st and KEY2 not in (billed / "state.json").read_text(encoding="utf-8"))
        code, out = run(["collect", *acct], fake4, pdf, key_wanted=KEY2)
        check("a later command with no --api-account bills the account of the run's newest batch, and status names it",
              code == 0 and "the account account2" in out)
        code, out = run(["collect", *acct, "--api-account", "account1"], fake4, pdf, key_wanted=KEY)
        check("while a batch is uncollected, a command naming another account is refused, naming no key",
              code not in (0, None) and KEY not in str(code) and KEY2 not in str(code))
        print("one run over two accounts' credit: another account once every batch is collected (kb/Work PB4995)")
        fake4.ended.add("msgbatch_1")
        fake4.results_by_id["msgbatch_1"] = [page_ok(1, "alpha beta gamma\n"), page_errored(2)]
        code, out = run(["collect", *acct], fake4, pdf, key_wanted=KEY2)
        # account1's server: it holds none of account2's batches, so retrieving one would fail
        fake5 = FakeBatches(prefix="msgbatch_one_")
        code, out = run(["retry", *acct, "--api-account", "account1"], fake5, pdf, key_wanted=KEY)
        st = json.loads((billed / "state.json").read_text(encoding="utf-8"))
        check("once every batch is collected, a retry bills another account, and its batch records that account",
              code == 0 and len(fake5.created) == 1 and [b["api_account"] for b in st["batches"]] == ["account2", "account1"])
        fake5.ended.add("msgbatch_one_1")
        fake5.results_by_id["msgbatch_one_1"] = [page_ok(2, "delta epsilon zeta\n")]
        code, out = run(["collect", *acct], fake5, pdf, key_wanted=KEY)
        check("a later command with no --api-account bills the newest batch's account and never retrieves another "
              "account's collected batch", code == 0 and "pages written 2/2" in out)
        check("status prints each account's spend", "the account account1 $" in out and "the account account2 $" in out)
        code, out = run(["collect", *acct, "--api-account", "account2"], fake5, pdf, key_wanted=KEY2)
        check("with every batch collected, the earlier account may bill again", code == 0)
        crashed = root / "crashed"
        fake6 = FakeBatches()
        fake6.crash_after_create = True
        cacct = ["--edition", "1985", "--state", str(crashed), "--api-key-file", str(pair)]
        code, out = run(["submit", *cacct, "--api-account", "account2"], fake6, pdf, key_wanted=KEY2)
        code, out = run(["collect", *cacct], fake6, pdf, key_wanted=KEY2)
        st = json.loads((crashed / "state.json").read_text(encoding="utf-8"))
        check("a first submit that died before its batch was recorded: a later command with no --api-account bills the "
              "intent's account and adopts the batch", code == 0 and [b["api_account"] for b in st["batches"]] == ["account2"])
        code, out = run(["status", "--edition", "1985", "--state", str(billed), "--api-account", "account2"], fake4, pdf)
        check("--api-account without --api-key-file is refused", code not in (0, None) and "give the key file" in str(code))
        check("the estimate is pages x the measured tokens at the batch rate", abs(T.estimate_usd(976, "claude-opus-5-5") - 976 * 0.02104) < 0.5)

        print("adopting a run that already exists")
        old = root / "old"
        (old / "pages").mkdir(parents=True)
        (old / "pages" / "p0001.md").write_text("alpha beta gamma\n", encoding="utf-8", newline="\n")
        (old / "pages" / "p0002.md").write_text("delta epsilon zeta\n" + STAMP + "\n", encoding="utf-8", newline="\n")
        (old / "state.json").write_text(json.dumps({"model": "claude-opus-5-5", "pages": 2, "pending": None, "usage": {"msgbatch_old/p0001": [5000, 900]},
                                                   "batches": [{"id": "msgbatch_old", "pages": [1, 2], "api_account": None, "collected": True, "bad": []}]}), encoding="utf-8", newline="\n")
        (old / "local.json").write_text(json.dumps({"p0002": {"model": "m", "digest": "d"}}), encoding="utf-8", newline="\n")
        fake3 = FakeBatches()
        code, out = run(["status", "--edition", "1985", "--state", str(old)], fake3, pdf)
        check("status of an adopted run calls no API and counts the local page", code == 0 and "pages written 2/2; local 1" in out and not fake3.created)
        code, out = run(["submit", "--edition", "1985", "--state", str(old), *key], fake3, pdf)
        check("an adopted run submits no page it already holds", not fake3.created and "0 page(s) not yet submitted" in out)
        text, problems = T.assemble(T.Run(old, 1985), 2, parts)
        check("assembly drops the stamp line of an adopted raw page", problems == [] and STAMP not in text)

        print("assembly")
        check("a part heading opens each part and a page heading each page",
              text == "# Front matter\n\n## Page 1\n\nalpha beta gamma\n\n# SECTION A: SECOND\n\n## Page 2\n\ndelta epsilon zeta\n\n")
        code, out = run(["assemble", *base, "--out", str(root / "out.md")], fake, pdf, parts)
        check("assemble writes the Markdown with LF only", code == 0 and b"\r" not in (root / "out.md").read_bytes() and "2 pages" in out)
        reconciled = root / "reconciled.md"
        reconciled.write_text("# Preface\n\nthe reconciled text\n", encoding="utf-8", newline="\n")
        saved_markdown = T.spec_pdf.markdown
        T.spec_pdf.markdown = lambda edition, repo=None: reconciled
        try:
            code, out = run(["assemble", *base], fake, pdf, parts)
        finally:
            T.spec_pdf.markdown = saved_markdown
        check("assemble never overwrites the reconciled transcription: without --out it refuses (kb/Work PB4850)",
              code == 1 and "--out" in out and reconciled.read_text(encoding="utf-8") == "# Preface\n\nthe reconciled text\n")
        (state / "pages" / "p0002.md").unlink()
        code, out = run(["assemble", *base, "--out", str(root / "out2.md")], fake, pdf, parts)
        check("a missing page refuses the assembly", code == 1 and "missing pages: [2]" in out and not (root / "out2.md").exists())
        (state / "pages" / "p0002.md").write_text("I cannot reproduce this page.\n", encoding="utf-8", newline="\n")
        code, out = run(["assemble", *base, "--out", str(root / "out3.md")], fake, pdf, parts)
        check("a refusal instead of a transcription refuses the assembly", code == 1 and "refusal" in out)
        pages_ok = T.Run(root / "scratch", 1985)
        check("a page that is only a stamp is not written", not pages_ok.put(1, STAMP + "\n") and not pages_ok.page_file(1).exists())
        check("the shipped parts ascend from page 1", T.PARTS[1985][0][0] == 1 and [p for p, _ in T.PARTS[1985]] == sorted(p for p, _ in T.PARTS[1985]))

        print("the local backend")
        calls: list = []

        def post(path, body):
            calls.append(path)
            if path == "/api/tags":
                return {"models": [{"name": "planted:1b", "digest": "abc123"}]}
            return {"done_reason": "stop", "message": {"content": "```markdown\nalpha beta gamma\n```"}, "prompt_eval_count": 11, "eval_count": 7}

        loc = T.Run(root / "loc", 1985)
        T.local_pages(loc, pdf, "planted:1b", [1], False, post=post)
        log = loc.local_log()
        check("the local page is unfenced", loc.page_file(1).read_text(encoding="utf-8") == "alpha beta gamma\n")
        check("provenance is recorded per page", log["p0001"]["model"] == "planted:1b" and log["p0001"]["digest"] == "abc123" and log["p0001"]["output_tokens"] == 7)
        n = len(calls)
        T.local_pages(loc, pdf, "planted:1b", [1], False, post=post)
        check("a written local page is skipped", calls.count("/api/chat") == 1 and len(calls) == n + 1)

        def cut(path, body):
            return post(path, body) if path == "/api/tags" else {"done_reason": "length", "message": {"content": "partial"}}

        T.local_pages(loc, pdf, "planted:1b", [2], False, post=cut)
        check("a reply that did not stop is not written", not loc.page_file(2).exists())

        print("the local backend survives errors (PB4912)")
        pdf3 = root / "planted3.pdf"
        plant_pdf(pdf3, ["alpha beta gamma", "delta epsilon zeta", "eta theta iota"])
        page_of = {base64.standard_b64encode(T.render_png(pdf3, n)).decode(): n for n in (1, 2, 3)}
        tries: dict[int, int] = {}
        sleeps: list[float] = []

        def http500():
            return urllib.error.HTTPError("http://x/api/chat", 500, "Internal Server Error", None, None)

        def flaky(path, body):
            """Page 1 fails once (a model still loading), page 2 fails every time, page 3 never."""
            if path == "/api/tags":
                return post(path, body)
            n = page_of[body["messages"][0]["images"][0]]
            tries[n] = tries.get(n, 0) + 1
            if n == 2 or (n == 1 and tries[n] == 1):
                raise http500()
            return post(path, body)

        flk = T.Run(root / "flaky", 1985)
        out = io.StringIO()
        with contextlib.redirect_stdout(out):
            failed = T.local_pages(flk, pdf3, "planted:1b", [1, 2, 3], False, post=flaky, sleep=sleeps.append)
        check("a failing request is retried and the page is then written", tries[1] == 2 and flk.page_file(1).exists())
        check("a pause separates the tries", sleeps == [T.LOCAL_RETRY_SECONDS] * (1 + T.LOCAL_TRIES - 1))
        check("a page that fails every try is tried LOCAL_TRIES times, not written, and returned",
              tries[2] == T.LOCAL_TRIES and not flk.page_file(2).exists() and failed == [2])
        check("the run goes on to the page after the failed one", flk.page_file(3).exists() and "p0003: written" in out.getvalue())
        flog = flk.local_log()
        check("the failure is recorded with the error and the model",
              flog["p0002"]["model"] == "planted:1b" and "HTTP Error 500" in flog["p0002"]["error"] and flk.local_failed() == [2])
        check("the written pages keep their provenance", flog["p0001"]["digest"] == "abc123" and "error" not in flog["p0003"])
        out = io.StringIO()
        with contextlib.redirect_stdout(out):
            T.print_status(flk, flk.batches.load(pages=3))
        check("status counts the written local pages and lists the failed one",
              "local 2;" in out.getvalue() and "local conversions failed 1: 2 (planted:1b: HTTPError" in out.getvalue())
        T.local_pages(flk, pdf3, "planted:2b", [2], False, post=post, sleep=sleeps.append)
        check("another model retries the failed page, and its record becomes the success",
              flk.page_file(2).exists() and flk.local_failed() == [] and flk.local_log()["p0002"]["model"] == "planted:2b")

        real_local = T.local_pages
        try:
            for what, state_dir, backend, want in (("the exit code is nonzero when a page failed", "flaky2", flaky, 1),
                                                   ("the exit code is zero when none did", "flaky3", post, 0)):
                tries.clear()
                T.local_pages = lambda r, pf, m, pg, rp, backend=backend: real_local(r, pf, m, pg, rp, post=backend, sleep=lambda s: None)
                with contextlib.redirect_stdout(io.StringIO()):
                    code = T.main(["pages", "1", "2", "3", "--edition", "1985", "--state", str(root / state_dir), "--local", "planted:1b"],
                                  pdf_override=pdf3)
                check(what, code == want)
        finally:
            T.local_pages = real_local

        def torn(path, body):
            if path == "/api/tags":
                return post(path, body)
            raise http.client.IncompleteRead(b"")

        def garbled(path, body):
            if path == "/api/tags":
                return post(path, body)
            return json.loads("<html>")

        for what, backend in (("a torn reply", torn), ("a reply that is not JSON", garbled)):
            with contextlib.redirect_stdout(io.StringIO()):
                got = T.local_pages(T.Run(root / "bad", 1985), pdf3, "planted:1b", [1], False, post=backend, sleep=lambda s: None)
            check(f"{what} is a failed page, not a crash", got == [1])

        always500 = lambda path, body: post(path, body) if path == "/api/tags" else (_ for _ in ()).throw(http500())
        before = dict(flk.local_log()["p0001"])
        text_before = flk.page_file(1).read_text(encoding="utf-8")
        with contextlib.redirect_stdout(io.StringIO()):
            got = T.local_pages(flk, pdf3, "planted:3b", [1], True, post=always500, sleep=lambda s: None)
        check("a failed --replace is returned but leaves the written page and its provenance untouched",
              got == [1] and flk.local_log()["p0001"] == before and flk.page_file(1).read_text(encoding="utf-8") == text_before
              and 1 not in flk.local_failed())

        print("a reply that is not a transcription is never written (PB4917)")
        printed = ("The MOVE statement transfers data, in accordance with the rules of editing, to one or more data areas. "
                   "When a sending and a receiving item share a part of their storage areas, the result of the execution of "
                   "such a statement is undefined. Any subscripting associated with the receiving identifier is evaluated "
                   "immediately before the data is moved to the respective data item.")
        pdf4 = root / "planted4.pdf"
        plant_pdf(pdf4, [printed, printed, printed, printed])
        refusal = ("I can't transcribe this page verbatim, because of the notice printed on it. In its place, the page "
                   "explains how MOVE copies data to receiving items and when the outcome is undefined.\n")
        partial = printed[:120] + "\n\nI'm unable to reproduce the remainder of this page.\n"
        paraphrase = ("This part of the standard says that MOVE copies a value into receiving items, that overlapping "
                      "storage gives an unspecified outcome, and that subscripts are worked out before copying.\n")
        judged = root / "judged"
        jb = ["--edition", "1985", "--state", str(judged), *key]
        fake5 = FakeBatches()
        run(["submit", *jb], fake5, pdf4)
        fake5.ended.add("msgbatch_1")
        fake5.results_by_id["msgbatch_1"] = [page_ok(1, refusal), page_ok(2, partial), page_ok(3, printed + "\n"), page_ok(4, paraphrase)]
        code, out = run(["collect", *jb], fake5, pdf4)
        st = json.loads((judged / "state.json").read_text(encoding="utf-8"))
        check("a refusal followed by a summary is not written", not (judged / "pages" / "p0001.md").exists())
        check("a polite partial reply is not written", not (judged / "pages" / "p0002.md").exists())
        check("a correct reply is written", (judged / "pages" / "p0003.md").read_text(encoding="utf-8") == printed + "\n")
        check("a paraphrase with no refusal sentence is caught by the word-agreement arm",
              not (judged / "pages" / "p0004.md").exists() and "word" in st["failed"]["p0004"]["error"]
              and "refusal" not in st["failed"]["p0004"]["error"])
        check("each refused page is recorded with its reason, the model and the batch",
              sorted(st["failed"]) == ["p0001", "p0002", "p0004"] and "refusal" in st["failed"]["p0001"]["error"]
              and st["failed"]["p0002"]["model"] == "claude-opus-5-5" and st["failed"]["p0001"]["batch"] == "msgbatch_1")
        check("collect reports the refused pages as bad, and status lists them",
              "p0001" in out and "batch replies refused 3" in out and "1 (claude-opus-5-5: a refusal" in out)
        check("a refused reply is still counted in the cost", sorted(st["usage"]) == [f"msgbatch_1/p000{n}" for n in (1, 2, 3, 4)])
        code, out = run(["retry", *jb], fake5, pdf4)
        check("retry resubmits the refused pages", fake5.created[-1]["ids"] == ["p0001", "p0002", "p0004"])
        fake5.ended.add("msgbatch_2")
        fake5.results_by_id["msgbatch_2"] = [page_ok(1, printed + "\n"), page_ok(2, partial), page_ok(4, printed + "\n")]
        code, out = run(["collect", *jb], fake5, pdf4)
        st = json.loads((judged / "state.json").read_text(encoding="utf-8"))
        check("a later good reply is written and leaves the failed record", sorted(st["failed"]) == ["p0002"]
              and (judged / "pages" / "p0001.md").exists() and "batch replies refused 1" in out)
        refused_local = T.Run(root / "judged-local", 1985)
        with contextlib.redirect_stdout(io.StringIO()):
            got = T.local_pages(refused_local, pdf4, "planted:1b", [1, 3], False, sleep=lambda s: None,
                                post=lambda path, body: post(path, body) if path == "/api/tags" else
                                {"done_reason": "stop", "message": {"content": refusal if body["messages"][0]["images"][0] == base64.standard_b64encode(T.render_png(pdf4, 1)).decode() else printed}})
        check("the local backend records a refusal as a failed page with its reason and writes the good one",
              got == [1] and not refused_local.page_file(1).exists() and refused_local.page_file(3).exists()
              and "refusal" in refused_local.local_log()["p0001"]["error"])
        check("the standard's own 'In summary' is not a refusal shape", T.judge("In summary, for such cases as these.", None, 1985) is None)
        check("a page with a short text layer is not judged by agreement", T.judge("other words entirely", "alpha beta gamma", 1985) is None)
        plant_pdf(pdf, ["alpha beta gamma", "delta epsilon zeta"])  # the cases below read the first planted PDF again

        print("the text-layer backend (PB4813)")
        L = T.text_layer

        def row(text, x, y, size=9.0, font="Univers"):
            return [L.Span(text, font, size, x, y, x + 0.5 * size * len(text), y + size)]

        bold = "Univers-Bold"
        layouts = {
            1: L.Layout(612, 792, (), (), 1, ()),
            2: L.Layout(612, 792, (*row("7.1.2 COPY statement", 56.7, 84, 10, bold), *row("The COPY statement copies.", 56.7, 106),
                                   *row("7.1.2.1 General format", 56.7, 700, 10, bold), *row("COPY", 72, 720),
                                   *row(STAMP, 56.7, 770, 7, "Helvetica"), *row("32", 56.7, 756))),
            3: L.Layout(612, 792, (*row("text-name-1", 110, 90), *row("7.1.2.2 Syntax rules", 56.7, 200, 10, bold),
                                   *row("1)", 56.7, 222), *row("A rule.", 74.7, 222))),
        }
        opened: list = []

        @contextlib.contextmanager
        def opener(path):
            opened.append(path)
            yield "planted"

        reader = lambda doc, pno: layouts[pno]
        drawn_rows: list = []

        def drawer(doc, pno, rows, edition):   # the vector arm, planted: it draws page 2's figure and refuses page 3's
            drawn_rows.append((pno, rows, edition))
            return None if pno == 3 else ['<pre style="line-height:1">', "<u>COPY</u> text-name-1", "</pre>"]

        tl = T.Run(root / "text", 2002)
        out = io.StringIO()
        with contextlib.redirect_stdout(out):
            failed = T.text_pages(tl, pdf, [1, 2, 3], False, read=reader, opener=opener, drawer=drawer)
        tlog = tl.local_log()
        check("an image page is not written, is recorded as a failed text-layer page, and is returned",
              failed == [1] and not tl.page_file(1).exists() and tlog["p0001"]["model"] == T.TEXT_LAYER and "image" in tlog["p0001"]["error"])
        p2 = tl.page_file(2).read_text(encoding="utf-8")
        check("a text-layer page is written from the layer: heading, prose, the format DRAWN by the vector arm, no stamp",
              p2.startswith("##### 7.1.2 COPY statement\n\nThe COPY statement copies.\n\n###### 7.1.2.1 General format\n\n"
                            '<pre style="line-height:1">\n<u>COPY</u> text-name-1\n</pre>')
              and L.FIGURE_MARK not in p2 and "store order" not in p2 and b"\r" not in tl.page_file(2).read_bytes())
        check("the vector arm is asked for each figure by its rows' tops on its page, in the run's edition",
              drawn_rows[:2] == [(2, (720.0, 720.0), 2002), (3, (90.0, 90.0), 2002)])
        check("the clause open at a page's end carries to the next page, and a figure the arm refuses stays a placeholder",
              tl.page_file(3).read_text(encoding="utf-8").startswith(L.FIGURE_MARK) and "1\\) A rule." in tl.page_file(3).read_text(encoding="utf-8"))
        check("each page's provenance names the backend, its folio, its figures, those undrawn and what was ambiguous",
              tlog["p0002"] == {**tlog["p0002"], "model": T.TEXT_LAYER, "folio": "32", "figures": 1, "undrawn": 0, "ambiguous": []}
              and tlog["p0003"]["figures"] == 1 and tlog["p0003"]["undrawn"] == 1)
        check("the PDF is opened once for the run", len(opened) == 1)
        tl.page_file(3).write_text("the canary's page\n", encoding="utf-8", newline="\n")
        with contextlib.redirect_stdout(io.StringIO()):
            T.text_pages(tl, pdf, [3], False, read=reader, opener=opener, drawer=drawer)
        check("a written page is kept without --replace", tl.page_file(3).read_text(encoding="utf-8") == "the canary's page\n")
        with contextlib.redirect_stdout(io.StringIO()):
            T.text_pages(tl, pdf, [3], True, read=reader, opener=opener, drawer=drawer)
        check("--replace on one page learns the open clause from the page before it, and keeps the old page in orig/",
              tl.page_file(3).read_text(encoding="utf-8").startswith(L.FIGURE_MARK)
              and (root / "text" / "orig" / "p0003.md").read_text(encoding="utf-8") == "the canary's page\n")
        out = io.StringIO()
        with contextlib.redirect_stdout(out):
            T.print_status(tl, tl.batches.load(pages=3))
        check("status counts the text-layer pages, their general formats and those the vector arm has not drawn",
              "text layer: 2 page(s), 2 general format(s), 1 not drawn by the vector arm on 1 page(s) [3]" in out.getvalue()
              and "local conversions failed 1: 1 (text-layer:" in out.getvalue())
        tl.page_file(1).write_text("cover\n", encoding="utf-8", newline="\n")
        text, problems = T.assemble(tl, 3, [(1, "Front matter")])
        check("the assembly refuses a page whose general format is not yet drawn",
              any("p0003: 1 general format(s) not yet drawn" in p for p in problems)
              and not any("p0002: 1 general format(s)" in p for p in problems))
        real_text = T.text_pages
        try:
            T.text_pages = lambda r, pf, pg, rp: real_text(r, pf, pg, rp, read=reader, opener=opener, drawer=drawer)
            with contextlib.redirect_stdout(io.StringIO()):
                code = T.main(["text", "1-2", "--edition", "2002", "--state", str(root / "text2")], pdf_override=pdf)
            check("`text` exits nonzero when a page failed, and calls no API", code == 1 and (root / "text2" / "pages" / "p0002.md").exists())
        finally:
            T.text_pages = real_text

        print("check and agreement")
        recall, precision, md_words, layer_words = T.agreement("alpha beta gamma\n" + STAMP, "alpha beta gamma delta", 1985)
        check("recall counts layer words found, precision markdown words found",
              abs(recall - 1.0) < 1e-9 and abs(precision - 0.75) < 1e-9 and md_words == 4 and layer_words == 3)
        out = io.StringIO()
        with contextlib.redirect_stdout(out):
            T.check_pages(T.Run(old, 1985), pdf, [1])
        check("check prints numbers and no page text", "recall 1.000" in out.getvalue() and "alpha" not in out.getvalue())
        check("pages parse as numbers and ranges", T.parse_pages(["3", "5-7", "3"], 9) == [3, 5, 6, 7])
        try:
            T.parse_pages(["10"], 9)
            check("a page outside the document is refused", False)
        except SystemExit:
            check("a page outside the document is refused", True)
        # 2002 reads its own stamp row (kb/Work PB4813): an invented stamp of its shape, assembled from pieces so the tree
        # check never reads it whole, is dropped from a page and the page's text is kept.
        stamp_2002 = ("Copyright by INCITS. Licensed to " + "Ann Example.  ANSI store order # X_" + "9876543. Downloaded "
                      + "1/2/" + "2026.  Single user license only. Copying and networking prohibited.")
        check("2002 drops its stamp line and keeps the page",
              T.clean_page("Level numbers\n" + stamp_2002 + "\n", 2002) == ("Level numbers\n", 1))
        saved = T.verify_publishable.FORBIDDEN
        T.verify_publishable.FORBIDDEN = {e: rows for e, rows in saved.items() if e != 2002}
        try:
            T.stamp_patterns(2002)
            check("an edition without stamp shapes exits naming the table", False)
        except SystemExit as ex:
            check("an edition without stamp shapes exits naming the table", "verify_publishable.FORBIDDEN" in str(ex.code))
        finally:
            T.verify_publishable.FORBIDDEN = saved

    print(f"=== TRANSCRIBE_PDF SELF-TEST: {'GREEN' if not failures else 'RED - ' + ', '.join(failures)} ===")
    return 1 if failures else 0

if __name__ == "__main__":
    ensure_utf8()
    sys.exit(main())
