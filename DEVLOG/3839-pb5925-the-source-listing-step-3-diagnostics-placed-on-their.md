## Entry 3839 — 2026-11-18 05:14 PST — PB5925: the source listing, step 3 - diagnostics placed on their lines, summarised, and cited as data

Step 3 of `docs/rearchitecture/DESIGN-listing.md` (kb/Work PB5925's decision): every diagnostic of a compilation becomes a
record, and the listing places each under the line it names, lists them all in a summary, and carries the rule each cites as
data.

**One diagnostic record for every phase.** The front end's `Diagnostic` is the record; its `Location` is now nullable (a
bind diagnostic with no cursor, a backend diagnostic with no position). The backend seam's `BackendDiagnostic` was the same
record without the span, so it is deleted (rule 4): `RoslynBackend.Neutral` returns `Diagnostic`, and the conversion "at the
driver" that design review R5-1 asked for has nothing to convert. `EditionContext` keeps `Records` (errors and warnings in
report order, each made by one `Record` from the cursor); `Diagnostics` and `Warnings` are rendered views, byte-identical to
the strings they were; `EcNameResolution` reads records instead of scanning lines. `CompilerDriver.Result.Diagnostics`
carries the records on every outcome: the front end's, the binder's, each expansion's own compilation's moved to its EXPANDS
entry (`ExpansionAssemblies.AtTheEntry(Diagnostic)` is now the one decider; each printed line of the expansion's report is its
moved record's rendering), and the backend's errors. The test compile cache stores them (format 4).

**The resultant line, and its frame.** The design said front-end diagnostics "gain the resultant line where the reporter
knows it". The code showed two things wrong with that: it is a change at every reporter, and a text stage numbers lines in a
frame of its own, so a bare resultant line can name the wrong copy of a copybook. The fact now lives where the mapping is
made: `SourceLocation` carries `ResultantLine` and `Frame`, which only `SourceLineMap.Locate` sets, and each map has a
process-unique `Frame`. Every diagnostic located through a map carries them, and no reporter changed. The listing trusts the
line only in the listed pass's frame (`ListingLineIndex.Place`).

**The listing.** `DiagnosticSectionBuilder` places each record (the inclusion of its resultant line in the listed frame, else
the first text listing that physical line; nothing for a file the listing does not list, so no listing names generated
source), converts the column to a position of the listed text, orders them (by line and column, then the rest) and writes
each line's ids. The model gains `ListingDocument.Diagnostics`, `ListingLine.Diagnostics`, `ListingDiagnostic`,
`ListingSeverity`, `ListingCitation` and `ListingSection.Diagnostics`; the schema is regenerated and stays 1.0, since no
release has shipped the JSON listing. The text listing prints a caret under the column of a line printed whole, then
`==> severity CODE (col n): message` wrapped at the page width, and ends with a `DIAGNOSTICS` summary page (counts by
severity, every diagnostic with listing line, file, line and column).

**Citations as data.** `ListingCitation.Scan` ports `scripts/spec/citation.py`'s `scan`; one vectors file,
`tests/WiseOwl.Cobol.Tests.Unit/Listing/citation-vectors.json`, holds the hand-written answers both readers must give
(`ListingCitationTests`, and a new case of `citation.py --self-test`; each shown red on a wrong vector). The field is a list,
`citations`, because a message can cite several rules. Open question 3 is decided against a structured citation beside the
text: `EditionInfo.Cite` chooses whole texts (38 calls), the compiler's messages carry their citations inline in about 9,000
literals, and a second copy would be kept in step with the first by nothing.

**Layering.** `compiler.binding -> frontend.diagnostics` and `compiler.listing -> frontend.diagnostics` are added to
`docs/rearchitecture/architecture-edges.json` (and DESIGN-architecture-review.md says why): the binder keeps the front end's
record, and the builder places it. `citation-vectors.json` is a `TOOLING_READS` entry of `scripts/lane_classification.py`,
since `citation.py --self-test` reads it.

**Tests.** Unit: `DiagnosticRecordsTests`, `ListingCitationTests`, `ListingLineIndexTests.ALocation_IsPlacedInTheBlockOfItsResultantLine_OnlyInTheIndexsFrame`,
`TextListingRendererTests` (caret, wrap, summary); the rewritten `ExternalRepositoryExpansionTests.ADiagnosticOfAnExpansionText_IsRelabelledToTheExpandsEntry`.
Conformance: `ListingDiagnosticsTests` (a bind error placed, printed and cited `§2023:13.18.40.3 SR14`; a diagnostic in the
second copy of a copybook placed in the second block; one record per printed diagnostic on a front-end error, a bind error
and a success with warnings) and the anchoring assertion design 4.4 asked of `ListingOutcomeTests.AFailedParse_ListsThePassWhoseDiagnosticsAreReported`.
The copybook cases go red with the frame branch of `Place` switched off.

**Measured** (design section 9; the README's step-3 table). Step 3 changes the no-listing path, so it measured `None` against
its parent, and the first run was OVER budget: IX113A's front end and bind allocated 0.85 MB more than the parent. An exact
probe (`GC.GetAllocatedBytesForCurrentThread` over 30 check-only compiles) and the runtime's allocation-tick sampling named the
cause: `MarginRCutScreen.Warnings` built a closure capturing the cut's `SourceLocation` once per CHARACTER of every token,
before asking whether there was a cut (about 112,000 closures, 5.4 MB per compile of IX113A), and the two new integers made
each 8 bytes larger. The screen now asks with a loop (`MarginRCutScreen.Holds`): IX113A's check-only compile allocates
68.1-68.2 MB against the parent's 73.1-73.7 MB, 7 % less. The same measurement found the text renderer copying the whole
listing to append the summary (1.4 MB on IX113A); both sections are now composed into one buffer (`PageComposer.Compose`
appends), and the text listing's cost over no listing is the parent's again.

**Leads filed** beside kb/Work PB5925: a text stage's diagnostic in a twice-copied copybook is placed in the first block
(`2026-11-18-text-stage-diagnostic-placement`); the model's records compare their lists by reference
(`2026-11-18-model-list-equality`).

