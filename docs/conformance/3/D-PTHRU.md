- **D-PTHRU — control that runs off the end of the source element inside an out-of-line PERFORM whose return mechanism
  is never reached (kb/Work PB3511, rows `GR-14.6.3-3`, `GR-14.9.28.4-5`).** §2023:14.9.28.4 GR5 b): "If procedure-name-2 is
  specified, the return mechanism is after the last statement of procedure-name-2"; GR6: "There is no necessary
  relationship between procedure-name-1 and procedure-name-2 except that a consecutive sequence of operations is to be
  executed beginning at the procedure named procedure-name-1 and ending with the execution of the procedure named
  procedure-name-2". In `PERFORM P2 THRU P1` with P1 written before P2 and P2 the last paragraph, control runs from P2 off
  the end of the procedure division and never reaches the return mechanism, so the PERFORM does not return under any
  reading. §2023:14.6.3 ("There is also no next executable statement after the last statement in a source element when the
  procedure in which it appears is not being executed under the control of some other COBOL statement … an implicit
  GOBACK statement without any optional phrases is executed") can be read two ways here: P2 IS in the PERFORM's range, so
  the sentence's first case does not literally fire and the standard names no next statement; or running off the source
  element is the implicit GOBACK whatever is active. The standard does not decide, so CLAUDE.md rule 1 takes GnuCOBOL
  3.2, which ends the activation after P2 (measured 2026-10-22 for kb/Work PB3511, `cobc -x -free -std=cobol2014`, with
  its `-Wsuspicious-perform-thru` warning). **Determination:** the implicit GOBACK, the same as when a GO TO has left the range (where §2023:14.6.3 does
  decide): `DispatchEmitter.EmitDispatchMethod`'s end-of-element arm throws the element's return signal from whatever
  nested dispatch reaches it. Witness: `conformance:cases/14.6.3-Explicit-and-implicit-transfers-of-control/pb3511_implicit_goback_perform_thru_inverted`.
  A SORT input or output procedure is the same question: a GO TO that leaves the named procedure for the source
  element's last paragraph stays in the procedure's range (§2023:14.9.40.4 GR13: "The range includes all statements that are
  executed as the result of a transfer of control in the range of the output procedure") but never reaches its return
  mechanism (GR11, GR14), and GnuCOBOL 3.2 ends the activation there too (measured 2026-10-22, `cobc -x -free
  -std=cobol2002`), so the same determination applies; the SORT never passes control to the statement after it.
  Witnesses: `pb3511_implicit_goback_sort_input_procedure` and `pb3511_implicit_goback_sort_output_procedure` in the same
  directory. A MERGE output procedure is the same question again (§2023:14.9.24.4 GR8, GR9), and in a called program the
  implicit GOBACK returns to the CALL with the abandoned MERGE's USING files still open, because GR7 c) terminates them
  only "after control passes the last statement in the output procedure" (GnuCOBOL 3.2 differs on this one point: it
  reads and closes the USING files before the output procedure); witness
  `pb3511_implicit_goback_sort_merge_called_program`. A procedure whose range ENDS at the last paragraph completes there
  and resumes the SORT or MERGE (`pb3511_sort_merge_procedure_ends_at_last_paragraph`, 85 to 2023).
