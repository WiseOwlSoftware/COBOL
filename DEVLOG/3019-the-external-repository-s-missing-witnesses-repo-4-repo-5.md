## Entry 3019 — 2026-11-02 22:22 PST — The external repository's missing witnesses: REPO-4, REPO-5, design 17.1's slice-7 cases, and one byte patch of the runtime version (kb/Work PB4455, PB4458, PB4460)

Wave 20261840 group B. Every note was re-probed on a fresh Debug build (0 warnings, 0 errors) before any edit. All three are
test-only: no program that compiled before compiles differently.

**PB4455.** Nothing in the tree witnessed REPO-4 (COBOL5965) or REPO-5 (COBOL5966) end to end, nor four of the multi-unit
cases design 17.1 lists for slice 7. The damaged-record fixture is a same-length byte patch of a compiled library's attribute
blobs, found by their ECMA-335 SerString form (a length byte then UTF-8, so a named argument matches, a longer text containing it does
not): `RenameNamedArgument` renames the mandatory `SignKind` argument (the reader refuses the entry), `BreakIndexRow` makes a `Units` row
one field instead of two (the index refuses it) and `RedirectRedefinition` points a `REDEFINES` at a name no preceding sibling carries
(the reader accepts the record and the codec cannot rebuild it). Three Facts in `ExternalRepositoryResolutionTests`, each asserting the
code, the file named, and that no COBOL5962 (REPO-1) and, for REPO-5, no COBOL5965 follows; REPO-5 compiles the undamaged entry first. The
four cases are under `conformance-suite/wiseowl/cases/12.3.8-REPOSITORY/`: REPO-13 across the repository, the word reaching a later-compiled
unit, the pb1115 split (EC-PROGRAM-ARG-MISMATCH checking on in both units, so a layout the activation judged different would show), and
an 85 program called through a prototype, with its COBOL0900 refusal. The 85 case is the one the suite could not state: every step
compiled at the test's edition. A build step may now name `std` (`wiseowl/options/<id>.<step file>.json`; refused for a whole test, which
runs once per accepted edition and could not keep that list): `generate.py` `STEP_EDITION_OPTION` with a self-test arm,
`ConformanceSuiteRunner.WithWiseOwlOptions`, the README (which also gains `repository-update`, the omission PB4185 names) and
`DESIGN-conformance-suite.md`. The two outcomes a case cannot observe, COBOL2969 across the repository and no COBOL5969 for the split, are
Facts in the same class (the split's control compiles a prototype whose layout differs and requires the warning).

**PB4458.** Discharged as already witnessed: the three `ExpansionPlacementPass` arms (the `TargetFile.Skewed` overwrite, `ReportForeignState`,
`ReportSameName`'s warning and error) have Unit tests that the PB2104 branch's round 1 commit (`82c85ae3d`) added before it landed; the note was
filed from the review of the head before it. Each asserts the arm's own message. The interface wording the note could not reproduce
("REPOSITORY CLASS" for an interface) is confirmed correct by `TheForeignStateRefusal_OfAnInterfaceExpansion_NamesAnInterfaceEntry`.

**PB4460.** `MakeStale` hand-built `Major - 1`. The patch loop it sat in existed three times (`MakeStale`, the Unit REPO-12 expansion test, the
UTF-16 variant in `RunUnitTerminationTests`); it is now `RuntimeVersionSkew.Patch(dll, encoding)`, with the same-length check inside, and
`MakeStale`'s skew text comes from `RuntimeVersionSkew.OtherMajor`.

Rows: none claimed, none closed (`closes_rows: []` with a reason on each). The new cases are witnesses PB1086 may cite for GR-12.3.8.4-10 and
-11, whose inventory text still says the external-repository arm is not implemented. Codes: none used.

