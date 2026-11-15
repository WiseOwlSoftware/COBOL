- **D-SMA — the sort-merge STATEMENT exception conditions (§2023:14.9.40.4 GR9, GR10, GR12 b), GR13; §2023:14.9.24.4 GR6,
  GR7, GR8, GR12; §2023:13.18.43.4 GR14 b), GR19 b); §2023:14.6.13.1.1; §2023:14.6.13.1.3 2))**: three readings. **(a) Which
  condition a statement raises when it detects two.** A RETURN inside an executing input procedure fails both
  §2023:14.9.40.4 GR10 (EC-SORT-MERGE-ACTIVE) and §2023:14.9.34.4 GR1 (EC-FLOW-RETURN, "any other time"); a RELEASE inside an
  output procedure fails GR13 / MERGE GR8 and §2023:14.9.32.4 GR1. §2023:14.6.13.1.1: "if more than one exception is detected
  during the execution of a statement, the one that is set to exist is undefined". WiseOwl COBOL raises
  **EC-SORT-MERGE-ACTIVE** when it is enabled — the rule written for exactly that range — and the EC-FLOW-* name only
  when it is not; a RELEASE's record-range test (EC-SORT-MERGE-RELEASE) comes last. **The survey behind (a)**
  (CLAUDE.md rule 1: ISO, then GnuCOBOL, then IBM / Micro Focus; kb/Work PB1571; measured on `cobc` 3.2.0 and read
  from the pinned 3.2 source, `libcob/fileio.c` and `libcob/exception.def`): **GnuCOBOL** lists all seven EC-SORT-MERGE
  names but raises exactly one, EC-SORT-MERGE-FILE-OPEN (a USING/GIVING file that fails its implicit OPEN with a '4x'
  status); it raises no -ACTIVE, -RELEASE, -RETURN or -SEQUENCE (its source marks the MERGE sequence test a FIXME) and
  no EC-FLOW-RELEASE or EC-FLOW-RETURN, with `-debug` as without. So it has no order to follow. What it does with the
  misplaced statements is a by-product of its sort store with no check behind it: a RETURN in an input procedure takes
  the record the SORT still owes its output procedure (the output procedure then meets end of file at once, a lost
  record), a RELEASE in an output procedure is kept when it comes before the first RETURN and is a run-time error '30'
  after a RETURN, and a nested SORT of another file runs. **IBM Enterprise COBOL** documents the restrictions (an input
  or output procedure must not contain SORT or MERGE; a called program cannot issue SORT, MERGE, RELEASE or RETURN)
  without a condition or a result. **Micro Focus** documents one fatal run-time error, COBRT099 "Illegal operation in
  SORT/MERGE module", for a RELEASE outside the input procedure and a RETURN outside the output procedure alike. Of
  the three, the one that names the condition (Micro Focus) names a single failure for both rules, and none of the
  three orders two conditions (the ISO exception-checking model of §2023:14.6.13 is WiseOwl COBOL's own to realise), so the
  determination stands: -ACTIVE first. The same survey leaves (c) as it is: the answer with checking off is
  "undefined" in every rule, and GnuCOBOL's depends on the state of its store and loses a record or adds one, where
  this compiler's answer (a RETURN in an input procedure answers at end, a RELEASE in an output procedure is not
  kept) is deterministic. Golden
  `conformance:wiseowl/cases/14.9.40-SORT/pb1571_sort_merge_active_vs_flow` pins (a) at source level, with the EC-FLOW names
  raised where -ACTIVE is not enabled and EC-SORT-MERGE-RETURN (GR3) unpre-empted. The dispositions with no USE procedure (abnormal termination for a written RETURN/RELEASE, the inner SORT ended and the run unit continuing for a nested SORT) are pinned by `SortMergeActiveUndeclaredDispositionTests`.
  **(b) The disposition when the SORT or MERGE itself raises a condition** (EC-SORT-MERGE-FILE-OPEN,
  -SEQUENCE, -RELEASE of the implicit USING release, -ACTIVE at a nested SORT/MERGE — and any other fatal condition
  its own execution raises, such as an EC-LOCALE-* from a locale-collated key): §2023:14.6.13.1.3 2) hands it to the verb, and
  each verb rule says the statement terminates (MERGE GR6/GR7/GR12, SORT GR12 b)) or its results are undefined (SORT
  GR9, GR10, GR13). The applicable declarative runs, the statement is terminated, and the run unit continues — the
  reading the implicit-transfer determination above applies to I-O statuses. **⛔ ONLY AT `--std 2023`** (kb/Work PB5612;
  VCR row 7.80, `DialectBehavior.SortMergeStatementDisposesItsFatalConditions`): §2023:14.6.13.1.3 2) is new in 2023.
  §2014:14.6.12.1.2 and §2002:14.5.12.1.2 have no item for a MERGE or SORT statement: a fatal exception condition with
  checking enabled and an applicable declarative runs it and, "If execution of the declarative completes normally the
  execution of the run unit is terminated abnormally" (item 2); with no declarative a `>>PROPAGATE ON` directive
  propagates it (item 3) and otherwise the run unit is terminated abnormally (item 4). At `--std 2002` and `2014`
  therefore the SORT/MERGE guard and the implicit-transfer hooks (`__IoCheckEc` without `__verbRule`) dispose of a
  checked fatal condition as they do for every other statement; the verbs' own sentences (the statement terminates, the
  results are undefined) say what the statement does, and the fatal exception procedure decides the run unit. With
  checking off the edition-independent rules of this entry apply unchanged. Goldens `pb5612_*_by_edition`,
  `pb5612_sort_file_open_fatal_2002`, `pb5612_sort_fatal_io_propagates_2002`. A terminated statement performs none of
  its remaining implicit functions (that determination's (d)), so the USING file of a MERGE terminated by -SEQUENCE is
  still open afterwards (the program's own CLOSE succeeds, golden leg G). The implicit USING release's size test (GR12
  b)) cannot fire from source: §2023:14.9.40.3 SR5 refuses, at compile time, a USING file that describes a record larger
  than the sort file's (kb/Work PB995), so that test is pinned at the unit level
  (`SortMergeStatementEcTests.ImplicitRelease_TestsTheReadSize`). The same names raised by a RELEASE or RETURN statement keep the general fatal
  disposition (§2023:14.6.13.1.3 5)/7)), since the executed statement is not a SORT or MERGE. **(c) With checking off**
  nothing is raised (§2023:14.6.13.1.1) and the statement proceeds: an open USING/GIVING file meets its implicit OPEN's
  '41', an out-of-range record is released as shaped (a DEPENDING ON size slices or space-extends the record area as
  the lenient reference modification `(1:size)` would; at COBOL-2002 and later only — COBOL-85's RELEASE is unsuccessful,
  D-SORT-RELEASE-SIZE-85), an out-of-order MERGE input merges in stream order, and a
  nested SORT/MERGE runs. Every one of those results is "undefined" in its rule. (kb/Work PB1036; golden
  `2002/pb1036_sort_merge_statement_ec`.)
