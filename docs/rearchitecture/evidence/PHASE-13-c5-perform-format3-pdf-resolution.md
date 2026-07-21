# PERFORM Format 3 (§14.9.28.2) — PDF-verified figure resolution

**Source:** ISO/IEC 1989:2023 PDF **page 712** (printed folio 682), rendered at 400 dpi
(`python3 scripts/render-spec-page.py 712 --dpi 400`) and read glyph-by-glyph 2026-07-20. This resolves the three
"open questions" the rejected C5 re-derivation left for owner adjudication (the corrected, implementation-ready
design is `PHASE-13-c5-perform-format3-DESIGN.md`; the defective re-derivation JSON was deleted) — they are all
answered by the DIAGRAM + the §8.9 reserved-word list, per the standing render-the-PDF rule
(`feedback_spec_diagrams_render_pdf`). No owner adjudication is required.

## The figure as printed (page 712)

```
PERFORM [ WITH LOCATION ]
   imperative-statement-1
⎧                                                                                      ⎫
⎪      ⎧              ⎧ { file-name-1 } …  ⎫                                          ⎪  ⎪
⎪      ⎪  EXCEPTION   ⎪ INPUT              ⎪                                          ⎪  ⎪
⎨ WHEN ⎨              ⎨ OUTPUT             ⎬     imperative-statement-2               ⎬  ⎬ …
⎪      ⎪              ⎪ IO                 ⎪                                          ⎪  ⎪
⎪      ⎪              ⎩ EXTEND             ⎭                                          ⎪  ⎪
⎪      ⎪  { exception-name-1 …                                                       ⎪  ⎪
⎩      ⎩  { exception-name-2 FILE file-name-2 } …                                    ⎭  ⎭
[ WHEN OTHER EXCEPTION  imperative-statement-3 ]
[ WHEN COMMON EXCEPTION imperative-statement-4 ]
[ FINALLY imperative-statement-5 ]
END-PERFORM
```

## Underlining (required vs optional words), read off the render
- **Required (underlined):** `PERFORM` · `LOCATION` · `WHEN` · `EXCEPTION` (the FIRST one, inside the WHEN operand) ·
  `INPUT` · `OUTPUT` · `IO` · `EXTEND` · `FILE` · `OTHER` · `COMMON` · `FINALLY` · `END-PERFORM`.
- **Optional (not underlined):** `WITH` (so `PERFORM LOCATION …` is legal, §8.3.2.4.3) · the SECOND `EXCEPTION` on
  the `WHEN OTHER EXCEPTION` / `WHEN COMMON EXCEPTION` lines (so `WHEN OTHER` / `WHEN COMMON` alone are legal).

## The three resolutions

**Q1 — At least one ordinary WHEN is REQUIRED.** The outer delimiter around the `WHEN … imperative-statement-2`
group is a **BRACE** `{ }` (the curly cusp is unambiguous on both sides of the rendered page), and the `…` sits
OUTSIDE the closing brace. A brace = a required group; the ellipsis = the group repeats (one or more). Therefore a
Format-3 PERFORM MUST carry ≥1 ordinary WHEN phrase; `PERFORM WITH LOCATION … WHEN OTHER … END-PERFORM` with no
ordinary WHEN is NOT admitted by the syntax. GR18 ("If the WHEN OTHER phrase is used…") and GR22 ("If WHEN is
specified…") are additive PROSE and do not override the normative syntax diagram. IMPLEMENTATION: parse as a superset
(`performWhenPhrase*`) per house doctrine and enforce ≥1 at bind with a diagnostic — do not fight ANTLR over the brace.

**Q2 — A single WHEN does NOT mix operand forms.** The WHEN operand is ONE selection brace enclosing THREE stacked
alternatives: (a) `EXCEPTION { {file-name-1}… | INPUT | OUTPUT | I-O | EXTEND }` (itself a mode-selection brace),
(b) `{ exception-name-1 } …` (a repeating list of exception-name-1), (c) `{ exception-name-2 FILE file-name-2 } …`
(a repeating list of exception-name-2 FILE file-name-2). Stacked-in-one-brace = pick exactly ONE of the three forms.
No mixing of bare exception-names with `exception-name FILE file-name` within one WHEN. (The `{ exception-name-1 …`
line's closing `}` is dropped on the printed page — a typesetting slip; the ellipsis marks the group's repeat.)
IMPLEMENTATION: the grammar may accept a superset via `useEcEntry+`; enforce the no-mix rule at bind if desired
(low-risk to accept the mixed superset, but the SYNTAX is three disjoint forms).

**Q3 — The figure's `IO` denotes `I-O`.** The diagram literally prints `IO` (underlined, no hyphen). But §8.3.2.4
requires an underlined format word to be a reserved word; `IO` is NOT in the §8.9 reserved-word list (nor in
`tests/version-matrix/reserved-words.json`), whereas `I-O` IS; and every sibling format that spells this open mode —
USE (§14.9.49.2, the very rules GR17 makes authoritative for these operands), OPEN (§14.9.27) — prints `I-O`. `IO`
occurs exactly once in the entire standard (this figure). Therefore the figure's `IO` is a **typesetting defect for
`I-O`**, resolved from the spec itself. Use the existing `I_O` token; do NOT introduce a bare `IO` keyword (`IO` is a
legal user-defined word at every edition).

## ⚠ These answers were ALREADY in the repaired markdown — the render only CONFIRMS the repair
The 2026-07-19 diagram-repair pass (specs submodule `763a521`) re-rendered all 244 general-format figures and
corrected the markdown notes. For THIS figure the repaired notes at `specs/ISO_COBOL.md` §14.9.28.2 (lines
29082–29083) already state, correctly: "the outermost delimiter around the WHEN group is a pair of **braces** (a
required group)" (⇒ Q1), and "printed as the two lines `{ exception-name-1 …` and `{ exception-name-2 FILE
file-name-2 } …`" (stacked ⇒ Q2), and they transcribe `IO` faithfully (Q3's `I-O` derives from §8.9). The 400-dpi
render of page 712 matches the repaired notes glyph-for-glyph — the repair HELD. So neither the C5 re-derivation nor
this session needed to treat these as "open questions": the corrected spec markdown answered Q1/Q2 outright and Q3 by
one step of §8.9 reasoning. LESSON (durable): trust the repaired figure NOTES first; render only to verify a doubt —
never escalate a figure-reading to owner adjudication.

## Remaining C5 blockers (NOT figure questions)
The C5 re-derivation's OTHER rejection defects still require a corrected design before implementation: the greedy
`useEcEntry+`/`useOnTarget` mis-parse of a following `RESUME`/statement, the ~12 missed cross-statement syntax rules
(POP/PUSH/CLOSE/DELETE/OPEN/MERGE/SORT/… restrictions and the `EXIT PERFORM CYCLE` ban), the stale diagnostic number,
and the wrong subclause citations. This note closes only the three figure-reading questions.
