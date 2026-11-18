## PB5925: the source listing, step 5 - the options, the program map, the statistics and verbs, and --listing-sections

Step 5 of `docs/rearchitecture/DESIGN-listing.md` (kb/Work PB5925; the owner's order of 2026-11-18 to build the full
listing). The default listing (`standard`) now has five sections, each starting a page: the options, the source, the program
map, the statistics with the verb counts, and the diagnostics summary. `--listing-sections` chooses them.

**What was built.**
- The model (`WiseOwl.Cobol.Listing`): `ListingOption` with `ListingOptionOrigin` (Default, Specified, Host);
  `ListingStatistics` with `ListingLineCounts` (every line kind counted, zero included, plus omitted, copied and unlisted
  lines), per-file counts, counts by severity and `ListingParseStatistics` (programs, statements, `ListingVerb`s);
  `ListingSection` gains `Options` and `Statistics`. `ListingSectionSet` is a value set over that one enum, so `all` holds a
  section the day it joins it; `ListingSections` is the one table of the command line's section words and their parser;
  `ListingRequest.Sections` (default `standard`) is checked by `SectionsProblem` in the constructor and every `init`.
- `OptionsSectionBuilder` (composition root): one row per settable public property of `CompilerDriver.Options` and of
  `ListingRequest`, each named as the command line spells it, then the compiler, runtime, .NET and operating system.
- `SourceUnitFacts.Statements`: each `statement` node of the parse, its verb as written (the first word in upper case) and
  position, so the verbs exist when the bind fails. `StatisticsSectionBuilder` counts the finished lines and the reported
  diagnostics and places each verb's statements through `ListingLineIndex`.
- The builder holds only the sections asked for (one request, one document, design section 13 item 2) and computes
  everything regardless, so a listing without its source section still counts every line. The text renderer dispatches on
  `SectionOrder`; the options, program map and statistics rows are classes of their own (`OptionsText`, `ProgramMapText`,
  `StatisticsText`). The schema is regenerated; the test validator `JsonSchemaSubset` now resolves `$ref`, which the
  exporter writes once the statistics hold one type twice.

**Design corrections** (each marked "(step 5)" in the design): the options table lives in the composition root, since the
builder's layer may not name `CompilerDriver.Options`; `Sections` is a value set over the one enum, because the JSON contract
refuses a flags enum in the model and a second enum of sections would be a second list; a section joins the enum with its
builder, as a format does, so `--listing-sections` names only sections the compiler produces; the header, files,
inclusions and programs are always in the document (the lines and every page heading refer to them); the section visitor
of section 7 waits for step 9's HTML renderer, the second renderer that walks sections.

**Tests.** `OptionsSectionDriftTests` (every settable property has one row, and setting it alone changes exactly that row),
`ListingSectionsTests`, `ListingSectionRenderingTests`, `ListingCliTests.TheListingSections_ReachTheRequest` and the
Conformance `ListingSectionsOfACompilationTests` (the program map, the verbs through two copies of a library text, the line
counts, the verbs of a failed bind, the options, only the sections asked for in text and JSON). Expected values come from the
files the tests write and the design's column layout. Six mutations were each shown red and reverted: a dropped options row,
a row reading another property, a section missing from the text order, nested statements not counted, the source lines kept
when not asked for, and unlisted lines miscounted. The page tests of earlier steps read a section's pages through
`ListingPages` instead of assuming the source is page 1.

**Measured** (tests/WiseOwl.Cobol.Benchmarks/README.md, step 5): the standard text listing is +0.2 to +5.4 % over no listing on
a full compile, inside the 10 % budget; step 5 adds +0.38 to +0.65 MB per program to the front end and bind with a listing;
the no-listing rows are within 0.23 MB of step 4a's.
