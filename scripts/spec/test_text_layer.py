#!/usr/bin/env python3
"""Self-test for text_layer.py, the text-layer backend of transcribe_pdf.py (kb/Work PB4813). Every page is PLANTED: a
Layout built here, word by word, at invented positions; no licensed PDF is opened. With PyMuPDF installed, the reader is
tested too, on a small PDF this test writes itself; without it (CI's self-test jobs install none) that case says so.
Run: python scripts/spec/test_text_layer.py   (every gate and CI run it through scripts/self_tests.py.)

Cases: a heading set as two spans; a paragraph's rows joined, a row-end hyphen kept with no space (2002 5.6), ligatures;
numbered rules and a nested rule, escaped; a note; a short-row list as hard breaks with its sub-level; the running header,
the footer's folio and the stamp row dropped; a General format clause's rows written as a placeholder with their extent,
its format labels kept, and the clause carried to the next page; a general-format glyph outside such a clause; a ruled
table with a cell that lists items; an unruled table whose row the print barely separates; Courier as <pre>, escaped;
Markdown's own syntax escaped in prose; a page in two columns; an image page and a stray stroke reported as ambiguous.
"""
import dataclasses
import pathlib
import sys
import tempfile

sys.path.insert(0, str(pathlib.Path(__file__).resolve().parent))
import text_layer as L  # noqa: E402
sys.path.append(str(pathlib.Path(__file__).resolve().parents[1]))  # scripts/, for standard_streams
from standard_streams import ensure_utf8  # noqa: E402

failures: list[str] = []
#: A planted stamp, built from pieces so no tracked file carries the shape whole.
STAMP = "Copyright by the publisher. store order " + chr(35) + " X_" + "9999999. Downloaded today"


def check(name: str, cond: bool) -> None:
    print(f"  {'ok  ' if cond else 'FAIL'} {name}")
    if not cond:
        failures.append(name)


def no_stamp(text: str) -> bool:
    return "store order" in text


def words(text: str, x: float, y: float, size: float = 9.0, font: str = "Univers") -> list[L.Span]:
    """A row of words at x, y: each word 0.5 font size per letter wide, a quarter font size apart."""
    out = []
    for w in text.split(" "):
        width = 0.5 * size * len(w)
        out.append(L.Span(w, font, size, x, y, x + width, y + size))
        x += width + 0.28 * size
    return out


def page(*rows: list[L.Span], tables=(), images=0, strokes=(), width=612.0, height=792.0) -> L.Layout:
    return L.Layout(width, height, tuple(s for r in rows for s in r), tuple(tables), images, tuple(strokes))


BOLD = "Univers-Bold"
LONG = ("alpha beta gamma delta epsilon zeta eta theta iota kappa lambda mu nu xi omicron pi rho sigma tau upsilon "
        "phi chi psi omega alpha beta gamma delta epsilon zeta eta theta iota kappa lambda mu nu xi omicron pi rho")


def full_row(x: float, y: float, last: str = "") -> list[L.Span]:
    """A row that runs to the right edge (about x 540), ending with `last`."""
    text = LONG
    while len(text) * 0.5 * 9 + x > 540:
        text = text.rsplit(" ", 1)[0]
    return words(text + (" " + last if last else ""), x, y)


def main() -> int:
    print("headings, paragraphs, hyphens, ligatures, furniture")
    p = page(words("ISO/IEC 1989:2002(E)", 56.7, 30, 11, BOLD), words("Screens", 56.7, 43, 10),
             words("9.2", 56.7, 84, 11, BOLD), words("Screens", 92.7, 84, 11, BOLD),
             words("9.2.1 Terminal screen", 56.7, 107, 10, BOLD),
             full_row(56.7, 129, "qualified-data-name-"), words("with-subscripts and the identiﬁer.", 56.7, 140),
             words("A second paragraph.", 56.7, 162),
             words(STAMP, 56.7, 770, 7, "Helvetica"),
             words("158", 56.7, 756), words("©ISO/IEC 2002 - All rights reserved", 390.9, 756))
    c = L.convert(p, 180, no_stamp)
    md = c.markdown
    check("a heading set as two spans is one heading, its depth the clause number's", md.startswith("#### 9.2 Screens\n\n##### 9.2.1 Terminal screen\n"))
    check("a paragraph's rows join, and a row-end hyphen joins with no space",
          "qualified-data-name-with-subscripts and the identifier." in md and "\n\nA second paragraph.\n" in md)
    check("the running header, the footer and the stamp are dropped; the folio is returned",
          "ISO/IEC 1989" not in md and "rights reserved" not in md and "store order" not in md and c.folio == "158")
    check("the headings are returned", c.headings == [("9.2", "Screens"), ("9.2.1", "Terminal screen")])

    print("rules, notes, lists")
    p = page(words("1)", 56.7, 100), full_row(74.7, 100), words("of the first rule.", 74.7, 111),
             words("a)", 74.7, 133), words("a nested rule", 92.7, 133),
             words("NOTE 1", 74.7, 155, 8), words("The note text.", 111.6, 155, 8),
             words("10)", 56.7, 177), words("* a starred <word> - here", 74.7, 177),
             words("Category of data", 56.7, 200), words("Alphabetic 111, 315", 74.7, 211), words("CD entry 655", 56.7, 222),
             words("- a dash opens this row", 56.7, 260))
    md = L.convert(p, 2, no_stamp).markdown
    check("a numbered rule's label is escaped and its rows join", "1\\) alpha" in md and "of the first rule." in md.split("1\\)")[1].split("\n\n")[0])
    check("a nested rule is indented three spaces, never four", "\n\n   a) a nested rule\n" in md)
    check("a note keeps its label, at its indentation", "\n\n   NOTE 1 The note text.\n" in md)
    check("emphasis and a tag in prose are escaped", "10\\) \\* a starred &lt;word> - here" in md)
    check("a short-row list is hard breaks, its sub-level an em space", "Category of data\\\n&emsp;Alphabetic 111, 315\\\nCD entry 655" in md)
    check("a list mark opening a paragraph is escaped", "\n\n\\- a dash opens this row\n" in md)

    print("general formats")
    brace = "\uf8f1"
    p = page(words("14.8.24 MOVE statement", 73.7, 84, 10, BOLD),
             words("14.8.24.1 General format", 73.7, 128, 10, BOLD),
             words("Format 1 (simple):", 73.7, 150, 9, BOLD),
             words("MOVE", 74.7, 184), words(brace, 108, 176, 9, "Symbol"), words("identifier-1", 117.9, 180),
             words("literal-1", 117.9, 192),
             words("14.8.24.2 Syntax rules", 73.7, 288, 10, BOLD), words("1)", 73.7, 332), words("The rule.", 91.7, 332))
    c = L.convert(p, 499, no_stamp)
    check("a general format's rows become ONE placeholder with its rows' tops, never its text",
          c.markdown.count(L.FIGURE_MARK) == 1 and "p0499 #1 y 176.0-192.0" in c.markdown and "identifier-1" not in c.markdown
          and c.figures == [(176.0, 192.0)])
    check("the format label is kept, and the clause after the format is prose",
          "Format 1 (simple):" in c.markdown and "1\\) The rule." in c.markdown and not c.format_open)
    open_end = L.convert(page(words("7.1.2.1 General format", 56.7, 700, 10, BOLD), words("COPY", 72, 720)), 54, no_stamp)
    check("a General format clause open at the page's end is reported", open_end.format_open)
    nxt = L.convert(page(words("text-name-1", 110, 90), words("7.1.2.2 Syntax rules", 56.7, 200, 10, BOLD),
                         words("1)", 56.7, 222), words("A rule.", 74.7, 222)), 55, no_stamp, format_open=True)
    check("the next page begins inside it", nxt.markdown.startswith(L.FIGURE_MARK) and "1\\) A rule." in nxt.markdown)
    check("opens_format reads a page's last heading",
          L.opens_format(page(words("7.1.2.1 General format", 56.7, 700, 10, BOLD)), no_stamp) is True
          and L.opens_format(page(words("plain", 56.7, 100)), no_stamp) is None)
    stray = L.convert(page(words("The glyph", 56.7, 100), words(brace, 120, 100, 9, "Symbol")), 76, no_stamp)
    check("a general-format glyph outside a General format clause is a placeholder and ambiguous",
          L.FIGURE_MARK in stray.markdown and any("outside a General format" in a for a in stray.ambiguous))

    # 5.1 of 2002 (folio 17) is headed `General formats` and DESCRIBES them: prose and a dash list, no figure.
    told = L.convert(page(words("5.1 General formats", 56.7, 84, 10, BOLD),
                          words("Elements used in depicting general formats are:", 56.7, 106),
                          words("— Keywords", 74.7, 128), words("— Optional words", 74.7, 140)), 39, no_stamp)
    check("prose and a list under a General formats heading are text, never a figure (render_figure.classify_row)",
          L.FIGURE_MARK not in told.markdown and "Elements used in depicting" in told.markdown and "— Keywords" in told.markdown)

    print("tables, code, columns, ambiguity")
    cells = (((74, 279, 296, 302), (296, 279, 557, 302)), ((74, 302, 296, 340), (296, 302, 557, 340)))
    p = page(words("Provision", 165, 286, 9, BOLD), words("Verbal form", 312, 286, 9, BOLD),
             words("a requirement on an implementation or a pro-", 79.7, 307), words("gram", 79.7, 318),
             words("shall", 302, 307), words("shall not", 302, 318), words("After the table.", 73.7, 360),
             tables=[L.Table((74, 279, 557, 340), cells)])
    md = L.convert(p, 43, no_stamp).markdown
    check("a ruled table is a Markdown table; a full cell row joins, a short one is a <br> item",
          "| Provision | Verbal form |\n|---|---|\n| a requirement on an implementation or a pro-gram | shall<br>shall not |" in md
          and md.endswith("After the table.\n"))
    p = page(words("Access mode", 91.7, 260), words("Lock mode", 181.7, 260), words("Single/multiple", 307.7, 260),
             words("Random or dynamic", 91.7, 282), words("Automatic", 181.7, 282), words("Single", 307.7, 282))
    md = L.convert(p, 733, no_stamp).markdown
    check("an unruled table's columns come from every row, so a barely separated cell is split",
          "| Random or dynamic | Automatic | Single |" in md)
    p = page(words("01 A PIC X.", 91.7, 600, 9, "Courier"), words("<a>", 91.7 + 6 * 5.4, 609, 9, "Courier"))
    md = L.convert(p, 733, no_stamp).markdown
    check("Courier rows are a <pre> block at their measured columns, escaped",
          '<pre style="line-height:1">\n01 A PIC X.\n      &lt;a&gt;\n</pre>' in md)
    p = page(*[words(f"Left{i} 1", 56.7, 100 + 11 * i) for i in range(4)], *[words(f"Right{i} 2", 322.7, 100 + 11 * i) for i in range(4)])
    md = L.convert(p, 862, no_stamp).markdown
    check("a two-column page is read column by column", md.index("Left3") < md.index("Right0"))
    # Folio 170 of 2002: one column, its words leaving the middle clear by a 1.7 pt word gap (kb/Work PB4813).
    p = page(*[words(f"Lead{i}", 56.7, 100 + 11 * i) for i in range(4)], *[words(f"Tail{i}", 307.5, 100 + 11 * i) for i in range(4)])
    p = L.Layout(p.width, p.height, tuple(dataclasses.replace(s, x1=305.8) if s.text.startswith("Lead") else s for s in p.spans),
                 p.tables, p.images, p.strokes)
    md = L.convert(p, 192, no_stamp).markdown
    check("a page whose middle is clear only by a word gap is one column", md.index("Tail0") < md.index("Lead1"))
    c = L.convert(page(images=1), 1, no_stamp)
    check("an image page has no text and is reported", c.markdown == "" and c.ambiguous == ["1 image(s)"])
    c = L.convert(page(words("Prose.", 56.7, 100), strokes=[(60, 400, 200, 400)]), 3, no_stamp)
    check("a stroke outside every table and general format is reported", any("vector stroke" in a for a in c.ambiguous))

    print("the reader (PyMuPDF)")
    try:
        import pymupdf  # noqa: PLC0415
    except ImportError:
        print("  NOT RUN: PyMuPDF is not installed here (CI's self-test jobs install none); the planted layouts above ran")
        pymupdf = None
    if pymupdf is not None:
        with tempfile.TemporaryDirectory() as tmp:
            path = pathlib.Path(tmp) / "planted.pdf"
            doc = pymupdf.open()
            pg = doc.new_page(width=612, height=792)
            pg.insert_text((56.7, 94), "9.2  Screens", fontname="hebo", fontsize=11)
            pg.insert_text((56.7, 137), "A terminal provides I-O via a screen.", fontname="helv", fontsize=9)
            pg.insert_text((56.7, 160), "1)", fontname="helv", fontsize=9)
            pg.insert_text((74.7, 160), "The rule.", fontname="helv", fontsize=9)
            pg.insert_text((56.7, 780), STAMP, fontname="helv", fontsize=7)
            pg.insert_text((56.7, 764), "158", fontname="helv", fontsize=9)
            doc.save(path)
            doc.close()
            with pymupdf.open(path) as d:
                c = L.convert(L.read_layout(d, 1), 1, no_stamp)
            check("a written PDF reads back as its heading, its paragraph and its rule, the stamp and folio dropped",
                  c.markdown == "#### 9.2 Screens\n\nA terminal provides I-O via a screen.\n\n1\\) The rule.\n" and c.folio == "158")

    print(f"=== TEXT_LAYER SELF-TEST: {'GREEN' if not failures else 'RED - ' + ', '.join(failures)} ===")
    return 1 if failures else 0


if __name__ == "__main__":
    ensure_utf8()
    sys.exit(main())
