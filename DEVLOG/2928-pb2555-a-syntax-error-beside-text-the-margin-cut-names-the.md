## Entry 2928 — 2026-11-01 01:07 PST — PB2555 — a syntax error beside text the margin cut names the cut (COBOL5788)

**What a user saw.** In fixed form the program-text area ends at margin R (§6.3.1; position 72 here, Annex A.1 item 158), so the
compiler is right to drop what lies past it. But a statement that ran past position 72 then ended early, and the only diagnostic was
`cannot parse construct near 'STOP'` on the NEXT statement, or, when the margin fell inside a word, `unexpected 'END-SEN'` for
`END-SEND`. Nothing pointed at the truncation.

**Re-probe.** Both shapes reproduced on this tree (a CALL cut after `BY`; `END-SEND` written across position 72). A reconstruction of the
note's first repro, a 74-column CALL whose last argument is cut, parses without error (the cut argument was a trailing one), so the symptom
exists only when the cut leaves the statement incomplete.

**Fix shape.** The reference format records the cut and the syntax-error listener asks, the same split as the COBOL-85 areas (PB3147).
`ReferenceFormatMap.CutAt` holds, per fixed-form physical line, the text past margin R (`MarginRCut`, in `Frontend.Common`), and
`FixedFormAreas` (now an `IMarginRCutLocator`) composes it with the resultant-line map. The locator interface is declared in `Common` so
`Parsing` gains no edge to `Preprocessor` (the architecture-edges ratchet refused the first draft that named `FixedFormAreas` in the
listener). `CobolErrorListener.NoteMarginRCut` reports COBOL5788, a warning BEFORE the error (the cause first, PB4131), once per cut,
in two shapes: the offending token ends at margin R with the cut text right behind it, or the token before the offending one is the last of
a cut line and the offending token stands on a later line. It names the line, the position where the text was cut and the text that lay
past it. No cut exists for blank text past margin R, a comment line, a line ending in an inline comment (§6.3.7.3), a numeric sequence
area, or the identification tags (one word of at most eight characters in positions 73-80) of a file that labels most of its code lines
with one: only a parse error asks, so a tagged program that parses never hears of it, and one that does not parse hears about its own
statement, not its tags. `Frontend` passes the pass's `FixedFormAreas` to the listener through `LexAndParse`, `LexAndParseOnce` and
`ParsePass`, and keeps it as `Areas` when the pass settles.

**Not covered.** A statement written only past margin R on a line with no program text (no token stands on that line), and a lone tag-shaped
word on a minority of lines (indistinguishable from a cut word).

**Goldens.** `unit:MarginRCutNoteTests` (ten cases: the position and text named, the note before the error, the word shape, once per cut,
and the six non-cut shapes plus a cut inside library text naming the copybook); `conformance:cases/A.1-Implementor-defined-language-element-list/`
`pb2555_text_past_margin_r_is_ignored` (accepted from 85: a `DISPLAY "A"` followed by `"B"` from position 74 shows `A`) and the refusals
`pb2555-statement-cut-at-margin-r` and `pb2555-word-cut-by-margin-r` (the refusal harness matches errors only, so the warning itself is pinned
by the unit tests). Rows re-verdicted: none (`closes_rows: []` with its reason). DOC-A.1-158 in docs/CONFORMANCE.md and the frontend
design document carry the rule.

