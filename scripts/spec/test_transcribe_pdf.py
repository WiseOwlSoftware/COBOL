#!/usr/bin/env python3
"""Self-test for transcribe_pdf.py (kb/Work PB4816): a planted 2-page PDF and a FAKE Message Batches client. It makes no API
call and opens no licensed PDF. The stamp shapes it plants are built from pieces, so no tracked file carries one.
Run: python scripts/spec/test_transcribe_pdf.py   (every gate and CI run it through scripts/self_tests.py.)

Cases: submit; a crash between `create` and the state write, adopted on the next run; collect before and after the batch
ends; an errored page and a cut-off page retried; a written page never overwritten, and replaced only by `--replace` (the
old text kept); the stamp line dropped; the cost guard; the key never printed or stored; the local backend; adoption of a
run that already exists; the check numbers; the assembly and its refusals.
"""
import contextlib
import datetime
import io
import json
import pathlib
import sys
import tempfile
from types import SimpleNamespace

import pymupdf

sys.path.insert(0, str(pathlib.Path(__file__).resolve().parent))
import transcribe_pdf as T  # noqa: E402
sys.path.append(str(pathlib.Path(__file__).resolve().parents[1]))  # scripts/, for standard_streams
from standard_streams import ensure_utf8  # noqa: E402

KEY = "sk-planted-secret-key"
#: A planted stamp line, built from pieces: the repository's own stamp audit would refuse the whole shape in this file.
STAMP = "Copyright by the publisher. store order " + chr(35) + " X_" + "9999999. Downloaded today"
failures: list[str] = []


def check(name: str, cond: bool) -> None:
    print(f"  {'ok  ' if cond else 'FAIL'} {name}")
    if not cond:
        failures.append(name)


class Crash(Exception):
    """A process death between the server accepting a batch and the state file recording it."""


class FakeBatches:
    def __init__(self) -> None:
        self.created: list[dict] = []      # server side: every accepted batch
        self.results_by_id: dict[str, list] = {}
        self.crash_after_create = False
        self.ended: set[str] = set()

    def create(self, requests):
        bid = f"msgbatch_{len(self.created) + 1}"
        self.created.append({"id": bid, "ids": [r["custom_id"] for r in requests], "requests": requests, "at": datetime.datetime.now(datetime.timezone.utc)})
        if self.crash_after_create:
            self.crash_after_create = False
            raise Crash
        return self._view(bid)

    def _view(self, bid):
        b = next(x for x in self.created if x["id"] == bid)
        n = len(b["ids"])
        ended = bid in self.ended
        counts = SimpleNamespace(processing=0 if ended else n, succeeded=n if ended else 0, errored=0, canceled=0, expired=0)
        return SimpleNamespace(id=bid, processing_status="ended" if ended else "in_progress", request_counts=counts, created_at=b["at"])

    def retrieve(self, bid):
        return self._view(bid)

    def list(self, limit):
        return [self._view(b["id"]) for b in reversed(self.created)][:limit]

    def results(self, bid):
        return iter(self.results_by_id[bid])


def message(text, stop="end_turn"):
    return SimpleNamespace(stop_reason=stop, content=[SimpleNamespace(type="text", text=text)],
                           usage=SimpleNamespace(input_tokens=5000, output_tokens=900))


def ok(pno, text, stop="end_turn"):
    return SimpleNamespace(custom_id=T.page_name(pno), result=SimpleNamespace(type="succeeded", message=message(text, stop)))


def errored(pno):
    return SimpleNamespace(custom_id=T.page_name(pno), result=SimpleNamespace(type="errored"))


def plant_pdf(path: pathlib.Path, texts: list[str]) -> None:
    doc = pymupdf.open()
    for t in texts:
        doc.new_page(width=300, height=400).insert_text((40, 80), t, fontsize=14)
    doc.save(path)
    doc.close()


def run(argv, fake, pdf, parts=None):
    out = io.StringIO()
    code = None
    with contextlib.redirect_stdout(out):
        try:
            code = T.main(argv, client_factory=lambda key: SimpleNamespace(messages=SimpleNamespace(batches=fake)) if key == KEY else None,
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
        fake.results_by_id["msgbatch_1"] = [ok(1, "alpha beta gamma\n" + STAMP + "\n"), errored(2)]
        code, out = run(["collect", *base, *key], fake, pdf)
        page1 = (state / "pages" / "p0001.md").read_text(encoding="utf-8")
        check("an ended batch's good page is written, the stamp line dropped", page1 == "alpha beta gamma\n")
        check("the errored page is not written", not (state / "pages" / "p0002.md").exists())
        check("collect names the bad page and the cost", "p0002" in out and "pages written 1/2" in out)
        check("pages are LF", b"\r" not in (state / "pages" / "p0001.md").read_bytes())
        before = page1
        fake.results_by_id["msgbatch_1"] = [ok(1, "CHANGED"), errored(2)]
        run(["collect", *base, *key], fake, pdf)
        check("a collected page is never overwritten by a re-run", (state / "pages" / "p0001.md").read_text(encoding="utf-8") == before)

        print("retry: errored, then cut off")
        code, out = run(["retry", *base, *key], fake, pdf)
        check("retry resubmits only the missing page", len(fake.created) == 2 and fake.created[1]["ids"] == ["p0002"])
        fake.ended.add("msgbatch_2")
        fake.results_by_id["msgbatch_2"] = [ok(2, "cut off text", stop="max_tokens")]
        run(["collect", *base, *key], fake, pdf)
        check("a cut-off reply is not written", not (state / "pages" / "p0002.md").exists())
        code, out = run(["retry", *base, *key], fake, pdf)
        check("a cut-off page is retried", len(fake.created) == 3 and fake.created[2]["ids"] == ["p0002"])
        fake.ended.add("msgbatch_3")
        fake.results_by_id["msgbatch_3"] = [ok(2, "delta epsilon zeta\n")]
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
        fake.results_by_id["msgbatch_4"] = [ok(1, "alpha beta gamma, corrected\n")]
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
        check("the estimate is pages x the measured tokens at the batch rate", abs(T.estimate_usd(976, "claude-opus-5-5") - 976 * 0.02104) < 0.5)

        print("adopting a run that already exists")
        old = root / "old"
        (old / "pages").mkdir(parents=True)
        (old / "pages" / "p0001.md").write_text("alpha beta gamma\n", encoding="utf-8", newline="\n")
        (old / "pages" / "p0002.md").write_text("delta epsilon zeta\n" + STAMP + "\n", encoding="utf-8", newline="\n")
        (old / "state.json").write_text(json.dumps({"model": "claude-opus-5-5", "pages": 2, "pending": None, "usage": {"p0001": [5000, 900]},
                                                   "batches": [{"id": "msgbatch_old", "pages": [1, 2], "collected": True, "bad": []}]}), encoding="utf-8", newline="\n")
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
        try:
            T.stamp_patterns(2002)
            check("an edition without stamp shapes exits naming the table", False)
        except SystemExit:
            check("an edition without stamp shapes exits naming the table", True)

    print(f"=== TRANSCRIBE_PDF SELF-TEST: {'GREEN' if not failures else 'RED - ' + ', '.join(failures)} ===")
    return 1 if failures else 0


if __name__ == "__main__":
    ensure_utf8()
    sys.exit(main())
