## The safe canonicalizer (wave 20262132 group g, branch train85e-e2-cite)

Every point of "The fix owed" is built, in `scripts/spec/citation_rewrite.py` (the rewrite moved out of
`audit_code_citations.py`, whose `--canonicalize` now runs it) and `scripts/spec/citation.py`:

- (a) `citation.SPACED_PATH` reads a later level with or without its space, and a level before a comma, semicolon or
  colon (`SR12 a)2.`, `GR22 d)2.a.`, `SR12 a)2., SR8`).
- (b) The parentheses are counted (`_kept`): a label's `)` that also closes a `(` opened before the citation is kept
  (`(ISO §2023:14.9.40.3 SR14 e)` becomes `SR14e)`). In a C# string the count is read over the literal's whole `+`
  concatenation of literals (`_chain`, through `csharp_lexer`); where the concatenation holds a non-literal operand, or
  the count does not balance, the designator is refused.
- (c) A designator followed by an interpolation hole or a `{0}` format item, or one that ends its literal where the
  string is concatenated, is refused.
- (d) `citation.GLUED_NUMBER`: a number glued straight after a letter (`SR12a2`) is `designator-not-canonical`, and its
  canonical form is `SR12a.2`. A rewrite is also refused when its result would still not read as canonical in its line
  (`_not_canonical`), and when `cite.resolve` refuses the canonical form it spells (`_unresolved`: a sub-item or
  ordinal the rule lacks is derived from the standard, never written).
- (e) A C# string under `src/` changes only with every expectation that quotes it (`_couple`): each rewrite carries its
  fragments (the whole citation, its tail, the stem without the label's closer, a rule named in words' head), and
  each fragment a C# test's string literal or a diagnostic expectation (`conformance-suite/wiseowl/diagnostics/*.json`)
  quotes is rewritten there in the same run. A quoted fragment is refused in every message when it is rewritten two
  ways, when an unrewritten message string still holds it, or when a test or suite file no reader rewrites quotes it.
  A tail written right after another clause's citation is that clause's (`§2023:6.1 3) a)` is not the `3) a)` of
  §2023:8.4.6.2.1). A test's own string literal and a generated file are never rewritten by themselves.

**Proof on the three breakages.** The base's `canonicalize_line` (HEAD 0ffdede4cc, run from a scratch copy) still
writes `SR12a2.)`, `(ISO §2023:14.9.40.3 SR14e"` and `GR9 {arm}))` for the three planted lines. The new self-test
(`scripts/selftests/spec/citation_rewrite/test_citation_rewrite.py`, 9 cases) plants each and passes.

**The run.** `--canonicalize src/ tests/ conformance-suite/ docs/ scripts/` rewrote 4,579 designators (the forms of
PB4976/PB6000), then 2,967 more once `r<n>` was read (PB4976's `r3d`, `citation.LOOSE_RULE`). 214 of them are in message
strings, rewritten with the 27 expectations that quote them. Nothing in the Characterization snapshots changed. The
inner loop on that tree: `audit_code_citations.py --check` 0 findings; Unit 42,775/42,775, RepoDrift 216/216 and
Characterization 37/37 whole; Conformance by filter (every changed test class, every touched golden, and the 61 cases
gate 000003 broke) 2,806/2,806 after the first run; after the second, Unit 42,775/42,775, RepoDrift
216/216, Characterization 37/37 and Conformance by filter (its changed classes and goldens) 898/898. The 31 glued `X12a2.` sites this note counted are rewritten too.

**Left:** the bare-ordinal form (`§2023:14.6.4 7`) is read by no audit: kb/Work PB6040.
