- **D-SORT-RELEASE-RETURN-RANGE-85 — where a RELEASE or RETURN may stand at COBOL-85** (kb/Work PB5104; the text decides, no latitude). §1985:XI.4.2.3 SR2
  and §1985:XI.4.3.3 SR3 bind a RELEASE to the range of an input procedure of a SORT naming its sort file, and a RETURN to the range of an output procedure
  of a SORT or MERGE naming its file. The range is dynamic (§1985:XI.4.4.4 GR7, GR10: every statement executed as the result of a CALL, EXIT, GO TO or
  PERFORM in the range), so the check is `ProcedureReach.Closure` with a GO TO followed: the procedure's paragraphs, the procedures its PERFORM, SORT and
  MERGE statements transfer to and return from, and every GO TO destination with the paragraphs control falls through to from it, until an unconditional GO TO,
  a STOP RUN or the last paragraph of the range, where the compiler-inserted return mechanism ends it (§1985:XI.4.1.4 GR9; §1985:XI.4.4.4 GR8, GR11;
  kb/Work PB6008: a section written after the procedure that no path reaches is outside the range, as the same section written before it is).
  A statement no such walk reaches is refused,
  COBOL7081, asked of the bound program by `VersionConformancePass.GateReleaseReturnRange`. A statement written in the declaratives is in the range when
  the range's statements execute its declarative (GR7: "all statements in declarative procedures that are executed as a result of the execution of
  statements in the range"). Whether one does is a run-time fact, but whether one CAN is decidable (kb/Work PB5768,
  `VersionConformancePass.DeclarativeExecutableFromRange`): a USE AFTER STANDARD ERROR procedure runs only after an input-output statement on a file
  its USE names (any file, for an open-mode USE), so its RELEASE or RETURN is exempted only where the range holds such a statement or a CALL, and
  refused otherwise; where the program has no input (output) procedure for the file the range is empty and the statement is refused like any
  other (kb/Work PB5572). A USE BEFORE REPORTING or USE FOR DEBUGGING procedure keeps the run-time latitude. The range crosses a CALL (kb/Work
  PB5628): a RELEASE in a program a CALL of the range activates is in the range (`VersionConformancePass.CalledFromRange`). At COBOL-2002 and later the same misuse is the run-time exception of
  kb/Work PB1036. Witnesses: `conformance:cases/XI.4.2@1985-RELEASE-STATEMENT/pb5104_release_return_in_performed_range_85`,
  `conformance:wiseowl/diagnostics/pb5104-release-of-file-no-sort-names-85`, `conformance:wiseowl/diagnostics/pb5104-return-of-file-no-sort-names-85`,
  `conformance:cases/XI.4.2@1985-RELEASE-STATEMENT/pb5572_release_in_declarative_of_input_procedure_85`,
  `conformance:wiseowl/diagnostics/pb5572-release-in-declarative-no-input-procedure-85`,
  `conformance:wiseowl/diagnostics/pb5572-return-in-declarative-no-output-procedure-85`,
  `conformance:wiseowl/diagnostics/pb5768-release-in-declarative-not-executable-from-range-85`,
  `conformance:wiseowl/diagnostics/pb5768-return-in-declarative-not-executable-from-range-85`,
  `conformance:wiseowl/diagnostics/pb6008-return-in-unreached-section-after-85`, `conformance:wiseowl/diagnostics/pb6008-release-in-unreached-section-after-85`,
  `conformance:cases/XI.4.3@1985-RETURN-STATEMENT/pb6008_return_through_fall_through_85`.
