## COPY names are user-defined words below 2002 (PB5482); the COBOL7039 area check counts expanded tabs (PB5503)

Two mechanisms of the COPY processor, one commit each, wave 20262027 group C.

**PB5482.** At --std 85 a data-name, index-name, paragraph-name or program-name spelled like a COPY statement's text-name or
library-name compiled (`COPY IX.` beside `INDEXED BY IX`, re-probed: exit 0, "RAN  B"). §1985:IV.4.2.2.1.1 makes text-names and
library-names user-defined words of disjoint sets of their own, "excluding any contained program"; §2002:F.2 item 135 lifts it. The
preprocessor alone resolves a COPY statement, so the names never reached `DataBinder.CheckOneTypePerWord`. Now `CopyProcessor` records
each name written as a word (`CopyNameUse`), `Frontend.CopyNames` and `EditionContext.CopyNames` carry them, `CopyNameCensus` assigns
each to the program unit it stands in, and `DataBinder.DeclareCopyWords` declares them first through `DeclareUserWord` (new kinds
`TextName`, `LibraryName`, gated by `EditionInfo.CopyNamesAreUserDefinedWords`). Every word kind is therefore checked, not only index-names;
the refusal is COBOL2692 sited at the second declaration, and 2002 onward is unchanged. Goldens `pb5482-copy-text-name-as-index-name`
(refusal) and `pb5482-contained-program-text-name` (accept: a contained program's names are its own), plus `CopyNameUserWordTests` for the
paragraph-name, program-name, library-name, text-name-also-library-name and edition arms. `1985-SR-VI.5.8.3-14` recorded CONFORMS (1985
GAP 229 to 228).

**PB5503.** The COBOL7039 room check (a literal a replacement puts on a debugging line must not need a continuation there) measured the
area a pseudo-text-2 literal was written in from the file's physical column, which counts a tab as one position; the line-entry stage
expands a tab to the next stop. Re-probed: a literal after a leading tab was refused though in area A, and a literal a tab moved into
area B was accepted. `ReferenceFormatMap.ProgramTextOffsetAt` (with `ColumnMap.ToExpanded`) now gives the expanded offset, and both
sites use `CopyProcessor.ProgramTextOffsetAt`; the formats of the texts read are passed into `ApplyReplaceStatements` and
`ApplyReplacements`. Goldens `pb5503-replace-literal-tab-area-a` (accept) and `-area-b` (refusal), and two unit facts.

Review round 1 (finisher): CopyNameCensus attributed a COPY written after a contained program's END PROGRAM to that ended unit; the census now picks the innermost unit whose first and last tokens bracket the statement (BinderDriver.UnitLastLine/LastToken, the mirror of FirstToken), pinned by CopyNameUserWordTests.ACopyAfterAContainedProgramEnds_BelongsToTheContainer. A stale cref in CopyProcessor.cs was corrected.
