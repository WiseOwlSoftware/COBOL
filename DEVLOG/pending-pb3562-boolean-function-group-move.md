## A boolean or national function or literal moved to a group now deposits its usage-bit or UTF-16BE representation (PB3562)

`MOVE FUNCTION BOOLEAN-OF-INTEGER(255 8) TO G`, with `G` an alphanumeric group of eight one-bit items (a USAGE clause at a
group level applies only to the elementary items, §13.18.60.4 1)), stored 00110001 where an elementary USAGE BIT item holding
the same value stores 11111111. The fuzz run fuzz-20261022-163846 found it three times (fz01430, fz02128, fz04225) from the
WiseOwl side and from GnuCOBOL's, which cannot run the function.

**Re-probe.** Reproduced on a fresh Debug build, both filed probes. The note's code site was one step short: `MoveBinder`
freezes a character-category function sender into `SendingValueTemp`'s run-time-length carrier (§14.9.25.4 GR1), and the
carrier's usage came from `CharacterUsage`, which said NATIONAL for a national function and DISPLAY for everything else. A
boolean function's returned value is "a boolean item of usage bit" (§15.13.4 1), §15.2 2)), so the sender that reached the
group move was a DISPLAY boolean whose storage image is its '0'/'1' text. A national function was right for the same
reason: its temporary is usage national.

**Fix.** (1) `CharacterUsage` maps boolean to USAGE BIT, and `OperandText.ElementaryCharacterImage` packs a run-time-length
usage-bit carrier with `BitsPackAll`. (2) `OperandText.NonElementaryMoveSender`'s non-field arm sends a national or boolean
literal or function value as `NatBytes` or `BitsPackAll`: the literal is the other un-materialized sender
(`MOVE N"EF" TO G` deposited the alphanumeric bytes 'E' 'F', a second defect of the same arm). (3) The CALL and INVOKE
boundaries into a KNOWN plain-group formal (a Format-2 CALL, CALL AS NESTED, a user-defined function, a method) read the
same codec through `CallEmitter.IsPlainGroupFormal`: a USAGE BIT or USAGE NATIONAL argument, a national or boolean literal
and a function value cross as their storage image. Before, `CALL .. AS NESTED USING BY CONTENT E8` into a group of eight
bit items arrived as 00110001, and INVOKE the same.

**Determination D-B3** (`docs/CONFORMANCE.md`): the standard states a usage for a boolean function (§15.2 2)) and a national
literal (§8.3.3.5.4 GR3 a)) but none for a boolean literal (§8.3.3.4.4 GR3 gives its value only), so a boolean literal is
USAGE BIT, the usage of a boolean function value; `MOVE B"01100110" TO G` and the function deposit one byte.

**Not fixed, filed as a lead in the note.** A Format-1 CALL that does not see the formal (no program-specifier, or one
defined later) still crosses a usage-bit or usage-national argument as its operand-text cell into a plain-group formal.
GR9's first branch makes that record the argument's own length "without conversion", so the cell needs both alphabets
(elementary formals read the text, a group formal the storage image): a `MoveSending` / `MoveLanding` and first-branch ABI
change beyond this slice.

**Goldens.** `14.9.25-MOVE/pb3562_group_move_function_values` (+ `..._below_2002`, refused at 85 with COBOLNET1502),
`14.2-Procedure-division-structure/pb3562_content_group_formal_usage_values`,
`wiseowl/14.9.25-MOVE/pb3562_boolean_literal_group_move`; drift tests in `Gr4GroupDecisionDriftTests` pin the sender arm,
the temporary's usage and the two boundary arms. No inventory row changed: the broken rules (GR-14.9.25.4-4, GR-14.2.3-9,
RV-15.13.4-1, GR-15.2-2, GR-8.3.3.4.4-3) were already CONFORMS.

**Self-review.** Architecture: the representation rule is written once, in the sender codec; the boundary asks it rather than
copying it, and the INVOKE and CALL lanes share one formal predicate. Full code: boolean-function consumers (DISPLAY, a
relation, STRING, a dynamic-length receiver, a reference-modified value) were probed unchanged after the temporary's usage
changed. Duplication: `CarrierDiffersFromStorage` restates the two conditions of `AsStorageImage`'s slice arm (national,
bit); it is the one new restatement and names that arm.
