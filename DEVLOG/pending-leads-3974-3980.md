## File the wave-20261728 leads as PB3974-PB3980

The enqueue unit's clerk filed the leads of wave 20261728 into `kb/Work/`, one note per mechanism, after grepping the
register for each (PB3268 and PB3514 are landed, so neither took a witness):

- PB3974: an EVALUATE arithmetic-expression subject truncated past 21 integer digits (wrong answer; from PB3953's report).
- PB3975: DIVIDE ... GIVING q REMAINDER r stores r after a quotient size error; 14.9.12.4 GR6 c) checked with `cite.py` (wrong answer; from PB3869's report).
- PB3976: no golden pins LENGTH(FUNCTION SQRT(...)) under a standard arithmetic mode (from PB3951's report).
- PB3977: `source_units.markdown_units` keeps its own non-CommonMark fence toggle (sibling of PB3938).
- PB3978: `CobolDecDivisionTests` never asserts the inexact DivToOdd rows its comment says it compares.
- PB3979: the golden `pb3954_exact_quotient_places.cob` tags RV-15.84.4-2 without exercising it.
- PB3980: four stale doc comments wave 20261728 left (FileConnector.OpenOnce, CobolDec.Div, the BY CONTENT landing docs, MaterializesInitialEvaluation).

Left out by the enqueue unit's direction: the three PB3972 review leads (left to PB3972's own branch, which rewrites that
note) and the `FunctionValueField` comment lead (fixed by the conflict finisher on worktree-wf_f781f6f8-448-25). No
witness paragraphs were added; no code changed.
