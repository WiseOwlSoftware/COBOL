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


def _rows(pdf: str, md: str, entries=(), figure: bool = False, amendments=None):
    words = [(w, figure, 1) for w in D.words_of_markdown(md)]
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


def _amendments(*notes: str):
    return D.amendment_notes("## Page 5\n\n" + "\n\n".join(notes) + "\n").get(5)


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
    drop = _amendments("> ⚠ **CORRECTED — see the Errata (C9).** The standard prints `a big file is open` (page 5). Amendment 1, "
                       "applied in place. Corrected to `a file is open`.")
    expect.equal("words the amended text drops, the Markdown drops", _classes("a big file is open", "a file is open", amendments=drop), ["AMENDMENT-APPLIED"])
    expect.equal("words the amended text keeps, the Markdown drops", "AMENDMENT-APPLIED" in _classes("a big file is open", "big is open", amendments=drop), False)
    added = _amendments("> ⚠ **CORRECTED — see the Errata (C8).** The standard prints `a file is open` (page 5). Amendment 1, "
                        "applied in place. Corrected to `a big file is open`.")
    expect.equal("words the amended text adds, the Markdown adds", _classes("a file is open", "a big file is open", amendments=added), ["AMENDMENT-APPLIED"])
    expect.equal("words the amended text does not add", "AMENDMENT-APPLIED" in _classes("a file is open", "a huge file is open", amendments=added), False)


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
            "| I-1 | Replace the sentence the file is open when read. |\n"
            "| I-7 | Replace the sentence the record is closed after the write |\n"
            "| I-9 | Replace the sentence the key is not valid |\n")
AMEND_LAYER = ("Page No. Change I-1 Replace the sentence the file is open when read. I-7 Replace the sentence the record is "
               "closed after the write I-9 Replace the sentence the key is not valid").split()


def _note(printed: str, page: str = "I-1", instr: str = "Replace the sentence the file is open when read.") -> str:
    quote = f': "{instr}"' if instr else ""
    return (f"> ⚠ **CORRECTED — see the Errata (C7).** The standard prints `{printed}` (page 5, folio {page}). Amendment 1, its "
            f"change for page {page} (page 831, folio A-1), applied in place{quote}. Corrected to the text above.")


def _entry_classes(pdf: str, md: str, **note):
    notes = _amendments(_note("the file is shut when read", **note))
    am, missing = D.with_cited_text({5: notes}, {831: AMEND_LAYER}, D.amendment_rows(AMEND_MD))
    return _classes(pdf, md, amendments=am[5]), missing


def case_14_the_amended_text_is_the_notes_own_entry_of_the_amendment_page(expect):
    notes = _amendments(_note("the file is shut when read"))
    expect.equal("the Amendment page and page cell are read from the note", (notes[0][2][0], notes[0][2][1]), (831, D.fold("I-1")))
    expect.equal("a note that quotes no amended text has none of its own", notes[0][1], [])
    expect.equal("the control: the entry holds the amended word",
                 _entry_classes("a b the file is shut when read x", "a b the file is open when read x"), (["AMENDMENT-APPLIED"], 0))
    cls, _ = _entry_classes("a b the file is shut when read x", "a b the file is closed when read x")
    expect.equal("a word of ANOTHER change on the same page is no evidence (round-3 probe)", "AMENDMENT-APPLIED" in cls, False)
    cls, _ = _entry_classes("a b the file is shut when read x", "a b the file is not valid when read x")
    expect.equal("words of another change on the page", "AMENDMENT-APPLIED" in cls, False)
    cls, _ = _entry_classes("a b the file is shut when read x", "a b the file is read x")
    expect.equal("dropped words the entry does not drop", "AMENDMENT-APPLIED" in cls, False)
    cls, n = _entry_classes("a b the file is shut when read x", "a b the file is open when read x", instr="Replace the unknown sentence of nothing here")
    expect.equal("an instruction no row opens: not located, not applied", ("AMENDMENT-APPLIED" in cls, n), (False, 1))
    cls, n = _entry_classes("a b the file is shut when read x", "a b the file is open when read x", instr="")
    expect.equal("a note with no instruction: not located", ("AMENDMENT-APPLIED" in cls, n), (False, 1))
    cls, n = _entry_classes("a b the file is shut when read x", "a b the file is open when read x", page="I-2")
    expect.equal("a page cell no row has: not located", ("AMENDMENT-APPLIED" in cls, n), (False, 1))
    twin = AMEND_MD + "| I-1 | Replace the sentence the file is open when read. |\n"
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


if __name__ == "__main__":
    ensure_utf8()
    sys.exit(selftest_cases.run(sys.modules[__name__]))
