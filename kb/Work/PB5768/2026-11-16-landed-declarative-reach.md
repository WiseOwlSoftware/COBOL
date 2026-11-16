**2026-11-16, landed (wave 20262068, group A), on the branch that also carries PB5628's CALL reach.** The note's fix
direction, at its root: `VersionConformancePass.GateReleaseReturnRange` no longer exempts a declarative RELEASE or RETURN
whenever the range is non-empty; it asks `DeclarativeExecutableFromRange`, which exempts a statement of a USE AFTER STANDARD
ERROR/EXCEPTION procedure only when the range's closure holds an input-output statement (OPEN, CLOSE, READ, WRITE, REWRITE,
DELETE, START, any organization: `UseTriggeringFiles`) on a file the USE names, any such statement for an open-mode USE, or a
CALL. A USE BEFORE REPORTING or USE FOR DEBUGGING procedure keeps the run-time latitude (the range may execute it whenever it
is non-empty), which is the one part not narrowed. Probes after the fix at `--std 85` (the pre-fix behaviour, accepted with
no diagnostic, is the note's re-run; it was not re-run here before the edit): `refute-b/t13.cob` COBOL7081 on its RETURN at
(21,12), `refute-b/t14.cob` COBOL7081 on its RELEASE at (21,30); the accepted shape
`pb5572_release_in_declarative_of_input_procedure_85` still MATCHES. Refusal witnesses
`pb5768-release-in-declarative-not-executable-from-range-85` and `pb5768-return-in-declarative-not-executable-from-range-85`
(diagnostics files refuse-at 85, COBOL7081). Rows 1985-SR-XI.4.2.3-2 and 1985-SR-XI.4.3.3-3 PARTIAL to CONFORMS (1985 GAP 34
to 32). `docs/conformance/3/D-SORT-RELEASE-RETURN-RANGE-85.md` restated, and its stale "The range's CALL is not followed"
sentence (false since PB5628) corrected in the same edit. Not added: an accepted witness whose range reaches the declarative
only through a CALL (the arm is one disjunct of `Triggers`, covered by PB5628's called-program witness shape only in part).
