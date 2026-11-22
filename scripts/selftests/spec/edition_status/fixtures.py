"""The planted sources, register notes and 1985 transcription the edition status cases build (kb/Work PB5924): no case
reads the real register."""
import json

import selftest_cases

C = selftest_cases.subject(__file__)   # scripts/spec/edition_status.py

NAMES = ("inventory GAP", "code reads", "open work notes", "catalog prose holes")


def planted(*counts):
    """Four sources that answer `counts` (None: a source whose reader refuses)."""
    def source(name, n):
        def read(year):
            if n is None:
                raise SystemExit(f"{name}: planted refusal")
            return C.Part(name, n)
        return read
    return tuple(source(n, c) for n, c in zip(NAMES, counts))


def line(*counts):
    return C.verdict(1985, C.status(1985, planted(*counts)))


def note(ident, status="open", body="", **fm):
    head = {"title": f'"{ident} - planted"', "id": ident, "kind": "defect", "status": status, **fm}
    return "---\n" + "".join(f"{k}: {v}\n" for k, v in head.items()) + f"---\n# {ident}\n\n{body}\n"


#: A 1985 transcription in the document's own structure: Section II (outside the normative region), Section VI with three
#: paragraphs (one partly in its row, one with no row, one whose text is the row of ANOTHER paragraph), and Appendix A of
#: Section XVII, whose letter is not Amendment 1's Section A.
TRANSCRIPTION_1985 = """## Page 1

# SECTION II: CONCEPTS

### 1.1 Discussion

The compiler must accept the program in this explanatory section.

# SECTION VI: NUCLEUS MODULE

### 1.1 Scope

The compiler must reject every undeclared name in the source text. The compiler shall report each rejected name once per program.

### 1.2 Notes

This paragraph states a description with no requirement at all in it.

### 1.3 Elsewhere

The compiler must reject every undeclared name in the source text.

# SECTION XVII: APPENDICES

# APPENDIX A: HISTORY

### 2.1 History

The compiler must have been built in some year long ago.
"""


#: A 1985 transcription whose paragraphs a manifest disposes (kb/Work PB6001): one prose paragraph per clause, a clause
#: whose `(n)` list a not-rules block holds (twice: one block's record read other text), a table in HTML markup with its
#: caption below, a figure's caption, and the page's repeated section title.
DISPOSED_1985 = """## Page 1

# SECTION VI: NUCLEUS MODULE

<u>SECTION VI: NUCLEUS MODULE</u>

### 2.1 Restated

The compiler must reject every undeclared name in the source text of a program.

### 2.2 Overturned

This paragraph is a pointer that its refuter overturned as a rule.

### 2.3 Stale

This paragraph was refuted UPHELD when its words were other words.

### 2.4 Pending

This paragraph awaits the reader of the units and stays a hole.

### 2.5 Listed

The conditions on the list are the following ones:

(1) The first condition of the list is a held item.

(2) The second condition of the list is a held item.

### 2.6 Relisted

The other conditions on the list are the following ones:

(1) The first condition of this list is read again now.

(2) The second condition of this list is read again now.

### 2.7 Tabled

<table>
<tr><td>A long cell of the table that is no paragraph</td><td>1</td></tr>
</table>

Table 3: A Caption Printed Below Its Table Here

Figure 1: The caption of a figure printed in the clause
"""


#: A 1985 clause headed "Examples" (§1985:VI.6.18.5's shape, kb/Work PB6001): a preamble, a label, a fenced code display
#: whose lines are long enough to be sentences, and a result table.
EXAMPLES_1985 = """## Page 1

# SECTION VI: NUCLEUS MODULE

### 3.1 Examples

In each of the following examples the counter is assumed to be zero before the statement.

<u>Example 1</u>:

```
INSPECT ITEM TALLYING
     COUNT-0 FOR ALL "AB" BEFORE "BC" AFTER "DE"
```

| Initial Value of ITEM | COUNT-0 |
|---|---|
| ABABC | 1 |
"""


def planted_1985(root, rows, text=TRANSCRIPTION_1985):
    """`root` holding the planted transcription `text` and a committed 1985 catalog of `rows`."""
    spec = C.catalog_prose_holes.spec_pdf.markdown(1985, root)
    spec.parent.mkdir(parents=True)
    spec.write_text(text, encoding="utf-8", newline="\n")
    cat = root / C.catalog_prose_holes.MEASURED[1985]
    cat.parent.mkdir(parents=True)
    cat.write_text(json.dumps({"rules": rows}), encoding="utf-8", newline="\n")
    return root
