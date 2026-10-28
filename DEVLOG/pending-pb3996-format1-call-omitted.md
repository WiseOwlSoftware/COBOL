## PB3996 — a Format 1 CALL refuses OMITTED, and the Format-1 narrowing is asked once per argument

**What was wrong.** `CALL "P" USING W OMITTED`, `USING BY REFERENCE OMITTED` and `USING BY CONTENT W OMITTED` compiled and
ran (the note's program printed `EXC EXC EXC END`), where ISO §14.9.4.2 prints OMITTED only in Format 2
(program-prototype) and Format 1 prints `{ identifier-2 } …`. PB130 had narrowed every other Format-2-only spelling, but
inside each binder arm; the two OMITTED arms in `CallBinder.BindCall` had none, so a new or forgotten arm was unnarrowed by
default.

**The fix is the structure, not two more checks.** `CallBinder.Format2OnlySpellingOf` names every alternative of
`callArgument` / `callByReference` / `callByContent` (BY VALUE, OMITTED, a BY CONTENT expression or literal, the bare
literal / boolean / arithmetic / grouped argument) and `BindCall` asks it once per argument before any arm.
`Format1Narrowing` frames the refusal: strict is the COBOL0899 error; `--permissive` keeps PB162's warning for the five
vendor spellings. OMITTED has no vendor lane (the note's decision), so it is refused under both. The five per-arm copies
and the two helpers they called (`BareNeedsFormat2`, `Format1VendorSpelling`) are deleted, with no forwarder.
`CallArgumentFormatDriftTests` reflects over the generated parser and fails when an alternative is neither read by the
classifier nor listed as printed by Format 1, and checks the classifier runs before the dispatch.

**Siblings swept.** INVOKE (§14.9.23.2), the inline method invocation (§8.4.3.2.2) and the function-identifier print
OMITTED in their one format, so they need no narrowing. No new diagnostic code: COBOL0899, its catalog description widened
from "BY CONTENT operand" to the whole USING phrase.

**Goldens.** Three negatives (`pb3996-call-format1-omitted-bare`, `-by-reference`, `-by-content`, each refused at 2002,
2014 and 2023 with COBOL0899) and the control `pb3996_call_format2_omitted` (Format 2, bare and BY REFERENCE OMITTED, the
callee's `IS OMITTED` answering from §14.9.4.4 GR11). Seven existing fixtures had spelled OMITTED in a Format 1 CALL
(`pb757_invoke_omitted`, `pb971_program_arg_omitted`, `pb971-arg-omitted-below-2002`, `pb2087_by_reference_area_formals`,
`pb2671_omitted_formal_category_default`, `_unassigned/pb2096`, `_unassigned/pb615`): each is now `CALL "P" AS P USING …
OMITTED` with a REPOSITORY program-specifier, and its expected output is unchanged.

**Rows.** PB3996 claimed none and closes none; `docs/INTERPROGRAM_DESIGN.md` carries the narrowing's current shape.
