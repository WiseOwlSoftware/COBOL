# Listing step 9, review round 1: how each finding was fixed

- R1-1 (a DISPLAY transfer inside a COPY ... SUPPRESS block was written inside the collapsed block): `HtmlListingRenderer`'s
  `SourceWriter.Displays` now closes the open inclusion blocks from the outermost SUPPRESS one in before it writes the transfers,
  and the inclusion's next line reopens its block through the one `Open` (its banner and "(continued)", no second id; `_opened`
  records which blocks have been opened once). Sibling sweep: the hidden-line runs already end at a line with a transfer; the
  diagnostics and object code under a hidden line are about that line and stay with it (the summary links reveal them); no other
  output stands under a line. DESIGN-listing.md 7.2 now states the mechanism; the user guide's HTML section says it. Witness:
  `HtmlListingRendererTests.ADisplayInASuppressedInclusion_StandsOutsideTheCollapsedBlock` (red with the break disabled).
- R1-2 (the HTML renderer re-typed the text listing's words): every word the page shares with the text listing is now read from
  the text listing's helper that prints it: `TextListingRenderer.CountsText`, `PlaceText`, `DiagnosticText`, `DisplayText`,
  `CompilerText`, `EditionText`, `ReplacedMarker`; `ListingSections.NotProduced`; `DataMapText.ItemHeadings`, `FileHeadings`,
  `FilesTitle`, `NumberText`, `LevelText`, `OrganizationText`, `AccessText`, `FlagWords`; `ObjectCodeText.Token` (now public),
  `RangeText`, `StretchText`, `AssemblyLabel`, `MethodHeadings`, `RangeHeadings`, `NoCodeNote`; `ProgramMapText.Headings`,
  `RecoveredNote`; `OptionsText.Headings`; `StatisticsText`'s labels, `VerbHeadings`, `NotParsedNote` (the divergent
  "Programs and statements" copy is gone), `RecoveredNote`, `CountsText`, `DiagnosticCountsText`. The text renderers print
  through the same members. Siblings beyond the finding's list, fixed the same way: the "Not produced" note, the section
  headings of every table, the identification's compiler and edition words, the 'R' marker, the DISPLAY row, the no-code note,
  the program map's recovered note, the statistics' per-file counts and diagnostic counts, the EXTERNAL / GLOBAL words (now after
  the records, where the text prints them), and `IlDisassembler`'s private `IL_xxxx` branch-target spelling (now
  `ObjectCodeText.Offset`). The new `HtmlListingRendererTests.EveryWordThePageSharesWithTheText_IsTheTextListings` renders one
  document holding every section both ways (and once with no parse counts) and finds every shared word of the page in the text
  listing (red with the old not-parsed copy restored). DESIGN-listing.md 7.2 states the rule ("ONE SET OF WORDS").
