## PB5949: the HTML listing colours the COBOL source by what the compiler read it as

The owner asked (2026-11-19) for syntax colouring of the source in the HTML listing. PB5949 decided the colours come from the
compiler's own reading at the edition in force, never from a published pattern grammar, and this step builds it
(DESIGN-listing.md section 7.2, "(PB5949)").

Each `ListingLine` now carries `Spans` (`ListingSpan(Column, Length, Class)`, 1-based character positions, column order, no
overlap), made once by `SyntaxSpanBuilder` from two readers. The reference format answers what never reaches the lexer: the
fixed-form areas (`ReferenceFormatMap.RegionsAt`, read by `FixedFormLine`, the one column reader), whole comment, comment-entry and
directive lines, and, for a listing only, the inline comment or comment-entry the logical conversion discards
(`PhysicalLineRecord`, which replaces the bare kinds array, and `ReferenceFormatMap.DiscardedAt`). The lexer's final stream of the
listed pass, after every retype, is classed by `SourceTextClassifier`: by token type, by the parse rule that holds each token, and
for a keyword by the edition's effective reserved words. Each stretch is placed through the line map's origin spans and the one
`ListingLineIndex`, and kept only where the written characters are the lexed ones. The HTML page wraps each span in `s-<class>`,
coloured for light and dark; the text listing ignores spans; the JSON carries them (schema 1.0, per section 7.3).

The 1985 witness: GOBACK, reserved from 2002, declared as a data item in a 1985 program is a user-defined word on every line, and
the GOBACK statement at 2023 is a reserved word. `SourceTextClassDriftTests` holds every token type of the vocabulary to a class
(red on a classifier that forgets STRINGLIT or DEFINED), the class map total both ways, and the style's colours for every class in
both schemes.

Not built: a COPY or REPLACE statement's words, which the text stages remove before the lexer reads the text (kb/Work PB5952).

The open review leads of every listing step were checked against origin/main. Two of PB5947's were still true and are fixed: the
MERGE placement removal now reports through its registry row's verdict and carries the removed-construct flag (the table of flagged
constructs counts it; `EditionFlagDriftTests` now scans every code a removal row pins), and `FlagOf` names its real witness.

Review round 1 found two stretches left uncoloured, both fixed at the root: a carried comment-entry header (its division decided after
REPLACE) now records where its entry begins when it is carried, read only of a candidate the parse settled as a header; and a stretch
is placed from the expanded index of its span's first unit, so a literal holding a supplementary-plane character or a tab is classed
whole (kb/Work PB5949's dated file `2026-11-19-listing-step-pb5949-review-r1`).
