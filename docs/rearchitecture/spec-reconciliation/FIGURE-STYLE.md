# How a general format is drawn in the transcription

> **Settled 2026-07-27 by rendering, not by reasoning.** Every rule below was arrived at by putting candidate
> figures in a browser and looking at them. Four of the six were found by the owner spotting a defect that was
> invisible in the markup — which is the main lesson: **a figure style cannot be reviewed as source, only as
> output.** Render before sweeping, not after.

A general format has to carry four things, and losing any of them changes what the language permits:

| carried thing | notation | what it decides |
|---|---|---|
| grouping | brackets, braces | §5.2.6.2 at most one · §5.2.6.3 exactly one |
| repetition/order | choice-indicator bars | §5.2.6.4 one-or-more, each once, any order |
| required vs optional word | underlining | §5.2.2 required · §5.2.3 optional word |
| operand structure | vertical alignment | which alternative belongs to which phrase |

## The rules

### 1. Block type — `<pre>`, never a fenced code block

A fence cannot carry underlining: `<u>` and `**` both render literally inside one. Since §5.2.2/§5.2.3 make
underlining load-bearing grammar, a fence structurally cannot express a general format. `<pre>` preserves
monospace and alignment and admits `<u>`.

### 2. Glyph family — BOX DRAWING (U+2500–U+257F), never Miscellaneous Technical

The bracket and brace extension glyphs (`⎡⎢⎣⎧⎨⎩⎪`, U+23A1–U+23AD) look right and **cannot be used**. Measured on
Windows: *not one* monospace font contains them —

| font | monospaced | `⎡` U+23A1 | `⎧` U+23A7 | `│` U+2502 | `┌` U+250C |
|---|---|---|---|---|---|
| Consolas · Courier New · Cascadia Mono/Code · Lucida Console | yes | — | — | **yes** | **yes** |
| Segoe UI Symbol · Cambria | **no** | yes | yes | yes | yes |

Only proportional faces carry them, so a browser must substitute per-glyph, and the substitute is not
width-matched. Worse, the substituted glyphs are not even mutually consistent, so columns drift *within* one
figure. Box-drawing glyphs are in every monospace font because the block was designed for character grids.

### 3. Brackets square, braces curved

```
bracket   ┌ │ └   ┐ │ ┘        U+250C U+2502 U+2514 · U+2510 U+2502 U+2518
brace     ╭ ┤ ╰   ╮ ├ ╯        U+256D U+2524 U+2570 · U+256E U+251C U+256F
```

Square box corners alone make a two-row brace indistinguishable from a two-row bracket, erasing the
§5.2.6.3-vs-§5.2.6.2 distinction. Curved corners restore it, and the middle piece keeps the brace's *point*:
`┤` on the left (pointing left, like `{`), `├` on the right.

⚠ `╭╮╰╯` are absent from Courier New and Lucida Console. Accepted risk: Consolas is the Windows browser and
VS Code default and sits in GitHub's monospace stack ahead of generic. macOS/Linux coverage is assumed, not
measured.

### 4. Choice indicators — `│`, one space clear of any adjacent delimiter

`╭│` reads as one compound mark; `╭ │` reads as two pieces of notation, which is what they are.

### 5. Minimum three rows per group

A corner glyph draws its stroke from the *centre* of its cell, so a two-row group spans centre-to-centre — one
row of height, where the printed bracket spans two. A group therefore never has fewer than three rows; a
two-alternative group gets a blank middle row.

**This is the faithful layout, not a workaround.** ACCEPT Format 3 is SEVEN rows in print, not five —
`identifier-3` y=170.1 · `LINE NUMBER` 175.0 · `integer-1` 184.2 · `AT` 194.1 · `COLUMN`/`identifier-4` 214–217 ·
`NUMBER` 222.3 · `COL`/`integer-2` 231.5. The standard puts each operand on its own row with the phrase label
centred between, which is exactly what supplies every delimiter with a middle piece.

### 6. Parentheses — curved like a brace, but **pointless**

COBOL's own `(` and `)` separators are set full height in some figures, and they are a third family, not a
variant of the other two. The function-identifier format (folio 127) nests all three at once — a bracket, then
parentheses, then a bracket — so they have to stay tellable apart:

```
bracket   ┌ │ └   ┐ │ ┘        square corners
brace     ╭ ┤ ╰   ╮ ├ ╯        curved corners, and a POINT
paren     ╭ │ ╰   ╮ │ ╯        curved corners, no point
```

The point is what carries §5.2.6.3 "exactly one of these"; a parenthesis groups without choosing, so it must
not have one. Curved-and-pointless is what is left once the bracket has the square corners.

### 7. A group one row tall is drawn with the plain separator

`┌ ACCESS MODE IS SEQUENTIAL ┐` is not a group — a corner glyph strokes from its cell centre, so on a single
row the two corners never join anything and the group reads as unclosed. A one-row group is therefore written
`[ ACCESS MODE IS SEQUENTIAL ]`, which is what the transcription already does for `[ END-START ]` and
`[ OPTIONAL ]`, and what the printed page draws: a single-height bracket.

This is rule 5 seen from the other end. Rule 5 grows a two-row group to three because two rows cannot express
a group; a one-row group is not made of rows at all, and takes the separator instead.

### 8. `line-height: 1` — a hard constraint, not a preference

Box-drawing glyphs TILE: the vertical stroke spans the full em box, so consecutive `│` join into one continuous
rule only when rows are exactly one em apart. Any leading leaves a gap at every row boundary. **A future
stylesheet change to `line-height` would silently break every choice indicator in the document with no other
symptom.**

## Where a figure IS, for the generator

Located from the **clause structure**, never from spacing. A bold numbered heading opens and closes every
region; only a region headed `General format(s)` holds figures, which is also what keeps the reserved-word
tables out — they are all-uppercase like a figure and their grid rules measure identically to choice-indicator
bars. Inside that region a `Format N (label):` line separates one format from the next.

Geometry cannot do this job, and the reason is worth keeping: **row spacing WITHIN one general format varies
more than the spacing BETWEEN two of them.** ACCEPT Format 3 steps its operand rows 4.9 pt apart and its phrase
groups 24 pt apart, while COMPUTE's two formats sit 31 pt apart — no threshold separates those. A gap-based
splitter fragmented single figures and merged neighbouring ones.

Over the whole standard this locates **475 figures on 339 pages**, all laying out without a collision.

## Layout rules, for the generator

### A blank row between groups

The printed figure separates ACCEPT Format 3's `AT` group, its exception group and `[ END-ACCEPT ]`, and the
worked example below keeps those blank rows. They are not decoration — without them a statement's phrases run
together into one block.

Where they go cannot be measured as a *gap*: 19.0 pt separates the exception group from `[ END-ACCEPT ]` while
17.3 pt separates the two rows **inside** that group. (That is the same trap that defeated gap-based band
detection one level up.) The enclosures already state it: **two adjacent rows belong together when some
delimiter spans both**, and are separate groups otherwise.

### A blank row between siblings

Two rows carrying content at the **same nesting depth** — alternatives of one delimiter — get a blank row
between them. `BIT` / `COMPUTATIONAL` / `COMP` / `DISPLAY` run down the USAGE clause and read as a wall
otherwise.

Siblings only. `identifier-3` and the centred label `LINE NUMBER` beside it are *not* siblings — the label sits
one level further out — which is why ACCEPT Format 3 keeps its printed seven rows instead of doubling.

Depth is counted against the delimiters spanning **both** rows of the pair, never against whatever crosses each
row separately: in the relation condition the operand braces cover only part of the figure's height, so
measuring per row made `IS <>` depth 3 and the row below it depth 1, and two plain siblings failed to pair.

### An outer enclosure's label may not subdivide an inner one

A clause label sits on the point row of the brace it annotates. When that row falls *between* the alternatives
of a brace nested inside, it must not claim a row of its own — it snaps onto the nearer neighbouring row.

The file-control entry is the case. `{ device-name-1 / literal-1 }` and `{ MANUAL / AUTOMATIC }` are the same
shape, yet the first drew **four** rows and the second three, because `ASSIGN` — the label of the *enclosing*
brace, at print y 175.1 — lands between `device-name-1` (165.6) and `literal-1` (179.8). Nothing encloses
`LOCK MODE IS`, so nothing intrudes on it.

A brace's **own** label is exempt, and that exemption is what keeps ACCEPT Format 3 at its printed seven rows:
`LINE NUMBER` also sits outside its brace and inside its span, but on that brace's own point row, where it
belongs.

### Columns align within a group; words flow within a cell

Two rows share a column only when the same delimiter spans them. Packing one column space across a whole figure
lets unrelated clauses shove each other about — the file-control entry stacks two dozen independent clauses, and
that is what produced `[ ORGANIZATION  IS  ]` and `[ FILE STATUS IS data-name-4  ]`.

Within a row, a **cell** is a run of words between two delimiters, and its words flow with single spaces. Only
cells align. Aligning individual *words* across rows aligns coincidences: on the COMPUTE page `ERROR` sits at
x 130.6 and `SIZE` on the row below at 129.7 — 0.9 apart, which is exactly the spread of a *genuine* alignment
(`FIRST`/`KEY`/`LAST`), so no tolerance can separate the two cases. Nothing needs to: those words are one
phrase and belong together.

## Construction rules, for the generator

- **Auto-size each group** to its widest row; never hand-place the closing delimiter.
- **A delimiter must never land on a character.** Text is placed first and delimiters over it, so a clash means
  the layout is wrong — and it corrupts the figure *silently*: `[ END-START ]` came out as `|N]-START` and
  still looked like a figure. The generator now fails loudly on any collision. Same shape as the
  `line-height` trap: the failure that leaves plausible-looking output is the one that gets shipped.
- **Take every decision from measurement, including the ones that look like defaults.** Each of these was a
  bug caused by inferring something the page already states: a bracket's *hand* comes from which way its feet
  turn, not from which side of a midpoint it sits (a lone stem is never left of its own midpoint, so it always
  drew as a closing bracket); a brace's *point* goes on the row where its middle piece was measured, not at
  the arithmetic centre of the span (ASSIGN's inner brace is four rows with its point on the second); and two
  delimiters in one column are cut apart at their top hooks, not merged by proximity (the file-control entry
  stacks clauses, and the LOCK MODE brace was drawing up through ACCESS MODE and FILE STATUS).
- **A foot belongs to the stem it is anchored to, and extends away from it.** Accepting a rule that merely
  *touches* either end of a stem let a choice-indicator bar adopt the foot of the bracket beside it — on folio
  503 the bracket's foot ends at 243.43 and the bar begins at 244.60, 1.17 pt away, so the bar was called a
  bracket and drew a closing corner in the middle of the group. Four stems in the whole standard were affected.
- **A hook brackets its content**: a delimiter's top piece maps onto the first row at or below it, its bottom
  piece onto the last row at or above it. Nearest-row snapping put the function-identifier's parentheses a row
  high, around an operand belonging to the brace beside them.
- **Rule 5 counts both delimiter families.** Braces are always glyph-drawn, so a spacer test that looked only at
  vector stems left every two-alternative brace two rows tall — with nowhere to put its point, which is the one
  mark distinguishing §5.2.6.3 from §5.2.6.2. The point is placed in the span's *interior* for the same reason.
- **Build bars into the row**, so their columns are structural rather than eyeballed.
- **Insert `<u>` tags AFTER layout**, right-to-left, so the block aligns on RENDERED width. Assert that stripping
  the tags reproduces the plain text exactly — that assertion is what proves the tags occupy no layout, and it
  is the check that makes a 254-figure sweep safe.
- **Take positions from measurement**, not by hand. Hand-specifying reintroduces exactly the drift this effort
  removes: my own hand-built ACCEPT flattened two operand rows and got the row count wrong.

## Worked example

```
ACCEPT screen-name-1

   ┌     ╭ │                        ╭ identifier-3 ╮   │ ╮  ┐
   │     │ │   LINE NUMBER          ┤              ├   │ │  │
   │     │ │                        ╰ integer-1    ╯   │ │  │
   │ AT  ┤ │                                           │ ├  │
   │     │ │   ╭ COLUMN ╮           ╭ identifier-4 ╮   │ │  │
   │     │ │   ┤        ├   NUMBER  ┤              ├   │ │  │
   └     ╰ │   ╰ COL    ╯           ╰ integer-2    ╯   │ ╯  ┘

   ┌ │ ON EXCEPTION imperative-statement-1     │ ┐
   │ │                                         │ │
   └ │ NOT ON EXCEPTION imperative-statement-2 │ ┘

   [ END-ACCEPT ]
```

Underlining is omitted above for legibility of the structure; in the file every required word is wrapped in
`<u>`, read from the underline rectangles rather than asserted.
