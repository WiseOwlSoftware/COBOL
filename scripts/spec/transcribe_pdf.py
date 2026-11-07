#!/usr/bin/env python3
"""transcribe_pdf.py - THE converter of a COBOL standard's PDF to Markdown (kb/Work PB4816; PB4812 and PB4813 run it).

One restartable, edition-aware tool replaces the five scripts of the 2023 conversion (convert_pdf, fix_pages, retry_failed,
build_links, validate_doc), which had a retired checkout's paths, a retired model and one synchronous call per page.
`--edition {1985,2002,2023}` names the PDF and the Markdown (spec_pdf.EDITIONS); `--state DIR` holds the run.

    python scripts/spec/transcribe_pdf.py submit   --edition 1985 --state DIR --api-key-file KEY [--max-usd N]
    python scripts/spec/transcribe_pdf.py status   --edition 1985 --state DIR [--api-key-file KEY]
    python scripts/spec/transcribe_pdf.py collect  --edition 1985 --state DIR --api-key-file KEY
    python scripts/spec/transcribe_pdf.py retry    --edition 1985 --state DIR --api-key-file KEY
    python scripts/spec/transcribe_pdf.py pages 12 40-44 --edition 1985 --state DIR --api-key-file KEY [--replace]
    python scripts/spec/transcribe_pdf.py pages 4 122 --edition 1985 --state DIR --local qwen3.6:35b [--replace]
    python scripts/spec/transcribe_pdf.py check    --edition 1985 --state DIR 4 122      # word agreement, numbers only
    python scripts/spec/transcribe_pdf.py assemble --edition 1985 --state DIR           # the pages -> specs/<edition>.md
    python scripts/spec/test_transcribe_pdf.py                                          # its self-test: planted PDF, fake client, no API call

RESTARTABLE WITH MINIMAL LOSS. Every subcommand can be re-run at any time. The state file (`state.json`) is written
atomically (a temporary file, then a rename) BEFORE a batch is created (the intent: when, which pages) and AFTER (the
batch's id), so a crash between the two is reconciled from the server's batch list on the next run (`adopt_pending`); a
page is written once (`pages/pNNNN.md`, atomically) and never overwritten except by an explicit `pages ... --replace`,
which first keeps the old text in `orig/`. `collect` takes each batch as soon as it has ENDED; `retry` resubmits the pages
that errored, expired, were cut off (a `stop_reason` other than `end_turn`) or are missing. The SDK client retries network
errors (`CLIENT_RETRIES`). A run that already exists (state.json, local.json, pages/) is ADOPTED as it stands: no page it
holds is ever resubmitted, and `status`, `check` and `assemble` call no API.

THE KEY is the first non-empty line of the file `--api-key-file` (api_key.read_api_key); it goes to the SDK client only and
is never printed, logged or written to state.

LOCAL BACKEND. `pages ... --local MODEL` sends the same render and prompt to a local vision model (Ollama) and records the
model, digest, time and token counts per page in `local.json`; the model's outer code fence is removed. A request that
fails (HTTP 500, a dropped connection, a timeout) is tried `LOCAL_TRIES` times with a pause; a page that fails them all is
recorded in `local.json` with the error and the model, the run goes on, `status` lists the failed pages and the exit code is
nonzero. `pages <list> --local ANOTHER` retries them. `check` compares a
page with the PDF's text layer (word recall and precision) and prints numbers only, never page text.

THE STAMP. Every page of a bought PDF carries the reseller's per-copy stamp, which no tracked file may carry
(verify_publishable). The prompt asks the model to omit it; every page written, and every page assembled, has any line that
matches the edition's stamp shapes (verify_publishable.FORBIDDEN, TREE_FORBIDDEN) dropped, and a page that still matches is
not written. The state directory is private (it holds the raw pages); only the assembly is tracked.

ASSEMBLY. The pages become the edition's Markdown: a `# ` heading opens each PART (front matter, the Sections of the
standard, an amendment: `PARTS`), a `## Page N` heading opens each PDF page, and the page text follows as converted: the
general formats keep the drawn figures the model produced (the figure tools cross-check them). The assembly refuses a
missing page, an empty page, a refusal or summary in place of a page, and a stamp.

COST. `submit --max-usd N` refuses a batch whose estimate (pages x the measured tokens per page, at the model's batch
rate) exceeds N.
"""
from __future__ import annotations

import argparse
import base64
import collections
import datetime
import http.client
import json
import pathlib
import re
import sys
import time
import urllib.request
from typing import Any, Callable, Iterable

sys.path.insert(0, str(pathlib.Path(__file__).resolve().parent))
import spec_pdf  # noqa: E402  (the one edition table, kb/Work PB4815)
import verify_publishable  # noqa: E402  (the stamp shapes of each edition)
sys.path.append(str(pathlib.Path(__file__).resolve().parents[1]))  # scripts/
from api_key import read_api_key  # noqa: E402  (THE reader of an API key file, kb/Work PB4816)
from standard_streams import ensure_utf8  # noqa: E402

#: The model the pilot chose (2026-11-06): Sonnet 5.5 got the words right but drew general formats wrongly.
DEFAULT_MODEL = "claude-opus-5-5"
#: Standard (non-batch) US dollars per million tokens, (input, output), by model; a batch costs half.
STANDARD_PRICE_PER_MTOK: dict[str, tuple[float, float]] = {"claude-opus-5-5": (4.0, 20.0)}
BATCH_DISCOUNT = 0.5
#: Measured on the 1985 pilot: input tokens of one page image (2,576 px long edge, grayscale PNG), and the mean output.
PAGE_INPUT_TOKENS = 5240
PAGE_OUTPUT_TOKENS = 1050
IMAGE_LONG_EDGE = 2576
MAX_OUTPUT_TOKENS = 32000
#: One batch is capped at 256 MB by the API; the converter splits under this budget.
BATCH_BYTES = 180 * 1024 * 1024
#: The SDK retries a network error or a 5xx this many times, and waits this long for one reply.
CLIENT_RETRIES = 8
CLIENT_TIMEOUT_SECONDS = 900
OLLAMA = "http://127.0.0.1:11434"
#: A local request that fails (a model still loading, a memory error: HTTP 500, a dropped connection, a timeout) is tried this
#: many times in all, with this pause between tries; a page that fails them all is recorded in local.json and the run goes on.
LOCAL_TRIES = 3
LOCAL_RETRY_SECONDS = 20
#: What a failed request raises: urllib's URLError/HTTPError and socket timeouts are OSErrors, a torn reply an HTTPException,
#: a reply that is not JSON a ValueError.
LOCAL_ERRORS = (OSError, http.client.HTTPException, ValueError)

#: Each edition's name in the prompt (the model is told what it is transcribing).
EDITION_NAMES: dict[int, str] = {
    1985: "ANSI X3.23-1985, COBOL",
    2002: "ISO/IEC 1989:2002, COBOL",
    2023: "ISO/IEC 1989:2023, COBOL",
}

PROMPT = """Transcribe this page of a printed programming-language standard ({name}) to Markdown, exactly
and completely. Do not summarize, paraphrase, correct or omit anything; reproduce the standard's own wording, spelling and
defects as printed. Keep headings, numbered rules, paragraphs and tables (Markdown tables).
Omit the per-copy stamp printed in the page margin or footer (the reseller's order text) and nothing else.
GENERAL FORMATS (syntax diagrams) are the critical part. Draw each one inside <pre>...</pre> (never a fenced block):
- an UNDERLINED word (required keyword) is wrapped in <u>...</u>; a word not underlined stays plain;
- square brackets (optional) and braces (choose exactly one) are drawn with box-drawing glyphs spanning every line they
  enclose: brackets ┌ │ └ / ┐ │ ┘, braces ⎧ ⎨ ⎩ / ⎫ ⎬ ⎭ (one middle mark at the brace's point), with the alternatives
  stacked vertically exactly as printed;
- the choice indicators (vertical bars or pipes), ellipses (...) and lowercase operand names are kept where printed;
- keep vertical alignment as printed; escape < as &lt; and > as &gt;.
Output only the transcription."""

#: The PARTS of each edition's assembly: (first PDF page, heading). Measured from the 1985 PDF's pages (kb/Work PB4812): the
#: front matter, the eighteen Sections of the standard, then the two amendments, each with its own pages and index.
PARTS: dict[int, list[tuple[int, str]]] = {
    1985: [
        (1, "Front matter: title, foreword, preface and contents"),
        (21, "SECTION I: INTRODUCTORY INFORMATION"),
        (85, "SECTION II: CONCEPTS"),
        (121, "SECTION III: GLOSSARY"),
        (147, "SECTION IV: OVERALL LANGUAGE CONSIDERATION"),
        (193, "SECTION V: COMPOSITE LANGUAGE SKELETON"),
        (233, "SECTION VI: NUCLEUS MODULE"),
        (375, "SECTION VII: SEQUENTIAL I-O MODULE"),
        (431, "SECTION VIII: RELATIVE I-O MODULE"),
        (471, "SECTION IX: INDEXED I-O MODULE"),
        (515, "SECTION X: INTER-PROGRAM COMMUNICATION MODULE"),
        (553, "SECTION XI: SORT-MERGE MODULE"),
        (575, "SECTION XII: SOURCE TEXT MANIPULATION MODULE"),
        (583, "SECTION XIII: REPORT WRITER MODULE"),
        (659, "SECTION XIV: COMMUNICATION MODULE"),
        (689, "SECTION XV: DEBUG MODULE"),
        (699, "SECTION XVI: SEGMENTATION MODULE"),
        (709, "SECTION XVII: APPENDICES"),
        (807, "SECTION XVIII: INDEX"),
        (821, "AMENDMENT 1: INTRINSIC FUNCTION MODULE (ANSI X3.23a-1989)"),
        (907, "AMENDMENT 2: CORRECTION AMENDMENT (ANSI X3.23b-1993)"),
    ],
}

#: A model that declined or summarised instead of transcribing says so in one of these ways (validate_doc's list).
REFUSAL = re.compile(r"I've chosen to summarize|I cannot reproduce|copyrighted.*verbatim|rather than reproduc|Here is a summary of",
                     re.I)
FENCE = re.compile(r"\A\s*```[a-z]*[ \t]*\n(.*?)\n```\s*\Z", re.S)
WORD = re.compile(r"[A-Za-z]{3,}")


# -- pages --------------------------------------------------------------------------------------------------------------

def page_name(pno: int) -> str:
    return f"p{pno:04}"


def parse_pages(items: Iterable[str], count: int) -> list[int]:
    """`12 40-44` -> [12, 40, 41, 42, 43, 44]; a page outside 1..count is an error."""
    out: list[int] = []
    for item in items:
        lo, _, hi = item.partition("-")
        out.extend(range(int(lo), int(hi or lo) + 1))
    bad = [p for p in out if not 1 <= p <= count]
    if bad:
        raise SystemExit(f"transcribe_pdf: page(s) {bad} outside 1..{count}")
    return list(dict.fromkeys(out))


def stamp_patterns(edition: int) -> list[re.Pattern[str]]:
    shapes = (spec_pdf.edition_data(verify_publishable.FORBIDDEN, edition, "per-copy stamp shapes (verify_publishable.FORBIDDEN)")
              + spec_pdf.edition_data(verify_publishable.TREE_FORBIDDEN, edition, "tree stamp shapes (verify_publishable.TREE_FORBIDDEN)"))
    return [re.compile(p) for p, _ in shapes]


def clean_page(text: str, edition: int) -> tuple[str, int]:
    """The page text with the model's outer code fence removed (a page's own figures are <pre>, never fenced) and every
    line that matches the edition's stamp shapes dropped; also the number of lines dropped."""
    m = FENCE.match(text)
    if m:
        text = m.group(1) + "\n"
    pats = stamp_patterns(edition)
    kept = [ln for ln in text.splitlines(keepends=True) if not any(p.search(ln) for p in pats)]
    return "".join(kept), len(text.splitlines(keepends=True)) - len(kept)


def write_atomic(path: pathlib.Path, text: str) -> None:
    """LF text, a temporary file then a rename: a crash never leaves half a file."""
    tmp = path.with_name(path.name + ".tmp")
    tmp.write_text(text, encoding="utf-8", newline="\n")
    tmp.replace(path)


class Run:
    """One run's directory: state.json (batches), local.json (provenance of locally converted pages), pages/, orig/."""

    def __init__(self, directory: pathlib.Path, edition: int):
        self.dir, self.edition = directory, edition
        self.pages_dir, self.orig_dir = directory / "pages", directory / "orig"
        self.state_file, self.local_file = directory / "state.json", directory / "local.json"
        self.pages_dir.mkdir(parents=True, exist_ok=True)

    def load(self, pdf_pages: int | None = None, model: str = DEFAULT_MODEL) -> dict[str, Any]:
        if self.state_file.exists():
            return json.loads(self.state_file.read_text(encoding="utf-8"))
        if pdf_pages is None:
            return {"model": model, "pages": 0, "batches": [], "pending": None}
        return {"model": model, "pages": pdf_pages, "batches": [], "pending": None}

    def save(self, st: dict[str, Any]) -> None:
        write_atomic(self.state_file, json.dumps(st, indent=1))

    def page_file(self, pno: int) -> pathlib.Path:
        return self.pages_dir / f"{page_name(pno)}.md"

    def written(self, count: int) -> set[int]:
        return {p for p in range(1, count + 1) if self.page_file(p).exists()}

    def local_log(self) -> dict[str, Any]:
        """local.json: per page name, the provenance of a locally converted page, or `{"error": ...}` for one that failed."""
        return json.loads(self.local_file.read_text(encoding="utf-8")) if self.local_file.exists() else {}

    def local_failed(self) -> list[int]:
        """The pages whose last local conversion failed (a later success replaces the page's record)."""
        return sorted(int(n[1:]) for n, rec in self.local_log().items() if "error" in rec)

    def put(self, pno: int, text: str, replace: bool = False) -> bool:
        """Write page `pno` once. An existing page is kept unless `replace`, which first keeps the old text in orig/.
        A page whose stamp lines cannot all be dropped is not written. True when the page was written."""
        text, _ = clean_page(text, self.edition)
        if any(p.search(text) for p in stamp_patterns(self.edition)) or not text.strip():
            return False
        target = self.page_file(pno)
        if target.exists():
            if not replace:
                return False
            self.orig_dir.mkdir(exist_ok=True)
            keep = self.orig_dir / target.name
            if not keep.exists():
                write_atomic(keep, target.read_text(encoding="utf-8"))
        write_atomic(target, text)
        return True


# -- the Message Batches API --------------------------------------------------------------------------------------------

def load_key(key_file: str) -> str:
    """The key in `key_file`; a missing or empty file exits naming the path (never the key)."""
    try:
        return read_api_key(key_file)
    except RuntimeError as e:
        raise SystemExit(f"transcribe_pdf: {e}") from None


def make_client(key: str) -> Any:
    import anthropic  # noqa: PLC0415  (only the network commands need the SDK)
    return anthropic.Anthropic(api_key=key, max_retries=CLIENT_RETRIES, timeout=CLIENT_TIMEOUT_SECONDS)


def render_png(pdf: pathlib.Path, pno: int) -> bytes:
    import pymupdf  # noqa: PLC0415
    with pymupdf.open(pdf) as doc:
        page = doc[pno - 1]
        zoom = IMAGE_LONG_EDGE / max(page.rect.width, page.rect.height)
        return page.get_pixmap(matrix=pymupdf.Matrix(zoom, zoom), colorspace=pymupdf.csGRAY).tobytes("png")


def pdf_page_count(pdf: pathlib.Path) -> int:
    import pymupdf  # noqa: PLC0415
    with pymupdf.open(pdf) as doc:
        return len(doc)


def prompt_for(edition: int) -> str:
    return PROMPT.format(name=spec_pdf.edition_data(EDITION_NAMES, edition, "edition name for the prompt"))


def batch_request(pdf: pathlib.Path, pno: int, model: str, edition: int) -> dict[str, Any]:
    image = base64.standard_b64encode(render_png(pdf, pno)).decode()
    return {"custom_id": page_name(pno), "params": {
        "model": model, "max_tokens": MAX_OUTPUT_TOKENS, "output_config": {"effort": "medium"},
        "messages": [{"role": "user", "content": [
            {"type": "image", "source": {"type": "base64", "media_type": "image/png", "data": image}},
            {"type": "text", "text": prompt_for(edition)}]}]}}


def estimate_usd(pages: int, model: str) -> float:
    price_in, price_out = STANDARD_PRICE_PER_MTOK[model]
    return pages * (PAGE_INPUT_TOKENS * price_in + PAGE_OUTPUT_TOKENS * price_out) / 1e6 * BATCH_DISCOUNT


def covered(st: dict[str, Any]) -> set[int]:
    return {p for b in st["batches"] for p in b["pages"]}


def adopt_pending(run: Run, st: dict[str, Any], client: Any) -> None:
    """A crash between `create` and the state write: find the batch the server accepted (the same request count, created
    after the recorded intent, and not yet in the state) and record it; the intent is then cleared."""
    pend = st.get("pending")
    if not pend:
        return
    known = {b["id"] for b in st["batches"]}
    for b in client.messages.batches.list(limit=50):
        rc = b.request_counts
        total = rc.processing + rc.succeeded + rc.errored + rc.canceled + rc.expired
        if b.id not in known and total == len(pend["pages"]) and b.created_at.timestamp() >= pend["t"] - 60:
            st["batches"].append({"id": b.id, "pages": pend["pages"], "replace": pend.get("replace", False)})
            print("adopted", b.id, "from an interrupted submit", flush=True)
            break
    st["pending"] = None
    run.save(st)


def submit_pages(run: Run, st: dict[str, Any], client: Any, pdf: pathlib.Path, pages: list[int], replace: bool = False) -> None:
    """Batches of at most BATCH_BYTES, each recorded as an intent before it is created and as a batch after."""
    chunk: list[dict[str, Any]] = []
    size = 0

    def flush() -> None:
        nonlocal chunk, size
        if not chunk:
            return
        ids = [int(r["custom_id"][1:]) for r in chunk]
        st["pending"] = {"t": time.time(), "pages": ids, "replace": replace}
        run.save(st)
        b = client.messages.batches.create(requests=chunk)
        st["batches"].append({"id": b.id, "pages": ids, "replace": replace})
        st["pending"] = None
        run.save(st)
        print("submitted", b.id, f"{page_name(ids[0])}-{page_name(ids[-1])}", len(ids), f"{size / 1e6:.0f} MB", flush=True)
        chunk, size = [], 0

    for p in pages:
        req = batch_request(pdf, p, st["model"], run.edition)
        n = len(json.dumps(req))
        if size + n > BATCH_BYTES:
            flush()
        chunk.append(req)
        size += n
    flush()


def collect_batches(run: Run, st: dict[str, Any], client: Any) -> None:
    """Take every batch that has ENDED and is not yet collected; write each succeeded page once."""
    for b in st["batches"]:
        bb = client.messages.batches.retrieve(b["id"])
        b["status"] = bb.processing_status
        print(b["id"], f"{page_name(b['pages'][0])}-{page_name(b['pages'][-1])}", bb.processing_status, flush=True)
        if bb.processing_status != "ended" or b.get("collected"):
            continue
        bad: list[list[str]] = []
        for res in client.messages.batches.results(b["id"]):
            if res.result.type != "succeeded":
                bad.append([res.custom_id, res.result.type])
                continue
            m = res.result.message
            if m.stop_reason != "end_turn":
                bad.append([res.custom_id, m.stop_reason])
                continue
            text = "".join(c.text for c in m.content if c.type == "text")
            pno = int(res.custom_id[1:])
            if not run.put(pno, text, replace=bool(b.get("replace"))) and not run.page_file(pno).exists():
                bad.append([res.custom_id, "stamp-or-empty"])
                continue
            st.setdefault("usage", {})[res.custom_id] = [m.usage.input_tokens, m.usage.output_tokens]
        b["bad"], b["collected"] = bad, True
        run.save(st)
        print("  collected; bad:", bad[:10], flush=True)
    run.save(st)


def retry_pages(st: dict[str, Any], run: Run) -> list[int]:
    """Pages with no file that no uncollected batch still holds: errored, expired, cut off or never submitted."""
    pending = [b for b in st["batches"] if not b.get("collected")]
    return [p for p in range(1, st["pages"] + 1)
            if not run.page_file(p).exists() and not any(p in b["pages"] for b in pending)]


def cost_so_far(st: dict[str, Any]) -> float:
    price_in, price_out = STANDARD_PRICE_PER_MTOK[st["model"]]
    return sum(i * price_in + o * price_out for i, o in st.get("usage", {}).values()) / 1e6 * BATCH_DISCOUNT


def print_status(run: Run, st: dict[str, Any]) -> None:
    count = st["pages"]
    missing = [p for p in range(1, count + 1) if not run.page_file(p).exists()]
    log = run.local_log()
    failed = run.local_failed()
    print(f"pages written {count - len(missing)}/{count}; local {len(log) - len(failed)}; cost so far ${cost_so_far(st):.2f}; missing {len(missing)} {missing[:20]}",
          flush=True)
    if failed:
        print(f"local conversions failed {len(failed)}: " + "; ".join(f"{p} ({log[page_name(p)]['model']}: {log[page_name(p)]['error']})" for p in failed[:20]),
              flush=True)


# -- the local backend --------------------------------------------------------------------------------------------------

def ollama_post(path: str, body: dict[str, Any] | None = None) -> Any:
    req = urllib.request.Request(OLLAMA + path, data=json.dumps(body).encode() if body is not None else None,
                                 method="POST" if body is not None else "GET", headers={"Content-Type": "application/json"})
    with urllib.request.urlopen(req, timeout=3600) as r:
        return json.loads(r.read())


def post_with_retries(post: Callable[[str, dict[str, Any] | None], Any], path: str, body: dict[str, Any] | None,
                      sleep: Callable[[float], None]) -> Any:
    """`post`, tried up to LOCAL_TRIES times with LOCAL_RETRY_SECONDS between; the last failure is raised."""
    for attempt in range(1, LOCAL_TRIES + 1):
        try:
            return post(path, body)
        except LOCAL_ERRORS as e:
            if attempt == LOCAL_TRIES:
                raise
            print(f"  {path}: {type(e).__name__}: {e}; try {attempt + 1} of {LOCAL_TRIES} in {LOCAL_RETRY_SECONDS} s", flush=True)
            sleep(LOCAL_RETRY_SECONDS)
    raise AssertionError("unreachable")


def local_pages(run: Run, pdf: pathlib.Path, model: str, pages: list[int], replace: bool,
                post: Callable[[str, dict[str, Any] | None], Any] = ollama_post,
                sleep: Callable[[float], None] = time.sleep) -> list[int]:
    """Convert named pages with a local vision model; the provenance of each is recorded in local.json.
    A page that cannot be converted (every try of the request failed, the reply did not stop, or the stamp check refused it) is
    recorded in local.json with the error and the model, and the run goes on to the next page; the failed pages are returned
    and `main` exits nonzero for them. `pages ... --local ANOTHER` retries them (a failed page has no file)."""
    digest = next((m["digest"] for m in post("/api/tags", None)["models"] if m["name"] == model), "")
    log = run.local_log()
    failed: list[int] = []

    def fail(pno: int, why: str) -> None:
        name = page_name(pno)
        print(f"{name}: NOT written, {why}", flush=True)
        log[name] = {"model": model, "error": why, "at": datetime.datetime.now().astimezone().isoformat()}
        write_atomic(run.local_file, json.dumps(log, indent=1))
        failed.append(pno)

    for pno in pages:
        name = page_name(pno)
        if run.page_file(pno).exists() and not replace:
            print(f"{name}: already written, skipped", flush=True)
            continue
        image = base64.standard_b64encode(render_png(pdf, pno)).decode()
        body = {"model": model, "stream": False, "think": False,
                "options": {"temperature": 0, "num_ctx": 32768, "num_predict": 16000},
                "messages": [{"role": "user", "content": prompt_for(run.edition), "images": [image]}]}
        try:
            r = post_with_retries(post, "/api/chat", body, sleep)
        except LOCAL_ERRORS as e:
            fail(pno, f"{type(e).__name__}: {e}")
            continue
        if r.get("done_reason") != "stop":
            fail(pno, f"done_reason={r.get('done_reason')}")
            continue
        if not run.put(pno, r["message"]["content"], replace=replace):
            fail(pno, "the stamp check refused it or it was empty")
            continue
        log[name] = {"model": model, "digest": digest, "at": datetime.datetime.now().astimezone().isoformat(),
                     "prompt_tokens": r.get("prompt_eval_count"), "output_tokens": r.get("eval_count")}
        write_atomic(run.local_file, json.dumps(log, indent=1))
        print(f"{name}: written, {r.get('eval_count')} tokens", flush=True)
    if failed:
        print(f"local conversion FAILED for {len(failed)} page(s): {failed}; rerun with --local ANOTHER-MODEL to retry them", flush=True)
    return failed


def bag(text: str) -> collections.Counter[str]:
    return collections.Counter(w.lower() for w in WORD.findall(text))


def agreement(layer: str, markdown: str, edition: int) -> tuple[float, float, int, int]:
    """(recall, precision, markdown words, layer words): the share of the PDF text layer's words (alphabetic, length >= 3,
    stamp lines left out) found in the Markdown, and of the Markdown's words found in the layer (a hallucination signal)."""
    pats = stamp_patterns(edition)
    a = bag("\n".join(ln for ln in layer.splitlines() if not any(p.search(ln) for p in pats)))
    b = bag(re.sub(r"<[^>]+>", " ", markdown))
    recall = sum(min(a[w], b[w]) for w in a) / max(1, sum(a.values()))
    precision = sum(min(a[w], b[w]) for w in b) / max(1, sum(b.values()))
    return recall, precision, sum(b.values()), sum(a.values())


def page_text(pdf: pathlib.Path, pno: int) -> str:
    """The PDF's own text layer of one page (OCR, with its errors: a signal, never the transcription)."""
    import pymupdf  # noqa: PLC0415
    with pymupdf.open(pdf) as doc:
        return doc[pno - 1].get_text()


def check_pages(run: Run, pdf: pathlib.Path, pages: list[int]) -> None:
    for pno in pages:
        md = run.page_file(pno).read_text(encoding="utf-8")
        recall, precision, md_words, layer_words = agreement(page_text(pdf, pno), md, run.edition)
        stamp = sum(1 for p in stamp_patterns(run.edition) if p.search(md))
        print(f"{page_name(pno)}: recall {recall:.3f} precision {precision:.3f} md-words {md_words} layer-words {layer_words} "
              f"| stamp matches {stamp}")


# -- assembly -----------------------------------------------------------------------------------------------------------

def assemble(run: Run, count: int, parts: list[tuple[int, str]]) -> tuple[str, list[str]]:
    """The edition's Markdown from the pages, and the problems found (empty when the assembly may be written)."""
    problems: list[str] = []
    missing = [p for p in range(1, count + 1) if not run.page_file(p).exists()]
    if missing:
        problems.append(f"missing pages: {missing[:30]}{' ...' if len(missing) > 30 else ''} ({len(missing)})")
    starts = {first: title for first, title in parts}
    if parts and (parts[0][0] != 1 or sorted(starts) != [f for f, _ in parts] or parts[-1][0] > count):
        problems.append("PARTS must start at page 1, ascend, and end inside the document")
    out: list[str] = []
    dropped = 0
    for pno in range(1, count + 1):
        if pno in missing:
            continue
        text, n = clean_page(run.page_file(pno).read_text(encoding="utf-8"), run.edition)
        dropped += n
        if not text.strip():
            problems.append(f"{page_name(pno)}: empty")
        if REFUSAL.search(text):
            problems.append(f"{page_name(pno)}: a refusal or summary instead of a transcription")
        if any(p.search(text) for p in stamp_patterns(run.edition)):
            problems.append(f"{page_name(pno)}: a per-copy stamp survives")
        if pno in starts:
            out.append(f"# {starts[pno]}\n\n")
        out.append(f"## Page {pno}\n\n{text.rstrip()}\n\n")
    if dropped:
        print(f"assemble: {dropped} stamp line(s) dropped", flush=True)
    return "".join(out), problems


# -- command line -------------------------------------------------------------------------------------------------------

def build_parser() -> argparse.ArgumentParser:
    ap = argparse.ArgumentParser(description="Convert a COBOL standard's PDF to Markdown (restartable, edition-aware).")
    sub = ap.add_subparsers(dest="cmd", required=True)

    def common(p: argparse.ArgumentParser, key: bool = True) -> None:
        spec_pdf.add_edition_argument(p)
        p.add_argument("--state", required=True, help="the run's directory (state.json, local.json, pages/, orig/)")
        if key:
            p.add_argument("--api-key-file", help="a file whose first non-empty line is the Anthropic API key")

    s = sub.add_parser("submit", help="submit a batch for every page no recorded batch covers")
    common(s)
    s.add_argument("--model", default=DEFAULT_MODEL)
    s.add_argument("--max-usd", type=float, help="refuse a submit whose estimate exceeds this many US dollars")
    common(sub.add_parser("status", help="pages written, local pages, cost; with a key, each batch's state too"))
    common(sub.add_parser("collect", help="write the pages of every ended batch"))
    common(sub.add_parser("retry", help="resubmit errored, expired, cut-off and missing pages"))
    g = sub.add_parser("pages", help="convert named pages again (a batch, or --local MODEL)")
    common(g)
    g.add_argument("pages", nargs="+", help="page numbers and ranges: 12 40-44")
    g.add_argument("--local", metavar="MODEL", help="an Ollama vision model; no API call")
    g.add_argument("--replace", action="store_true", help="replace a written page (the old text is kept in orig/)")
    c = sub.add_parser("check", help="word agreement of converted pages with the PDF's text layer (numbers only)")
    common(c, key=False)
    c.add_argument("pages", nargs="+")
    a = sub.add_parser("assemble", help="write the edition's Markdown from the pages")
    common(a, key=False)
    a.add_argument("--out", help="the Markdown to write (default: the edition's transcription in specs/)")
    return ap


def main(argv: list[str] | None = None, client_factory: Callable[[str], Any] = make_client,
         pdf_override: pathlib.Path | None = None, parts: list[tuple[int, str]] | None = None) -> int:
    a = build_parser().parse_args(argv)
    run = Run(pathlib.Path(a.state), a.edition)
    needs_pdf = a.cmd in ("submit", "retry", "pages", "check")
    pdf = pdf_override or (spec_pdf.require(a.edition) if needs_pdf else None)
    model = getattr(a, "model", DEFAULT_MODEL)
    if a.cmd in ("check", "assemble"):
        st = run.load()
        count = st["pages"] or pdf_page_count(pdf or spec_pdf.require(a.edition))
        if a.cmd == "check":
            check_pages(run, pdf, parse_pages(a.pages, count))
            return 0
        text, problems = assemble(run, count, parts if parts is not None else spec_pdf.edition_data(PARTS, a.edition, "assembly PARTS"))
        if problems:
            for p in problems:
                print("assemble:", p)
            print("=== TRANSCRIBE ASSEMBLE: REFUSED ===")
            return 1
        out = pathlib.Path(a.out) if a.out else spec_pdf.markdown(a.edition)
        write_atomic(out, text)
        print(f"=== TRANSCRIBE ASSEMBLE: {count} pages -> {out} ===")
        return 0
    st = run.load(pdf_page_count(pdf) if pdf else None, model)
    if a.cmd == "pages" and a.local:
        return 1 if local_pages(run, pdf, a.local, parse_pages(a.pages, st["pages"]), a.replace) else 0
    if a.cmd == "status" and not a.api_key_file:
        print_status(run, st)
        return 0
    if not a.api_key_file:
        raise SystemExit(f"transcribe_pdf: {a.cmd} calls the API and needs --api-key-file")
    client = client_factory(load_key(a.api_key_file))
    adopt_pending(run, st, client)
    if a.cmd == "submit":
        todo = [p for p in range(1, st["pages"] + 1) if p not in covered(st) and not run.page_file(p).exists()]
        estimate = estimate_usd(len(todo), st["model"])
        print(f"{len(todo)} page(s) not yet submitted; estimate ${estimate:.2f}", flush=True)
        if a.max_usd is not None and estimate > a.max_usd:
            raise SystemExit(f"transcribe_pdf: estimate ${estimate:.2f} exceeds --max-usd {a.max_usd}; nothing submitted")
        submit_pages(run, st, client, pdf, todo)
    elif a.cmd == "pages":
        pages = parse_pages(a.pages, st["pages"])
        if not a.replace:
            pages = [p for p in pages if not run.page_file(p).exists()]
        submit_pages(run, st, client, pdf, pages, replace=a.replace)
    elif a.cmd == "status":
        for b in st["batches"]:
            print(b["id"], client.messages.batches.retrieve(b["id"]).processing_status, flush=True)
        print_status(run, st)
    elif a.cmd == "collect":
        collect_batches(run, st, client)
        print_status(run, st)
    elif a.cmd == "retry":
        todo = retry_pages(st, run)
        print(f"retrying {len(todo)} page(s): {todo[:20]}", flush=True)
        submit_pages(run, st, client, pdf, todo)
    return 0


if __name__ == "__main__":
    ensure_utf8()
    sys.exit(main())
