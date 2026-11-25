#!/usr/bin/env python3
"""The 1985 text-layer diff (kb/Work PB6135) on planted pages, one case per finding of its round-1 review.

The tolerance is asymmetric: a PDF word is OCR-NOISE only as the damaged reading of a well-formed Markdown word. A Markdown
word that is malformed or unknown (`file-name-l`, `identifier-i`, `comer`, `date-name-1`), a PDF word the document also uses,
two real words (`of`/`or`, `B-1`/`B-2`), a dropped `not` and a recorded note about something else are never explained;
punctuation and case are classes of their own; the committed result is bound to its inputs."""
import contextlib
import io
import sys

import selftest_cases
from standard_streams import ensure_utf8

D = selftest_cases.subject(__file__)   # scripts/spec/text_layer_diff.py

#: what the planted Markdown spells at least twice (the lexicon of well-formed words)
CORPUS = ("the file is open or of for not name file-name-1 file-name-1 data-name-1 data-name-1 identifier-1 identifier-1 "
          "corner corner B-1 B-1 B-2 B-2 evaluated evaluated consecutive consecutive IS IS the file is open or of for not "
          "name Section Section O O").split()


#: a planted page: its heading, a blank line, then the Markdown from line 3 (`_rows`); `_amendments` writes its notes after a
#: one-line block on that line, so a note governs a one-line planted Markdown
PAGE, FIRST_LINE = "## Page 5\n\n", 3


def _rows(pdf: str, md: str, entries=(), figure: bool = False, amendments=None):
    words = [(w, figure, n) for w, _, n in D.markdown_pages(PAGE + md + "\n")[5]]
    return D.classify_page(pdf.split(), words, list(entries), D.Lexicon.of(CORPUS), amendments=amendments)


def _classes(pdf: str, md: str, entries=(), figure: bool = False, amendments=None):
    return sorted(r["class"] for r in _rows(pdf, md, entries, figure, amendments))


def case_01_ocr_garbling_on_the_pdf_side_is_noise(expect):
    expect.equal("the alignment key folds nothing", D.lite("file-name-l") == D.lite("file-name-1"), False)
    expect.equal("1 read as l in a well-formed identifier", _classes("the file-name-l is open", "the file-name-1 is open"), ["OCR-NOISE"])
    expect.equal("IS read as lS", _classes("the file lS open", "the file IS open"), ["OCR-NOISE"])
    expect.equal("rn read as m", _classes("the comer is open", "the corner is open"), ["OCR-NOISE"])
    expect.equal("a letter lost", _classes("the valuated is open", "the evaluated is open"), ["OCR-NOISE"])
    expect.equal("a letter read as a glyph", _classes("the :onsecutive is open", "the consecutive is open"), ["OCR-NOISE"])
    expect.equal("a stray mark", _classes("the file q~7 is open", "the file is open"), ["OCR-NOISE"])
    expect.equal("the layer's spacing", _classes("the file - name is open", "the file-name is open"), ["OCR-NOISE"])


def case_02_a_malformed_markdown_word_is_unexplained(expect):
    expect.equal("file-name-l is the transcription's", _classes("the file-name-1 is open", "the file-name-l is open"), ["UNEXPLAINED"])
    expect.equal("identifier-i", _classes("the identifier-1 is open", "the identifier-i is open"), ["UNEXPLAINED"])
    expect.equal("comer for corner", _classes("the corner is open", "the comer is open"), ["UNEXPLAINED"])
    expect.equal("date-name-1 for data-name-1", _classes("the data-name-1 is open", "the date-name-1 is open"), ["UNEXPLAINED"])


def case_03_two_real_words_are_never_noise(expect):
    expect.equal("B-1 against B-2", _classes("add a to B-1", "add a to B-2"), ["UNEXPLAINED"])
    expect.equal("of against or", _classes("the file is open of the name", "the file is open or the name"), ["UNEXPLAINED"])
    expect.equal("a digit-suffixed word reaches UNEXPLAINED", _classes("the data-name-1 is open", "the data-name-2 is open"), ["UNEXPLAINED"])
    expect.equal("a word the document also uses", _classes("the file for open", "the file or open"), ["UNEXPLAINED"])


def case_04_a_note_accounts_only_for_its_own_hunk(expect):
    other = ([D._keys("is of mine")], [D._keys("and or yours")])
    expect.equal("a note containing of and or, about something else", _classes("the file is open of the name", "the file is open or the name", [other]), ["UNEXPLAINED"])
    own = ([D._keys("open of the name")], [D._keys("open or the name")])
    expect.equal("the pair the entry records", _classes("the file is open of the name", "the file is open or the name", [own]), ["CORRECTION"])
    expect.equal("a hunk the entry's was does not hold", _classes("the file is open for the name", "the file is open or the name", [own]), ["UNEXPLAINED"])
    note = D.note_entries("## Page 1\n> ⚠ CORRECTED: printed `the file is open of the name`, now `the file is open or the name`\n")[1]
    expect.equal("a note quoting both sides", _classes("the file is open of the name", "the file is open or the name", note), ["CORRECTION"])


def case_05_a_dropped_word_and_a_word_elsewhere_do_not_cancel(expect):
    expect.equal("a dropped not", _classes("the file is not open", "the file is open"), ["UNEXPLAINED"])
    gone_and_gained = _classes("the not file is open for the name or of", "the file is open for the not name or of")
    expect.equal("not gone here and gained there", gone_and_gained, ["UNEXPLAINED", "UNEXPLAINED"])
    expect.equal("an adjacent swap", _classes("the name file is open", "the file name is open"), ["UNEXPLAINED", "UNEXPLAINED"])


def case_06_a_run_in_another_order_is_a_move(expect):
    expect.equal("two words read in another place", _classes("the file name is open or of for", "or of for the file name is open") == ["OCR-NOISE"], True)


def case_07_punctuation_and_case_stay_visible(expect):
    expect.equal("a period for a comma", _classes("the file is open, of the name", "the file is open. of the name"), ["PUNCT"])
    expect.equal("a dropped bracket", _classes("the file [is] open", "the file is open"), ["PUNCT"])
    expect.equal("a capital", _classes("the File is open", "the file is open"), ["CASE"])
    expect.equal("identical text has no row", _classes("the file is open", "the file is open"), [])


def case_08_a_figure_is_reported_apart_and_the_stamp_and_hyphen_go(expect):
    expect.equal("inside a figure", _classes("the file is open", "the name is open", figure=True), ["FIGURE"])
    stamp = "the file Copyright by INCITS. Licensed to X. Downloaded 1. Copying and networking prohibited. is open"
    expect.equal("the stamp is dropped", D.drop_licence_stamp(stamp.split()), "the file is open".split())
    expect.equal("a line-end hyphen joins", D.join_line_end_hyphens("copy- ing".split(), {D.lite("copying")}), ["copying"])


def case_09_the_result_is_bound_to_its_inputs(expect):
    expect.equal("current inputs are not stale", D.stale({"inputs": D.inputs()}), [])
    expect.equal("a result with no digests is stale on all three", sorted(D.stale({"inputs": {}})), ["ledger", "markdown", "script"])
    changed = dict(D.inputs(), markdown_sha256="0" * 64)
    expect.equal("a changed Markdown is named", D.stale({"inputs": changed}), ["markdown"])


def _arbitrated(pdf: str, md: str, tess: str):
    rows = _rows(pdf, md)
    D.arbitrate_rows(rows, tess)
    return sorted(r["class"] for r in rows)


def case_10_a_second_reading_arbitrates_by_place(expect):
    pdf, md = "the file is open of the name", "the file is open or the name"
    expect.equal("tesseract reads the Markdown's word", _arbitrated(pdf, md, "the file is open or the name"), ["OCR-ARBITRATED"])
    expect.equal("tesseract reads the layer's word", _arbitrated(pdf, md, "the file is open of the name"), ["LIKELY-ERROR"])
    expect.equal("neither reading", _arbitrated(pdf, md, "the file is open for the name"), ["UNEXPLAINED"])
    expect.equal("both readings (two places)", _arbitrated(pdf, md, "the file is open or the name the file is open of the name"), ["UNEXPLAINED"])
    expect.equal("the words elsewhere on the page are no support", _arbitrated(pdf, md, "or file is open for of name"), ["UNEXPLAINED"])
    expect.equal("a dropped not, tesseract has none", _arbitrated("the file is not open", "the file is open", "the file is open"), ["OCR-ARBITRATED"])
    expect.equal("a dropped not, tesseract has it", _arbitrated("the file is not open", "the file is open", "the file is not open"), ["LIKELY-ERROR"])
    rows = _rows(pdf, md)
    D.arbitrate_rows(rows, "the file is open or the name")
    expect.equal("the evidence is kept", "open or the" in rows[0]["tesseract"], True)
    expect.equal("no private field is left", "_src" in rows[0], False)
    stamped = _rows(pdf, md)
    D.arbitrate_rows(stamped, "Licensed to X Downloaded 1 the file is open or the name Single user license")
    expect.equal("the purchaser's stamp is never kept as evidence", "icens" in stamped[0].get("tesseract", ""), False)


NOTE_AMENDMENT = ("> ⚠ **CORRECTED — see the Errata (C40).** The standard prints `The standard defines 11 modules` (page 21). "
                  "Amendment 1, its change for page I-1, applied in place: \"Replace\". Corrected to `defines 12 modules`.")
NOTE_DAMAGE = ("> ⚠ **CORRECTED — see the Errata (C2).** The standard prints `Presentation Rules _ables` (page 17): a damaged "
               "letter on the printed page, which the transcription had reproduced. Corrected to `Tables`.")


def _amendments(*notes: str, page: str | None = None):
    """The page's amendment notes, each written after the one-line block on FIRST_LINE (or in the planted `page` text)."""
    return D.amendment_notes(page if page is not None else PAGE + "governed\n\n" + "\n\n".join(notes) + "\n").get(5)


def case_11_an_amendment_note_governs_the_words_it_quotes(expect):
    am = _amendments(NOTE_AMENDMENT)
    expect.equal("a note that says an Amendment was applied in place is read", am is not None and len(am), 1)
    expect.equal("a damaged-letter note is no amendment", _amendments(NOTE_DAMAGE), None)
    pdf, md = "the standard defines 11 modules of cobol", "the standard defines 12 modules of cobol"
    expect.equal("the layer's words are the note's printed words", _classes(pdf, md, amendments=am), ["AMENDMENT-APPLIED"])
    expect.equal("with no amendment note it is unexplained", _classes(pdf, md), ["UNEXPLAINED"])
    other = "the file lists 11 files of cobol"
    expect.equal("words the note does not quote are not governed", _classes(other, "the file lists 12 files of cobol", amendments=am), ["UNEXPLAINED"])
    expect.equal("a run with no neighbour in the quote is not governed", _classes("an open 11 or", "an open 12 or", amendments=am), ["UNEXPLAINED"])
    expect.equal("the Markdown's number is not the amended one", _classes(pdf, "the standard defines 13 modules of cobol", amendments=am), ["UNEXPLAINED"])
    expect.equal("a damaged amended word is not the amended text", _classes(pdf, "the standard defines 12 moduls of cobol", amendments=am), ["UNEXPLAINED"])
    expect.equal("words the Markdown dropped, the note does not drop", _classes(pdf, "the standard defines of cobol", amendments=am), ["UNEXPLAINED"])
    drop = _amendments("> ⚠ **CORRECTED — see the Errata (C9).** The standard prints `one two three a big file is open now here` (page 5). "
                       "Amendment 1, applied in place. Corrected to `one two three a file is open now here`.")
    p_drop = "one two three a big file is open now here"
    expect.equal("words the amended text drops, the Markdown drops", _classes(p_drop, "one two three a file is open now here", amendments=drop), ["AMENDMENT-APPLIED"])
    expect.equal("words the amended text keeps, the Markdown drops", "AMENDMENT-APPLIED" in _classes(p_drop, "one two three a big is open now here", amendments=drop), False)
    added = _amendments("> ⚠ **CORRECTED — see the Errata (C8).** The standard prints `one two three a file is open now here` (page 5). "
                        "Amendment 1, applied in place. Corrected to `one two three a big file is open now here`.")
    p_add = "one two three a file is open now here"
    expect.equal("words the amended text adds, the Markdown adds", _classes(p_add, "one two three a big file is open now here", amendments=added), ["AMENDMENT-APPLIED"])
    expect.equal("words the amended text does not add", "AMENDMENT-APPLIED" in _classes(p_add, "one two three a huge file is open now here", amendments=added), False)


def case_12_zero_read_as_o_in_a_number(expect):
    z = D.zero_read_as_o
    expect.equal("a number of two digits", z(["1oo"], ["100"]), True)
    expect.equal("a lone 0 is never decided by context", z(["o"], ["0"]), False)
    expect.equal("other letters differ too", z(["1ob"], ["100"]), False)
    expect.equal("the reverse (the Markdown has the letter) is no noise", z(["0"], ["o"]), False)
    expect.equal("both readers give 1OO: noise", _arbitrated("the 1OO file", "the 100 file", "the 1OO file"), ["OCR-NOISE"])
    expect.equal("tesseract has the number: the layer's reading is the damage", _arbitrated("the 1OO file", "the 100 file", "the 100 file"), ["OCR-ARBITRATED"])
    expect.equal("a quoted list O between letters and digits stays likely", _arbitrated("the Z O 9 file", "the Z 0 9 file", "the Z O 9 file"), ["LIKELY-ERROR"])
    expect.equal("a lone O beside a digit stays likely", _arbitrated("the 1 O file", "the 1 0 file", "the 1 O file"), ["LIKELY-ERROR"])


def case_13_the_excerpt_is_the_aligned_block(expect):
    rows = _rows("open comer of name", "open corner or name")
    un = [r for r in rows if r["class"] == "UNEXPLAINED"]
    expect.equal("one unexplained row", len(un), 1)
    expect.equal("its Markdown excerpt keeps the words that paired off", un[0]["md"], "corner or")
    expect.equal("its text-layer excerpt too", un[0]["pdf"], "comer of")


AMEND_MD = ("## Page 831\n\n| Page No. | Change |\n|---|---|\n"
            "| I-1 | Replace the sentence the file is open when read it then closed. |\n"
            "| I-7 | Replace the sentence the record is closed after the write |\n"
            "| I-9 | Replace the sentence the key is not valid |\n")
AMEND_LAYER = ("Page No. Change I-1 Replace the sentence the file is open when read it then closed. I-7 Replace the sentence the record is "
               "closed after the write I-9 Replace the sentence the key is not valid").split()


def _note(printed: str, page: str = "I-1", instr: str = "Replace the sentence the file is open when read it then closed.") -> str:
    quote = f': "{instr}"' if instr else ""
    return (f"> ⚠ **CORRECTED — see the Errata (C7).** The standard prints `{printed}` (page 5, folio {page}). Amendment 1, its "
            f"change for page {page} (page 831, folio A-1), applied in place{quote}. Corrected to the text above.")


def _entry_classes(pdf: str, md: str, **note):
    notes = _amendments(_note("the file is shut when read it", **note))
    am, missing = D.with_cited_text({5: notes}, {831: AMEND_LAYER}, D.amendment_rows(AMEND_MD))
    return _classes(pdf, md, amendments=am[5]), missing


def case_14_the_amended_text_is_the_notes_own_entry_of_the_amendment_page(expect):
    notes = _amendments(_note("the file is shut when read it"))
    expect.equal("the Amendment page and page cell are read from the note", (notes[0][2][0], notes[0][2][1]), (831, D.fold("I-1")))
    expect.equal("a note that quotes no amended text has none of its own", notes[0][1], [])
    expect.equal("the control: the entry holds the amended word",
                 _entry_classes("a b the file is shut when read it x", "a b the file is open when read it x"), (["AMENDMENT-APPLIED"], 0))
    cls, _ = _entry_classes("a b the file is shut when read it x", "a b the file is closed when read it x")
    expect.equal("a word of ANOTHER change on the same page is no evidence (round-3 probe)", "AMENDMENT-APPLIED" in cls, False)
    cls, _ = _entry_classes("a b the file is shut when read it x", "a b the file is not valid when read it x")
    expect.equal("words of another change on the page", "AMENDMENT-APPLIED" in cls, False)
    cls, _ = _entry_classes("a b the file is shut when read it x", "a b the file is read x")
    expect.equal("dropped words the entry does not drop", "AMENDMENT-APPLIED" in cls, False)
    cls, n = _entry_classes("a b the file is shut when read it x", "a b the file is open when read it x", instr="Replace the unknown sentence of nothing here")
    expect.equal("an instruction no row opens: not located, not applied", ("AMENDMENT-APPLIED" in cls, n), (False, 1))
    cls, n = _entry_classes("a b the file is shut when read it x", "a b the file is open when read it x", instr="")
    expect.equal("a note with no instruction: not located", ("AMENDMENT-APPLIED" in cls, n), (False, 1))
    cls, n = _entry_classes("a b the file is shut when read it x", "a b the file is open when read it x", page="I-2")
    expect.equal("a page cell no row has: not located", ("AMENDMENT-APPLIED" in cls, n), (False, 1))
    twin = AMEND_MD + "| I-1 | Replace the sentence the file is open when read it then closed. |\n"
    expect.equal("two rows open alike: ambiguous, not located", D.with_cited_text({5: notes}, {831: AMEND_LAYER}, D.amendment_rows(twin))[1], 1)
    short = "Page No. Change I-1 Replace the sentence".split()
    expect.equal("an entry whose successor the layer does not show: not located",
                 D.with_cited_text({5: notes}, {831: short}, D.amendment_rows(AMEND_MD))[1], 1)
    expect.equal("another Amendment page's text is no evidence",
                 D.with_cited_text({5: notes}, {832: AMEND_LAYER}, D.amendment_rows(AMEND_MD))[1], 1)
    out = io.StringIO()
    with contextlib.redirect_stdout(out):
        D.verdict({"pages": {}, "amendment_entries_not_located": 3})
    expect.equal("the summary names the count", "amendment entry not located: 3" in out.getvalue(), True)



def case_15_an_insertion_is_anchored_at_its_place_in_the_entry(expect):
    """kb/Work PB6144: a Markdown-only run has no printed side to locate it, so its neighbours must stand around it in the
    note's entry; and the entry ends before the next row's page cell (the round-4 review probes)."""
    notes = _amendments(_note("the file is shut when read it"))
    am, _ = D.with_cited_text({5: notes}, {831: AMEND_LAYER}, D.amendment_rows(AMEND_MD))

    def applied(pdf: str, md: str, run: str | None = None, amendments=am[5]) -> bool:
        """The row of the inserted `run` (every row when None) is AMENDMENT-APPLIED."""
        return "AMENDMENT-APPLIED" in [r["class"] for r in _rows(pdf, md, amendments=amendments) if run is None or r["md"] == run]

    expect.equal("a correct one-word insertion still passes", _entry_classes("a b the file is when read it x", "a b the file is open when read it x"),
                 (["AMENDMENT-APPLIED"], 0))
    expect.equal("a correct run of four words still passes", _entry_classes("a the sentence the read it then x", "a the sentence the file is open when read it then x"),
                 (["AMENDMENT-APPLIED"], 0))
    expect.equal("a run of the entry inserted at another place (probe: a misplaced duplicate)",
                 applied("a b c d e f g h the file is shut when read it x", "a b c the file is d e f g h the file is open when read it x", "the file is"), False)
    expect.equal("a run of the entry inserted again after itself", applied("a b the file is shut when read it x", "a b the file is open when read it the file is x", "the file is"), False)
    expect.equal("an insertion whose one neighbour only matches", applied("a b the file is when read it x", "a b the file is open x read x"), False)
    expect.equal("an insertion at the page's edge has no anchor", applied("open when read it x", "the file is open when read it x"), False)
    expect.equal("a stray next-row page cell", applied("a b the file is shut when read it x", "a b the file is open when read it 17 x", "17"), False)
    paged = ("Page No. Change page I-1 Replace the sentence the file is open when read it then closed. page I-7 Replace the sentence the record is "
             "closed after the write page I-9 Replace the sentence the key is not valid").split()
    for label, layer in (("a page cell", AMEND_LAYER), ("a page cell printed with `page`", paged)):
        keys = [k for k in (D.fold(w) for w in layer) if k]
        entry = D.entry_text(D.amendment_rows(AMEND_MD)[831], keys, notes[0][2])
        expect.equal(f"the entry ends before the next row's {label}", entry[-2:], [D.fold("then"), D.fold("closed")])
    paged_am, _ = D.with_cited_text({5: notes}, {831: paged}, D.amendment_rows(AMEND_MD))
    expect.equal("a stray `page` beside the amended text (probe)",
                 applied("a b the file is shut when read it x", "a b the file is open when read it page x", "page", paged_am[5]), False)
    expect.equal("the probes' replacement itself is still applied",
                 applied("a b the file is shut when read it x", "a b the file is open when read it page x", "open", paged_am[5]), True)


KEY_MD = ("## Page 831\n\n| Page No. | Change |\n|---|---|\n"
          "| I-1 | Replace the sentence the key is not a name at all. |\n| I-7 | Replace the sentence the record is closed |\n")
KEY_LAYER = "Page No. Change I-1 Replace the sentence the key is not a name at all. I-7 Replace the sentence the record is closed".split()


def case_16_a_note_excuses_only_the_block_it_governs(expect):
    """kb/Work PB6144 round 1: a note governs the Markdown block it is attached to (the nearest block above it), and a
    difference outside that block is never AMENDMENT-APPLIED, whatever words the entry holds."""
    note = _note("the key is a name at all", instr="Replace the sentence the key is not a name at all.")

    def classes(pdf: str, md: str, page_md: str | None = None, run: str | None = None) -> list[str]:
        """The classes of the rows of `run` (every row when None); the note sits after the first block of `md`."""
        blocks = md.split("\n\n")
        text = PAGE + "\n\n".join([blocks[0], note, *blocks[1:]]) + "\n"
        am, missing = D.with_cited_text({5: _amendments(page=page_md if page_md is not None else text)}, {831: KEY_LAYER},
                                        D.amendment_rows(KEY_MD))
        words = [(w, False, n) for w, _, n in D.markdown_pages(text)[5]]
        rows = D.classify_page(pdf.split(), words, [], D.Lexicon.of(CORPUS), amendments=am[5])
        return [r["class"] for r in rows if run is None or r["md"] == run]

    printed = "a b the key is a name at all x c d this item is a name y"
    expect.equal("the control: the amended sentence in the governed block",
                 classes(printed, "a b the key is not a name at all x\n\nc d this item is a name y"), ["AMENDMENT-APPLIED"])
    expect.equal("the reviewer's probe: `not` gained by the look-alike in another block",
                 "AMENDMENT-APPLIED" in classes(printed, "a b the key is a name at all x\n\nc d this item is not a name y", run="not"), False)
    expect.equal("the look-alike changed as well as the amended sentence: only the governed one is applied",
                 classes(printed, "a b the key is not a name at all x\n\nc d this item is not a name y"), ["AMENDMENT-APPLIED", "UNCOVERED"])
    expect.equal("the amended sentence in a block the note does not follow",
                 "AMENDMENT-APPLIED" in classes("c d this item is a name y a b the key is a name at all x",
                                                "c d this item is a name y\n\na b the key is not a name at all x"), False)
    expect.equal("a note at the top of its page governs nothing",
                 _amendments(page=PAGE + note + "\n\na b the key is not a name at all x\n")[0][3], None)
    expect.equal("a note past another note governs the block above both",
                 _amendments(page=PAGE + "one\ntwo\n\n" + _note("other") + "\n\n" + note + "\n")[1][3], (3, 4))
    expect.equal("a figure is one block, blank lines and all",
                 _amendments(page=PAGE + "<pre>\nA\n\nB\n</pre>\n\n" + note + "\n")[0][3], (3, 7))
    expect.equal("a fenced figure is one block too",
                 _amendments(page=PAGE + "```\nA\n\nB\n```\n" + note + "\n")[0][3], (3, 7))


def case_17_an_insertion_is_anchored_on_the_entrys_own_run_inside_a_wide_block(expect):
    """kb/Work PB6204: inside one governed block a neighbour word a side is no place; the insertion needs ANCHOR (three)
    words of the entry's own run on each side. The probe is one wide paragraph where `is` and `a` stand around `not` in
    the amended sentence and again in a sentence the Amendment does not touch."""
    note = _note("the key is a name at all", instr="Replace the sentence the key is not a name at all.")
    printed = "a b the key is a name at all x c d this item is a name y"

    def applied(md: str, run: str = "not") -> bool:
        page = PAGE + md + "\n\n" + note + "\n"
        am, _ = D.with_cited_text({5: _amendments(page=page)}, {831: KEY_LAYER}, D.amendment_rows(KEY_MD))
        words = [(w, False, n) for w, _, n in D.markdown_pages(page)[5]]
        rows = D.classify_page(printed.split(), words, [], D.Lexicon.of(CORPUS), amendments=am[5])
        return "AMENDMENT-APPLIED" in [r["class"] for r in rows if r["md"] == run]

    expect.equal("the control: the amended sentence's own insertion", applied("a b the key is not a name at all x c d this item is a name y"), True)
    expect.equal("the probe: `not` in a sentence the Amendment does not touch, in the same block",
                 applied("a b the key is a name at all x c d this item is not a name y"), False)
    expect.equal("the entry's words on the left only", applied("a b the key is not a thing at all x c d this item is a name y"), False)
    expect.equal("the entry's words on the right only", applied("a b my key is not a name at all x c d this item is a name y"), False)
    expect.equal("fewer than three words of the page on a side", applied("key is not a name at all x c d this item is a name y"), False)
    expect.equal("the anchor is three words", D.ANCHOR, 3)


def _table_classes(pdf: str, md: str):
    """The classes of a planted page whose table headers are read from its Markdown (`table_header_groups`)."""
    page = PAGE + md + "\n"
    words = [(w, False, n) for w, _, n in D.markdown_pages(page)[5]]
    lex = D.Lexicon.of(CORPUS + "ELEMENT STD 2ND 3RD ELEMENT STD 2ND 3RD".split())  # the document spells its header words many times
    return sorted(r["class"] for r in D.classify_page(pdf.split(), words, [], lex, headers=D.table_header_groups(page).get(5)))


def case_18_a_table_header_the_layer_reads_in_another_order_is_one_reading(expect):
    """kb/Work PB6135: a header cell that wraps is read line by line across the columns by the layer. The leftover words
    beside the header row must be the same words, as a bag, as the header row's leftover words in the Markdown."""
    pipe = "| ELEMENT | 2ND STD | 3RD STD |\n|---|---|---|\n| name | file | open |"
    expect.equal("the header groups are the header row's line", D.table_header_groups(PAGE + pipe + "\n"), {5: [(3, 3)]})
    expect.equal("a header cell wrapped to rows: the layer's order is one reading",
                 _table_classes("2ND 3RD ELEMENT STD STD name file open", pipe), ["TABLE-HEADER"])
    expect.equal("the same words, in the Markdown's own order, are no difference", _table_classes("ELEMENT 2ND STD 3RD STD name file open", pipe), [])
    expect.equal("a header word the layer reads as another word is not a reading",
                 "TABLE-HEADER" in _table_classes("2ND 3RD ELEMENT STD FOR name file open", pipe), False)
    expect.equal("a header word the layer lacks is not a reading", "TABLE-HEADER" in _table_classes("2ND ELEMENT STD STD name file open", pipe), False)
    expect.equal("a header word the layer has twice is not a reading", "TABLE-HEADER" in _table_classes("2ND 3RD ELEMENT STD STD STD name file open", pipe), False)
    expect.equal("a body row is no header: the same words are unexplained",
                 "TABLE-HEADER" in _table_classes("name file open note 2ND STD", "| name | file | open |\n|---|---|---|\n| 2ND STD | note |"), False)
    expect.equal("no table, no header", "TABLE-HEADER" in _table_classes("2ND 3RD ELEMENT STD STD", "ELEMENT 2ND STD 3RD STD"), False)
    expect.equal("a stray header word far from the header is no part of it",
                 "TABLE-HEADER" in _table_classes("2ND ELEMENT STD name file open " + "x " * 8 + "3RD STD",
                                                  pipe + "\n" + " ".join("x" for _ in range(8))), False)
    html = "<table>\n<tr>\n<th>ELEMENT</th>\n<th>2ND<br>STD</th>\n</tr>\n<tr>\n<th>3RD STD</th>\n</tr>\n<tr><td>name</td><td>file</td></tr>\n</table>"
    expect.equal("`<th>` rows with only tags between them are one header group", D.table_header_groups(PAGE + html + "\n"), {5: [(5, 9)]})
    expect.equal("a wrapped HTML header read in another order", _table_classes("2ND 3RD ELEMENT STD STD name file", html), ["TABLE-HEADER"])
    expect.equal("a body row between header rows ends the group",
                 D.table_header_groups(PAGE + "<tr><th>a</th></tr>\n<tr><td>b</td></tr>\n<tr><th>c</th></tr>\n"), {5: [(3, 3), (5, 5)]})


def case_19_a_marker_renumbered_by_one_offset_with_the_text_equal(expect):
    """kb/Work PB6135: the transcription numbers footnotes (and rules) higher than the print, after an Amendment's insertion;
    each marker needs the words beside it equal, one positive offset per shape that never falls, and a second marker of the
    page at that offset."""
    base = "alpha beta {0} the file is open {1} the name is open {2} corner of the file"
    print_ = base.format("(1)", "(2)", "(3)")

    def classes(md: str, pdf: str = print_) -> list[str]:
        return _classes(pdf, md)

    expect.equal("three markers one higher: one reading each",
                 classes(base.format("(2)", "(3)", "(4)")), ["FOOTNOTE-RENUMBERED"] * 3)
    expect.equal("the markers unchanged are no difference", classes(print_), [])
    expect.equal("a lone renumbered marker is not a renumbering", classes(base.format("(1)", "(2)", "(4)")), ["UNEXPLAINED"])
    expect.equal("a marker numbered lower than the print is no renumbering", classes(base.format("(0)", "(1)", "(2)")), ["UNEXPLAINED"] * 3)
    expect.equal("an offset that falls on the page", classes(base.format("(2)", "(4)", "(4)")).count("FOOTNOTE-RENUMBERED"), 0)
    expect.equal("the offset that rises is still a renumbering", classes(base.format("(2)", "(3)", "(5)")).count("FOOTNOTE-RENUMBERED"), 2)
    expect.equal("words beside the marker that differ",
                 classes(base.format("(2)", "(3)", "(4)").replace("the name is open", "my name is open")).count("FOOTNOTE-RENUMBERED"), 2)
    expect.equal("a marker of another shape is not the same marker", classes(base.format("2.", "(3)", "(4)")).count("FOOTNOTE-RENUMBERED"), 2)
    expect.equal("a word that is another word in the hunk",
                 classes(base.format("(2) file", "(3)", "(4)")).count("FOOTNOTE-RENUMBERED"), 2)
    d = "alpha beta (1) file-name-l. the file is open (2) the name is open (3) corner"
    expect.equal("the layer's damaged word beside the marker is the same reading",
                 classes("alpha beta (2) file-name-1. the file is open (3) the name is open (4) corner", d), ["FOOTNOTE-RENUMBERED"] * 3)
    expect.equal("a hunk at the page's edge has no word beside it", classes("(2) the file", "(1) the file"), ["UNEXPLAINED"])
    expect.equal("rules numbered `n.` are a shape of their own",
                 classes("alpha 5. the file is open 6. the name is open 7. corner", "alpha 3. the file is open 4. the name is open 5. corner"),
                 ["FOOTNOTE-RENUMBERED"] * 3)
    expect.equal("two shapes, each with its own offset, in one hunk",
                 classes("alpha 8. (3) the file is open 9. (4) corner", "alpha 7. (1) the file is open 8. (2) corner"),
                 ["FOOTNOTE-RENUMBERED"] * 2)


def case_20_an_uncovered_run_is_weighed_by_the_second_reading_too(expect):
    """kb/Work PB6135: Markdown words the layer lacks (UNCOVERED) take the same positional second reading as an UNEXPLAINED
    row: the second reading holding them at that place confirms the Markdown, its neighbours touching without them is two
    readings against the transcription, and their presence elsewhere on the page only is no witness either way."""
    pdf, md = "the file is the name", "the file is open or the name"
    expect.equal("the layer alone lacks them", _classes(pdf, md), ["UNCOVERED"])
    expect.equal("tesseract reads them at that place", _arbitrated(pdf, md, "the file is open or the name"), ["OCR-ARBITRATED"])
    expect.equal("tesseract, too, reads the print without them", _arbitrated(pdf, md, "the file is the name"), ["LIKELY-ERROR"])
    expect.equal("tesseract reads them elsewhere only", _arbitrated(pdf, md, "the file is the name corner open or"), ["UNCOVERED"])
    expect.equal("tesseract reads neither", _arbitrated(pdf, md, "corner of for"), ["UNCOVERED"])


if __name__ == "__main__":
    ensure_utf8()
    sys.exit(selftest_cases.run(sys.modules[__name__]))
