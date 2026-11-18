## Listing step 1, review round 1: how each finding was fixed

- **R1-1 (heading cut its facts).** `TextListingRenderer.IdentificationLine(header, program, page, width)` now composes the
  whole first heading line, and when it does not fit the fields yield in a fixed order: the version (at its end, so a
  release build's commit goes first), then the edition, then the program-name; the instant and the page number never
  yield. The old `Fit` (which cut the left part, instant and program-name included) is deleted. Siblings swept and
  fixed: heading line 2 cut a long source path at its end and so lost the file's name (it now keeps the end behind
  `...`); a COPY banner was never fitted at all and could exceed the page (it now wraps onto `+` rows as a long line
  does). Tests: `TextListingRendererTests.TheHeading_*` (the release-shaped version, a 30-character `--std 85` name, at
  widths 100, 132 and 255) and `ALongPath_NeverWidensARow`. DESIGN-listing.md sections 6.1 and 6.2 and the manual's
  `listing.md` say what yields.
- **R1-2 (failed-compilation tests incomplete).** New `Conformance/Listing/ListingOutcomeTests.cs`: check-only, the
  refused assembly write of the last return (the listing is still written), an output refused before the source is
  read (no listing), a refused listing write (one error, outcome unchanged), and the PB1066-shape listed pass of a
  failed parse (the lines are listed in the form the REPORTED pass read them in; seen red when `Frontend.Parse` listed the
  last pass instead). `Listing1985Tests.AFailedCompilation_StillListsItsSource` now asserts the `ListingSectionStatus`
  of every section for the front-end and bind errors. A backend error has no source that reaches it, so
  `Unit/Listing/ListingOutcomeMapTests.cs` holds `CompilerDriver.ListingOutcomeOf` (now internal) total over every
  outcome. `Unit/Listing/ListingLineIndexTests.cs` covers the index over hand-built maps: a copybook copied at two
  depths, framing lines, a file no inclusion holds, and omission. The design's sections 4.4, 4.6, 10 items 7 and 8 and 11
  name these tests, and section 4.4 says that step 3 adds the anchoring assertion. The deferral is no longer recorded
  only in a report.
- **R1-3 (no-listing path evaluated trace arguments).** Every recording call into `TextStageTrace` is now made only when
  `TextStageTrace.Records` is true (`CopyProcessor.ExpandCopiesOneLevel` Begin and End,
  `ConditionalCompilationProcessor.Run.RenderCopybook` Place, and `ConditionalCompilationProcessor` ToResultantFrame). The
  sibling sweep found all four call sites. `TextStageTrace.None`'s recording methods now THROW, so a future call site
  that skips the check fails `ListingOffDoesNoWorkDriftTests` (whose program has a COPY) and every compilation of a COPY
  without a listing. DESIGN-listing.md section 9 states this rule.
- **R1-4 (section 11 disagreed with the code).** Section 11 step 1 now names `ExpandCopiesOneLevel` as the recording site,
  puts `ListingOffDoesNoWorkDriftTests` in the Unit assembly, lists every test file step 1 built, and gives the
  benchmark's `[Params] Listing` of type `CompileBenchmarks.ListingMode`, with `[MemoryDiagnoser]` already on the class.
