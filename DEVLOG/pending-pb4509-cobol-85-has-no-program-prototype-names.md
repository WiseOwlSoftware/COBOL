## PB4509: COBOL-85 has no program-prototype-names, so a bare word after CALL or CANCEL is identifier-1

**The defect.** PB4503's branch review (lead R2-2) found that at `--std 85` the compiler still took a bare word naming
the program's own or a containing program definition as a program-prototype-name. `BinderDriver.ProgramPrototypesOf`
registers those names with no REPOSITORY entry (§8.4.6.8), and CALL's Format 2 spelling (b) asked that table at every
edition. The re-probe on this branch's base: `CALL P7 USING N W` inside P7 at 85 drew COBOL1684, the prototype CALL's
argument-count check. A nested program's `CALL OUTR` and `CANCEL OUTR` of its container compiled clean at 85.
`CANCEL NOSUCH` at 85 drew COBOL1760, a message about the REPOSITORY paragraph and §8.4.6.8, neither of which COBOL-85
has. With Format 2 selected and PB4503's up-front argument judgement also on, a skipped argument shifted every later
operand against the callee's formals.

**The rule.** ISO/IEC 1989:2002 introduced program-prototype-names; its §8.4.5.7 carries the sentence the 2023 standard
prints as §8.4.6.8 (OK §8.4.6.8). With them came §14.9.4.2 Format 2's bracket-omitted spelling
`CALL program-prototype-name-1` and §14.9.5.2's third CANCEL operand alternative. Below 2002 the word is identifier-1, which
"shall be defined as an alphanumeric, national, or program-pointer data item" (OK §14.9.4.3 1)) for CALL and as "an
alphanumeric or national data item" (OK §14.9.5.3 1)) for CANCEL. A word that names no data item is the §8.4.2.1
undefined reference.

**The fix.** One edition read, `CallBinder.ProgramPrototypeNamesExist`, over a new construct row
`program-prototype-name-2002` (introduced 2002, COBOL1639 below it). Both arms of CALL's spelling (b), CANCEL's prototype
arm and the PB4503 up-front judgement all ask it, so below 2002 a CALL can no longer be both judged up front and Format 2.
The registry stays the same at every edition. The spelling is written exactly like identifier-1, so no recognition gate
can see it, and the read is the program twin of IntrinsicBinder's keyword-omission routing: a behavioral read in
DESIGN-version-conformance-pipeline §1.1 item 3, which now lists three. The `call-as-phrase-2002` row claimed the bare
spelling needed no gate because REPOSITORY is 2002-gated; that was false for the containing spelling, which needs no
REPOSITORY entry, and the row now says so. INTERPROGRAM_DESIGN's edition-gating list names the new row.

**Goldens.** `14.9.4-CALL/pb4509_call_containing_program_by_name` is accepted from 2002: a nested program CALLs its
RECURSIVE container by name, and the output (`PPNA N=1`, `IN PPNB`, `PPNA N=2`, `PPNB DONE`, `PPNA END N=2`) follows
from §14.9.4.4 GR3 and the shared working storage. The refusals `14.9.4-CALL/pb4509-call-containing-name-85` and
`14.9.5-CANCEL/pb4509-cancel-own-name-85` draw COBOL1639 at 85. The program-prototype-name-2002 matrix row is the
third witness.

**Also fixed (PB4491 review leads R1-2 / R2-1).** The order-pin comment above `CheckPrototypeSignaturePairs` in
`BinderDriver.Bind` still said a restricted pointer formal is typed by its prototype's signature class. It is now
described by the externalized name of the prototype its TO phrase names, so a check run early reads the written word
and refuses two words for one prototype. Two comments in `ActivationConformanceDriftTests` named the deleted signature
classes' partition refinement.

**Rows.** No inventory change. FMT-14.9.4.2, FMT-14.9.5.2, SR-14.9.4.3-1 and SR-14.9.5.3-1 were already CONFORMS.
PB4509 is landed with `closes_rows: []` and a reason.
