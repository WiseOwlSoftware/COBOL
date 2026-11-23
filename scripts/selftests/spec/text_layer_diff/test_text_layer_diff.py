#!/usr/bin/env python3
"""The 1985 text-layer diff (kb/Work PB6135) on planted pages: OCR confusions fold to the printed word, a real different
word is UNEXPLAINED, a word the correction notes name is CORRECTION, a drawn figure's difference is FIGURE, the purchaser's
stamp is dropped, and a line-end hyphen joins."""
import sys

import selftest_cases
from standard_streams import ensure_utf8

D = selftest_cases.subject(__file__)   # scripts/spec/text_layer_diff.py


def _classes(pdf: str, md: str, explain: str = "", figure: bool = False, vocab: str = "the file name is open or of for"):
    words = [(w, figure, 1) for w in D.words_of_markdown(md)]
    rows = D.classify_page(pdf.split(), words, D.explained_keys([explain]), {D.fold(w) for w in vocab.split()})
    return sorted((r["class"] for r in rows))


def case_01_ocr_confusions_fold_to_the_printed_word(expect):
    expect.equal("file-name-l is file-name-1", D.fold("file-name-l"), D.fold("file-name-1"))
    expect.equal("lS is IS", D.fold("lS"), D.fold("IS"))
    expect.equal("a damaged page reads as the printed one", _classes("the file-name-l is open", "the file-name-1 is open"), [])


def case_02_a_different_real_word_is_unexplained(expect):
    expect.equal("of for or", _classes("the file is open of the name", "the file is open or the name"), ["UNEXPLAINED"])


def case_03_a_stray_mark_is_noise_and_a_dropped_word_is_unexplained(expect):
    expect.equal("a stray mark", _classes("the file q~7 is open", "the file is open"), ["OCR-NOISE"])
    expect.equal("a word the Markdown lacks", _classes("the file name is open", "the file is open"), ["UNEXPLAINED"])


def case_04_a_named_correction_is_explained_and_a_figure_is_reported_apart(expect):
    expect.equal("note names the word", _classes("the file is open", "the name is open", explain="prints file. Corrected to name"), ["CORRECTION"])
    expect.equal("inside a figure", _classes("the file is open", "the name is open", figure=True), ["FIGURE"])


def case_05_stamp_hyphen_and_reading_order(expect):
    stamp = "the file Copyright by INCITS. Licensed to X. Downloaded 1. Copying and networking prohibited. is open"
    expect.equal("the stamp is dropped", D.drop_licence_stamp(stamp.split()), "the file is open".split())
    expect.equal("a line-end hyphen joins", D.join_line_end_hyphens("copy- ing".split(), {D.fold("copying")}), ["copying"])
    expect.equal("words in another order are no difference", _classes("the name file is open", "the file name is open"), [])


if __name__ == "__main__":
    ensure_utf8()
    sys.exit(selftest_cases.run(sys.modules[__name__]))
