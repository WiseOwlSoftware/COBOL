# The ISO PDF text layer — what was wrong with it, and how it was fixed

> **Status: FIXED.** `specs/ISO_COBOL_2023.pdf` now carries correct `/ToUnicode` CMaps and
> its text extracts, copies and greps correctly. Regenerate with `scripts/spec/pdf_deobfuscate.py`.
>
> This supersedes the long-standing working belief that the text layer was deliberately obfuscated and that
> rendering page images was the only way to read the printed original.

## The diagnosis we had wrong for months

Extracting text from the PDF produced garbage:

```
'\x0c\x16\x12Ȁ\x0c\x08\x06\x03ͳͻͺͻǣʹͲʹ͵'      # the page reads: ISO/IEC 1989:2023
'\x06\x12\x10\x13\x18\x17\x08'                  # the page reads: COMPUTE
```

Because the bytes landed in Greek and Coptic codepoint ranges, this was recorded as *"the ToUnicode CMap maps to
Greek combining characters"* — i.e. as deliberate obfuscation. Every downstream decision followed from that: the
`render-spec-page.py` workflow, the "render the page and squint at it" rule for diagrams, and the reliance on
agent verification for figure questions that ought to have been mechanical.

**That diagnosis was wrong, and the mistake was assuming malice where there was only an omission.** The real
cause, from the font dictionary itself:

```
<<  /BaseFont /MLLGNI+Cambria  /DescendantFonts [...]  /Encoding /Identity-H
    /Subtype /Type0  /Type /Font  >>
```

There is **no `/ToUnicode` entry at all**. 16 of the 26 fonts are Type0 / Identity-H TrueType subsets with none.

Under `Identity-H` the character codes in the content stream **are glyph indices** into the subsetted font. A
`/ToUnicode` CMap is the thing that says which Unicode character each glyph index represents. With none present,
every extractor falls back to emitting the raw glyph index as though it were a codepoint — and glyph indices in
the 0x300–0x400 range print as Greek.

Nothing is scrambled, nothing is encrypted, and nothing is missing from the page. **The mapping was simply never
written down.** It is a defective export, not a protection measure.

## Why there is no "cipher" to document

The first look at the data suggested a shift cipher — uppercase at `GID = ord(c) - ord('A') + 4`, lowercase at
`0x83 + (ord(c) - ord('a'))`, digits at `0x372 + d`. That pattern is real but **coincidental and not general**.
Glyph indices are assigned by the subsetter in whatever order it happened to emit glyphs, and each of the 16
subset fonts has its own order. In `MLLGNI+Cambria`, for instance, `SPACE` is GID `0x002`, while punctuation is
scattered (`,`=0x1E1, `:`=0x1E3, `.`=0x1E4, `-`=0x1E6, `+`=0x3AA, `’`=0x1EF, `•`=0x208).

So there is no formula to apply. The mapping has to be **recovered per font, empirically**.

## How the mapping is recovered

Critically, **without consulting our own transcription**. `specs/ISO_COBOL.md` is the artifact under repair; it
cannot serve as its own reference. The recovery is pure geometry:

1. The subset fonts are Cambria, Arial, Times New Roman, Courier New, Calibri and Symbol — all stock fonts,
   present unsubsetted in `C:/Windows/Fonts`.
2. **Font subsetting copies glyph outlines verbatim.** So a glyph's outline (contour endpoints plus point
   coordinates) is an exact key: each subset glyph matches exactly one glyph in the stock font.
3. The stock font *does* have a `cmap`, which gives that glyph's Unicode value.

`subset GID → outline → stock glyph → Unicode`. The subsetter stripped the `cmap` and `post` tables out of the
embedded fonts, which is why the mapping cannot just be read back out of them.

**Blank glyphs are the one case shape-matching structurally cannot handle.** A SPACE has no outline, so there is
nothing to compare. Without it the decode runs every word together (`TheCOMPUTEstatementassigns…`). Blanks are
matched on **advance width** against the stock font's own space instead. This was caught by the verification, not
by inspection — the first build looked plausible and was unusable.

Recovered coverage, from `--report`:

| font | mapped |
|---|---|
| MLLGNI+Cambria | 101/102 |
| MLLGKI+Cambria,Bold | 83/84 |
| MLOJIP+CourierNew | 3761/3762 |
| MLNFBJ+Arial | 163/164 |
| … 12 more | ≥ 95% each |
| MMGKMO+TimesNewRoman,Bold | 14/15 |

## How it is proved

**Glyph coverage is not evidence.** A mapping can be 100% covered and uniformly wrong. `--verify` therefore
re-opens the written file and checks it against facts about the printed standard that are independent of anything
in this repository — the cover title, and statement names that must appear on their own clauses' pages. The check
is built to fail, and it *did* fail on the first two builds (missing spaces, then a naive substring test that
did not allow for the cover setting "INTERNATIONAL STANDARD" across two lines).

Two further checks guard the replacement itself:

- **Rendering identity.** 53 pages rendered at 110 dpi and compared by SHA-256 of the pixel buffer: zero
  differences. The injection touches metadata only; not one pixel of the document changed.
- **Byte preservation.** The file is written with an **incremental save**, which copies the publisher's bytes
  verbatim and appends only the new objects. A full re-serialisation expands the publisher's object streams and
  doubles the file (9.4 MB → 18.7 MB). The delta is now **34 KB**.

## What this unlocks

The general-format figures — the thing the whole reconciliation effort is about — **extract as text, with
coordinates**:

```
y= 236.6  COMPUTE { identifier-1 [ rounded-phrase ] }  ...  =  arithmetic-expression-1
y= 259.0  ON SIZE ERROR imperative-statement-1
y= 276.3  NOT ON SIZE ERROR imperative-statement-2
y= 295.3  [ END-COMPUTE ]
```

Combined with `scripts/spec/figure_geometry.py`, which measures the bracket/bar/underline **vector rectangles** on
the same page, a printed general format can now be reconstructed mechanically: the words from the text layer, the
delimiters and the underlining from the geometry. That converts the remaining work — the misplaced-content batch,
and above all the 1,659-item grammar↔spec audit — from agent judgement into verification that can fail.

## Regenerating

```
python scripts/spec/pdf_deobfuscate.py --report                       # per-font coverage
python scripts/spec/pdf_deobfuscate.py --write out.pdf --verify       # write and prove
```

The tool never modifies the input. The pre-fix original (no `/ToUnicode`, sha256 `197ed0f6d5f01185…`) remains in
the `specs` submodule history at the commit before the replacement.

## Related

- `scripts/spec/figure_geometry.py` — measures printed delimiters; the other half of mechanical figure checking.
- `scripts/render-spec-page.py` — still useful for looking at a page, no longer the *only* way to read one.
- `REPAIR-PLAN.md` — the batches this feeds.
