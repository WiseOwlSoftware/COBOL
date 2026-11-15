- **D-SORT-RELEASE-RETURN-RANGE-85 — where a RELEASE or RETURN may stand at COBOL-85** (kb/Work PB5104; the text decides, no latitude). §1985:XI.4.2.3 SR2
  and §1985:XI.4.3.3 SR3 bind a RELEASE to the range of an input procedure of a SORT naming its sort file, and a RETURN to the range of an output procedure
  of a SORT or MERGE naming its file. The range is dynamic (§1985:XI.4.4.4 GR7, GR10: every statement executed as the result of a CALL, EXIT, GO TO or
  PERFORM in the range), so the check is `ProcedureReach.Closure` with a GO TO followed: the procedure's paragraphs, the procedures its PERFORM, SORT and
  MERGE statements transfer to and return from, and every GO TO destination and the paragraphs after it. A statement no such walk reaches is refused,
  COBOL7081, asked of the bound program by `VersionConformancePass.GateReleaseReturnRange`. A statement written in the declaratives is in the range when the
  range's statements execute its declarative, a run-time fact, so it is not asked where an input (output) procedure for its file exists; where the
  program has none, the range is empty and the declarative statement is refused like any other (kb/Work PB5572). The range's CALL is not followed: a
  statement of a contained program is asked against its own program's procedures. At COBOL-2002 and later the same misuse is the run-time exception of
  kb/Work PB1036. Witnesses: `conformance:cases/XI.4.2@1985-RELEASE-STATEMENT/pb5104_release_return_in_performed_range_85`,
  `conformance:wiseowl/diagnostics/pb5104-release-of-file-no-sort-names-85`, `conformance:wiseowl/diagnostics/pb5104-return-of-file-no-sort-names-85`,
  `conformance:cases/XI.4.2@1985-RELEASE-STATEMENT/pb5572_release_in_declarative_of_input_procedure_85`,
  `conformance:wiseowl/diagnostics/pb5572-release-in-declarative-no-input-procedure-85`,
  `conformance:wiseowl/diagnostics/pb5572-return-in-declarative-no-output-procedure-85`.
