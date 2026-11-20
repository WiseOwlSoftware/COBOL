## The two low leads of the PB5947 review, fixed in PB5949 (branch listing-step-pb5949)

Both were recorded by the branch review (`review-listing-step-pb5947.jsonl`, R1-1 and R1-2) and landed unfixed with PB5947. Each was
re-read on origin/main 7f7ae8c3da and was still true; PB5949 (the controller's rule of 2026-11-19: a CLEAN review's low leads are the
next step's) fixed both at the root.

- **R1-1 (comment accuracy).** `DiagnosticSectionBuilder.FlagOf` named `DiagnosticRecordsTests.TheFlagKindMap_IsTotal`; the test is
  `FlaggedConstructTests.TheFlagKindMap_IsTotal`. The comment names it now.
- **R1-2 (architecture).** The registry row `merge-in-sort-merge-proc-removed-2023` (removed in 2023, pinning COBOL1572) was reported by
  hand in `VersionConformancePass` with `DiagnosticCatalog.MergeInSortMergeProc` and the removed-construct severity, outside
  `ConstructRegistry.VerdictAt`, so it carried no `EditionFlag` and the table of flagged constructs omitted it, while
  `EditionFlagDriftTests` stayed green (its scan knew the three band codes only). The report now takes its code, severity and flag from
  `VerdictAt` (same code and severity; the flag is new); the scan knows every code a removal row pins (all but the user-defined-word rows'
  COBOL0901, which DESIGN-listing.md section 6.11 decides is no construct) and every catalog descriptor of one, shown red on the pre-fix
  source (findings at `VersionConformancePass.cs:486` and `:487`) and on planted text. Witness
  `BoundArmDiagnosticPositionTests.MergeInASortProcedure_At2023_IsAFlaggedRemovedConstruct`. DESIGN-listing.md section 6.11 corrected.
