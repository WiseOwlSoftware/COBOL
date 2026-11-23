#!/usr/bin/env python3
"""The 1985 text-layer diff (kb/Work PB6135) on planted pages, one case per finding of its round-1 review.

The tolerance is asymmetric: a PDF word is OCR-NOISE only as the damaged reading of a well-formed Markdown word. A Markdown
word that is malformed or unknown (`file-name-l`, `identifier-i`, `comer`, `date-name-1`), a PDF word the document also uses,
two real words (`of`/`or`, `B-1`/`B-2`), a dropped `not` and a recorded note about something else are never explained;
punctuation and case are classes of their own; the committed result is bound to its inputs."""
import sys

import selftest_cases
from standard_streams import ensure_utf8

D = selftest_cases.subject(__file__)   # scripts/spec/text_layer_diff.py

#: what the planted Markdown spells at least twice (the lexicon of well-formed words)
CORPUS = ("the file is open or of for not name file-name-1 file-name-1 data-name-1 data-name-1 identifier-1 identifier-1 "
          "corner corner B-1 B-1 B-2 B-2 evaluated evaluated consecutive consecutive IS IS the file is open or of for not "
          "name Section Section").split()


def _rows(pdf: str, md: str, entries=(), figure: bool = False):
    words = [(w, figure, 1) for w in D.words_of_markdown(md)]
    return D.classify_page(pdf.split(), words, list(entries), D.Lexicon.of(CORPUS))


def _classes(pdf: str, md: str, entries=(), figure: bool = False):
    return sorted(r["class"] for r in _rows(pdf, md, entries, figure))


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


if __name__ == "__main__":
    ensure_utf8()
    sys.exit(selftest_cases.run(sys.modules[__name__]))
