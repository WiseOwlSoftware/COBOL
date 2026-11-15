- **D-F3S — the reach of an exception-checking PERFORM's WHEN phrase: dynamic within the activation, bounded by it
  (§2023:14.9.28.4 GR1, GR17, GR20; §2023:14.6.13.1.4 2); §2023:9.1.12 1); the clauses word the scope three ways)** (kb/Work PB442).
  GR17 reads "during the execution of imperative-statement-1", §2023:9.1.12 1) "an active PERFORM statement", §2023:14.6.13.1.4
  2) "the executed statement is within imperative-statement-1", and GR1 puts in the range "all statements that are
  executed as the result of a transfer of control in the range", a CALLed program's included. **Pinned reading, which
  the standard's own propagation rules decide:** (a) **within one activation the reach is the PERFORM's dynamic
  range** — a condition raised in a paragraph PERFORMed (or reached by GO TO) from imperative-statement-1 selects the
  WHEN exactly as one written inside it, and GR20's "statement in imperative-statement-1 in which the exception
  condition occurred" is the statement that raised it, the one §2023:14.9.33.4 GR2 a) 1. names for RESUME NEXT STATEMENT;
  (b) **it stops at the activation boundary** — an exception status indicator "exists for each function, method, or
  program" (§2023:14.6.13.1.1), so a condition raised in a CALLed program, an activated function or an INVOKEd method is
  processed there (its own conditional phrase, its own exception-checking PERFORM, its own USE statements), and it
  reaches the activator only when propagated: GOBACK / EXIT … RAISING raises it "in the activating runtime element"
  (§2023:14.9.18.4 GR1 b)), as does `>>PROPAGATE ON` (§2023:14.6.13.1.3 6)), with the CALL or INVOKE as the statement it
  occurred in (§2023:14.9.33.4 GR2 a) 2.) — and there the activator's WHEN selects it. Reading (b) is also the one the
  design recorded as the owner's default (`docs/CONDITIONS_EXCEPTIONS_DESIGN.md` D12 (d)). (c) **Enablement stays
  lexical and is a separate question**: GR14's implicit TURN is a directive "assumed before the first statement in
  imperative-statement-1", and a TURN enables checking for the statements "that follow in the compilation group"
  (§2023:7.3.25.4 GR6), so a raise in a PERFORMed paragraph written outside the PERFORM is raised, and so reaches the WHEN,
  only where checking is enabled at that paragraph's text; an I-O statement there whose EC-I-O checking is off raises
  nothing, and §2023:14.9.49.4 GR6 then runs its USE AFTER STANDARD EXCEPTION procedure. Realized by one mechanism: each WHEN
  frame carries the activation that pushed it, and the frame walk stops at the first frame of another activation
  (`ExceptionEngine.RunTopFrame`), so every activation mechanism is bounded by the run unit's one activation record.
  Witnesses `conformance:cases/14.9.28-PERFORM/pb442_f3_when_activation_scope` (the PERFORMed paragraph, a CALLed program holding an
  exception-checking PERFORM of its own, GOBACK … RAISING), `conformance:cases/9.1.12-Input-output-exception-processing/pb442_f3_when_method_scope` (an I-O
  raise in an INVOKEd method of a class holding such a PERFORM) and `conformance:cases/9.1.12-Input-output-exception-processing/pb442_f3_when_io_enablement`
  (reading (c): a READ in a PERFORMed paragraph written before, and after, a `>>TURN`).
