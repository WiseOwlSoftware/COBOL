# DESIGN - the source listing

> **Status: DESIGN, APPROVED BY THE CODEBASE OWNER (Claude, CLAUDE.md "Ownership"; decision kb/Work PB5925). STEP 1 BUILT
> (the 1985 standard minimum, kb/Work PB5923, branch `listing-step-1`); STEP 2 BUILT (the JSON listing and its schema, branch
> `listing-step-2`); STEP 3 BUILT (diagnostics, branch `listing-step-3`); STEP 4a BUILT (the directive hooks, witnessed at 2023,
> branch `listing-step-4a`); STEP 4b BUILT (the hooks witnessed at 2002 and 2014, branch `listing-step-4b`); STEP 5 BUILT (the
> options, the program map, the statistics and verbs, `--listing-sections`, branch `listing-step-5`); STEP 6 BUILT (the
> finished symbol table, the data map and file map, the program attributes and the margin, branch `listing-step-6`); STEP 7a
> BUILT (every name slot of the grammar labelled and held by `GrammarNameSlotDriftTests`, branch `listing-step-7`); STEP 8 BUILT
> (one sequence point per COBOL statement and a portable PDB from the Roslyn backend, the object-code map and the object code read
> from the assembly and its PDB, branch `listing-step-8`); STEP 6b BUILT, LANDED BY NAME CLASS (the one store of names,
> kb/Work PB5932; slice 1, branch `listing-step-6b`: the drift test, the table's place, `Identify`, `Declarations`, the cd-names
> and the file-names, and the temporary shrink-only baseline of the classes not yet moved; slice 2, branch `listing-step-6b-2`: the
> tables keyed by no COBOL user word re-keyed by types of their own, and the procedure-names; slice 3, branch `listing-step-6b-3`:
> the constant-names; slice 4, branch `listing-step-6b-4`: the CONFIGURATION SECTION names; slice 5, branch `listing-step-6b-5`: the
> REPORT SECTION names; slice 6, branch `listing-step-6b-6`: the program-prototype-names; slice 7, branch `listing-step-6b-7`: the
> function-prototype-names; the OO lane, branch `listing-step-6b-oo`: the object-class-names, interface-names and method rosters in the
> compilation group's tier; the data-name lane's first slice, branch `listing-step-6b-data`: the DEBUG-ITEM, type, CAPACITY register and
> index-names; the closing slice's first session, branch `listing-step-6b-close`: the data-name, condition-name and screen-name tables,
> the USE and WHEN file registers, the end markers; remainder slice 1, branch `listing-step-6b-r1`: every procedure-division data-name
> and condition-name whose item is used identified through `IdentifyDataName` and `IdentifyConditionName`; the entry-id lane, branch
> `listing-step-6b-ids`: every class the table declares enters its entry where it is declared and every identification hands back
> its id; remainder slice 2, branch `listing-step-6b-r2`: the file clauses' operands kept as a `WrittenDataName` and resolved by the
> table, the record-key-names read off their file; remainder slice 3, branch `listing-step-6b-r3`: the data-name candidate sets and the
> qualifier walk private to the table behind the written words, the data-entry and report clause operands kept as a `WrittenDataName`;
> remainder slice 4, branch `listing-step-6b-r4`: the data division's declarations, the §2023:8.3.2.2 census, the procedure division
> header's operands and the CORRESPONDING pairing; remainder slice 5, branch `listing-step-6b-r5`, the last: the group tier's function
> units, the parameterized expansion's names in the group's tier, the TURN file operand, the refused record-key operands and every
> data-name's and condition-name's entry made where it is declared, the baseline emptied and deleted; sections 6.7.1 "Step 6b, as
> built so far" and 11); STEP 9 BUILT (the HTML page, the section visitor, branch `listing-step-9`); kb/Work
> PB5951 BUILT (the object code's exception-handling regions, branch `listing-step-pb5951`); STEP 7b BUILT (the
> occurrences, the cross-reference and the HTML page's name links, branch `listing-step-7b-2`; section 6.7.1, "Step 7b, as
> built"). Section 11, "What is not built", lists every
> part of this design not yet built and why.** Where building a step proved this document wrong it was
> corrected in the same change; each correction is marked "(step 1)", "(step 2)", "(step 3)", "(step 4a)", "(step 4b)", "(step 5)",
> "(step 6)", "(step 6b)", "(step 7a)", "(step 7b)", "(step 8)", "(step 9)" or "(PB5951)" in the section it changed.
> PB5947 BUILT (the diagnostic summary's table of flagged constructs by edition, section 6.11, branch `listing-step-pb5947`).
> kb/Work PB5941 (branch `listing-step-pb5941`, the `all` row's budget): every format streams to its file, the page shows each IL
> instruction once, the object code is decoded into one array per method; corrections marked "(PB5941)" in sections 4.2, 7, 7.1,
> 7.2, 7.3, 8, 9, 10, 11 and 13; review round 1 (R1-1) corrected sections 4.2, 8 and 10 (marked "PB5941 review R1-1").
> PB5949 BUILT (the source's syntax colouring in the HTML page, from the compiler's own reading of the text, section 7.2, branch
> `listing-step-pb5949`; corrections marked "(PB5949)").
> Architecture review round 1 (2026-11-17, `review-w20262103-listing.jsonl`): R1-1 to R1-4 and the leads R1-6 to R1-9 are
> answered in sections 2.2, 3, 4.2, 4.4, 4.4.1, 4.6, 6.7.1, 9 and 11; R1-5 is a finding against the citation audit.
> Round 2 (same record): R2-1 in sections 6.7.1 and 6.7.2 (one recording point, `SymbolTable.Identify`), R2-2 in 6.7.1, R2-3 in 2.2;
> the owner's order of 2026-11-18 (build the full listing support) in section 11.
> Round 3 (same record): R3-1 and R3-2 in sections 6.7.1 (both halves structural) and 6.7.2 (mechanism, then evidence).
> Round 4: R4-1 (`DeclarationFact`), R4-2 (name equality private, classified) and R4-3 (per-element slot count, planted fixture) in 6.7.1 and 6.7.2.
> Round 5: R5-1 in sections 4.3, 6.3 and 6.9 (a backend diagnostic crosses the seam as the front end's backend-neutral
> `Diagnostic` with a COBOL position), R5-2 in section 6.7.1 (a use-site label classifies a reference rule's head slot, so a
> section or paragraph header is `def`), R5-3 in the evidence file (the store is `SymbolTable`).
> Owner directions of 2026-11-18, folded into round 4: the listing is backend-neutral (no C#: section 6.9 reads the assembly and its PDB),
> and it is built from the source plus the symbol table (section 4.1; `SymbolTable` is the one store of names, section 6.7.1).
> Work register: kb/Work PB5923 (the 1985 requirement, closed by step 1 of section 11), PB5925 (the decision this design
> carries out), PB5924 (the 1985 catalog rows the listing makes testable).
> Inputs: `E:/COBOL-coord/scratch/my-20261115/listing-survey/listing-requirements.md` (every listing rule of every
> edition) and `survey.md` (competitors). Every citation below was re-checked with `python scripts/spec/cite.py --check`
> on 2026-11-17; every verdict was `OK`.

## 1. Purpose

The owner, 2026-11-17: "Our compiler should have the best listing." 1985 requires a listing (PB5923); 2002, 2014 and
2023 leave it to the implementor. This design builds ONE listing model from the compiler's own data and renders it three
ways: a paginated text listing for people and printers, a hyperlinked HTML page, and a versioned JSON document with a
published schema.

The bar is IBM Enterprise COBOL's listing in every section, plus what no competitor has (survey.md section 10):

| Section | IBM Enterprise COBOL 6.x | This design |
|---|---|---|
| Source | SOURCE, NUMBER, COPY text | Every physical line of every file in inclusion order, with file and line, COPY depth, the REPLACE and REPLACING results shown at the line they changed, conditionally omitted lines marked, debugging lines marked |
| Diagnostics | FLAG, inline and at the end | Inline at the line and column, summary at the end, each citing the rule of the edition in force; listing line and file line both shown |
| Options | Options in effect | Every option, its value and whether it is the default; compiler, runtime and .NET versions; edition |
| Data map | MAP: offsets, lengths, nested-program map | Every item including INDEXED BY indexes, constants and condition-names: level, offset, length, usage, picture, OCCURS/ODO, REDEFINES/RENAMES, and the .NET field or type the item became |
| Cross-reference | XREF: defined and referenced lines | Every use classified defined / read / written / modified, for data, procedures, files, programs and copy members; the PERFORM and CALL graph |
| Verb counts and statistics | VBREF, statistics | Verb counts with every line, lines by kind and by file, diagnostics by severity |
| Object code | LIST / OFFSET (assembler) | Each COBOL statement's method and IL offset range, read from the produced assembly and its portable PDB, and (on request) its IL: the same for any backend |
| Machine-readable | SYSADATA (binary records; the text listing "is not a programming interface") | Versioned JSON with a generated schema: the stable programming interface |
| Standard hooks | EJECT, SKIP, TITLE; others not found | '/' and '*', DATE-COMPILED, `>>LISTING`, `>>PAGE`, `>>DISPLAY` UPON LISTING, COPY SUPPRESS, the effect of logical conversion: all of them |

## 2. Requirements, edition by edition

### 2.1 1985 (required)

| # | Rule | Citation | How this design meets it |
|---|---|---|---|
| 85-1 | "each compiler accepts source programs written in reference format and produces an output listing of the source program in reference format" | §1985:IV.7.1 | On by default at `--std 85` (section 8); every physical line listed as written |
| 85-2 | "The meaning of lines and character positions is defined by the implementor" | §1985:IV.7.1 | A line is a physical line of the file; the medium is a file; page length and width are options (section 7.1) |
| 85-3 | "The slant in the indicator area causes page ejection prior to printing the comment line in the listing of the source program"; a '*' line prints "at the next available line position in the listing" | §1985:IV.7.2.4 | `ListingLine.PageBreakBefore` for a listed '/' line (section 4.5); '*' lines print in sequence |
| 85-4 | "The entries on the output listing need be indented only if the input is indented" | §1985:IV.7.4 | The text of each line is the physical text, so indentation is as written |
| 85-5 | the program-name "identifies the source program, the object program, and all listings pertaining to a particular program" | §1985:VI.3.3.4 GR1 | Every page heading names the program containing its first line; the program map lists every program (sections 6.1, 6.5) |
| 85-6 | the same for nested programs | §1985:X.3.1.4 GR1 | The heading names the innermost program; the program map shows the nesting |
| 85-7 | DATE-COMPILED "provides the compilation date in the Identification Division source program listing"; the paragraph "is replaced during compilation with a paragraph of the form" `DATE-COMPILED. current date.` | §1985:VI.3.4.1, §1985:VI.3.4.4 GR1 | The listing prints the paragraph as `DATE-COMPILED. <compilation timestamp>.` in place of the comment-entry (section 6.2) |
| 85-8 | DATE-COMPILED is obsolete but required: obsolete elements "in the Identification Division have been placed in the obsolete element category"; "A conforming implementation of Standard COBOL is required to support obsolete language elements" | §1985:C.1, §1985:I.1.5.2.3 | As 85-7 |
| 85-9 | WHEN-COMPILED: the "compilation date and time if provided in the listing of the source program" denote the same time as the returned value | §1989:A.2.46.3 RV3 | One compilation timestamp (section 5) |
| 85-10 | "An implementation must accept the syntax and provide the function for all Standard COBOL language elements" | §1985:I.1.5.2.2 | The '/' line and DATE-COMPILED have their function |

### 2.2 2002, 2014, 2023 (implementor-defined; every rule applies once a listing is produced)

| # | Rule | 2002 | 2014 | 2023 | How this design meets it |
|---|---|---|---|---|---|
| L-1 | Listings outside the standard's scope: "The form and content of listings produced during compilation, if any"; so are "The form or content of error, flagging, or warning messages" (the same text in all three editions) | §2002:1 | §2014:1 | §2023:1 | The form is ours; this document is its definition |
| L-2 | "Whether the compiler produces a source listing is implementor defined" | §2002:7.2.15.2 GR1 | §2014:7.3.16.2 GR1 | §2023:7.3.18.3 GR1 | Produced on request (`--listing`); documented in DOC-A.1-117 |
| L-3 | Annex item: "Listings (whether and when produced by the compiler, effect of logical conversion). This item is required" (2002 item 90, 2014 item 106, 2023 item 117) | §2002:B.1 | §2014:A.1 | §2023:A.1 | DOC-A.1-117 rewritten (section 12) |
| L-4 | Logical conversion: "The implementor shall define the effect on the source listing, if any, of logical conversion" | §2002:6.4 | §2014:6.5 | §2023:6.5 | The listing shows the physical lines (before conversion); continuation lines are listed as written; DOC-A.1-117 says so |
| L-5 | Stage: "The implementor defines the stage during which actions associated with listings, if any, take place"; the annex item of the stage (2002 item 165, 2014 item 182, 2023 item 200) "is conditioned on the implementor producing listings" | §2002:7, §2002:B.1 | §2014:7.1, §2014:A.1 | §2023:7.1, §2023:A.1 | The text-manipulation stage records the actions; the listing applies them after compilation (section 4.4); DOC-A.1-200 |
| L-6 | Comment lines: '/' "causes page ejection followed by printing of the comment line"; '*' at the next line position | §2002:6.2.6.1 | §2014:6.3.6.1 | §2023:6.3.7.2 | As 85-3 |
| L-7 | COPY SUPPRESS: library text not listed; "If a listing is being produced, the COPY statement itself is listed" | §2002:7.1.2.3 GR4 | §2014:7.2.2.3 GR4 | §2023:7.2.3.4 GR4 | `ListingInclusion.Suppress`; the visibility pass (section 4.5) |
| L-8 | After COPY, "the LISTING directive that is in effect for the COPY statement itself is considered to be in effect" | §2002:7.1.2.3 GR5 | §2014:7.2.2.3 GR5 | §2023:7.2.3.4 GR5 | The visibility pass restores the state at the end of each inclusion |
| L-9 | The NOTE: "It is recommended that the implementor provide a listing of the original compilation group" (2023: "The implementor can provide a listing of the original compilation group") | §2002:7.2.15 | §2014:7.3.16 | §2023:7.3.18.1 | The source section IS the original compilation group; the manipulated text is shown as annotations |
| L-10 | "The default LISTING directive is '>>LISTING ON'" | §2002:7.2.15.2 GR2 | §2014:7.3.16.2 GR2 | §2023:7.3.18.3 GR2 | Visibility starts ON |
| L-11 | "Each LISTING directive shall be listed, even if the listing is being suppressed by a LISTING directive" (the same text in all three editions) | §2002:7.2.15.2 GR3 | §2014:7.3.16.2 GR3 | §2023:7.3.18.3 GR3 | A LISTING directive line is always listed |
| L-12 | OFF: "source lines shall not be listed until a LISTING directive specifying or implying the ON phrase is encountered"; ON: "source lines shall be listed until either a LISTING OFF directive is encountered" or the end | §2002:7.2.15.2 GR4, GR5 | §2014:7.3.16.2 GR4, GR5 | §2023:7.3.18.3 GR4, GR5 | The visibility pass |
| L-13 | "a PAGE directive shall cause page ejection followed by listing of the PAGE directive" | §2002:7.2.16.3 GR2 | §2014:7.3.17.3 GR2 | §2023:7.3.19.4 GR2 | `PageBreakBefore` on a listed PAGE line |
| L-14 | "If a source listing is not being produced, a PAGE directive shall have no effect" | §2002:7.2.16.3 GR3 | §2014:7.3.17.3 GR3 | §2023:7.3.19.4 GR3 | No listing: the PAGE event is recorded only into `TextStageTrace.None`, which drops it |
| L-15 | DISPLAY directive (2023 only): operands transferred "to the source listing or compile-time-device-1 or both"; "Any conversion of data required is defined by the implementor"; with a listing, "the data transfer shall take place irrespective of whether it is suppressed by the LISTING directive"; UPON LISTING: "data is transferred to the same device as that used for source listings"; no UPON: "the default is as if UPON LISTING were specified" | | | §2023:7.3.12.4 GR1, GR2, GR5a, GR6 | A `ListingDisplayLine` at the directive's place, always listed; to standard output when no listing is produced (unchanged) |
| L-16 | WHEN-COMPILED: the same time as "the compilation date and time if provided in the listing" | §2002:15.80.2 RV3 | §2014:15.92.2 RV3 | §2023:15.99.3 RV3 | One compilation timestamp (section 5) |
| L-17 | The warning mechanism: "An implementation shall provide a warning mechanism that optionally may be invoked by the user at compile time" | | | §2023:4.2.10 | Not a listing rule; its warnings (COBOL2894) are diagnostics and appear inline |

**The 2002 inventory rows these rules are.** On this tree `inventory_view.inventory(2002)` reports twelve listing rows
at GAP: `2002-FMT-7.2.15.1`, `2002-FMT-7.2.16.1` (the LISTING and PAGE formats), `2002-GR-7.1.2.3-4`, `-5`,
`2002-GR-7.2.15.2-2`, `-3`, `-4`, `-5`, `2002-GR-7.2.16.3-2`, `2002-RV-15.80.2-3`, `2002-DOC-B.1-90` and
`2002-DOC-B.1-165` (`2002-GR-7.2.15.2-1` and `2002-GR-7.2.16.3-1`, `-3` are OK). Step 4b (section 11) builds the
behaviour they need and adjudicates all twelve. 2014 has no inventory (no rule catalog), so its rules are met by the
same code and witnessed by `Listing2014Tests`, with no rows to close. (Step 4b:) the behaviour needed no code: step 4a's
trace and visibility rules are edition-neutral, and both editions' rules are the 2023 sentences (each re-read and checked
in its own text; 2002 and 2014 have no PUSH, POP or DISPLAY directive and no exception-checking PERFORM). Ten of the twelve
rows are CONFORMS with `Listing2002Tests` as witnesses, and `2002-GR-7.2.15.2-1`'s prose, which said no listing is
produced, is restated. The two DOC rows, `2002-DOC-B.1-90` and `2002-DOC-B.1-165`, could not close: a 2002 DOC row's
CONFORMS owes its own register anchor `docs/CONFORMANCE.md#2002-DOC-B.1-<n>`, and section 7 of the statement is the 2023
A.1 register, whose audit refuses any other key (kb/Work PB5930, which holds both rows; it blocks every 2002 DOC row). Their
determinations are documented all the same, in DOC-A.1-117 and DOC-A.1-200, which name B.1 items 90 and 165 and 2014's A.1
items 106 and 182. Steps 4a and 4b are built in order with the rest
(section 11; owner order 2026-11-18).

2002 removed DATE-COMPILED ("AUTHOR, INSTALLATION, DATE-WRITTEN, DATE-COMPILED, and SECURITY paragraphs", §2002:F.1), so
its replacement happens at `--std 85` only; at a later edition the paragraph is already refused (COBOL0902) unless
`--permissive`, where it is replaced as at 85.

### 2.3 What the standard leaves to us, decided here

- The medium: a file per format beside the assembly (section 8).
- When: at `--std 85` by default; at 2002, 2014 and 2023 on request.
- Copied library text IS listed (1985 is silent; the later editions list it unless SUPPRESS).
- Stage (L-5): LISTING, PAGE and COPY SUPPRESS act during text manipulation, recorded by the text stages and applied
  when the listing is built.
- `>>PAGE` under `>>LISTING OFF`: GR4 stops the listing of "source lines", and a PAGE directive line is a source line
  that GR3 does not exempt, so it is neither listed nor ejects a page. A '/' line under OFF likewise.

## 3. What the compiler already knows, and where

The design names the structures each part reads. Every claim is from the tree at 16cf618813.

| Fact | Where it lives today |
|---|---|
| The physical text of every file read, tabs expanded | `ReferenceFormatMap.PhysicalText` (src/WiseOwl.Cobol.Frontend/Preprocessor/ReferenceFormatMap.cs), one map per file in `CopyProcessor.ReferenceFormats`, each pass's maps returned by `Frontend.RunTextStages` |
| Fixed or free form of each physical line | `ReferenceFormatMap.AreaAt`, `ReferenceFormatMap.Create` (format changes by line) |
| The indicator area of a fixed-form line | `FixedFormLine.Indicator` (src/WiseOwl.Cobol.Frontend/Preprocessor/FixedFormLine.cs): `internal`, THE one reader of fixed-form columns (kb/Work PB1966), and the front end grants `InternalsVisibleTo` to `WiseOwl.Cobol.Tests.Unit` only, so the Compiler cannot read it |
| The kind of each fixed-form line | `ReferenceFormatProcessor.FixedFormConverter.KindOf` classifies each line it converts into a PRIVATE enum `FixedFormConverter.LineKind` (`Source`, `Comment` for both '*' and '/', `Continuation`, `Debugging`, `CcvsExcluded`, `NotAnIndicator`) and keeps no record of the answer; `FixedFormConverter._inCommentEntry` knows a comment-entry line; `FreeFormConverter.Convert` classifies free-form lines the same way, unrecorded |
| Resultant line to physical file, line and column | `SourceLineMap.Origin`, `SourceLineMap.Locate`, `LineOrigin.Spans`, `OriginSpan.Layout` (src/WiseOwl.Cobol.Frontend/Common/SourceLineMap.cs); `Frontend.LineMap` |
| Lines a false `>>IF`/`>>WHEN` branch omitted | `LineOrigin.Omitted`, `SourceLineMap.IsOmitted`, marked in `ConditionalCompilationProcessor.Run.Render` |
| COPY expansion, its depth, the copybook found | `CopyProcessor.ExpandCopiesOneLevel` (the `expandCopybook` callback gets the depth and the resultant line piece where the copybook begins), `CopyProcessor.ResolveOneCopy` (`OneCopyResult`: outcome, path, mapped text), `CopyProcessor.ParseCopyStatement` (reads SUPPRESS) |
| COPY text-names and library-names | `CopyNameUse`, `Frontend.CopyNames` |
| REPLACE and COPY REPLACING | `CopyProcessor.ApplyReplaceStatements`, `CopyProcessor.ApplyReplacements` |
| `>>LISTING`, `>>PAGE` | (before step 4a) Consumed with no effect: `ConditionalCompilationProcessor.Run.Render`'s default arm (`CompilerDirectiveCatalog.IsDirective`); `DirectiveStateRegistry`'s `Constructs.ListingDirective2002` row said "NONE HELD". (Step 4a:) `Render`'s `LISTING` and `PAGE` arms; the driver holds the LISTING state, carried for PUSH / POP; section 4.4 |
| `>>DISPLAY` | `ConditionalCompilationProcessor.ApplyDisplay` and `DisplayDestinations` (ConditionalCompilationProcessor.Display.cs): (before step 4a) LISTING mapped to `CompileOutputStream.Standard`; lines collected on `DiagnosticBag.CompileOutput` as `CompileOutputLine(Stream, Text)` with no position. (Step 4a:) LISTING maps to `CompileOutputStream.Listing`, which `ApplyDisplay` sends into the trace when a listing is produced |
| Front-end diagnostics | `Diagnostic(Code, Severity, Message, Location, Span)` (src/WiseOwl.Cobol.Frontend/Diagnostics/Diagnostic.cs), structured, physical `SourceLocation` |
| Bind diagnostics | (before step 3) `EditionContext.Diagnostics` and `EditionContext.Warnings` were `List<string>`, rendered at report time from the resultant `EditionContext.Cursor`; `CompilerDriver.Result.Errors` and `Result.Warnings` are strings. (Step 3:) `EditionContext.Records` holds each as a `Diagnostic`; section 6.3 |
| Edition citation in a message | `EditionInfo.Cite` (src/WiseOwl.Cobol.Editions/EditionInfo.cs), chooses the edition's text when the message is built |
| Program units | `BoundUnit.Name`, `ExternalizedName`, `ClassName`, `Parent`, `Children`, `Initial`, `Common`, `Recursive`, `IsFunction`, `IsPrototype`, `SourcePosition` (src/WiseOwl.Cobol.Compiler/Binding/Model/BoundUnit.cs); only after a successful bind |
| Data items | `DataItem.Level`, `CobolName`, `ClrName`, `StructName`, `Pic`, `PictureText`, `Occurs`, `OccursSpec`, `RedefinesTarget`, `Renames`, `Storage` (`StorageForm`: `NativeInt`, `NativeFloat`, `CharImage`, ...), `DeclaredAt`, `Indexes`, `Own88s`; roots in `DataBindResult.Forest` (`ForestSlice.Roots`) |
| Offsets and lengths | `RecordLayout.OffsetInRootWithEveryIndexOne`, `RecordLayout.FirstOccurrenceOffsetOf`, `RecordLayout.OccupiedBytes`, `ItemLength.Positions` |
| Data references of a statement | `BoundReferences.Of` returns `Reach` (the `Place`s a bound statement references, in source order) and a `Place` carries NO source position; `BoundStores.StoreKindOf(BoundStatement, DataItem)` answers per ITEM per statement (`StoreKind.Write`, `ReadWrite`, `WriteInPart`), not per occurrence; a written name reaches its symbol along many paths (section 6.7.2), none of which records the use: `ReferenceResolver.ResolveImpl` is one of them, not a funnel every reference passes |
| Statement position | `BoundStatement.At` (a resultant `DiagnosticCursor`, stamped by `StatementBinder.BindStatement`) |
| The object code | The backend behind the `ICodeGenBackend` seam writes the assembly; (before step 8) today's Roslyn backend (`RoslynBackend.Emit`, `RoslynBackend.Compile`) emitted no PDB and no `#line` directive, and DESIGN.md plans a CIL backend behind the same seam. (Step 8:) it writes `<out>.pdb` with one sequence point per COBOL statement at `BoundStatement.Span`. The listing reads neither backend's internals: it reads the produced assembly and its portable PDB (section 6.9) |
| The compile timestamp | `IntrinsicBinder.CompileClock` (the clock seam), read through `CompilationInputs.ReadCompilationTime`, captured lazily once per emit in `ProgramEmitter._whenCompiledStamp` and baked as WHEN-COMPILED's constant (`IntrinsicRenderer`); `CompilationInputs.ReadsCompilationTime` records that the output depends on it. CURRENT-DATE reads the run-time clock (`WiseOwl.Cobol.Runtime.IO.Clock`), not the compile timestamp |
| The CLI | `Program.BuildParser` (System.CommandLine options), `CliOptions`, `Program.RunCompile`; the library entry `CompilerDriver.Compile(CompilerDriver.Options)` returns `CompilerDriver.Result` on every outcome |

Four facts the listing needs are missing today and are added by this design:
1. the KIND of each physical line, recorded where the normalizer already decides it and exposed through a public type
   (section 4.4.1; step 1);
2. which INCLUSION a resultant line belongs to: a copybook copied twice has two inclusions whose lines share one file
   and line number (section 4.4; step 1);
3. structured bind diagnostics (section 6.3; step 3, built);
4. ONE symbol table holding every name with its attributes and its occurrences (position, length, use), which today
   is spread over dozens of tables and recorded nowhere (section 6.7.1: (step 6) the finished table's entries and attributes
   in step 6, the one store in step 6b, (step 7a) the grammar's classified name slots the occurrences are checked against in
   step 7a, the occurrences in step 7b).

## 4. The ONE listing model

### 4.1 Shape: the listing is the SOURCE plus the SYMBOL TABLE

The owner, 2026-11-18: "Listing should I think be created from symbol tables plus source code." The listing model has
TWO primary inputs, and every section is a view of them:

1. **The source, with its provenance**: every physical line of every file read, each line's kind
   (`ReferenceFormatMap.KindAt`, section 4.4.1), and the text stages' trace of COPY, REPLACE, conditional compilation,
   LISTING and PAGE (`TextStageTrace`, section 4.4). It exists as soon as the source is read.
2. **The symbol table**: the ONE store of the program's names (`SymbolTable`, section 6.7.1). Each entry is one
   declared name with the attributes the listing needs (for a data item: level, picture, usage, offset, length,
   OCCURS/ODO, REDEFINES, the native field and its .NET type; for a program: its attributes and its .NET type; ...)
   and every OCCURRENCE of the name (position, length, use: defined, read, written or modified, and its statement).
   The binder writes it as it declares and resolves; the listing reads only the FINISHED table, never a binder,
   emitter or Roslyn type.

Two kinds of fact are PLACED on the source by their source position, not inputs of their own: the diagnostics of every
phase, and the object-code map read from the assembly's PDB (section 6.9). The options and the compilation timestamp
are the header.

| Section | View of |
|---|---|
| Source (with inline diagnostics, DISPLAY lines) | the source; diagnostics placed by position |
| Data map, file map | the symbol table's data, index, condition, constant and file entries (attributes) |
| Cross-reference | the symbol table's entries and their occurrences |
| Program map | the symbol table's program entries, joined to the source's program ranges |
| Statistics and verb counts | the source (lines, verbs) and the symbol table (counts of names and uses) |
| Object-code map | the PDB's sequence points, placed by source position |
| Options, header | the compilation's options and timestamp |

```
 SOURCE (text stages, Phase 0-1)                 SYMBOL TABLE (Bind)                 PLACED BY POSITION
   ReferenceFormatMap per file: text, kinds        SymbolTable entries                  diagnostics (every phase)
   SourceLineMap: resultant -> physical              attributes (written where the       object-code map (assembly +
   TextStageTrace: inclusions, replacements,          binder decides them)                 PDB, after the backend)
     LISTING, PAGE, DISPLAY                          occurrences (written by Identify)
   SourceUnitFacts: program ranges, verbs,
     DATE-COMPILED
                \______________________________________|_____________________________________/
                                                      v
                       ListingBuilder (WiseOwl.Cobol.Compiler/Listing): ListingLineIndex places every position,
                       ListingVisibility decides printed lines and page breaks, one builder per section (a view)
                                                      v
                                ListingDocument (WiseOwl.Cobol.Listing, immutable)
                                 /                    |                    \
                     TextListingRenderer     HtmlListingRenderer     JsonListingWriter
```

**One index places every position.** Every fact that has a position has it in RESULTANT space: a token's line, a
symbol-table occurrence's position, `BoundStatement.At`, `EditionContext.Cursor`. The listing turns a resultant
position into a listing line through ONE index, `ListingLineIndex`, built once from the line map and the inclusion
trace; a sequence point's physical position goes through the same index. A new section is placed by construction,
never by a second mapping (rule 5). The model itself stores listing line numbers and physical `(file, line, column)`
coordinates, never a resultant position.

### 4.2 The assembly: `WiseOwl.Cobol.Listing`

The model and the three renderers live in a new project, `src/WiseOwl.Cobol.Listing`, which references NO project of
the solution. A renderer therefore cannot reach a binder, parser or emitter type: "renderers read only the model" is
enforced by the compiler, and `docs/rearchitecture/architecture-edges.json` gives the project its own lowest layer so
`ArchitectureEdgesDriftTests` keeps it so. (Step 1:) the layers are by NAMESPACE, so the builder in the Compiler cannot share
the model's namespace: the model and renderers are `WiseOwl.Cobol.Listing` and `WiseOwl.Cobol.Listing.Text` (layer
`listing`), the builder is `WiseOwl.Cobol.Listing.Building` (layer `compiler.listing`, which may use `listing`, the front
end's `frontend.pipeline`, `frontend.preprocessor` and `frontend.common`, and `editions`). The builder names no type of the
composition root: the driver hands `ListingFacts` plain values (the source path, the output path, the `EditionInfo`, the
compile-clock delegate) and maps its own `Outcome` to `ListingOutcome`, so no edge runs from `compiler.listing` up to
`compiler.pipeline`. The Compiler references it and fills the model. A tool written in .NET can
reference the same assembly to read the JSON into the same types.

The model's root (all types are immutable `sealed record`s with `IReadOnlyList` members):

```csharp
namespace WiseOwl.Cobol.Listing;

public sealed record ListingDocument(
    ListingHeader Header,                         // compiler version, edition, timestamp, main source, outcome, parse
    IReadOnlyList<ListingFile> Files,             // every file read: id, path, role (source | library), SHA-256
    IReadOnlyList<ListingInclusion> Inclusions,   // every COPY, in text order, with parent, depth, outcome, SUPPRESS
    IReadOnlyList<ListingLine> Lines,             // the source section, in listing order
    IReadOnlyList<ListingProgram> Programs,       // every program, function, class, method; nesting; line ranges
    IReadOnlyList<ListingDiagnostic> Diagnostics, // in listing order
    IReadOnlyList<ListingDisplay> Displays,       // >>DISPLAY UPON LISTING transfers (step 4a)
    IReadOnlyList<ListingOption> Options,
    ListingStatistics Statistics,                 // (step 5) ListingStatistics?: null when not requested
    ListingDataMap? DataMap,                      // null when not requested or not producible
    ListingCrossReference? CrossReference,
    ListingObjectCodeMap? ObjectCode,             // from the assembly and its PDB (section 6.9)
    IReadOnlyList<ListingSectionStatus> Sections); // each requested section: produced, or why not
// (Step 1 builds Header, Files, Inclusions, Lines, Programs and Sections; step 3 Diagnostics and ListingLine.Diagnostics;
// step 4a Displays, ListingInclusion.Suppress and ListingReplacement.Replacing; step 5 Options and Statistics; step 6 DataMap;
// step 8 ObjectCode; each later step adds its members.)

public sealed record ListingLine(
    int Number,                 // the listing line number, 1-based, unique in the document
    int File, int FileLine,     // ListingFile id and the 1-based physical line in it
    int? Inclusion,             // ListingInclusion id; null for the main source
    int Depth,                  // inclusion depth, 0 for the main source
    ListingLineForm Form,       // Fixed | Free
    ListingLineKind Kind,       // Source | Comment | PageEjectComment | DebuggingLine | Continuation | Directive
                                //   | CommentEntry | Blank: the front end's PhysicalLineKind (section 4.4.1), mapped
    string Text,                // the physical text as the file holds it, tabs expanded (ReferenceFormatMap.PhysicalText)
    bool Omitted,               // a false >>IF / >>WHEN branch omitted it (LineOrigin.Omitted)
    bool Listed,                // the visibility pass's answer (section 4.5): the text and HTML renderers print it
    bool PageBreakBefore,       // the visibility pass's answer: '/' comment, >>PAGE, new outermost program
    int? Program,               // ListingProgram id of the innermost program containing the line
    ListingReplacement Replacement, // (step 1) WHICH replacement: None | DateCompiled (the paragraph's first line, printed
                                //   in place of Text, marked 'E') | DateCompiledEntry (a further line of its comment-entry,
                                //   replaced with it and not printed) | (step 4a) Replacing (REPLACE or COPY REPLACING
                                //   matched text beginning on the line: printed as written, marked 'R', with 'R' rows UNDER
                                //   it). The kinds render differently, so the kind is data.
    string? Replaced,           // what the compiler compiled in place of Text, when different: DATE-COMPILED's
                                //   replacement, or (step 4a) every resultant line the index places on the line, joined by
                                //   LF (empty when the replacement removed what the line held)
    IReadOnlyList<int> Diagnostics,  // ListingDiagnostic ids anchored on this line
    ListingMargin? Margin,      // the embedded margin, computed ONCE by the builder (section 6.2): a definition line's
                                //   offset, length and usage, or a statement line's defining listing lines
    IReadOnlyList<ListingNameUse> Names, // each name occurrence on the line: (Column, Length, Symbol, Use), the text
                                //   margin's and the HTML links' one input
    IReadOnlyList<ListingSpan> Spans); // (PB5949) what the compiler read each stretch of Text as: (Column, Length, Class),
                                //   made once by SyntaxSpanBuilder (section 7.2); the HTML colours by it, the text ignores it
```

`ListingMargin` and `ListingNameUse` are joins of the data map and the cross-reference onto lines. They are made ONCE,
by `LineAnnotationBuilder` in the Compiler, and stored on the line; the text renderer prints `Margin` and the HTML
renderer turns each `ListingNameUse` into a link. No renderer joins two sections (PB5925 decision 1), so the text and
the HTML cannot disagree about a line. `PhysicalLineKind` is the front end's public enum: the Listing assembly
references nothing, so the model declares its own `ListingLineKind` with the same members and the builder maps one to
the other in one `switch` that a drift test holds total (a member added to either enum without the other is red).
(Step 1:) `ListingLineKind` has every member of `PhysicalLineKind` but `CommentEntryCandidate`, which the builder settles
from the parse before it maps (section 4.4.1); the switch is `ListingLineKinds.Of` and the drift test
`PhysicalLineKindDriftTests.TheKindMap_IsTotal_BothWays`.

Every other record (`ListingInclusion`, `ListingProgram`, `ListingDiagnostic`, `ListingDataItem`, `ListingSymbol`,
`ListingReference`, `ListingObjectRange`, ...) refers to lines by `Number` and to files by id. Section 6 defines each.

(Step 5:) **What a request changes is what the document HOLDS, never what is computed.** `Sections` has one status per
section ASKED FOR, in the order of the one enum `ListingSection`; a section not asked for has no status and its part of the
document is empty: `Lines` and `Displays` are the source section's, `Diagnostics` (and each line's `Diagnostics`) the
diagnostics section's, `Options` the options section's (an empty list), `Statistics` the statistics section's (null). The
header, `Files`, `Inclusions` and `Programs` are always held: every line, inclusion and diagnostic refers to them, and every
page heading names a program (§1985:VI.3.3.4 GR1), so the `programs` section decides whether the program MAP is printed, not
whether the units exist. The builder places, decides and counts every line whatever was asked for, since the program
ranges, the diagnostics' placement and the statistics stand on them; so the statistics of a listing without its source
section count every line, and those of one without its diagnostics section count every diagnostic.

`ListingRequest` (also in this assembly) is the input: `Formats` (flags: Text, Html, Json), `OutputBase` (a path without
extension, or null for the default), `Sections` (flags), `PageLength`, `PageWidth`. (Step 1 builds `OutputBase`,
`PageLength` and `PageWidth`, with the one validation of each, `ListingRequest.PageLengthProblem` and `PageWidthProblem`,
that the command line calls; `OutputBase` `-` writes no file. `Formats` arrives with JSON in step 2 and `Sections` in step 5.)
(Step 5:) `Sections` is no flags enum: the JSON contract refuses a flags enum in the model (`ListingJson`'s converter, a value
is one member's name), and a second enum of sections for the request would be a second list of them. It is
`ListingSectionSet`, a value set over the one enum `ListingSection`, so `ListingSectionSet.All` holds a section the day it
joins the enum; `ListingSectionSet.Standard` is section 8's default. Like a format, a section joins the enum WITH its
builder (`ListingSection` has `Source`, `Programs`, `Diagnostics`, `Options` and `Statistics`; the data map, the
cross-reference and the object-code sections join with steps 6 to 8), so no request names a section the compiler cannot
produce, and `all` equals `standard` until step 6. (Step 6:) `ListingSection.DataMap` (`data-map`) joins with its builder,
`DataMapBuilder`; it is not in `standard` (section 13 item 1), so `all` is `standard` plus the data map. ONE table, `ListingSections`, holds each section's command-line word and
parses a list (`Spelling`, `Spellings`, `TryParse`, `StandardSpelling`, `AllSpelling`); `ListingSectionSet.ToString` spells a
set back through it. `ListingRequest.SectionsProblem` (at least one section) is asked by the constructor and the `init`
accessor like the request's other rules; `Sections` is an init property beside the positional ones, `Standard` unless set.
(Step 2:) `ListingFormats` has `Text` and `Json` only: a format joins the enum WITH its renderer, so `Html` is added by
step 9, and no request can name a format the compiler cannot write. (Step 9:) `ListingFormats.Html` (`html`, `.lst.html`) joined
with `HtmlListingRenderer`. ONE table, `ListingOutput`, holds each format's
command-line spelling, file extension and renderer (`Spelling`, `Extension`, `Render`, `TryParse`, `Each`); the listing's
exit writes files through it and the command line prints `-` through it, and `ListingOutputTests` calls every switch for
every member. `ListingRequest.FormatsProblem` is the one rule for formats (at least one; exactly one when `OutputBase` is
`-`, since two would interleave in one stream), asked by the command line and by the request itself, which refuses a
request it would fail. (Step 2, review R1-3:) the request asks all three of its rules (`PageLengthProblem`,
`PageWidthProblem`, `FormatsProblem`) in its constructor AND in each property's `init` accessor, so an object initializer
or a `with` expression (`request with { OutputBase = "-" }` on a two-format request) cannot build a request the listing
would fail; a property initializer alone checked only the constructor. `ListingRequest.TextExtension` is deleted: the extension is `ListingOutput.Extension`.
The document's schema identity is two properties of `ListingDocument` itself, `Schema` (JSON `$schema`, the constant
`ListingDocument.SchemaUrn`) and `SchemaVersion` (the constant `ListingDocument.CurrentSchemaVersion`), init properties
beside the positional ones so a reader sees the version the file was WRITTEN at, not this compiler's. `CompilerDriver.Options.Listing` is a
`ListingRequest?`; null means no listing and no listing work (section 9).
(PB5941:) `ListingOutput.Render`, which returned a format as a string, is replaced by `ListingOutput.Write(format, document, request,
Stream)`: each format is WRITTEN to a stream as it is rendered, in `ListingOutput.Utf8` (UTF-8, no byte order mark), and the stream is
flushed and left open for its owner. A format is never held whole: the listing's exit (`ListingWriter`) opens each file as a
`FileStream`. Holding a format whole cost three times its size
(the buffer, the string copied from it, the encoding of the string), and an `all` listing is several times the source's size
(section 9, PB5941). The table's one switch still dispatches every format to its renderer: the text and HTML renderers write
characters to a `TextWriter` (a `StreamWriter` the table makes over the stream), the JSON writer serializes bytes to the stream itself.
(PB5941 review R1-1, correction:) the table has a second entry, `ListingOutput.Write(format, document, request, TextWriter)`, which
writes a format as CHARACTERS, streamed the same way: the text and HTML renderers write to the writer directly, and the JSON
serializer's bytes are decoded into it as they arrive (a private write-only stream with a UTF-8 decoder, so no format is held
whole there either). The `Stream` entry is a format's file; the `TextWriter` entry is the command line's `-`, which writes through
the process's standard output writer (section 8). `ListingOutputTests.EveryFormat_WrittenAsCharacters_IsWhatItsFileHolds` ties them.
A test reads a format as text through the same one call (`tests/_shared/ListingRendering.cs`).

### 4.3 Who fills what, at which stage

| Part | Filled from | Stage | Producible when |
|---|---|---|---|
| `Files` (path, role, SHA-256) | the source path and each inclusion's file; the digest recorded by `CompilationInputs.ReadSourceText` when `CompilationInputs.DigestsFiles` is set, which only a listing compilation sets (step 1) | Text stages | The source was read |
| `Lines` (text, form, kind) | `ReferenceFormatMap.PhysicalText`, `ReferenceFormatMap.AreaAt` and `ReferenceFormatMap.KindAt` (section 4.4.1) of every file of the listed pass (section 4.4) | Text stages (Phase 0) | The source was read |
| `Inclusions`, `Line.Inclusion`, `Line.Depth` | `TextStageTrace` inclusion events | Text stages | The source was read |
| `Line.Omitted` | `SourceLineMap.IsOmitted` | Text stages | The source was read |
| `Line.Replaced` (REPLACE) | `TextStageTrace` replacement events | Text stages | The source was read |
| `Line.Listed`, `PageBreakBefore` | `ListingVisibility` over the directive events and inclusions | Listing build | Always |
| `Programs` (names, nesting, ranges), DATE-COMPILED, verb counts | `SourceUnitFacts`, read off the parse tree the parser built, recovered or not | Parse (Phase 1) | The text stages finished |
| `Programs` (attributes, the .NET type) | the symbol table's program entries; (step 6) `ListingProgram.Attributes`, joined by `DataMapBuilder.ProgramAttributesOf` through the program entry's position | Bind | The bind finished with no error |
| `Diagnostics` | `CompilerDriver.Result.Diagnostics`: the front end's `Diagnostic`s, the binder's records (`EditionContext.Records`), each expansion's own compilation's records moved to its EXPANDS entry, and the backend's, which the seam reports as the same `Diagnostic` (step 3; section 6.3) | Every phase | Always (what was reported) |
| `Displays` | `CompileOutputLine` with destination Listing and its origin | Text stages | The source was read |
| `DataMap` | the symbol table snapshot's entries and their attributes (section 6.6); (step 6) `DataMapBuilder`, and the margin by `LineAnnotationBuilder` | Bind | (step 6) The bind finished with no error: a refused program's sizes are not a compiled program's, so the section says `the bind refused the program` (`the source was not parsed` after a front-end error) |
| `CrossReference`, `Line.Names`, `Line.Margin` | the symbol table snapshot's entries and occurrences (section 6.7.1) | Bind | The bind finished |
| `ObjectCode` | the produced assembly and its portable PDB, read with `System.Reflection.Metadata` (section 6.9); (step 8) deposited by the driver as plain paths (`ListingFacts.DepositObjectCode`) once the backend wrote them, and read only when `object-map` or `object-code` is asked for (`ListingFacts.ReadsObjectCode`) | After the backend | An assembly was written: otherwise the section says `check-only compile: no code generated`, `the source was not parsed`, `the bind refused the program`, `the backend produced no assembly` or `the assembly was not written`, and `the backend wrote no PDB` for an assembly written without one |
| `Options`, `Header` | `CompilerDriver.Options`, the compilation timestamp; (step 5) the options rows are built by `OptionsSectionBuilder` in the composition root and deposited as plain values (`ListingFacts.Options`, section 6.4) | Driver | Always |
| `Statistics` (step 5) | the finished lines, the diagnostics, the programs and `SourceUnitFacts.Statements` (`StatisticsSectionBuilder`, section 6.8) | Listing build | The source was read; the verbs once it was parsed |

A listing is produced on EVERY outcome once the source text was read: frontend error, bind error, backend error and
success. A section that cannot be built at the outcome reached is not omitted silently: its `ListingSectionStatus` says
`NotProducible` and names the phase that stopped (for example `DataMap`: "the bind refused the program"). This matters
most when the compile fails, which is when a listing is most read.

### 4.4 Provenance through COPY, REPLACE and conditional compilation: `TextStageTrace`

The text stages run several times per compilation (the §2023:14.9.28.4 GR14 fixed point and the comment-entry fixed point
in `Frontend.Parse` and `Frontend.RunTextStages`). The trace follows the discipline the per-pass `DiagnosticBag` already
follows: one `TextStageTrace` per pass, created in `Frontend.RunTextStages` beside `passDiagnostics`, handed to
`CopyProcessor` and to `ConditionalCompilationProcessor`. When no listing is requested the front end passes
`TextStageTrace.None`, whose methods do nothing.

**Which pass is listed.** The listing takes EVERY fact from ONE pass, the pass whose diagnostics are the compilation's:
the settled pass when the parse succeeds, and on a failed parse the pass `Frontend.Parse` already reports
(`firstFailure ?? this pass`: the FIRST failing pass when a speculative re-run did no better, else the last). Today the
`firstFailure` tuple holds that pass's diagnostics, directives and `LineMap`; it gains the pass's `TextStageTrace`, its
format maps (`ReferenceFormatMap` per file) and its `SourceUnitFacts`, and `Frontend.Parse` sets `Frontend.ListingTrace`,
`Frontend.ReferenceFormats` and `Frontend.UnitFacts` from the same tuple it sets `LineMap` from, in both arms. So the
line index, the inclusion ranges and the anchored diagnostics always describe one resultant frame. A test pins the
failed case: the PB1066 shape (a `>>SOURCE FORMAT` in an exception-checking PERFORM's WHEN phrase) plus a syntax error,
which forces the speculative re-run, and the listing anchors each syntax error on the line `diagnostics` names. (Step 1,
review R1-2:) step 1 has no diagnostics section, so its test (`ListingOutcomeTests.AFailedParse_ListsThePassWhoseDiagnosticsAreReported`)
pins the pass itself: the lines after the WHEN phrase's directive are listed in the form the REPORTED pass read them in (free;
the speculative re-run reads them fixed from END-PERFORM on), and the line the reported error names is a listed line
holding the offending word; listing the last pass instead turns it red. (Step 3:) the same test asserts the anchoring: the
error naming the offending word is placed on its line at its column, and every reported error at the line and column its record
names.

Events, each recorded at the one place the stage acts:

| Event | Recorded in | Carries |
|---|---|---|
| `InclusionBegun` | (step 1) `CopyProcessor.ExpandCopiesOneLevel`, which brackets the expansion (`TextStageTrace.BeginInclusion` before it hands the library text on, `EndInclusion` after, so a COPY met inside is a child) from `ResolveOneCopy`'s result (`OneCopyResult` carries the outcome, the path, the text-name and the library-name; SUPPRESS joins in step 4a), and `ConditionalCompilationProcessor.Run.RenderCopybook` (`PlaceInclusion`: the output-frame range it renders into), carried to the resultant frame by `ConditionalCompilationProcessor.Manipulate` through `CopyProcessor.ReplaceLineMap` (`ToResultantFrame`) | id, parent id, depth, the COPY statement's first and last origin, text-name, library-name, the file read (or none), `CopyOutcome`, SUPPRESS, the RESULTANT line range the library text occupies in the final frame |
| `Replaced` | `CopyProcessor.ApplyReplacements` (COPY REPLACING) and `CopyProcessor.ApplyReplaceStatements` (REPLACE) | the inclusion or REPLACE statement that acted, the physical span of the matched text, the matched text and the replacement, the resultant line |
| `ListingDirective` | `ConditionalCompilationProcessor.Run.Render`, emitting branch only | origin, ON or OFF |
| `PageDirective` | `ConditionalCompilationProcessor.Run.Render`, emitting branch only | origin |

**As built (step 4a), with the corrections the code proved.**

- **The LISTING state has ONE holder, the conditional-compilation driver, and the trace records its CHANGES.** The table above
  records only the LISTING directive and leaves §2023:7.2.3.4 GR5 (the state at the end of library text) to the listing's
  visibility pass. The code proved two more ways the state changes, which no pass over physical lines can see: a PUSH saves
  and a POP restores it (the LISTING directive is pushable, §2023:7.3.22.4 GR2 saves every directive "other than EVALUATE, IF,
  PAGE, POP, or PUSH"), and the implicit PUSH ALL / POP ALL of an exception-checking PERFORM (§2023:14.9.28.4 GR14) restore it
  at END-PERFORM. Deciding GR5 in the listing would have been a second implementation of a rule the driver must apply anyway
  (a PUSH after the COPY saves the restored state). So `ConditionalCompilationProcessor.Run` holds `_listingOn` (ON by default,
  §2023:7.3.18.3 GR2), carried by its `DirectiveStateStack` (`DirectiveStateRegistry`'s LISTING row names the driver) and
  restored in `RenderCopybook` at the end of each library text, and records every change as a `TextListingSwitch(Cause, On,
  Inclusion, Line)`: `Directive` (the directive's physical line in its inclusion; the line itself is always listed, GR3),
  `Pop` (a written POP's line), `ImplicitPop` (the END PERFORM phrase's RESULTANT line, the op's own line) and `InclusionEnd`
  (the inclusion that ended). The trailing implicit ops, keyed after the last encounter, are applied at the end of the source
  text when a listing records, since the lines after them are listed by the state they restore. A LISTING directive changes
  state a PUSH saves only while a listing is produced (GR1), so `StateEncounter.ChangesState` is set for it only then:
  a compilation without a listing re-runs no text stage for it.
- **GR5 reads the state at a library text's START and END, so those are encounters too** (review round 1, R1-1). The driver
  applies an implicit op lazily, immediately before the encounter it is keyed to, which is exact for every state a directive
  reads. GR5 is the one reader that is not a directive: `RenderCopybook` takes "the LISTING directive that is in effect for
  the COPY statement itself" at the COPY and puts it back at the end of the library text. Keyed to directives alone, the
  POP ALL before an END-PERFORM written between the last directive and a COPY acted only at the first directive INSIDE the
  library text, after the state at the COPY was taken, and the end of the library text then put back the stale OFF, hiding
  every line after the COPY. So while a listing records, each library text's first line and the line after its last are
  `StateEncounter`s (`ChangesState` false; `MeetLibraryTextBoundary`), the ops key to them like to a directive
  (`KeyImplicitOps`, by resultant line: the COPY expansion begins and ends a line, so no op shares an encounter's line), and
  an op inside the library text after its last directive acts before GR5 restores. Without a listing nothing reads the
  LISTING state (GR1), so no boundary is recorded and the encounter program is the one a compilation without a listing had.
- **Every fact carries the inclusion OPEN when the stage met it** (`TextStageTrace.Current`): a directive, a DISPLAY transfer,
  a PAGE directive, and a COPY REPLACING match (made in `ResolveOneCopy` before the inclusion begins, so `OneCopyResult`
  carries the matched lines to `BeginInclusion`). A copybook copied twice therefore places each fact in its own block. A
  REPLACE match is made after every inclusion closed, so it is recorded by the merged driver's output line
  (`ApplyReplaceStatements` reports each match's offset to an observer only a listing passes) and carried to the resultant
  frame by `ToResultantFrame` with the inclusion ranges; the listing places it through `ListingLineIndex.Of(int, SourceOrigin)`.
- **The 'R' rows are the resultant text, placed by the one index.** The `Replaced` event of the table above carried the matched
  and the replacement text; the listing needs the text the compiler COMPILED from the line, which the resultant text holds
  (`Frontend.ResultantText`, the listed pass's text, beside its `LineMap`). `DirectiveEffectsBuilder.MarkReplaced` gives each
  changed line every resultant line the index places on it, so a replacement that spans lines or joins them is shown whole.
- **SUPPRESS** is parsed by `ParseCopyStatement` and recorded on the inclusion (`TextInclusion.Suppress`, `ListingInclusion.Suppress`).
- **DISPLAY** (the paragraph below): `CompileOutputLine` does NOT gain an origin. The decision "into the listing or to an output
  stream" is made once, in `ApplyDisplay`: a `Listing` destination goes into the trace (`TextStageTrace.Display`, with the
  inclusion and the directive's physical line) when the trace records, and is a `CompileOutputStream.Listing` transfer on the
  bag otherwise, which the CLI writes to standard output. So a compilation with a listing transfers no `Listing` line at all,
  and the placement needs the inclusion, which an origin alone does not name.
- **Placement.** `DirectiveEffectsBuilder` (Compiler, `Listing/`) is the one placement of these facts: each becomes a listing
  line through `ListingLineIndex`, a fact no listing line holds throws (an unresolvable input fails loudly), and
  `DirectiveEffects` (the state from each listing line on, the LISTING directive lines, the PAGE lines, the suppressed lines) is
  what `ListingVisibility` reads.

The inclusion's RESULTANT range is what makes a copybook copied twice two blocks of listing lines with a right answer
for "which one": `ListingLineIndex` maps a resultant line to the innermost inclusion whose range holds it, then the
line's origin (`SourceLineMap.Origin`) to the physical line within that inclusion. The merged driver already places a
directive met inside library text in the final frame (`_renderBase + output.Count`, kb/Work PB1066); the inclusion range
uses the same arithmetic. A test pins it: a copybook copied twice with different REPLACING operands, a diagnostic in the
second copy, and the listing anchors the diagnostic in the second block.

Listing order: the main source's physical lines in order; after the last physical line of a COPY statement, that
inclusion's lines (recursively), each at `Depth + 1`. An inclusion that found no library text (`NotFound`, `Circular`)
lists no lines; its diagnostic is anchored on the COPY statement.

`DISPLAY` transfers: `CompileOutputStream` gains a `Listing` member. `DisplayDestinations` returns `Listing` for LISTING and
for no UPON phrase (GR6). When the compilation produces a listing, the transfer is recorded into the trace at its directive
(step 4a: not as a `CompileOutputLine` with an origin, see above) and the builder places it as a `ListingDisplay`; when it
does not, the CLI writes `Listing` lines to standard output exactly as `Standard` lines are written, which is the documented
no-listing behaviour of DOC-A.1-53/54 and §2023:7.3.12.4 GR2's implementor-defined case.

#### 4.4.1 The kind of each physical line: `PhysicalLineKind` and `ReferenceFormatMap.KindAt`

The Compiler cannot read `FixedFormLine` (internal; the front end's only `InternalsVisibleTo` is
`WiseOwl.Cobol.Tests.Unit`, and none is added for this), and must not: `FixedFormLine` is THE one reader of fixed-form
columns (kb/Work PB1966), and a second reader of position 7 in the listing would misplace the indicator of a line whose
sequence area holds a supplementary-plane character. The boundary is therefore a public RESULT, not public access to
the reader. The normalizer already classifies every line it converts (`FixedFormConverter.KindOf`) and throws the answer
away; the design keeps it:

- `src/WiseOwl.Cobol.Frontend/Preprocessor/PhysicalLineKind.cs` (new, public):
  `enum PhysicalLineKind { Source, Comment, PageEjectComment, DebuggingLine, Continuation, Directive, CommentEntry,
  CommentEntryCandidate, CcvsExcluded, NotAnIndicator, Blank }`. It REPLACES the private `FixedFormConverter.LineKind`
  (rule 4: one classification, not two): `KindOf` returns it, splitting today's `Comment` into `Comment` ('*') and
  `PageEjectComment` ('/'); every other member keeps today's meaning, so the converter's behaviour is unchanged.
- Recorded where the decision is made, with no new reading of any column: `FixedFormConverter.ConvertLine` (the
  indicator kind, `CommentEntry` while `_inCommentEntry`, `CommentEntryCandidate` for a carried candidate,
  `Directive` for a `>>` line, `Blank`), `FreeFormConverter.Convert` (free form: `Source`, `Comment` for a `*>` line,
  `DebuggingLine` for `>>D`, `Directive`, `CommentEntry`, `Blank`) and `ReferenceFormatProcessor.NormalizeToFreeFormMapped`
  (a discarded `>>SOURCE` line is `Directive`). Each writes into a `PhysicalLineKind[]` allocated beside the text's
  `ReferenceFormatMap` (one byte per physical line). (Step 1:) the classifiers record the INDICATOR's answer, and the
  shared `LogicalLineBuilder` refines a line where IT decides it, through its one `Classify`: `Blank` (no program text),
  `Comment` (the floating comment indicator, where the edition reads it) and `Directive` (its `IsDirectiveLine`, where the
  edition has compiler directives). At `--std 85` a `>>` line is text, never `Directive`.
- `ReferenceFormatMap.Create` takes that array; `ReferenceFormatMap.KindAt(int line)` (public) answers it. Copybooks
  pass through the same `NormalizeToFreeFormMapped`, so every file read gets its kinds with no second path.
- A `CommentEntryCandidate` (a COBOL-85 paragraph the conversion could not place, decided after COPY by
  `CommentEntryResolver.Resolve`) is settled by the builder from `SourceUnitFacts`: lines inside a comment-entry
  paragraph the parse read are `CommentEntry`, the rest `Source`.
- `PhysicalLineKindDriftTests` (Unit, which sees the internals): for a corpus of fixed and free lines, each kind equals
  the classification `FixedFormLine.Indicator` gives, so the recorded kind and the one reader cannot drift.
- (PB5949) The kinds travel in a `PhysicalLineRecord` (one per text the conversion reads) instead of a bare array, beside which,
  only when a listing is produced (`NormalizeToFreeFormMapped`'s `recordsDiscarded`), the conversion records where it DISCARDED a
  line's program text as an inline comment (§2023:6.5 GR3, at the one place the literal-aware scan stops on a comment indicator) or as
  the comment-entry of the paragraph the line heads (`CommentEntryStart`, the one reading of where it begins); `ReferenceFormatMap.DiscardedAt`
  answers it, and
  `ReferenceFormatMap.RegionsAt` answers where a fixed-form line's areas begin and end, read by `FixedFormLine`, the one reader of
  its columns. Section 7.2 colours by both: discarded text never reaches the lexer. A CARRIED candidate's header line (its division
  decided after COPY and REPLACE) is recorded too, when the conversion carries it, as where its comment-entry begins SHOULD it head
  a comment-entry paragraph: the builder reads it only of a candidate the parse settled as a header (`LineDraft.HeadsCarriedCommentEntry`,
  per listed copy of the line), so a carried header colours its entry as an immediate one does (correction, review round 1: the record
  was first made only where the conversion decided the header, and a carried header's entry went uncoloured).
- (Step 1) A physical line is what the line-entry stage says (DOC-A.1-156: a text with n LINE FEEDs has n + 1 lines), so a
  file that ends in a LINE FEED lists an empty last line, as its reference-format map holds it.

### 4.5 Visibility and page breaks: `ListingVisibility`

One pass in the builder, over `Lines` in listing order, owns every rule that decides whether a line is printed and
whether a page starts before it. Renderers never re-decide it.

- State starts ON (§2023:7.3.18.3 GR2).
- A `ListingDirective` line is `Listed = true` whatever the state (GR3), then sets the state (GR4, GR5).
- Any other line: `Listed = state`. An `Omitted` line is still listed (it is source text; the marker says it was not
  compiled), as IBM's CONDCOMP(NOSKIPSRC) does.
- At an inclusion: if `Suppress`, every line of it (and of inclusions nested in it) is `Listed = false`; the COPY
  statement's own lines are listed by the ordinary rule (§2023:7.2.3.4 GR4). At the end of every inclusion, suppressed or
  not, the state returns to the state at its COPY statement (GR5).
- (Step 4a:) the pass DECIDES none of the state: it replays `DirectiveEffects.States`, the driver's own state at each change
  (a LISTING directive, a POP, an implicit POP ALL, the end of an inclusion, section 4.4), applying the changes that govern
  from one listing line in the order the driver made them. A line of `Suppress` library text is not listed even when it is a
  LISTING directive: §2023:7.3.18.3 GR3's exception names suppression "by a LISTING directive", and §2023:7.2.3.4 GR4 says the
  library text "is not listed". A POP line under OFF is a source line and is not listed; the state it restores governs the
  lines after it.
- `PageBreakBefore = true` for a listed line of kind `PageEjectComment` (§1985:IV.7.2.4, §2023:6.3.7.2), a listed
  `PageDirective` line (§2023:7.3.19.4 GR2), and the first line of each outermost program after the first. (Step 1:) an
  outermost program's listing range begins right after the previous one's last line (the first at line 1, the last ends
  at the last line), so the comment lines before an IDENTIFICATION DIVISION header are on the page of the program they
  introduce (`ProgramRangeBuilder`).
- At `--std 85` no directive exists, so every line is listed and only '/' and program starts break pages.

### 4.6 The builder

`ListingBuilder.Build(ListingFacts facts, ListingOutcome outcome)` in `src/WiseOwl.Cobol.Compiler/Listing/` is the one
constructor of a `ListingDocument` (step 1: the request travels in the facts). `ListingFacts` is what the driver gathered at the outcome reached (the frontend's
maps, trace and unit facts; the diagnostics; the finished symbol table or null; the path of the produced assembly and
its PDB or null; the options; the timestamp). Each section has one builder class (`SourceSectionBuilder`, `DataMapBuilder`, `CrossReferenceBuilder`,
`ObjectCodeMapBuilder`, ...) that reads only `ListingFacts` and `ListingLineIndex`; no builder class is a god class and no two
build one fact.

The driver gets ONE exit for the listing. `CompilerDriver.Compile` has ten return points today (`CompilerDriver.cs`
lines 169, 175, 251, 271, 275, 284, 306, 312, 316 and 321 at 16cf618813), and the hook must not depend on that count:
`Compile` becomes a thin wrapper over the existing body (`CompileCore`, the current method with every return point
unchanged). The body takes a `ListingFacts` collector and DEPOSITS each fact as its phase finishes: the front end's
listed pass (after `Frontend.Parse`), the diagnostics ((step 3) once, from `Result.Diagnostics`, which every return carries), the bound compilation (after
`BinderDriver.Bind`, which hands over the finished symbol table), the assembly and PDB paths (after `backend.Emit`). The wrapper, when `Options.Listing` is set, builds and writes
the listing from whatever the collector holds and returns the result with `Result.Listing` set and the listing files
added to `Result.OutputFiles`.

What is listed is decided by the collector's STATE, never by which return was taken:
- no source text deposited (the two refusals before the source is read, `SourceNotFound` and the `OutputNotWritable` of
  a path with no directory): no listing;
- source deposited: a listing, each section present or `NotProducible` with the phase that stopped it. So a front-end
  error lists source and diagnostics; a bind error adds the program names from the parse; `CheckOnly` (a library caller
  may ask for both) has no object-code map ("check-only compile: no code generated"); an expansion's failure (return 306) and a
  backend error have the symbol-table sections and no object-code map; a host that refuses the assembly write (return 321) gets the
  listing written if its own path is writable, and each listing file the host refuses to write adds one error to
  `Result.Errors` without changing the outcome, the other formats still written (step 2).

A return point added to the body later needs no listing code. The no-listing path is the body alone, with a null
collector. (Step 1:) `Compile` builds the facts, runs `CompileCore`, returns at once when no source text was deposited
(`ListingFacts.Frontend` is null), and otherwise asks `ListingWriter.Write` for a `ListingWritten(Document, Files, Errors)`,
which it folds into the result. (Step 2, review R1-4:) `ListingWriter.Write` renders each requested format through
`ListingOutput` (`Each`, `Extension`, `Render`) into `BASE` plus that format's extension; `Files` lists each file written
and `Errors` holds one error per file the host refused, the other formats being written all the same (witness
`ListingJsonTests.ARefusedJsonFile_IsOneError_AndTheTextIsStillWritten`); with `OutputBase` `-` both lists are empty and
the caller prints the one format; the driver maps its `Outcome` to `ListingOutcome` (`ListingOutcomeOf`). The front end is
built by the one `CompilerDriver.NewFrontend`, which the no-work drift test reads. (Step 1, review R1-2:) each kind of
outcome is a test (section 10 item 8): `Listing1985Tests.AFailedCompilation_StillListsItsSource` (front-end error, bind
error, no source) and `ListingOutcomeTests` (check-only, the refused assembly write of the last return, an output refused
before the source is read, a refused listing write, the listed pass of a failed parse), each asserting the outcome the
header names and the `ListingSectionStatus` of every section. A backend error has no source that reaches it (it is Roslyn
refusing generated C#, a compiler defect), so its witness is the outcome map: `ListingOutcomeMapTests` holds
`ListingOutcomeOf` total, every outcome that read the source mapping to the listing outcome of its own name.

## 5. The single compilation timestamp

The listing heading, DATE-COMPILED's replacement and WHEN-COMPILED must denote one instant (§1989:A.2.46.3 RV3,
§2023:15.99.3 RV3). Today the instant is read once per emit through `CompilationInputs.ReadCompilationTime`. The design
makes `CompilationInputs` the owner of the instant: `ReadCompilationTime` captures the clock on its FIRST call and
returns the captured value on every later call of the same compilation, recording it in a new
`CompilationInputs.CompilationTime` property beside `ReadsCompilationTime`. The emitter (`ProgramEmitter`'s
`_whenCompiledStamp`, unchanged) and the listing builder both call it, so whichever runs first captures and the other
reads the same instant. `IntrinsicBinder.CompileClock` stays the one clock seam (tests set it).

Formats: WHEN-COMPILED keeps its §2023:15.99 21-character form (`RuntimeApi.DateFormat21`). The listing heading and the
DATE-COMPILED replacement print the same instant as ISO 8601 local time with offset, `2026-11-17 14:03:22 -05:00`;
`DATE-COMPILED. 2026-11-17 14:03:22 -05:00.` (the "current date" form is the implementor's). A compilation that asks for
a listing reads the clock even when the program has no WHEN-COMPILED, so `ReadsCompilationTime` is true for it: the
listing is output, and its content depends on the time.

## 6. Section content

### 6.1 Page heading and identification

Each page: line 1 `WiseOwl COBOL <version>  <edition name> (--std <year>)  <program-name>  <timestamp>  PAGE <n>`;
line 2 the section title and, in the source section, the main source path; a blank line. The program-name is that of
the innermost program containing the page's first listed source line (§1985:VI.3.3.4 GR1, §1985:X.3.1.4 GR1); outside
the source section it is the first outermost program. A compilation group of several outermost programs starts each on
a new page.

(Step 1, review R1-1:) line 1 never exceeds the page width, and when it does not fit, the fields YIELD in a fixed order:
the compiler's version first, cut at its end (a release build's version is `30.0.0+` and a 40-character commit, so its
build metadata goes first), then the edition, cut at its end, then the program-name, cut at its end. The instant and the
page number never yield: with their separators they take at most 40 columns, and the narrowest page is 100. So the two
facts the 1985 heading exists to carry, the program-name and the instant, are whole at every width for every name of 60
characters or fewer, which covers every `--std 85` name (30 characters at most) and every 63-character name at the default
width; only a longer name (a literal program-name of a later edition) on a narrow page is cut, after everything else has
yielded. One function composes the line (`TextListingRenderer.IdentificationLine`, the page number included). Line 2 keeps
the END of a source path that does not fit (the file's name), behind `...`.

(Step 5, review R1-1:) the header also carries `Parse` (`ListingParse`: `NotParsed`, `Parsed`, `Recovered`), what the parse
made of the source. The front end decides it once, on the pass the listing is of: `Recovered` is its own verdict that the parse
did not read the source cleanly (`LexAndParse`'s `parsed` is false: a syntax error or an embedded block), carried as
`SourceUnitFacts.Recovered`; `ListingBuilder.ParseOf` only maps it. Everything read off the parse tree (the program map,
section 6.5, and the statistics' programs, statements and verbs, section 6.8) is read off a recovered tree too, and the
recovery may skip source, so after a recovery those parts hold only what it kept; each says so (below), from this one fact.

### 6.2 Source

One row per listed line, at the default width 132:

```
cols 1-6    listing line number
col  8      inclusion depth (blank at 0, 1-9, '+' beyond)
col  9      marker: '/' page-eject comment, '*' comment, 'D' debugging line, 'X' omitted by conditional compilation,
            'R' changed by REPLACE or REPLACING, 'E' DATE-COMPILED replaced, '>' directive, '-' continuation
cols 11-16  the physical line number in its file, right-aligned, printed only where it is not the listing number
            (`TextListingRenderer.FileLineColumn`; kb/Work PB5950): blank before a program's first inclusion, where the
            two are the same number, and on every later line of the source and every line of a library text, where
            they differ, the line in the file the programmer edits
cols 18-97  the line as written: a fixed-form line's 80 positions (sequence 1-6, indicator 7, area A 8-11, area B 12-72,
            identification 73-80); a free-form line's text, wrapped at col 97 onto rows marked '+'
cols 100-132 the embedded margin: the line's `ListingLine.Margin`, printed as the builder computed it (on a data
            definition line its offset, length and usage; on a statement line the listing line numbers where the names
            it uses are defined), present when the data map or the cross-reference was requested
```

A ruler (`----+----1----+----2 ...`) under each page heading marks the source columns. Each inclusion opens with a
banner row naming the text-name, library, the file read and the depth (`COPY PAYREC OF LIB1 -> lib/PAYREC.cpy (depth 1)`),
and a row `R` under each line REPLACE changed prints the compiled text. (Step 1:) the banner is text in the source
columns like any line, so a banner longer than 80 positions (a long path) wraps onto `+` rows as a long line does: no row
of a page is wider than the page. A DATE-COMPILED paragraph prints its replacement
in place of its first line, marked 'E', and its comment-entry continuation lines are not printed (the paragraph "is
replaced"); the JSON keeps the written text. (Step 4a:) a line REPLACE or COPY REPLACING changed is printed as written, marked
'R', and under it each line of its compiled text in a row with 'R' in column 9 and the text in the source columns
(`TextListingRenderer.ReplacedRows`); a caret still stands under such a line, since the row shows it as written. A DISPLAY
directive's transfer prints after its directive's rows, or where they would stand when the line is not listed
(`TextListingRenderer.DisplayRows`, section 6.10).

### 6.3 Diagnostics

Inline: under the line a diagnostic is anchored on, a row `==> severity CODE (col n): message`, with a caret under the
column when the line is printed in full. Summary at the end: counts by severity, then every diagnostic in listing order
with listing line, file, line and column. Each message keeps its citation of the rule of the edition in force
(`EditionInfo.Cite`, kb/Work PB4950); the JSON also carries the citation as a field (section 7.3).

To do this the binder's diagnostics become structured (step 3): `EditionContext` records each error and warning as a
`Diagnostic` (code, severity, message, physical `SourceLocation`) plus its resultant cursor, and
`EditionContext.Diagnostics` / `Warnings` and `CompilerDriver.Result.Errors` / `Warnings` become views rendered from those
records by the one `Diagnostic.Render`. Front-end diagnostics gain the resultant line where the reporter knows it (the
parser's token line), so a diagnostic in a twice-copied copybook is anchored in the right inclusion. A backend diagnostic
is placed by its COBOL source position like every other (ruling of 2026-11-18: never by a position in generated
code). The Roslyn backend's `#line` directives (section 6.9) make Roslyn report its own diagnostics at COBOL
positions, which `RoslynBackend.Neutral` already reads through the mapped span; the CIL backend reports at COBOL
positions natively. A backend diagnostic with no COBOL position goes to the summary.

**As built (step 3), with the corrections the code proved:**

- **ONE diagnostic record for every phase.** `WiseOwl.Cobol.Frontend.Diagnostics.Diagnostic(Code, Severity, Message,
  Location, Span)` is it; `Location` is a `SourceLocation?` (null for a bind diagnostic reported with no cursor, a
  diagnostic about the compilation unit as a whole, and for a backend diagnostic that names no position). (Step 3, correcting
  R5-1:) the seam's `BackendDiagnostic` was the same record without the span, so it is DELETED (rule 4): the backend reports
  the front end's `Diagnostic` (`BackendArtifact.Diagnostics`, `RoslynBackend.Neutral`), and no conversion at the driver
  exists, because there is nothing to convert.
- **The binder's records.** `EditionContext.Records` holds every error and warning in report order, each made by the one
  `EditionContext.Record` from the cursor (`LocationOf(Cursor)`, or no location when no cursor is set; a diagnostic about
  a skeleton read from the repository is located at its EXPANDS entry with the expansion's name in its message).
  `EditionContext.Diagnostics` and `Warnings` are `IReadOnlyList<string>` views (`Diagnostic.ToString`, that is
  `Diagnostic.Render`), byte-identical to the strings they were; `HasErrors` counts records; `WithdrawWarningsOf` removes
  records. The once-per-fact rule keys on the rendered line as before.
- **The resultant line, and its frame (correction).** "Front-end diagnostics gain the resultant line where the reporter
  knows it" would have meant a change at every reporter, and the text stages report in numberings of their OWN (each stage
  that maps text builds a `SourceLineMap` of its own), so a resultant line is meaningless without the numbering it is in.
  The fact therefore lives where the mapping happens: `SourceLocation` gains `ResultantLine` and `Frame`, set ONLY by
  `SourceLineMap.Locate` (the one resultant-to-physical mapping), and each `SourceLineMap` has a `Frame`, a process-unique
  number. Every diagnostic located through a map (the parser's, the lexer's, the post-parse screens', every bind diagnostic)
  carries them with no reporter changed; one located by a text stage carries that stage's frame. The listing uses the
  resultant line only when the frame is the listed pass's `LineMap` (the map the reported parse and the bind located
  through), so a line of another numbering can never pick the wrong inclusion.
- **`CompilerDriver.Result.Diagnostics` (correction).** Every return of the body carries the records of the source's
  diagnostics: the front end's bag, the binder's records, each expansion's own compilation's records moved to its EXPANDS
  entry (`ExpansionAssemblies.AtTheEntry(Diagnostic, ...)`, the one decider; each printed line of the expansion's report is
  its moved record's rendering), and the backend's errors. `Result.Errors` / `Warnings` stay the printed lines: each line
  that reports a diagnostic is its record's `Diagnostic.Render`, and the only other lines name no source (the source file is
  missing, the host refused an output, a listing file could not be written), which have no code and no position and are
  not diagnostics of the source. `ListingDiagnosticsTests.EveryPrintedDiagnostic_IsARecord_AndTheListingHoldsEachOne` holds
  that every line in a diagnostic's shape is a record's rendering. The test compile cache stores the records (format 4).
- **Deposit.** The one exit deposits `Result.Diagnostics` into the collector once, after the body returns
  (`ListingFacts.DepositDiagnostics`): every return carries them, so no return point has listing code.
- **Placement.** `DiagnosticSectionBuilder` places each record through `ListingLineIndex.Place(SourceLocation)`: a location
  of the index's frame goes to the inclusion its resultant line belongs to (`ListingLineIndex.Of(int, SourceOrigin)`, the
  innermost inclusion holding the line whose file is the location's), any other to the first text that lists that
  physical line; a location in no listed file (a generated `.g.cs`, an expansion's throw-away text) is placed on no line and
  names no file, so no listing names generated source. The column is a 1-based CHARACTER position of the line's listed text
  (`ReferenceFormatMap.CharacterPositionAt`, tabs expanded, a supplementary-plane character counted once), the one column every
  listing record names (the data map's and the object-code map's too). (Correction, listing step 3 review lead R1-1, fixed in step
  6b slice 7:) it was the UTF-16 index of `ExpandedIndexAt` plus 1, one greater per supplementary-plane character before it than the
  schema's "character position" and the caret row. Listing order: placed diagnostics by line and column, then the
  rest in report order; ids are 1-based in that order; `ListingLine.Diagnostics` names the ids on each line.
- **Text.** Under a printed line, per diagnostic: a caret row when the line is printed whole as written (one row, no
  replacement), at the column counted in positions (a supplementary-plane character is one); then
  `==> severity CODE (col n): message` from the source text's column, wrapped at the page width under the message
  (`TextListingRenderer.DiagnosticRows`, `Indented`). A diagnostic on a line the listing does not print is in the summary
  only. The summary (`SummaryRows`) starts a page after the source section with the heading line `DIAGNOSTICS`: the counts
  (`Errors: n   Warnings: n   Information: n`), then each diagnostic as `line  severity CODE file(line,col): message`
  (`-` for no line), wrapped; its page heading names the first outermost program (section 6.1).
- **Witnesses.** `DiagnosticRecordsTests` (Unit: the records and their views, the resultant line and frame, the severity map),
  `ListingLineIndexTests.ALocation_IsPlacedInTheBlockOfItsResultantLine_OnlyInTheIndexsFrame`, `TextListingRendererTests`
  (caret, wrap, summary), `ListingCitationTests`; `ListingDiagnosticsTests` (Conformance: a bind error placed, printed and
  cited, run in a directory whose path is shorter than the page and in one longer, so the summary entry wraps on every host and is
  asserted whole, never as a substring of one row; the second copy of a copybook copied twice; one record per printed diagnostic) and the anchoring assertion of
  `ListingOutcomeTests.AFailedParse_ListsThePassWhoseDiagnosticsAreReported` (section 4.4).
### 6.4 Options

Every option of the compilation with its value and whether it is the default: each public property of
`CompilerDriver.Options`, the edition, `--permissive`, the listing request itself, the compiler version (the assembly's
informational version), the runtime version, the .NET runtime version and the host operating system. Built from one table
in `OptionsSectionBuilder`; a drift test holds it to every public property of `CompilerDriver.Options`. The compiler and
runtime versions, and the page heading's, are spelled by one function, `ListingBuilder.VersionOf` (an assembly's
informational version, else its assembly version), so the rows of one listing spell their versions alike (review R1-2).

**As built (step 5), with the corrections the code proved:**

- **The table lives in the composition root.** The builder's layer (`compiler.listing`) may not name `CompilerDriver`
  (section 4.2, step 1), and the table must read `CompilerDriver.Options`, so `OptionsSectionBuilder` is in the namespace
  `WiseOwl.Cobol` beside the driver, and the driver hands its rows to `ListingFacts` as plain `ListingOption` values, built
  only when the options section is asked for.
- **One row per settable public property** of `CompilerDriver.Options` (`OptionsSectionBuilder.Table`, each row naming its
  property by `nameof`, the command line's spelling of the option and of its value), and the listing request expands into one
  row per settable public property of `ListingRequest` (`RequestTable`: `--listing-output`, `--listing-format`,
  `--listing-sections`, `--listing-page-length`, `--listing-page-width`). Then four `Host` rows: `compiler`, `runtime` (the
  `WiseOwl.Cobol.Runtime` assembly's informational version), `.NET` and `operating system`. The edition is the `--std` row
  (`85 (ANSI X3.23-1985)`).
- **Default means the library's default.** `ListingOption.Origin` is `Default` when the value is spelled as
  `new CompilerDriver.Options(source)`'s (or `new ListingRequest()`'s) is, `Specified` otherwise, and `Host` for a fact of the
  compiler or the host. Two rows decide it themselves: the source, which has no default, and the output path, whose default is
  DERIVED from the source path (`Row.DerivedFrom`): it is the default when `OutputPath` is null, whatever path it derives to.
  The command line's own per-edition default (a listing at `--std 85`) is not the library's, so `--listing` is `Specified` in
  every listing; the guide says so.
- **The drift test, `OptionsSectionDriftTests` (Unit), holds the table to both types by behaviour, not by name alone:** every
  settable public property has exactly one row and no row names another; at the defaults every option row is `Default` but the
  source and `--listing`; and for each property, setting it ALONE to another value (a value of its type, made by the test)
  changes exactly its row, which becomes `Specified`, besides the rows `DerivedFrom` it. A property added without a row, a
  row naming no property, and a row reading another row's property are each red (shown by mutation, kb/Work PB5925's
  step-5 note).

### 6.5 Program map (nested programs)

Every program, function, class, factory, object and method: its name, kind, nesting depth, containing program, COMMON /
INITIAL / RECURSIVE / IS PROTOTYPE, first and last listing line, and, once bound, its externalized name and the .NET
type it became. A view of the symbol table's program entries (whose attributes the binder writes from `BoundUnit` when
it creates them) joined to the source's program ranges (`SourceUnitFacts`, from the parse tree, so names and ranges
exist on a failed bind).

(Step 5:) the names, kinds, nesting and listing lines are built (`ProgramRangeBuilder`, step 1); the text listing prints them
as the `PROGRAMS` section (`ProgramMapText.Rows`): one row per unit in text order, its id, kind, first and last
listing line, and its name indented two columns per level of nesting, so a contained unit stands under its container. COMMON,
INITIAL, RECURSIVE, IS PROTOTYPE, the externalized name and the .NET type are the symbol table's program attributes and join
in step 6. After a parse that recovered (`ListingHeader.Parse` is `Recovered`, section 6.1) the map ends with a blank row and
the note `TextListingRenderer.RecoveredNote` composes: the source units are only those the recovery kept (review R1-1).
(Step 6:) the attributes join as `ListingProgram.Attributes` (`ListingProgramAttributes`: COMMON, INITIAL, RECURSIVE, IS
PROTOTYPE, the externalized name, the .NET type `BoundUnit.ClassName`), printed on a row under the program
(`ProgramMapText.AttributesText`). The join needs no name comparison: the program entry is declared at the unit's first token
(`BoundUnit.DeclaredAt`, new), which the one index places on a listing line whose `Program` is that unit
(`DataMapBuilder.ProgramAttributesOf`). They are present when the program map or the data map is asked for and the bind accepted
the program; the bound class, factory, object and method units carry no position yet, so their attributes are kb/Work
PB5932's lead.

### 6.6 Data map: a view of the symbol table

Every data item of every program, in definition order, including those GnuCOBOL omits (INDEXED BY indexes, level-78 and
CONSTANT entries, condition-names, level-66 entries, the items the standard defines implicitly, marked as such). (Step 6
review round 1, correction:) "compiler-implicit items" read as the compiler's own temporaries, which no rule of the standard
defines and no program names; they are no declared name and have no entry. What is listed and marked implicit is what the
STANDARD defines implicitly where an entry names it: a CAPACITY IN register (ISO §2023:13.18.38.3 SR30) and the record area
of a CD (ISO §1985:XIV.2.2.4 GR2). The data map is a VIEW of the symbol table's data, index, condition, constant, report and
cd entries (section 4.1); it does not walk `DataBindResult.Forest`
or any binder type. Each attribute is written into the entry by the binder at the point it decides it:

| Column | Written into the entry by |
|---|---|
| level, name, definition position | the data division binder when it declares the item (`DataBinder.BindEntry`, from `DataItem.Level`, `CobolName`, `DeclaredAt`) |
| section | the same (`DataItem.Section`, `EntrySection`) |
| usage, picture, category | the same, once the picture is settled (`PicInfo`, `DataItem.PictureText`) |
| OCCURS / DEPENDING ON / INDEXED BY | the same (`DataItem.Occurs`, `OccursSpec`, `Indexes`); the DEPENDING ON object and each index are entries of their own, linked by entry id |
| REDEFINES / RENAMES | the redefinition and renames passes (`DataItem.RedefinesTarget`, `Renames`), as entry ids |
| GLOBAL / EXTERNAL / BASED | the declaring binder (`HasGlobalClause`, `HasExternalClause`, `IsBased`) |
| offset in its record (first occurrence), length in character positions and bytes | the layout step that computes them (`RecordLayout.OffsetInRootWithEveryIndexOne`, `ItemLength.Positions`, `RecordLayout.OccupiedBytes`), once the sizes are final |
| the native field and its .NET type | the storage-form decision (`StorageForm`, taken in Bind): the field's name (`DataItem.ClrName`, `StructName` for a group's record struct) and the .NET type the decision requires (`NativeInt` -> `long` or `Int128` of n digits, `CharImage` -> a character image of width n, `NativeFloat`, `DynamicTable`, `ObjectRef`, `PointerRef`, `TierBWindow` -> a window of a redefinition class), spelled by ONE function on `StorageForm` that both backends implement against. It is the binder's decision behind the backend-neutral seam, so it names no emitted source and is the same for the Roslyn and the CIL backend |

Files get a file map, a view of the file entries: the file-name, organization, access, record keys, the record area and
the connector's native field. (Step 6 review round 1, as built:) the record keys are the RECORD KEY's and each ALTERNATE
RECORD KEY's (the key as written, the entries it is made of, the DUPLICATES phrase) and the RELATIVE KEY item; the record
area is the file's record entries; the file map also lists the reports its REPORT clause names. A file connector is held
in the run unit's registry under a NAME, not in a native field (correction): the file map gives that name (`FileModel.CobolName`,
the program's path and the file-name, or an EXTERNAL file's external name) and, for a file of an object or a factory, the
field that holds each object's own connector key (`FileModel.InstanceKeyField`). So the table is finished after the run unit
names every connector (`BinderDriver`, after `QualifyClassFiles`).

**As built (step 6), with the corrections the code proved:**

- **The snapshot is plain values in a layer of its own.** `SymbolTableSnapshot`, `SymbolEntry`, `NameClass`, `SymbolPosition`
  and one attributes record per class (`ProgramAttributes`, `DataAttributes`, `ConditionAttributes`, `IndexAttributes`,
  `ConstantAttributes`, `FileNameAttributes`) live in `WiseOwl.Cobol.Symbols` (src/WiseOwl.Cobol.Compiler/Symbols/), the layer
  `compiler.symbols` of architecture-edges.json, which depends on nothing; the binder, the listing builder and the driver may use
  it. So the builder reads the table and cannot reach a binder type, by the layering test.
- **Attributes are read off the symbol once the bind is finished, not written beside it (correction).** "Each attribute is
  written into the entry by the binder at the point it decides it" would store every fact twice, on the symbol the binder
  decides it for (`DataItem`, `FileModel`, `Condition88`, `IndexDeclaration`) and on the entry, kept in step by nothing (one
  decider, rule 4). The binder already records each decision on its symbol, and the offsets are final only when the layout is.
  So `SymbolTableFinish.Of` (binder layer, after `ElementBinders.Freeze`) declares each name into a `SymbolTableWriter` with
  its symbol, and `SymbolTableWriter.Snapshot` reads every entry's attributes then, from the frozen `DataBindResult` of each
  unit: the level, `DataItem.Section`, the usage (`UsageFamilies.UsageWord`), `PictureText`, the category, `OccursSpec`
  (minimum, maximum, the DEPENDING ON object), `Indexes`, `RedefinesTarget`, `Renames`, GLOBAL, EXTERNAL, BASED, the offset
  (`RecordLayout.OffsetInRootWithEveryIndexOne`: the first occurrence's, none in a table of dynamic capacity), the length
  (`ItemLength.Positions`) and the bytes (`RecordLayout.OccupiedBytes`).
- **The .NET type has one spelling already (correction).** "Spelled by ONE function on `StorageForm`" is
  `StorageFormPass.StorageElementType` (made internal for this), which `DataItem.ElementType` reproduces and
  `DataItem.FieldType` wraps for a table; the data map prints `DataItem.ClrName` and `FieldType`, and an index-name's cell
  `StorageElementType(StorageForm.IndexCell)`. Both are the binder's decision, the same for every backend.
- **What the table holds.** Every name a source element declared, in definition order: each record's entries in the order
  written (FILLER included, each level-88 under its conditional variable, each INDEXED BY index-name under its table, a named
  CAPACITY register after its table's subordinates, marked implicit, the level-66 entries after their record), the type
  declarations (`ForestSlice.TypeDeclarations`: listed where written, with no offset, bytes or field, since "A type declaration has no
  storage associated with it", ISO §2023:13.18.58.4 GR2), the constant entries (`ConstantEntryBinder.DeclaredConstants`: a constant
  makes no data item, ISO §2023:13.10.4 GR1, so it is read off the binder), the file-names (`FileModel.SelectName`), each report
  description entry with its report group description entries (`ReportScope.Declared`, `ReportModel.WrittenEntries`: stored
  nowhere of their own, so no offset, bytes or field), and each CD's
  cd-name with the implicit description of its record area (`CommunicationSlice.Declared`), ordered by their positions. The
  source elements are each program unit, the OBJECT and FACTORY of each class with each method's own LOCAL-STORAGE and LINKAGE
  entries and type declarations (`OoMethodSymbol.Binding`, `DataScope.TypeDecls`), and each interface's methods. Compiler
  temporaries are no declared name and have no entry. A condition-name carries its position (`Condition88.DeclaredAt`, copied by
  `CopyOnto`); an index-name stands on its table's entry; a file on its SELECT entry (`FileModel.EntryAt`). (Step 6 review round
  1, correction:) a level-1 entry of a CD's area or record is owned by its CD, as a report group's is by its report.
- **A report group entry is described by the item its data-name names (step 6 review round 2, correction).** Round 1 described
  an entry by the printable item a COLUMN clause places, so a SUM entry with no COLUMN clause was listed as a group, and a
  printing SUM entry by its printable item, though "the data-name is the name of the sum counter, not the name of the associated
  printable item" (ISO §2023:13.18.54.4 GR5). The binder now records, where it makes each item, the one item each written entry
  describes (`ReportModel.ItemOf`, written by `ReportModel.Describe`): a SUM entry's counter register first, else the printable
  item, else, for an elementary entry with a PICTURE and no COLUMN clause, the unprintable item (ISO §2023:13.18.53.4 GR3: it
  "defines an unprintable item"), analysed once by the printable item's analyzer; a group entry has none.
  `ReportSourceFamily.Category` reads the same item (its families now carry their written `Entry`), so SR4's "numeric data item"
  and the data map read one answer. A sum counter is listed with category NUMERIC and its digits as its length, and no picture
  or usage: it "is a conceptual data item that behaves as a data item of the category numeric" (GR1). The entry is declared as
  that item, so an attribute naming the counter names the entry.
- **No field for a LINKAGE item; a type declaration's condition-names and index-names are marked (step 6 review round 2,
  correction).** "The linkage section describes formal parameters and returning items" (ISO §2023:13.7.1): their storage is the
  activating element's, and the compiled code holds only an argument carrier whose form the backend picks per calling convention
  (a program's `ref` parameter, a method's formal pointer and local), which no bound fact names. So a LINKAGE item has no native
  field or type, as an entry of a type declaration has none. A method's LOCAL-STORAGE item's native field is the method's local
  variable of that name and type. A condition-name or index-name of a type declaration's entry is marked `TypeDeclaration` like
  the entry (`ConditionAttributes`, `IndexAttributes`), and the index-name has no field either.
- **A copy stands where its clause is written (step 6 review round 1, correction).** A TYPE or SAME AS clause's effect is "as
  though the data description identified by type-name-1 had been coded in place" (ISO §2023:13.18.57.4 GR1), but a copied entry
  keeps its template's `DeclaredAt`. The table places every data entry by ONE answer, `SymbolTableFinish.WrittenAt`: the entry's
  `DataItem.ReferenceSite` (the entry that wrote the clause), whose position is its own for a written entry; a copy's
  condition-names and index-names stand on the same line. So a copy belongs to the program, the line and the margin of its
  reference, and the type declaration's lines carry only the declaration's own facts. `DataBinder.CloneRenamesOnto` now records
  `ClonedFrom` like every other copy, so a level-66 entry of a type is placed the same way.
- **The program a name belongs to is placed, not carried.** A data map entry's `Program` is the innermost program of the listing
  line its declaration stands on (`ListingLine.Program`), so the snapshot carries no unit and no name is compared.
- **What is finished, and when.** `EditionContext.RecordsSymbols` (the driver sets it through `ListingFacts.ReadsSymbols`: the
  program map or the data map asked for) finishes the program entries; `EditionContext.RecordsNames` (`ListingFacts.ReadsNames`:
  the data map) every other name. The standard listing holds the program map and not the data map, so it finishes only the
  program entries: the first benchmark run, which finished every name for it, put the standard listing of SM201A at +12.6 %
  over no listing on a full compile, over its 10 % budget (section 9).
- **Text.** `DataMapText.Rows`: a row per entry (id, line, level, kind, the name indented under its owner, offset, length, bytes,
  usage, picture), the rest on a row under it (`DataMapText.Details`), then the file map. The margin (section 6.2) is
  `ListingMargin(Item, Offset, Length, Usage)`, made once by `LineAnnotationBuilder.MarkMargins` for each line that declares a
  data item, printed by `TextListingRenderer.WithMargin` from column 100 on a page at least 132 wide.
- **Witnesses.** `ListingDataMapTests` (Conformance: every name in the order written with its line, each offset, length and
  byte count derived from the PICTURE character-strings and §2023:13.18.40.4 GR14, §2023:13.18.38.4 GR4, §2023:13.18.44.4 GR1 and
  §2023:13.18.45.4 GR2, the links between entries, the file map, the program attributes, the margin equal to the data map's
  facts and printed from column 100, a bind error's `NotProducible`, no data map when none is asked);
  `ListingOffDoesNoWorkDriftTests.WithoutASymbolTableSection_TheBindFinishesNoSymbolTable` and its control (section 9).

### 6.7 Cross-reference

Symbols: data items, condition-names, index-names, procedure-names (sections and paragraphs, `SymbolTable`'s `ProcedureNames<T>`), files,
programs (CALL targets and units), and copy members (from the inclusions). Each use is one `ListingReference(Symbol,
Line, Column, Length, Use)` with `Use` one of:

- `Defined` - the declaration (`DataItem.DeclaredAt`, a paragraph header, a file control entry's SELECT, a PROGRAM-ID, a COPY
  statement). (Step 7a, correction:) this said "an FD"; step 6's table declares a file at its SELECT entry
  (`FileModel.EntryAt`), and the file-name of an FD or SD "shall be specified in a file control entry" (§2023:13.4.5.3 SR1,
  §2023:13.4.6.3 SR1), so it is a reference to that declaration (its slot is labelled `ref`), and each name has one `Defined`
  occurrence. Likewise the data-name of OCCURS DYNAMIC's CAPACITY phrase is a definition (`def`): a SET statement's operand
  "shall reference a data item defined in the CAPACITY phrase" (§2023:14.9.39.3 SR29). (Step 7b:) the `Defined` reference is the
  ENTRY's own position, where section 6.6 places it (a data description entry at its level-number, an index-name on its table's
  line, a program at its first token), so a definition is no name of its line (`ListingLine.Names` holds references only);
- `Read` - an occurrence at no store position of its statement;
- `Written` - an occurrence at a store position of kind `Write` or `WriteInPart`;
- `Modified` - an occurrence at a store position of kind `ReadWrite` (ADD TO, SUBTRACT FROM, INSPECT REPLACING, a STRING
  POINTER, ...).

Procedure references carry the transfer kind (PERFORM, GO TO, ALTER, USE, SORT INPUT/OUTPUT PROCEDURE) and build the
PERFORM graph; CALL references build the call graph; a file reference carries its verb. Sorted by name (Annex C fold of
the name, `CobolNames.UpperFold`), then by line. A symbol defined and never referenced is flagged.

#### 6.7.1 The symbol table: the one store of names, its entries, its occurrences, its invariants (steps 6 and 7)

The bound tree cannot answer "where, and how": a `Place` carries no position, `BoundReferences.Of` yields places, and
`BoundStores.StoreKindOf(BoundStatement, DataItem)` answers once per item. For `ADD WS-A` / `TO WS-B` written on two
lines both uses would get the statement's line, and in `MOVE WS-X (WS-X) TO WS-X` the three uses of one item would get
one kind. And the names themselves live in dozens of tables (section 6.7.2). The design gives the binder ONE symbol
table, `SymbolTable` (`src/WiseOwl.Cobol.Compiler/Binding/Model/SymbolTable.cs`: today's scope-aware resolver of that
name grows into the whole table, and every other name table moves into it), and the listing reads only it:

```csharp
// src/WiseOwl.Cobol.Compiler/Binding/Model/SymbolEntry.cs (new)
internal sealed class SymbolEntry
{
    public required int Id { get; init; }
    public required NameClass Class { get; init; }   // data, condition, index, procedure, file, report, report group,
                                                     //   sum counter, CD, configuration name, register, program, ...
    public required CobolName Name { get; init; }
    public required DiagnosticCursor DeclaredAt { get; init; }     // resultant position of the declaration
    public SymbolAttributes Attributes { get; internal set; }      // one record type per class (DataAttributes:
                                                     //   level, picture, usage, category, section, OCCURS/ODO, REDEFINES,
                                                     //   RENAMES, GLOBAL/EXTERNAL/BASED, offset, lengths, NativeField),
                                                     //   written by the binder where it decides each (section 6.6)
    public IReadOnlyList<SymbolOccurrence> Occurrences { get; }    // appended by Identify only
}

internal readonly record struct SymbolOccurrence(
    DiagnosticCursor At,             // resultant line and column of the reference's first token
    int Length,                      // characters of the reference as written (qualifiers, subscripts, ref-mod included)
    SymbolUse Use,                   // Defined | Read | Written | Modified for data; the transfer kind or verb otherwise
    DiagnosticCursor Statement);     // the BoundStatement.At of the statement it is written in
```

The table is the only store of references: there is no ledger beside it. Occurrences go into their entry when
`Identify` resolves a name; the table also keeps the global append order (an index into the entries' occurrence lists,
not a copy), so `StatementBinder.BindStatement` holds a MARK where its statement began and classifies exactly the
occurrences appended since (the store kind, below). When the bind finishes, `BinderDriver.Bind` hands the finished
table to the listing collector as an immutable snapshot (`SymbolTable.Finish()` -> `SymbolTableSnapshot`, entries and
occurrences only, no binder type), the one thing the listing's symbol-table views read.

**The table's invariants, and their proofs.** The table is complete, and the listing can trust it, because of the
guarantees of review rounds 2 to 4. Each is an invariant of the table; each drift test below is its proof:

| Invariant | Proof |
|---|---|
| Every name table is the symbol table: no table keyed by a COBOL name exists outside it | `NameTableOwnershipDriftTests` rule (1) |
| `Identify`, and for the compilation group's object-class-names and interface-names `SymbolTable.ClassNames.Lookup` (which `Resolve`, its diagnosing half, calls), are the only ways from a written name to a symbol, and the only recording points | rules (2) and (3), rule (2)'s companion (only the table makes `IdentifiedName` and `OoNameResult`, the two lookups' answers), and `OoNameResolutionDriftTests` (the group-wide readers of a written word are adjudicated declaration questions) |
| A declaration question cannot answer a reference (`DeclarationFact`) | rule (4) |
| Name equality is the table's alone | rule (5) |
| Every name the programmer wrote to USE something is one occurrence | `GrammarNameSlotDriftTests` (the slots, from the grammar) and `EveryWrittenReferenceIsRecorded` (each slot, one occurrence) |
| Each occurrence carries its true store kind | the pairing test (`StorePositionsOf`) |

- **ONE recording point, made structural in both halves (rounds 2 and 3, R2-1, R3-1, R3-2).** A list of resolve paths
  was incomplete twice, so no list is the guarantee. The occurrence is recorded where a written name becomes a symbol,
  `SymbolTable.Identify`, and two mechanisms, each checked by a drift test that needs no list, make that the only way:
  - *(The compilation group's tier, correction of step 6b's OO lane, review round 1 R1-1.)* The object-class-names and
    interface-names are not one element's names: they are the compilation group's (section "Step 6b, the OO names" below),
    held by `SymbolTable.ClassNames`, one per group, and a written one is identified by that tier's funnel,
    `SymbolTable.ClassNames.Lookup(RuleContext? site, ParserRuleContext written, OoNameWant want) -> OoNameResult` (and
    `Resolve`, which calls `Lookup` and words the diagnostic when nothing answers). It is the second recording point, the
    group tier's twin of `Identify`, and it holds the same contract: `written` is the word as the parser read it (one token),
    the answer (`OoNameResult`) is made only inside `SymbolTable` (rule (2)'s companion), and it already records the
    word's §2023:8.3.2.2 use in the census of the element `site` lies in (`OoClassTable.RecordReferenceUse`). Step 7b records
    the occurrence there, at `written`, under the same condition it records that use (the word resolved, or named a formal
    parameter-name), into the same occurrence store `Identify` appends to; a site that asks the funnel twice for one written
    word (`RaisingPhrase.Partition` re-asks a formal under FACTORY OF with `OoNameWant.Class`) records one occurrence, keyed
    by the written token, as `Identify`'s probe flag keeps a probe and its resolution one occurrence. What the tier answers
    WITHOUT recording is no reference: `GroupClassNamed` and `GroupInterfaceNamed` (a REPOSITORY specifier's own question
    of the group, §2023:12.3.8.3 SR6, SR9 and SR16, adjudicated by `OoNameResolutionDriftTests`), `ResolveSpecifier` (what
    a specifier, a declaration, stands for) and the carrier readers (`ClassOf`, `InterfaceOf`, `SkeletonOf`: a read of a
    name resolved, and recorded, where it was written).
  - **(a) Every name table is private to `SymbolTable`.** `src/WiseOwl.Cobol.Compiler/Binding/SymbolTable/SymbolTable.cs`
    and its partial files OWN every table keyed by a COBOL name whose value is a symbol, of every name class: data,
    condition, index, procedure (`ProcedureNames<T>`, step 6b slice 2), file (`FileSlice`, `FileNameScope`, `InheritedGlobals.Files`, the `SELECT` table),
    report, report group, report VARYING, sum counter, CD (`CommunicationSlice`), screen, the CONFIGURATION SECTION names
    (alphabet, class, symbolic-character, mnemonic, locale, through `ConfigurationNames<T>`), type and constant names,
    the special registers that are implicitly defined names (CAPACITY, DEBUG-ITEM), the REPOSITORY specifiers and the
    OO class and interface tables (the group tier's, `SymbolTable.ClassNames`; a method roster is keyed by the
    externalized method name, no COBOL word, and stays on its class). Its one public LOOKUP of an element's names is
    `SymbolTable.Identify(ParserRuleContext written, NameClass wanted, bool report) -> IdentifiedName`, where `written`
    is the name as the parser read it and `NameClass` says which classes the position admits (a name a reference qualifies
    whose candidate set is a struct is identified by its class's typed method beside it, `IdentifyIndexName`: section 6.7.1,
    "Step 6b, data-name lane, first slice"); the group tier's is `ClassNames.Lookup`, above. Every other lookup the
    tables serve today becomes a private member of `SymbolTable`. The declaring binders create the entries
    (`SymbolTable.Declare`, which records the `Defined` occurrence and returns a `SymbolId`, an opaque number, never an
    entry or a symbol) and write each entry's attributes where they decide them (`SymbolTable.SetAttributes(SymbolId,
    ...)`, section 6.6); those two and `Identify` are the table's only writers. (Step 6b, the entry ids:) built as `EntryStore.Enter`, made
    by every class's declaration, with the id beside the symbol in the store (below, "Step 6b, the entry ids"). (Step 7b, dispatched early, correction: kb/Work
    PB5925's dated file `2026-11-19-step-7b-not-buildable-yet`:) as step 6b built it, each class's declaration keys its SYMBOL
    (`DeclareParagraph`, `DeclareSection`, `ConfigurationNames<T>`, `DeclareDataName`, ...) and returns no `SymbolId`; the entries are
    made after the bind by `SymbolTableFinish.Of`. The `SymbolId` made at declaration is what lets an occurrence join its entry: the
    identified answer is not the entry for most classes (a constant, an ordering-name, a paragraph, a report group), so it is owed by
    step 6b's remaining slices, each class's declaration giving its entry id as it moves, before step 7b records. (Step 6b, remainder
    slice 5:) the data-names and condition-names, the last classes, enter theirs where they are declared too
    (`SymbolTable.EnterDataItem`, `EnterConditionName`), so every class's declaration makes its entry and the finish declares none.
  - **Declarations cannot answer a reference (R4-1).** The data division's declaration questions (does this name exist
    in this scope, does it conflict with another declaration, where was it declared, is its description still open) go
    to `SymbolTable.Declarations`, a nested class whose members return only `DeclarationFact` values: a
    `readonly record struct DeclarationFact(bool Exists, DeclarationConflict Conflict, NameClass Class,
    SourceLocation DeclaredAt)`. The fact names a place in the source, never a `DataItem`, `FileModel`, `Condition88`
    or any other symbol, so no caller can turn it into an operand: a reference needs a symbol, and only
    `Identify` hands one out (inside an `IdentifiedName`; a class or interface, `ClassNames.Lookup` inside an `OoNameResult`).
    Every position the programmer wrote a name in to USE it goes through `Identify` (an object-class-name or interface-name
    through `ClassNames.Lookup`) and records, whatever division it is in: a RECORD KEY or ALTERNATE RECORD KEY name
    (`SymbolTable.RecordKeyNamesOf` since step 6b's remainder slice 2), an ASSIGN USING data-name, a KEY phrase of SORT, MERGE or READ, an
    OCCURS DEPENDING ON or INDEXED BY operand, a REDEFINES or RENAMES object, a CONSTANT FROM or LENGTH OF operand, and
    every caller of what was `DataBinder.QualifiedCandidates` (which asks for symbols, so it is a resolution; since step 6b's remainder
    slice 3 the table's private `DataNameCandidates`, reached through `IdentifyDataName`, `IdentifyEntryClauseOperand`,
    `IdentifyRegionOperand` and `IdentifyWithin`).
    `NameTableOwnershipDriftTests` rule (4) holds this: every non-private member of `SymbolTable.Declarations` returns
    `DeclarationFact`, `bool` or a collection of `DeclarationFact`, and takes no `out` parameter of another type; a
    member that would leak a symbol from it is red.
  - **The name key is a type.** A table is keyed by `CobolName` (a `readonly record struct` holding the Annex C fold of
    the word, `CobolNames.UpperFold`), made only by `SymbolTable` from a parser token. So "a member that takes a name"
    has a type a test can see.
  - **`NameTableOwnershipDriftTests` (Unit, Roslyn over the compiled `WiseOwl.Cobol.Compiler`).** Red on: (1) any
    field or property OUTSIDE `SymbolTable`, of any accessibility, whose type is or implements a dictionary or lookup
    keyed by `CobolName` or `string` whose value (or element, or an open type parameter) is a binder type: a new table
    anywhere is caught by its type; (2) any non-private member outside `SymbolTable` that takes a `CobolName` and
    returns, or gives through an `out` parameter, a binder type; (3) any non-private member outside `SymbolTable` that
    takes a `string` and returns a binder type and is not in the committed classification
    `tests/WiseOwl.Cobol.Tests.Unit/Listing/string-parameter-members.json`, where each entry says why its string is no
    COBOL name (a C# identifier, a message, a clause keyword) and which may only shrink. A new lookup by string is red
    until it is converted or classified with a reason; there is no allowlist of tables. Rule (4) is the
    `Declarations` rule above.
  - **Equality of COBOL names is private too (R4-2).** Two spellings are the same name by the Annex C fold
    (`CobolNames.Same`, `CobolNames.Comparer`, both in `WiseOwl.Cobol.Runtime`). A search that compares a symbol's name
    with a written name is a lookup without a table, so it is as much a resolution path as a dictionary. `SymbolTable`
    therefore holds the binder's only comparison of names, and `NameTableOwnershipDriftTests` rule (5), through the
    `SemanticModel` (by symbol, not by text), is red on every invocation of `CobolNames.Same`, every use of
    `CobolNames.Comparer`, and every `==`, `!=` or `string.Equals` of which one operand is a binder symbol's `string`
    name property or a `CobolNames.Fold` or `UpperFold` call, anywhere in `WiseOwl.Cobol.Compiler` outside `SymbolTable`, and every call there of one of the table's static `bool`
    equalities over a `string` that is handed a binder symbol's name property for the string or, for a binder-typed parameter, a
    lambda's parameter or a `foreach` variable (step 6b-r5 review round 1: an equality of the table made the caller's comparison),
    unless the site is in the shrink-only `tests/WiseOwl.Cobol.Tests.Unit/Listing/name-comparisons.json` with one of the
    non-resolution classes below and its reason. A resolution (class RESOLVE) moves into `Identify`; a declaration
    comparison (class DECLARE) moves into `Declarations`; the other classes stay where they are, classified:
    KEYWORD (a written word against a fixed word of the standard or the implementor: FILLER, ZERO, ALL, LINE-COUNTER,
    an intrinsic, exception or implementor-name), SAMESYMBOL (two already-resolved symbols compared: type equivalence,
    OO conformance), EXTERNAL (an externalized or repository-record name from compiled metadata) and MESSAGE (the
    wording of a diagnostic). The runtime's own uses (CALL by name at run time, ASSIGN resolution) are outside the
    binder and outside the rule. Today's classification of every site is evidence in section 6.7.2.
  - **Identify's arms all record.** The arms of today's `ReferenceResolver.Identify` that answer before the data-name
    lookup (a CAPACITY register, EXCEPTION-OBJECT, DEBUG-ITEM and its members, a REPORT SECTION sum counter, the
    LINAGE-COUNTER, LINE-COUNTER and PAGE-COUNTER registers) are references the programmer wrote, so each records an
    occurrence like every other arm, with the register or counter as its symbol (`ReferenceTarget.Register`,
    `ReferenceTarget.SumCounter`). No arm is exempt.
  - **Recording.** `Identify` appends the `SymbolOccurrence` (position and length from `written`) to the identified
    entry when `report` is true and the name identified one symbol; a probe
    (`ReferenceResolver.Probe`, `ProbeDeclaration`, `DescribesOwnItem`, and every classifying predicate) passes
    `report: false` and records nothing, as it reports nothing today.
  - `IdentifiedName` is a sealed record whose constructor is private to `SymbolTable`; it carries the occurrence id.
    Every builder of a place for a written reference takes it: `PlaceBuilder.PlaceForItem(IdentifiedName, ...)` and
    `PlaceBuilder.WholeTable(IdentifiedName, ...)`, which link the place INSTANCE they return to the occurrence
    (returning `place with { }` when the instance is already linked: a new reference, equal by value, so value
    comparisons of places elsewhere are unchanged). The overloads that take a bare `DataItem` stay for places nobody
    wrote (compiler temporaries, implicit operands, connector stores) and link nothing.
  - **(b) The completeness oracle reads its name slots from the grammar** (below, Guards): nothing lists a slot by hand.
- **Store kind.** `BoundStores`' exhaustive visitor gains one query, `BoundStores.StorePositionsOf(BoundStatement)`,
  that returns `(Place, StoreKind)` for each store position the statement's WRITTEN operands occupy, the place being the
  instance the bound node holds: `TargetHit`, `IntoHit` and `ReceiversHit` see those. `ConnectorHit` does not
  contribute: the places it sees are built by a file connector's callback from a `FileModel` (the record area, FILE
  STATUS, RELATIVE KEY, DEPENDING ON), not references written at the statement, so they belong to no occurrence. A
  statement that stores into a whole table with no place (`BoundTableSort.Storage`) reports the table item as
  `ReadWrite`, matched to the statement's occurrence of that table. `StatementBinder.BindStatement`, the one statement
  funnel, classifies the occurrences appended since its mark when the statement is bound: an occurrence whose linked place instance stands
  at a store position (found by walking from the stored place inward through the places it wraps, to the first instance
  linked to an occurrence) takes that position's kind; every other occurrence is `Read`. A subscript or
  reference-modifier operand is its own `Identify`, so it is its own occurrence and is `Read`.
- **Procedure, file and index references** are recorded by the same `Identify` (classes `Procedure`, `File`, `Index`);
  the occurrence carries the transfer kind (PERFORM, GO TO, ALTER, USE, SORT procedure) or the verb, which the statement
  funnel sets from the bound statement as it classifies data uses.
- **Where the result lives.** The classified occurrences are the entries' own, in the finished snapshot, read by
  `CrossReferenceBuilder` (the cross-reference section and the JSON), and by `LineAnnotationBuilder` (`ListingLine.Names`
  for the HTML links, `ListingLine.Margin` for the text margin). Nothing else derives a reference's position or use.
- **(Step 6:) where Finish lives.** `SymbolTable` is in `compiler.model`, which is backend input and may not name the binder's
  constant table or `StorageFormPass` (`ArchitectureEdgesDriftTests` and `BackendContractDriftTests` refused a first build that
  put `Finish` there). The finishing is therefore `SymbolTableFinish.Of` in the binder layer. Step 6b, which makes the table the
  one store of names, decides where the grown `SymbolTable` lives (kb/Work PB5932).
- **(Step 6b, as built so far, with the corrections the code proved.)**
  - *Where the table lives.* In the BINDER layer, not the model: `src/WiseOwl.Cobol.Compiler/Binding/SymbolTable/`, namespace
    `WiseOwl.Cobol.Binding` (`compiler.binding`). The one store owns tables of binder types (`FileModel`, `ReportModel`,
    `ConstantDef`, the OO symbols) and resolves through the binder, so it cannot be backend input; the move ended the three
    tolerated `compiler.model` -> `compiler.binding` pairs it held. `Scope` stays in the model (`Binding/Model/Scope.cs`).
    `SymbolTable` is ONE type written as a partial class, one file per name class (`SymbolTable.Communication.cs`,
    `SymbolTable.Files.cs`, ...), with `SymbolTable.Identify.cs` and `SymbolTable.Declarations.cs` beside them: one store
    without one file of every class's code.
  - *`DeclarationFact` is the fact the questions already answered (correction).* The data division's declaration questions
    (`DataBinder.NonDataNames.cs`, deleted) answered `NonDataDeclaration`: a kind (screen entry, alphabet-name, constant-name,
    index-name, condition-name, type-name, a type declaration's member or condition, file-name, cd-name), the type's name, whether
    the declaration was refused, and the depth of the declaring element. It names no symbol, so it IS the fact: it is renamed
    `DeclarationFact(DeclarationKind Kind, string? TypeName, bool DeclarationRefused, int Depth, string? Spelling)`, not the
    `(Exists, Conflict, Class, DeclaredAt)` drafted above, which no question asked. Rule (4) admits `DeclarationFact`,
    `DeclarationFact?` (a fact or none), `bool` and a collection of `DeclarationFact`. The instance is `SymbolTable.Declared`
    (a member may not share its nested type's name).
  - *`IdentifiedName` is made only by the table, by a rule, not by C# access (correction).* "A constructor private to
    `SymbolTable`" cannot be written: an outer type cannot call a nested type's private constructor, and a top-level type cannot
    restrict construction to another type. `IdentifiedName` and `CobolName` have internal constructors, and
    `NameTableOwnershipDriftTests` refuses an object creation of either outside `SymbolTable` (rule (2)'s companion).
  - *`Identify` takes a reach.* `Identify(ParserRuleContext written, NameClass wanted, bool report, NameReach reach =
    NameReach.Visible)`: a position that names only the element's OWN entries (an FD or SD's file-name, §2023:13.4.5.3 SR1,
    §2023:13.4.6.3 SR1; I-O-CONTROL SAME AREA; APPLY COMMIT) passes `NameReach.Own`, so it does not find a container's GLOBAL
    file as today's own-files lookups did not.
  - *`Identify` keys one word (correction, PB5932 review round 1).* `written` is a parse context because step 7b records its
    span, but a name is ONE word and a COBOL word is one token (`cobolWord`, `CobolWords.g4`), so the key is that token's
    text. A context of several tokens denotes no name and `Identify` answers null: `GetText()` glues tokens, and slice 1's
    first build let `FUNCTION EXCEPTION-FILE (A - B)` bind to the file `A-B` because it handed the whole argument context to
    the table. The rule is the table's, so no caller can bring the glued-text hazard (kb/Work PB2040) back.
  - *A question that answers no symbol is the table's too.* The qualifier walk asks `CdQualifies` and `FileQualifies` (does
    this outermost qualifier name a cd or a file whose record is the root), and an enumeration reads `VisibleFiles` or
    `OwnFiles`: a `bool` or a list of symbols in declaration order is no lookup by name, so rules (2) and (3) do not see it and
    the table stays the only place that turns a NAME into a symbol.
  - *The rules as the test applies them.* "Binder type" is a type declared in a binder namespace other than an enum (a value,
    never a symbol), the name key and the declaration fact. Rules (2) and (3) ask every member whose EFFECTIVE accessibility is
    not private, `SymbolTable`'s own included: its one lookup is `Identify`, which takes the parser's context, so a second
    public lookup on the table is as red as one outside it. Rule (5) also leaves `CobolName`'s own equality alone: it is the
    one definition of the name equality the keys use (`CobolNames.Comparer`'s, the equality of every table it replaces). The
    analysis compiles the compiler's sources with Roslyn, its source generator run as in the build, and asserts the
    compilation has no error, so no site is unbound.
  - *Measured red, at the branch's first commit (4527cb3dcd):* rule (1) 65 tables (the evidence's 65), rule (3) 188 members
    (the evidence's superset, 194, less the enum returns), rule (5) 299 sites (125 SAME, 130 CMP, 44 NAMEEQ; the evidence's
    124, 130, 40 at 7bc7e33469), rules (2) and (4) red because neither `CobolName` nor `Declarations` existed. The
    classifications were seeded from the evidence and a reading of each member: rule (3) 122 members whose string is no COBOL
    name, rule (5) 86 sites of a staying class.
  - *(Step 6b, slice 1, correction: the TEMPORARY baseline.)* Step 6b lands by name class (section 11, kb/Work
    PB5932's dated file `2026-11-18-land-by-class`), so between slices the test reads
    `tests/WiseOwl.Cobol.Tests.Unit/Listing/name-table-ownership.baseline.json`: the findings of rules (1), (3) and (5) no slice
    has yet moved, each by the key the classifications use (the member for rules (1) and (3), the site for rule (5)). A finding
    neither classified nor in the baseline is red, so no new table, lookup or comparison can appear; a baseline entry no longer
    found is red, so each slice lowers the baseline in the same change; the baseline is under a ceiling in the test that only
    falls, holds no rule other than (1), (3) and (5), and shares no key with a classification (a site is either classified for
    good or waiting for its slice). Rules (2) and (4) take no baseline. At slice 1 it holds 325 entries: 56 tables, 63 lookups,
    206 comparison sites. It is not a permanent ratchet: the LAST slice empties it and deletes the file and the code that reads
    it, and the test is again the five rules above, green with no exceptions. (Step 6b, remainder slice 5:) done: the slice emptied
    the baseline and deleted the file, its ceiling and the code that read it; all five rules are green with no exceptions.
  - *(Step 6b, slice 2, branch `listing-step-6b-2`.)* Two classes moved, and the baseline holds 308 (47 tables, 61 lookups, 200
    sites):
    - **The tables keyed by no COBOL user word** (section 6.7.2's last row) are keyed by a type that says what the key is, so rule
      (1) no longer reads them as name tables: `FileModel.FdClauses` by the enum `FdClause` (the 1985 agreement-rule clauses are a
      fixed set); `ScreenFacility.ClauseFacts` by the generated grammar rule index; `Polynomial._terms` by a private `MonomialKey`
      ordered as the string was; `ExpansionPlacements.ByName` by `ClrTypeName`; and `IntrinsicCatalog`'s table,
      `IntrinsicArgumentRules.Verified` and `DeliberatelyUnscreened` and `ImplementorNameRegistry`'s rows by `StandardWord`, a word
      the standard or the implementor defines, equal on the basic letters alone (`CobolKeywords`, §2023:8.1.3.2 GR3a). Three of
      those compared with the user-word fold before; every key of theirs is basic letters and the fold maps no extended letter to a
      basic one, so every lookup answers as before (the oracle is identical).
    - **The procedure-names.** The binder's `ProcedureNameMap<T>` (deleted) became the table's private `ProcedureNames<T>`, keyed by `CobolName`, and the
      scopes it served are the table's: the program's, each method's (keyed by its `OoMethodScope`) and each section's own
      paragraphs. `ProcedureTableBuilder` declares through `SymbolTable.DeclareParagraph` and `DeclareSection`, and every
      procedure-name reference resolves through `Identify(written, NameClass.Procedure, report, site: ProcedureSite)`, whose body is
      the §2023:8.4.2.2 algorithm `ResolveProcedureWord` was (`SymbolTable.ProcedureNamed`). `NameClass` gains `Procedure`.
  - *`Identify` reads a REFERENCE RULE's words from its children (correction, slice 2).* "`Identify` keys one word" holds for a
    name rule, but a procedure-name is a reference rule (`procedureName`: a head and an optional `OF`/`IN` qualifier), and so is a
    USE FOR DEBUGGING operand, which the grammar spells as a `dataReference`. The table reads the head and the qualifier from the
    context's children (`SymbolTable.ProcedureWords`), never its glued text, so the hazard the one-word rule closes stays closed;
    any other context names a procedure only when it is one word. The data-name class will read `dataReference` the same way.
  - *A reference's SITE is an input (correction, slice 2).* A procedure-name resolves from where it is written: its method (a
    method's procedure-names are its own, §2023:8.4.6.1) and its containing section (§2023:8.4.2.2.1 SR6). The ambient cursor
    that held them is the procedure binder's (`BinderContext.CurrentMethodScope`, `CurrentSection`), not the table's, and a USE FOR
    DEBUGGING subject is resolved after collection, when no cursor stands at it; so the caller states the site
    (`ProcedureSite(Method, Section)`), `Identify` refuses a procedure-name without one, and no other class reads it.
  - *A name several declarations carry (correction, slice 2).* `IdentifiedName.Symbol` is one symbol, except for a procedure-name
    that several procedures carry: it is then `ProcedureCandidates` (the rule broken, the spelling, every candidate in declaration
    order, and SR7's section), which the procedure table describes for COBOL2122 or the qualification message (kb/Work PB466). It
    is no identification, so step 7b records no occurrence for it. The paragraph a name identified is `ParagraphDefinition(Pc)`,
    and its owning section is read where the pc space is (`ProcedureTableBuilder.ParaSections`), never stored a second time.
  - *(Step 6b, slice 3, branch `listing-step-6b-3`.)* The constant-names moved, and the baseline holds 294 (43 tables, 56 lookups,
    195 sites). `SymbolTable.Constants.cs` owns what `ConstantEntryBinder.ConstantScope` and `InheritedGlobals` held: each source
    element's declared and bound constant entries (the binder's own element and each METHOD's, §2023:11.7.4 GR5), the words a
    method's data division defines before its entries bind, and the containers' GLOBAL constants with their depths
    (`InheritConstants`, `DropShadowedConstants`), all keyed by `CobolName`. Every constant-name reference is
    `Identify(written, NameClass.Constant, report)`, whose arm binds a declared entry on demand (§2023:13.10.3 SR4 and SR5 forbid
    only a circular dependence); `ConstantEntryBinder` declares the entries (`SymbolTable.Declare`, `DeclareMethodWord`), binds them
    in order or on the table's demand (`BindDeclaredConstant`) and joins each bound value through `SymbolTable.BindConstant`, which
    answers §2023:13.10.3 SR9's earlier duplicate. The questions that answer no symbol (`IsDeclaredConstant`, `IsConstantName`,
    `IsRefusedConstantName`, `IsFailedConstant`, `NamesConstant`, `InheritedConstantDepthOf`, `MethodDeclaresWord`,
    `ClosesConstantCycle`, `UnitHasConstants`) are the table's too, so no caller compares a constant's name. The corrections the code
    proved:
    - *A name no parse rule holds alone (correction, slice 3).* Three constant-name positions have no context of their own: an
      integer constant-name in a PICTURE character-string's repetition, `X(K)` (§2023:13.10.3 SR2: "it may also be used to specify
      repetition in a picture character-string"), which the scanner reads as part of the one PICTURE token; a subscript of a LENGTH
      OF operand, read from the operand's token list (§2023:13.10.3 SR3); and the word the compile-time evaluator substitutes. The
      first two are `Identify(WrittenWord written, NameClass wanted, bool report)`, where `WrittenWord(IToken Token, int Start, int
      Length)` is the token and the word's span in it, so step 7b records the occurrence at the word's own column; the evaluator's
      name callback (`CompileTimeExpressionEvaluator`'s `resolveName`) takes the `cobolWord` context it already held instead of its
      text. *(Corrected by slice 5, kb/Work PB5932's dated file `2026-11-19-review-leads-slices-2-and-3`:)* the slice still let two
      constant-name RESOLUTIONS take the written word as text, `NamesConstant` and `IsFailedConstant`, which bind an entry on demand
      as a reference does; slice 5 made both take the written word (`NamesConstant(ParserRuleContext)`,
      `IsFailedConstant(ParserRuleContext)` and `IsFailedConstant(WrittenWord)`), so no resolution of a constant-name takes a string.
      Strings still reach the table's constant-name DECLARATION questions, which bind nothing and answer a `bool` or a depth: a
      declaring word (`DeclareMethodWord`, from the data description entry that declares it), and the questions the data-name class
      asks with the text it holds its operands as (`IsDeclaredConstant`, `IsConstantName`, `IsRefusedConstantName`,
      `InheritedConstantDepthOf`, `MethodDeclaresWord`: an unidentified reference's verdict, the ranking of
      §2023:8.4.6.2.1 3)); they take the written word when the data-name class carries it.
    - *Before the data bind there is no table (correction, slice 3).* The AS phrase of PROGRAM-ID, FUNCTION-ID, CLASS-ID,
      INTERFACE-ID, METHOD-ID and a REPOSITORY class or interface specifier is read before the unit's binder exists, and its word may
      be a constant-name (§2023:13.10.3 SR2; kb/Work PB2588). That reading (`PreBindConstants`, deleted) is a resolution, so it is
      the table's: the static `SymbolTable.IdentifyConstantBeforeBind(CobolWordContext written, ...)`, which reads the entries off
      the parse tree and compares with the table's one equality. It records nothing (there is no table to record in); kb/Work
      PB5925's dated file `2026-11-19-as-phrase-constant-slot` carries what step 7b must decide for that word, which step 7a
      labelled `nn`.
    - *The name-index drift test sees the table (correction, slice 3).* `GlobalNameClassDriftTests` found a source element's name
      indexes by reflection over the binder, its state slices and `ConstantEntryBinder`; it now walks `SymbolTable` in its place and
      counts a table keyed by `CobolName` as well as by `string`, so a class moved into the table stays under the test (shown red on a
      planted `Dictionary<CobolName, DataItem>` in the table).
  - *(Step 6b, slice 4, branch `listing-step-6b-4`.)* The CONFIGURATION SECTION names moved, and the baseline holds 279 (40 tables,
    51 lookups, 188 sites). `SymbolTable.Configuration.cs` owns what the binding layer's `ConfigurationNames<T>` (deleted) and `ConfigurationSlice` held:
    the switch-status condition-names, class-names, alphanumeric and national alphabet-names, symbolic-characters, ordering-names,
    dynamic-length-structure-names and locale-names (§2023:8.4.6.1's list), each a private `ConfigurationNames<T>` keyed by `CobolName`
    with its containment depths, the mnemonic-names, the symbolic-characters a SPECIAL-NAMES paragraph declares ahead of its
    clauses and the inherited symbolic-characters a nearer declaration hides. `NameClass` gains one member per class
    (`SwitchCondition`, `ClassName`, `Alphabet`, `NationalAlphabet`, `SymbolicCharacter`, `OrderingName`, `DynamicLengthStructure`,
    `Locale`, `Mnemonic`), and every reference is `Identify(written, class, report)`. The corrections the code proved:
    - *`report` is also the §2023:8.3.2.2 USE (correction, slice 4).* A reference to a configuration-section name is a use of the
      word in the element it is written in (kb/Work PB3153): `ConfigurationNames.TryUse` recorded it in that element's census and
      `TryPeek` did not. That is exactly the reference/probe split `report` already draws, so `Identify` records the census use when
      `report` is true and the name identified a configuration-section symbol; `DataBinder.RecordConfigurationNameUse` has one caller,
      `Identify`. Step 7b records the occurrence under the same flag.
    - *A container's names reach the contained table (correction, slice 4).* A contained program, and a class's OBJECT and FACTORY
      halves, inherit the names (§2023:12.3.4 GR1), so `ConfigurationStep.Produce` takes the unit's binder and calls
      `SymbolTable.InheritConfigurationNames(container)`: each class copies the container's entries one level farther out, and the
      mnemonic-names are read from the container's table after the element's own. `ClassConfiguration` carries the class's table.
    - *The written word travels, never its text (correction, slice 4).* `ChoiceIndicators.AlphabetPair` answers the alphabet-name
      WORDS; the class condition's word, a bare EVALUATE class-name, the IN phrase of a THROUGH range and a STANDARD-COMPARE or locale
      argument travel as their contexts; `SymbolicOf`, `CodedCharacterSetOf`, `ResolveLocaleName`, `TryResolveRangeAlphabet` and the
      mnemonic registry take the word. A COLLATING SEQUENCE clause's alphabet-name is read where it is written into a
      `CollatingAlphabet` (the name and what it names in each class), which the one class test, `CollatingAlphabetFault`, and the
      file's key resolution read after the bind; the model keeps the names as written for the external-file identity, and the
      binder keeps what they name (`FileControlBinder`), so no model type depends on the binder layer.
    - *A range carries its carrier's identity (correction, slice 4; corrected again by review round 1).* The THROUGH range's
      `IN alphabet-name-1` phrase rode the bound node and `Condition88` as the alphabet-NAME, which the renderer looked up in a name
      table of the data binding (`DataBindResult.RangeCollations`, keyed by the name): a lookup by name in the backend.
      `TryResolveRangeAlphabet` now answers a `RangeCollationId` (model layer), an IDENTITY the unit's binder makes once per alphabet
      it names (keyed by the alphabet's definition) and the carrier holds; `DataBindResult.RangeCollations` is the unit's carriers in
      the order first named, and the field names are their ordinals. The slice first carried the carrier's POSITION in that list, and
      review round 1 measured why a position is wrong: a level-88 node crosses units. A container's GLOBAL condition-name is rendered
      in every contained program that references it (§2023:13.18.27.4 GR2), and a clone of a GLOBAL TYPEDEF's condition-name keeps
      the template's phrase, so the container's position read against the contained program's list named another alphabet's carrier
      (a silent wrong answer) or none (the compiler failed); the name-keyed table before it found nothing in a contained program that
      named no range of its own, so that program weighed the range natively. The backend's one reader, `CodeGen.Emit.RangeCollationFields`,
      finds the identity in the rendering unit's own carriers and then in each container's, outward, and reaches a container's static
      field through the container's type, which encloses the contained program's (`_PRG_OUTER.__COLLATE_R0`); an identity no unit of
      the chain made fails loudly. `conformance:cases/13.18.63-VALUE/pb5932_global_condition_range_in_alphabet` pins a contained, a
      doubly contained and a TYPEDEF-clone reference.
    - *A converted member keeps its diagnostic strings.* `CodedCharacterSetOf`, `ResolveLocaleName` and `TryResolveRangeAlphabet` take
      the word as its context and still take a site and a rule citation, so rule (3) sees a string; they are classified in
      `string-parameter-members.json` with that reason, and its ceiling rose 122 -> 123 (+3, less the two entries whose string was a
      name now carried as its context), in review's sight as the test's ceiling comment asks.
  - *(Step 6b, slice 5, branch `listing-step-6b-5`.)* The REPORT SECTION names moved, and the baseline holds 234 (35 tables, 42
    lookups, 157 sites). `SymbolTable.Reports.cs` owns what `ReportScope`, `DataBindState`, `DataBinder` and `ReferenceResolver`
    held: the report-names (this element's own RDs, and the visible set its containers' GLOBAL reports join nearest first, a name
    already there hiding a farther one, §2023:8.4.6.2), the sum counters (a list per name, kb/Work PB882), the report group
    description entries as the RD entries write them, and the walks over them (the report group candidates, the sum counter
    qualifier walk, the report entries a data-name reference also reaches, the VARYING clause's SR2 walk). `ReportScope` keeps the
    reports in order, their depths and the refused PRESENT WHEN conditions, and names nothing. `NameClass` gains `ReportGroup`,
    `SumCounter`, `ReportVarying`, `ReportEntry` and `ReportSumOperand`; every reference is `Identify`: INITIATE, TERMINATE, GENERATE,
    USE BEFORE REPORTING, a SUM clause's UPON phrase and data-name-1, a LINE-COUNTER or PAGE-COUNTER report-name qualifier, a sum
    counter, a VARYING counter, a constant's LENGTH OF report item, an FD's REPORT clause, FLAG-02 f's TERMINATE. The corrections the
    code proved:
    - *A report-section name may reach several entries (correction, slice 5).* A report group, a sum counter and a report entry may
      legally share a name (§2023:13.18.54.4 GR1 gives every SUM entry its own counter; two RDs may name a group alike), and the
      reference is ambiguous only when its qualifiers leave two (§2023:8.4.2.2.3 SR1), which each class's funnel words in its own
      terms (`ReportGroupResolution.Resolve` counts a data item of the spelling too, kb/Work PB5775; `CounterResolver` counts the
      ordinary data items, kb/Work PB3232). So `Identify` answers every candidate, `ReportCandidates<T>`, the shape
      `ProcedureCandidates` first took, and the funnel decides; a sum counter whose qualifiers reach none answers an EMPTY set, which
      the counter's funnel words as "none is subordinate to the qualifiers". Step 7b records an occurrence for a single candidate.
    - *A reference's reach selects reports, never declarations of other classes (correction, slice 5 review round 1).* A report
      group reference passes `NameReach.Visible` (GENERATE, USE BEFORE REPORTING: the GLOBAL reports join, nearest first) or
      `NameReach.Own` (the SUM clause's UPON operand, a data division site), and the reach chose only which RDs the table searches.
      The first cut of this slice also let it decide whether `ReportGroupResolution.Resolve` counts the data items of the spelling,
      so the UPON operand stopped counting them and an unqualified `UPON DA` beside a Working-Storage `DA` compiled silently. At every
      site an unqualified reference counts them (§2023:8.4.2.2.3 SR1, kb/Work PB5775); the witness is
      `conformance:cases/13.18.54-SUM/pb5775-upon-homonym`. The head and the report-name qualifier are read by ONE decomposition,
      `ReportGroupResolution.Words`, which the table's lookup, the funnel's diagnostic and an UPON operand's quote
      (`ReportDetailRef.ToString`) all call, so the reference the table reads as qualified is the one the funnel treats as qualified.
    - *A report clause's site is an input (correction, slice 5).* The VARYING counters in scope were a name table the clause binder
      set for each entry (`ReferenceResolver.VaryingScope`, a dictionary). They are the reference's SITE, as a procedure-name's
      method and section are: `ReportSite(Report, Varyings)` carries the enclosing report (§2023:13.18.53.3 SR4, "a sum counter
      defined in the current report") and the counters in scope, in declaration order (§2023:13.18.64.3 SR2), and the table finds
      the nearest declaration of a spelling. Only the sum-counter and VARYING classes read it.
    - *A report entry is named at two stages (correction, slice 5).* A constant entry's length phrase names a report item before the
      report section binds (§2023:13.10.3 SR11), so `ReportEntry` answers the entry as the RD entries write it
      (`DataBinder.ReportEntryLocation`); a SUM clause's data-name-1 names a bound entry that carries a value (§2023:13.18.54.3 SR4),
      so `ReportSumOperand` answers the entry family. Each keeps the qualifier walk it had, so the oracle is identical.
    - *A text-held operand gets a fact, not a symbol (correction, slice 5).* The data division's clause operands (FILE STATUS,
      RECORD KEY, OCCURS DEPENDING ON, ASSIGN USING, APPLY COMMIT) and its data-name lookups reach the report entries with the name
      as TEXT, because the data-name class still holds its operands so. What they ask of a report entry is a declaration fact (is
      the name a report entry's, nearer than the data items, a sum counter, of which report), so `Declarations.ReportEntriesNamed`
      answers `DeclarationFact`s (`DeclarationKind.ReportEntry` and `ReportSumCounter`, with the report-name) and `NearerEntryHides`
      a `bool`. The one exception answers a symbol: at COBOL-85 a sum counter IS such an operand (§1985:XIII.3.9.3 SR10d), so
      `SymbolTable.SumCounterOperand85` returns its register; it moved into the table with the counters it reads, and its baseline
      entry moved with it, to leave the baseline when the data-name class carries the written word.
    - *A declaration agreement stays a comparison, in `Declarations` (correction, slice 5).* An FD's REPORT clause names a report
      (§2023:13.18.46.3 SR1), which is a reference, identified once where the clauses are screened (`NameReach.Own`; the written word
      is kept by `FileControlBinder`, as slice 4 kept a file's collating alphabets, so the model names no parse context). Which file
      each RD is written to is the agreement of two declarations (§2023:13.14.3 SR1, "one and only one REPORT clause specifying
      report-name-1"), so `Declarations.NamesReport` compares them, and an RD a refused duplicate report-name declares keeps its file.
    - *A member the first measurement counted as a lookup resolves no name.* `DataBinder.ReportPrintablePicture` takes the report-name
      and the entry's data-name to word its site in a diagnostic; it is classified in `string-parameter-members.json` with that
      reason, beside `ReportGroupResolution.Resolve`, which now takes the written context and keeps its site string (123 -> 125).
  - *(Step 6b, slice 6, branch `listing-step-6b-6`.)* The program-prototype-names moved, and the baseline holds 223 (31 tables, 40
    lookups, 152 sites). `SymbolTable.Repository.cs` owns what `ConfigurationSlice`, `DataBinder`, `BinderDriver` and `StatementBinder`
    held: the REPOSITORY paragraph's program-specifiers as written and the refused ones (§2023:12.3.8.2; kb/Work PB4708), inherited by a
    contained unit and a class half with the configuration-section names (`InheritConfigurationNames`, §2023:12.3.8.4 GR10), the program
    prototypes they resolve to with §2023:8.4.6.8's containing program definitions (`ResolveProgramPrototypes`, which was
    `BinderDriver.ProgramPrototypesOf`), and the §2023:12.3.8.3 SR1 memory of every name the paragraph specifies
    (`RepeatedRepositorySpecification`, which was `DataBinder.CheckRepositorySpecification` and its table). `NameClass` gains
    `ProgramPrototype`; every program-prototype-name a statement writes is `Identify(written, NameClass.ProgramPrototype, report)`: CALL's
    AS phrase and its bare-word spelling (b), CANCEL's operand, a program-address-identifier's program-prototype-name-1, and the
    statement funnel's pure question (`CallBinder.CalleeSignatureOf`, `report: false`). The corrections the code proved:
    - *The progress table's "program, function, prototype, repository" row is two classes (correction, slice 6).* ISO §2023:8.3.2.2
      lists program-prototype-name and function-prototype-name as two user-defined words, each with its own scope rule (§2023:8.4.6.8 and
      §2023:8.4.6.6), and their tables, lookups and comparisons share nothing but the REPOSITORY paragraph that declares them. Slice 6
      moves the program-prototype-names whole; the function-prototype-names (the user-function specifiers, the intrinsic specifiers, the
      group's user-function table and the function-name lookups of `IntrinsicBinder`, `UdfBinder`, `SetBinder` and `SubscriptBinder`)
      are the next slice's class. The SR1 memory is the paragraph's, of every class, so it moved with the first of them.
    - *A name a SYMBOL carries is resolved by the table from the symbol (correction, slice 6).* A restricted program-pointer's USAGE TO
      phrase (§2023:13.18.60.4 GR25) is held as text by the item (`PicInfo.RestrictedPrototypeName`, also decoded from a repository
      record that has no parse tree), and a program-address-identifier's program-prototype-name-1 by the bound node
      (`BoundProgramAddress.Prototype`, which the bound tree keeps as text for the activation description). The readers that compared
      their signatures took the name (`StatementBinder.ProgramSignatureOf(string?)`, `ProgramRestrictionOf(string)`, both deleted). They
      now hand the table the CARRIER, the item or the bound node (`SymbolTable.ProgramSignatureOf(DataItem)`,
      `ProgramSignatureOf(BoundProgramAddress)`, `ProgramRestrictionOf(BoundProgramAddress?)`, `RestrictedProgramPrototypeOf(DataItem)`),
      and the table reads the text it holds, in the referencing element's scope as before. No caller handles the name, so rule (3) has
      nothing to see, and the written word (the USAGE clause's TO phrase, a `ref` slot of the grammar) is the occurrence step 7b records
      where it is declared.
    - *The AS NESTED table is keyed by an externalized name (correction, slice 6).* `StatementBinder.NestedCallables` is read with the name
      a CALL's literal-1 forms (§2023:14.9.4.3 SR15), compared by the implementor's mapping rule exactly as the run-time program table
      compares it (`ExternalizedNames`, DOC-A.1-68), never a word the program writes. Like slice 2's tables keyed by no COBOL user word,
      it is keyed by a type that says so, `ExternalizedProgramName` (in the model layer, which the bound layer's `StatementBinder` may
      name), and stays the procedure binder's.
  - *(Step 6b, slice 7, branch `listing-step-6b-7`.)* The function-prototype-names moved, and the baseline holds 201 (28 tables, 38
    lookups, 135 sites). `SymbolTable.Functions.cs` owns what `ConfigurationSlice` (`UserFunctionNames`, `FunctionSpecifiers`,
    `RefusedFunctionSpecifiers`), `StatementBinder` (`UserFunctions`, `UdfSelfName`) and `BinderDriver.UserFunctionsOf` held: the words
    the REPOSITORY paragraph's user-defined-function specifiers declare (§2023:12.3.8.2), the specifiers in the order written (the
    container's first, an element's own specifier of a name replacing it in place), the ones whose `AS literal-5` was refused
    (§2023:12.3.8.3 SR2, kb/Work PB4822), inherited with the configuration-section names (`InheritConfigurationNames`, §2023:12.3.4 GR1),
    and the user-defined functions the element may name (`ResolveFunctionPrototypes`, which was `UserFunctionsOf`: §2023:12.3.8.4 GR11
    a) to c) with §2023:8.4.6.7's word, and §2023:8.4.6.6's containing function definition registered last, §2023:12.3.8.3 SR11).
    `NameClass` gains `FunctionPrototype`, whose symbol is a `FunctionPrototypeReference` (the word, the function it resolved to or none,
    whether its specifier's literal was refused); every function-prototype-name a statement writes is `Identify`: a FUNCTION-keyword
    function-identifier and the reserved intrinsic words (`IntrinsicBinder.BindFunctionResult`, `report: true`), the keyword-omitted
    form (its classifying probe `report: false`, its bare activation `report: true`), `SignatureOf` and `DefinitionPermitsArguments`
    (probes), and SET ... TO ADDRESS OF FUNCTION's prototype arm. `UdfBinder.UdfBindCall` takes the identified prototype. The
    corrections the code proved:
    - *A function-prototype-name is keyed by the word the directive made of its token (correction, slice 7).* A function-name reaches
      every function lookup as `FunctionWord.Name`, the word the >>COBOL-WORDS directive left (§2023:7.3.10.4 GR2 to GR4), resolved
      once from the token (kb/Work PB1372), never its text. So `Identify` of `NameClass.FunctionPrototype` keys
      `FunctionWord.OfToken(token).Name` (a written word no token holds alone, `FunctionWord.OfWrittenWord`), in both overloads, reading
      the element's directive map (`DataBinder.CobolWords`, a reader `binder-member-readers.json` lists); the
      keyword-omitted form and SET's prototype arm, which keyed the raw text, now key the same word as the FUNCTION-keyword form (one
      rule; without a directive the two are equal, and `compare_oracle.py` is IDENTICAL). SET's operand is a `dataReference`, so the
      one-word rule now holds there too: a qualified or subscripted operand is no function-prototype-name, where its glued text was
      looked up before (kb/Work PB2040's shape).
    - *The containing-definition set is the table's (correction, slice 6 review lead R1-1).* §2023:8.4.6.6's and §2023:8.4.6.8's
      no-specifier leg, the names of the definitions containing the element, was `DataBinder.ContainingDefinitionNames`, a
      `HashSet<string>` under the name comparer that the program class left outside the table. `SymbolTable.DeclareContainingDefinitions`
      records it before the element's data binds; `DeclaresPrototypeName(name, function)` is the USAGE TO phrase's one scope screen
      (§2023:13.18.60.4 GR25, GR26), and `DeclaresFunctionPrototype` the bool question of a word no token holds (a SUBSCRIPT-mode
      capture, the ADDRESS OF FUNCTION claim scan). The function definition's own name is the same set's member, so `UdfSelfName` is
      deleted: the procedure binders' "am I a function?" is `UnitKind` (COMMIT and ROLLBACK, §2023:14.9.7.3 SR1, §2023:14.9.36.3 SR1).
    - *A function-prototype-name a SYMBOL carries is resolved from the symbol, as slice 6's program twin is.* A restricted
      function-pointer's USAGE TO phrase is text the item holds; its readers hand the table the item (`FunctionPrototypeOf(DataItem)`,
      `FunctionSignatureOf(DataItem)`, the one signature reader of the SET function-pointer pairs, §2023:14.9.39.3 SR20, and GR14's
      run-time granularity; `RestrictedFunctionPrototypeOf(DataItem)`), so `DataBinder.ResolveRestrictedPrototypes` takes no table.
    - *An intrinsic-function-name is a word of the standard, not a name of the table (correction, slice 7).* The REPOSITORY's
      `FUNCTION name INTRINSIC` specifiers (`ConfigurationSlice.RepositoryIntrinsics`) are keyed by `StandardWord`, as slice 2 keyed
      the intrinsic catalog: §2023:8.11's names are basic letters, and the lookup is by the catalog's canonical name.
    - *The group's functions by word are a sequence the table keys (correction, slice 7).* `GroupRepositoryProvider.FunctionsAt`
      answered a name-keyed table built under the name comparer; `FunctionsVisibleAt` answers every function unit that may supply
      details as of the element in D-R3 step 2's order, and `ResolveFunctionPrototypes` keeps each word's first.
    - *What stays: the compilation group's tier.* Three comparisons judge the group's FUNCTION-ID and program units against each other
      or a REPOSITORY word, across source elements, not inside one element's table: `GroupRepositoryProvider.FunctionsByWord`
      (§2023:8.4.6.7's search of the group by user-function-name), `BinderDriver.CheckFunctionUnits` (the same-word duplicate, COBOL1508)
      and `CheckDefinitionNameUniqueness`. They wait in the baseline for the run-unit tier the OO class names need, which is the same
      tier; `DataBinder._repositoryNameReported` (the once-per-word report of §2023:12.3.8.3 SR12 and SR13 in the user-word
      declaration funnel) moves with the data-name class's declarations (since step 6b's remainder slice 4, `SymbolTable.FirstRefusal`).
      (Step 6b, remainder slice 5:) the first two moved into the group tier, `SymbolTable.UnitNames`; the third's comparison could
      not be reached and was deleted (section "Step 6b, remainder slice 5" below).
  - *(Step 6b, the OO names, branch `listing-step-6b-oo`, a lane beside the data-name lane.)* The object-class-names,
    interface-names and method rosters moved, and the PROPERTY specifiers; the lane lowers the baseline by 46 (rule (1) 8 tables,
    rule (3) 7 lookups, rule (5) 31 sites): 155 with slice 7's. `SymbolTable.ClassNames` (`SymbolTable.ClassNames.cs`) owns what `OoClassTable`,
    `OoNameResolution` and `IdentityScope` (both deleted) and `OoInterfaceSymbol` held: the group's classes and interfaces by name, the
    imported ones by identity and the identities asked of the importer, the §2023:8.4.6.4 funnel (`Resolve`, and `Lookup`, its
    non-diagnosing half), each source element's REPOSITORY scope (`SymbolTable.RepositoryScope`, which was `IdentityScope`), an
    interface's method surface (`AllPrototypes`) with the §2023:11.6.3 SR5 conforming choice, and the questions that answer no symbol.
    `SymbolTable.Properties.cs` owns the PROPERTY specifiers (`ConfigurationSlice.OoRepositoryProperties`, deleted), inherited with the
    configuration-section names. The corrections the code proved:
    - *The class names are the COMPILATION GROUP's tier, not an element's table (correction).* The progress table said "run-unit
      wide: the table needs a run-unit tier". A class or interface definition is a source unit of the group, built before any element
      binds so that a reference resolves a class defined later in the file (OO deep-dive D1), and one element's REPOSITORY names
      another unit's class (§2023:8.4.6.4), so the names are the group's, never one element's: one `ClassNames` per `OoClassTable`
      (`OoClassTable.Names`), which keeps the symbols in source order, the skeletons and the §2023:8.3.2.2 censuses. It is a nested
      type of `SymbolTable`, so its tables are the one store's and rules (1) to (5) see them; it is the tier slice 7's group-wide
      function comparisons wait for.
    - *A method roster is keyed by no COBOL word (correction).* An INVOKE names its method only by literal-1 or identifier-2
      (§2023:14.9.23.2), resolved "as described in 8.3.2.2, User-defined words" (§2023:14.9.23.4 GR2) against the method's
      externalized name (§2023:11.7.4 GR1 b)), so a class's instance and factory rosters and an interface's prototypes are keyed by a
      type of their own, `ExternalizedMethodName` (`OoMethodSymbol.RosterKey`), as slice 6 keyed the AS NESTED table by
      `ExternalizedProgramName`; its one equality is the Annex C fold the rosters compared with, one classified comparer site.
    - *A written name's SITE and its WORD are two inputs (correction).* The funnel took the site and the name as text. The census a
      use is recorded in and the scope it is resolved in are the SITE's (an IMPLEMENTS operand is asked at its class definition,
      whose census is not its OBJECT paragraph's), and the name is the word, one token: `Resolve(edition, site, written, want, ...)`
      and `Lookup(site, written, want)`. A context of several tokens names nothing (the one-word rule of `Identify`), where a
      receiver's glued text was looked up before. `Lookup` is therefore the group tier's recording point, `Identify`'s twin
      (the invariant table and "The compilation group's tier" above, review round 1 R1-1), and the census it records into takes
      the word as written, as the declaration funnel (`DataBinder.DeclareUserWord`) does, so a COBOL2692 names the word in the
      programmer's spelling at every site (review round 1 R1-2: the RAISING partition passed the upper-folded word before).
    - *A name a symbol carries resolves from the carrier (as slice 6's prototypes).* An object-reference description and a RAISING
      element hold the class's table key as text, resolved where it was written; their readers hand the tier the carrier
      (`ClassOf(ObjectRefDescriptor)`, `InterfaceOf`, `ClassOf(RaisingTarget)`, `SkeletonOf`), and `OoClassTable.Find` and
      `FindInterface` are deleted. A REPOSITORY specifier's own question of the group (§2023:12.3.8.3 SR6, SR9, SR16) reads the group
      whatever the scope (`GroupClassNamed`, `GroupInterfaceNamed`, by the written word); `OoNameResolutionDriftTests` adjudicates
      those callers, as it adjudicated the direct `Find` callers before.
    - *An interface's method SURFACE is keyed by method-name (correction).* `AllPrototypes` presents one prototype per method-name
      and the SR5 choice is keyed by method-name, both user words, so both are the tier's; `MethodRoster`'s interface arm holds the
      surface the tier presents (`MethodRoster.Of(IReadOnlyList<OoMethodSymbol>)`), and the symbol keeps only the presented set the
      emitter reads (`OoInterfaceSymbol.PresentedInherited`). The surface is keyed by method-name while the inherited-prototype
      grouping and every roster are keyed by the externalized name: kb/Work PB5932's dated file `2026-11-19-step-6b-oo` carries that
      lead (behaviour kept exactly; the oracle is identical).
    - *What the OO class leaves (a decision for its next sub-slice).* The parameterized definitions' expansion (`OoExpansion.Expand`)
      runs before the class table exists and compares skeleton names, EXPANDS operands and formal parameter-names with the token
      text it substitutes: 18 rule (5) sites. Its names are class-names, interface-names and parameter-names, so they are the
      group tier's, but the tier is made with the table the expansion produces; whether the tier is made first (before the
      expansion) or the expansion's names get a tier of their own is the next sub-slice's design question, and the 18 sites stay
      in the baseline until it is answered. (Decided by step 6b's remainder slice 5: a tier of their own, the group's,
      `SymbolTable.ExpansionNames`; section "Step 6b, remainder slice 5" below.)
  - *(Step 6b, data-name lane, first slice, branch `listing-step-6b-data`.)* The DEBUG-ITEM register names, the type-names, the named
    CAPACITY registers and the index-names moved, each with its resolution, and the baseline fell by 33 (rule (1) 13 tables, rule (3)
    9 lookups, rule (5) 11 sites): 122 with slice 7's and the OO lane's. `SymbolTable.DebugRegisters.cs` keys the X3.23-1985 DEBUG-ITEM family (§1985:XV.3.2.4 GR10)
    where `ActivateDebugRegisters` makes it; `SymbolTable.Types.cs` owns the element's, the containers' GLOBAL and each method's
    type-names with the §2023:8.4.2.2.1 SR4 lapsed members (`WithLapsedTypeMembers`, `WithLapsedTypeConditions`,
    `WithLapsedTypeIndexes`, private now); `SymbolTable.CapacityRegisters.cs` owns the registers (§2023:13.18.38.3 SR30), their one
    insert, SR30's duplicate test and the one lookup behind the `OccursResolved` watermark; `SymbolTable.IndexNames.cs` owns the
    index-name namespaces (`IndexNameTable`, which was the model's `IndexNameRegistry`). `NameClass` gains `DebugRegister`, `TypeName`
    and `CapacityRegister`, and every written name of the four classes is `Identify`. The corrections the code proved:
    - *The progress table's first row is several classes (correction).* It moved the data, condition, index, capacity, type and
      debug-register names "as one unit, because they resolve together". §2023:8.3.2.2 lists data-name, condition-name, index-name and
      type-name as separate user-defined words, and the two registers are names the standard or SR30 defines implicitly; what ties
      them is §2023:8.4.6.2.1 3)'s ranking ACROSS the classes, and those questions (`NearerDataOrConditionNameHides`,
      `Declarations.DeclaredNearerThan`, `NonConditionDeclarationsOf`) were already the table's members reading each class through
      a question. So each class moved alone, the oracle IDENTICAL after each. The data-names and the condition-names stay one unit
      (each ranks the other both ways, and one qualifier walk serves both) and are the lane's next slice.
    - *A method's tables are the table's (correction).* The method overlay (`OoMethodDataScope`) held a method's name indexes as model
      dictionaries. They are `SymbolTable.MethodNames`, one per method, and `MethodNames.Declares` asks every table of it, so a
      method's declaration of a spelling in any class hides the object's (§2023:11.7.4 GR5); `MethodDeclaresWord` asks it, and
      `GlobalNameClassDriftTests` finds the tables by reflection and holds `Declares` to each, as it held `OoMethodDataScope.IndexesDeclare`.
    - *A type-name an entry carries is resolved from the entry.* A TYPE clause, a typed entry's type and a restricted pointer's
      `USAGE POINTER TO type-name` are held as text by the `DataItem` (slice 6's pattern): `SymbolTable.TypeDeclarationOf(DataItem,
      TypeNameCarried)` reads the text the entry carries, in the scope of the element whose entry writes it.
    - *A qualified name no parse rule holds with its qualifiers.* An index-name in a subscript is a token of the subscript's token list,
      its `OF`/`IN` qualifiers the tokens after it (§2023:8.4.2.2.2 Format 3). `WrittenQualifiedName(Head, Qualifiers)` carries the
      tokens (made from a token list by the one qualifier walk, which replaced `SubscriptBinder.QualifiersAfter` and its copy in
      `SearchAllFormat2Rules`, or from a `dataReference`'s qualifications), and `IdentifyIndexName(WrittenQualifiedName, report)`
      answers the §2023:8.4.2.2 candidate set, whose count stays `NameResolution.ResolveIndexName`'s verdict.
    - *A qualifiable name's identification is its typed candidate set (correction, review round 1).* The slice first answered an
      index-name through `Identify(WrittenQualifiedName, NameClass.Index, report)`, an `IdentifiedName` whose `Symbol` was the
      `NameCandidates<IndexDeclaration>` boxed, and the qualifier words as a fresh array at each read. Every subscript name is asked
      whether it is an index-name, several times per reference, so every compile without a listing paid two objects per ask and
      IX113A's front end and bind allocated 0.14 to 0.26 MB over its parent: over section 9's `None` budget, which names no
      identification object. A name a reference qualifies (§2023:8.4.2.2) is identified as its candidate set, and that set is a
      struct: so the class's own method answers it typed and unboxed (`SymbolTable.IdentifyIndexName`, beside `Identify`, with the
      same `report` flag, at which step 7b records the occurrence when the set has one member), and `WrittenQualifiedName` makes
      its name and qualifier words off their tokens once, when it is made (a token's text is a new string at each read), and no list for
      an unqualified name; the CAPACITY register arm, which every data reference asks first, asks the table before it reads the word. The data-names and condition-names, the
      lane's next slice, are qualifiable names too and get their own typed method the same way (`IdentifyDataName`), never the
      boxed `Identify` arm. `FlagConformancePass.FlagIndexSet` asks with the receiver's written qualifiers
      (`WrittenQualifiedName.Of(dref)`), as the binder resolves the same SET, where it had asked with none.
    - *The forest keeps the declarations, in order.* The element's own index declarations (the cells it emits, `ForestSlice.OwnIndexes`)
      and type declarations (`ForestSlice.TypeDeclarations`, and a method's `OoMethodDataScope.TypeDeclarations`) stay model lists, added
      by the table's `DeclareIndex` and `DeclareType` once they have keyed the name: an enumeration is no lookup (slice 1's files).
    - *For step 7b.* `NameResolution.ResolveIndexName` identifies with `report: !Probing`, but SET classifies its operand and then binds
      it, both committing, so the occurrence needs the once-per-written-reference gate the resolver already applies to its diagnostics;
      `ReferenceResolver.CapacityRegisterFor` takes `report` (the reference arm's own, false for the classifying probes).
  - *(Step 6b, the closing slice's first session, branch `listing-step-6b-close`.)* The data-name, condition-name and screen-name
    TABLES moved, with the questions that answer no symbol, and the baseline holds 86 (1 table, 22 lookups, 63 sites).
    `SymbolTable.DataNames.cs` owns the element's data-names (the multimap, own entries then the inherited GLOBAL items),
    condition-names and the refused SCREEN SECTION's names, their one insert each (`DeclareDataName`, `DeclareConditionName`,
    `DeclareScreenNames`), `DeclaresOwnDataName` (which was `ForestSlice`'s), the screen questions, the enumeration the §2023:8.4.2.1
    screens read (`DataNameDeclarations`), and `SelectWhenConditionsOf(DataItem)`; a method's own are `MethodNames.DataNames` and
    `ConditionNames`. `ForestSlice` and `OoMethodDataScope` hold no name table: the forest keeps the roots, the declarations in order and
    the declaring depth of each inherited item (`ForestSlice.Inherit`), and `SymbolTable.InheritGlobalItem` keys the item's names. The
    corrections the code proved:
    - *A register of written file-names keys the FILE (correction).* USE's §2023:14.9.49.3 SR8 ("The same file-name shall not appear in
      more than one USE AFTER EXCEPTION statement within the same procedure division") and SR14 (the exception-name and file pair) and
      the WHEN census of §2023:14.9.28.3 SR14 and SR15 kept the file-name's spelling under the
      name comparer, beside the `Identify` that had just answered the file. Two file-names are the same file-name when they identify the
      same file, so the registers key the `FileModel` (its reference identity) and the exception-name the catalog's spelling; no
      comparison of written names is left there.
    - *THE §2023:11.7.4 GR5 question is one member (correction).* "Does this method declare the word" was `DataBinder.MethodDeclares`, the
      OR of the model scope's half (`OoMethodDataScope.IndexesDeclare`, its data-names and condition-names) and the table's
      (`SymbolTable.MethodDeclaresWord`). With every table of a method in `MethodNames`, `MethodNames.Declares` asks them all and
      `MethodDeclaresWord` is the question; the two halves are deleted, and `GlobalNameClassDriftTests` holds the model scope to no
      table and `Declares` to each of the table's.
    - *A name the record carries is resolved from the record.* A SELECT WHEN clause's condition-name (§2023:13.18.51.1: it "specifies a
      condition-name condition under which a record description entry is associated" with a record) is text the
      record holds; `ResolveRecordFormatPass` read the condition table by it. `SelectWhenConditionsOf(DataItem record)` reads it in the
      table, as slice 6 resolved a restricted pointer's prototype from its item.
    - *An end marker's name is the table's question at every kind of unit (correction).* The OO lane made `ClassNames.EndMarkerAgrees`
      for END CLASS, END INTERFACE and END METHOD, while END PROGRAM and END FUNCTION still compared text in `EndMarkerPass`.
      `SymbolTable.EndMarkerAgrees` and `EndMarkerNamesAny` (the test that the markers of nested programs are out of order) answer all five, before any
      element's table exists, from the written contexts. The identity rules are §2023:10.7.3 SR2 and SR4 to SR9 (END PROGRAM for a
      program SR2 and a program prototype SR8, END CLASS SR4, END METHOD SR5, END INTERFACE SR6, END FUNCTION for a user function SR7
      and a function prototype SR9); the order rule is SR3; SR1 is the end marker's presence, which neither member answers (review
      R1-2 corrected a first citation of "SR1 to SR6").
    - *Which file holds a record is one member, and a method asks it of its half (correction, review R1-1).* The binder's record-name-1
      rule (`StatementValidation.ResolveRecordName`) and the flag pass's probe of the same WRITE each walked the files themselves, the
      pass over program units only, so in a FACTORY or OBJECT method `WRITE FILE FF` found the LINAGE file and `WRITE FREC` did not.
      `SymbolTable.FileWhoseRecordIs` is the one question (own files, then visible GLOBAL ones) and both ask it;
      `FilesOfContainingChain` (every file of the element and its containers, GLOBAL or not) answers the FILE STATUS role, so
      `FlagConformancePass` keeps no unit walk of its own, and a method's own USE declaratives govern its FLAG-14 d test.
    - *A pass before the bind keys its facts by the table's key.* `VersionConformancePass` remembers each level-01 record's EXTERNAL
      clause by its name for a later REDEFINES; it keys the written word's `CobolName` (`SymbolTable.WordOf`, now internal), so its
      equality is the table's, and a subscript's written word is pre-filtered against an INDEXED BY declaration by
      `SymbolTable.SpellsIndexName` before it is resolved by its declaration (kb/Work PB919).
    - *Rule (2)'s companion sees a copy (correction, review lead R2-1 of the OO lane).* It scanned only object creations, so a `with`
      expression made a new `IdentifiedName` or `OoNameResult` unseen. It refuses a `with` expression of every table-made type and a
      `default` of the name key; a `default` of an answer is the empty answer, which names nothing.
    - *What remains is the RESOLUTION of the data-names and condition-names (correction of the "closing slice" estimate).* The 22
      lookups that take the name as text, about 100 call sites, are now readers of the table's private tables; converting each to the
      typed `IdentifyDataName(WrittenQualifiedName, ...)` the data-name lane named, with the clause operands carrying their written
      words or resolved from their carrier, is the next slices' work, with the declaration sites, the census, the group tier's
      function units and the parameterized expansion (kb/Work PB5932's dated file `2026-11-19-step-6b-close` holds the table).
  - *(Step 6b, remainder slice 1, branch `listing-step-6b-r1`.)* The procedure division's data-name and condition-name references
    whose item is used are identified by the table, and the baseline holds 75 (1 table, 11 lookups, 63 sites): eleven text lookups are deleted
    (`NameResolution.FindItem`, `ResolveQualified`, `ResolveUnqualified`, `FindReportedItem(dref, string, ...)`,
    `DeclarationOf(dref, string)`, `ReferenceResolver.ResolveByName(string, ...)`, `ObjectPropertyResolver.ResolveObjectProperty(dref,
    string, ...)`, `DataBinder.QualifiedConditionCandidates`, `ConditionTier`, and the table's public `TryResolveCondition` and
    `NearestDeclaringConditions`, private now). Every procedure-division reference whose item is used (an identifier
    `ReferenceResolver` identifies, a subscript or reference-modification data-name, a condition-name, a table operand, a table(ALL)
    argument, SET ADDRESS OF, an object property's head, function-pointer-name-1 of a function-identifier, INITIALIZE's whole dynamic
    table), and the operands a procedure-division scan collects for the data division (the storage-claim heads of ADDRESS OF and BY REFERENCE, a LENGTH OF
    operand of a constant entry), hands the table the written words: `SymbolTable.IdentifyDataName(WrittenQualifiedName, report)`
    answers the §2023:8.4.2.2 candidate set (`NameCandidates<DataItem>`), and `IdentifyConditionName(WrittenQualifiedName, report)`
    the condition-names' (`List<Condition88>`); `NameResolution.DataItemNamed(WrittenQualifiedName)` is the procedure division's one
    verdict over the set, as `ResolveIndexName` is the index-name's. The corrections the code proved:
    - *A qualifiable class's typed method, not `SymbolNamed`'s arm (correction of the remainder's brief).* The remainder was written
      as "`SymbolNamed` gaining its Data and Condition arms" beside the typed `IdentifyDataName`. `SymbolNamed` is the boxed
      `Identify` arm, which answers an `IdentifiedName` around the symbol; this section's own correction (the data-name lane, review
      round 1) measured that boxing a qualifiable name's candidate set costs every compilation two objects per ask, over section 9's
      `None` budget. A data-name and a condition-name are qualifiable names whose identification IS the candidate set, so each class
      has its own typed method beside `IdentifyIndexName`, and `SymbolNamed` gains no arm for them: one rule for the three
      qualifiable classes.
    - *A reference that writes no name is no name (correction).* `WrittenQualifiedName.Of(dataReference)` answers null for a
      LINAGE-COUNTER, LINE-COUNTER or PAGE-COUNTER reference, whose `cobolWord` is its file-name or report-name QUALIFIER
      (§2023:8.4.2.2.2 Formats 7 and 8), and for a recovery node with no word. Several callers had read that qualifier as the referenced name,
      or the reference's glued text (`dref.GetText()`, kb/Work PB2040's shape): the condition binder, the SEARCH ALL screens, the
      storage-claim scan, the subscript reader, the table(ALL) argument, the flag pass and INITIALIZE's whole-dynamic-table arm
      (which the first build left reading `dref.cobolWord()?.GetText() ?? dref.GetText()`, review R1-1). Each now asks the one maker.
    - *An identification outside the resolver asks the LINKAGE screen itself (correction, review R1-1's sibling sweep).* Two
      procedure-division references reached their item without `ReferenceResolver.Identify`: INITIALIZE's whole-dynamic-table arm
      (§2023:14.9.20.4 GR10) and function-pointer-name-1 of a function-identifier (§2023:8.4.3.2.3 SR4), so `INITIALIZE L-TAB` and
      `FUNCTION L-FP (1)` over a LINKAGE record the header does not admit compiled clean (§2023:13.7.3 SR4, the PB5846 shape). The
      first asks `NameResolution.ScreenLinkageReference`, the second `ScreenLinkageFunctionPointer` (a word, not a `dataReference`),
      and `ReferenceIdentificationDriftTests` finds a binder file by every member that answers a data-name's items (`DataItemNamed`,
      `IdentifyDataName`, `QualifiedCandidates`, `TryResolveUnqualified`), not by the verdict alone (review R1-3).
    - *The two arms of a data-name keep the two answers the procedure division gave (a lead, not a change).* An unqualified
      reference names the nearest declaring element's items (`TryResolveUnqualified`); a qualified one is `DataBinder.QualifiedCandidates`,
      which also drops a set of lapsed type-declaration members alone. `IdentifyDataName` keeps both, so the move is behaviour-neutral;
      whether an unqualified reference to a type-declaration member alone should be dropped the same way is kb/Work PB5932's dated file
      `2026-11-19-step-6b-r1`'s lead, with the sum-counter funnel that still asks the qualified set for an unqualified name.
    - *Each written word is read once (correction, section 9).* The first build answered the `None` row 0.05 to 0.32 MB over its
      parent: the identification step read the head word for its arms and again to make the `WrittenQualifiedName`, and a qualified
      reference's qualifier words in `ReferenceResolver.ReadWritten` and again in the name. `WrittenQualifiedName.OfRead(dataReference,
      qualifierWords)` makes the name from the words `ReadWritten` already read, reading the head once, and the identification step reads
      the reference once before its arms; every `None` row is then below the parent's (section 9).
    - *What remains of the class: the procedure division's word probes and ambiguity counts, and the data division's operands
      (correction, review R1-1: this item first named the data division's operands alone).* The procedure division still answers a
      spelling's text in two shapes, neither of which uses the item it finds. The word-class probes ask whether a word is declared
      as a data-name at all before choosing a reading (`SymbolTable.TryResolve` with its answer discarded: `SortBinder.SortBindTable`'s
      file-or-table split, `InspectBinder`'s CONVERTING alphabet-name test, `IntrinsicBinder.KeywordOmittedFunction`, `KeywordWordOf`
      and `TypeNameArgument`, `SubscriptBinder.IsFunctionBearing`, `StatementValidation.RejectProcedureName`). The ambiguity counts
      count a spelling's data items beside a declaration of another class (`DataBinder.QualifiedCandidates` from
      `KeyedIoBinder.RecordKeyNameOf`, `CounterResolver.SumCounterFamilyFor` and `SumCounterFor`, `ReportGroupResolution`, and
      `NameResolution.ReportUnidentified`'s wording, which also asks `TryResolve`); they are the arm kb/Work PB5932's dated file
      `2026-11-19-step-6b-r1`'s lead decides. The clause operands the data division keeps as `(name, qualifiers)`
      text (FILE STATUS, RELATIVE and RECORD KEY, PADDING, LINAGE, ASSIGN USING, APPLY COMMIT, VALUE OF, OCCURS DEPENDING ON and KEY,
      REDEFINES, RENAMES, SAME AS, a report clause's operands, a BASED claim) still reach `QualifiedCandidates`, `EntryClauseCandidates`,
      `RegionClauseCandidates`, `SubtreeCandidates`, `ResolveClauseOperand`, `RecordKeyNameCandidates`, `ReportItemOperandRefused`,
      `SumCounterOperand85` and, through them, `TryResolve`, `TryResolveUnqualified` and `NearestDeclaringData`: the remainder's
      part B, then the declarations (C) and the entry ids (D).
  - *(Step 6b, remainder slice 2, branch `listing-step-6b-r2`; kb/Work PB5932's dated file `2026-11-19-step-6b-r2`.)* Part B's
    first half: the data division's FILE clause operands, the record-key-names and two comparisons read off their carriers, and the
    baseline holds 63 (1 table, 7 lookups, 55 sites). As built, with the corrections the code proved:
    - *A clause operand keeps a `WrittenDataName`, not a `WrittenQualifiedName` (correction of the remainder's brief).* The brief
      said "the clause operands keep a `WrittenQualifiedName` instead of (name, qualifiers)". A clause operand is resolved after the
      data division is declared, from the MODEL (`FileModel`, `OccursSpec`, ...), which holds no parse tree: no model type references
      a parser type, and a published entry decoded from a repository (`RepositoryCodec`) has no tokens at all. So the model keeps the
      operand as a `Binding.Model.WrittenDataName(Name, Qualifiers)`, the written words of data-name-n innermost qualifier first, one
      value where every clause kept a `(name, qualifiers)` pair; it is made by the one capture of a data-name-n operand
      (`DataBinder.ClauseDataName`, `ClauseDataNameSubscripted`, which now answer it) and by the codec, and only the table resolves it:
      `SymbolTable.IdentifyDataName(WrittenDataName, Scope)` beside the procedure division's `IdentifyDataName(WrittenQualifiedName,
      report)`. The clause's own position is the model's cursor beside the operand (`FileStatusAt`, `DependingAt`, ...), where step
      7b records the occurrence. `NameTableOwnershipDriftTests` treats `WrittenDataName` as carrying no symbol (the shape's opaque
      types, beside `SymbolId`), so a member that takes or makes one is no lookup by that fact alone.
    - *What moved.* FILE STATUS, RELATIVE KEY, RECORD KEY and ALTERNATE RECORD KEY (data-name-1 and each SOURCE data-name-2), the
      key-level COLLATING SEQUENCE operands, ASSIGN USING, PADDING CHARACTER, each LINAGE operand, RECORD VARYING DEPENDING ON, APPLY
      COMMIT identifier-1, VALUE OF, and OCCURS DEPENDING ON (the data division's `OccursSpec` and the report section's
      `ReportOccursSpec`) keep a `WrittenDataName`; `DataBinder.ResolveClauseOperand`, `ReportSectionBinder.ReportItemOperandRefused`
      and `SymbolTable.SumCounterOperand85` take it (each keeps the diagnostic's words as a string, classified in
      `string-parameter-members.json` with that reason). The record-key-names (§2023:8.3.2.2.24, scoped by the file control entry,
      §2023:8.4.6.2.4) are read off the file that declares them: `SymbolTable.RecordKeyNamesOf(FileModel, WrittenDataName)` and
      `DeclaresUnresolvedKeyNamed(FileModel, WrittenDataName)` replace `FileModel.RecordKeyNameCandidates` and its four comparisons;
      `ContainerLocalFileSpelled` answers APPLY COMMIT's containing-element file (§2023:12.4.6.3.3 SR1), and `FormalSpelled` the
      formal an OMITTED condition's data-name-1 spells (§2023:8.8.4.8.3 SR1), each comparison now the table's.
    - *What remains of part B.* The data-entry clause operands still kept as text (OCCURS KEY, REDEFINES, RENAMES, SAME AS, a
      DYNAMIC CAPACITY IN name) and the lookups that read them (`DataBinder.QualifiedCandidates`, `EntryClauseCandidates`,
      `RegionClauseCandidates`, `SubtreeCandidates`, `QualifierWalk`; `SymbolTable.TryResolve`, `TryResolveUnqualified`,
      `NearestDeclaringData`); `IdentifyDataName(WrittenDataName, Scope)` still asks `QualifiedCandidates` until those move into the
      table, private. The procedure division's word probes and ambiguity counts (section "Step 6b, remainder slice 1", "What remains
      of the class") are part B's second half with them.
  - *(Step 6b, remainder slice 3, branch `listing-step-6b-r3`; kb/Work PB5932's dated file `2026-11-20-step-6b-r3`.)* Part B's
    second half, so part B is built: no lookup takes a data-name's text, and the baseline holds 52 (1 table, 0 lookups, 51 sites).
    As built, with the corrections the code proved:
    - *The candidate sets are the table's, private (`SymbolTable.DataNameCandidates.cs`).* `DataBinder.QualifiedCandidates`,
      `SubtreeCandidates`, `EntryClauseCandidates`, `RegionClauseCandidates`, the qualifier chain matchers and the one §2023:8.4.2.2.3
      SR4 walk (`QualifierWalk`, whose comparison is now the table's) moved into the table; `TryResolve`, `TryResolveUnqualified` and
      `NearestDeclaringData` are private. A caller hands the written words: the procedure division a `WrittenQualifiedName`
      (`IdentifyDataName(WrittenQualifiedName, report)` and its scoped overload for the storage-claim scan, `IdentifyWithin` for a
      SORT table key, §2023:14.9.40.3 SR14 a)), a data division clause the `WrittenDataName` its model keeps
      (`IdentifyDataName(WrittenDataName, Scope)`, `IdentifyEntryClauseOperand` for OCCURS DEPENDING ON and SAME AS with
      §2023:8.4.2.2.1 SR5's implicit qualifiers, `IdentifyRegionOperand` for OCCURS KEY and RENAMES). The questions that answer no
      symbol take the words as text: `DeclaresDataName` (the procedure division's word-class probes: INSPECT CONVERTING's
      alphabet-name test, the keyword-omitted function, a keyword word or type-name argument, the subscript's function test, SORT's
      file-or-table split, the procedure-name diagnostic), `DataNameDeclarationCounts` (the counts the unidentified-reference wording
      quotes) and `QualifierChainMatches` (a candidate already in hand: the CAPACITY register's qualifier test).
    - *One rule for the unqualified and the qualified data-name (remainder slice 1's lead, decided).* An unqualified reference was
      `TryResolveUnqualified`'s set and a qualified one `QualifiedCandidates`', which also drops a set of lapsed type-declaration
      members alone (§2023:13.18.58.4 GR1) and a repeated candidate. They were one answer already: with no qualifier the walk keeps
      every candidate, the table's list for a spelling is never lapsed members alone (it holds the spelling's storage definitions,
      never empty, and the members that lapse are added beside them, `WithLapsedTypeMembers`), and the multimap holds each item once.
      So `DataNameCandidates` is both arms; an unqualified reference reads the table's list without a copy (section 9), and the sum
      counter's count of the ordinary items of its spelling (`CounterResolver`), which asked the qualified set for an unqualified
      name, asks the same method as the ordinary lookup. `compare_oracle.py` IDENTICAL over the corpus, `--permissive` and TYPEDEF
      cases included.
    - *The data-entry and report clause operands keep a `WrittenDataName` (as remainder slice 2's file operands do).* OCCURS KEY
      (`OccursKey.Operand`), RENAMES (`RenamesInfo.FromOperand`, `ThruOperand`), SAME AS (`DataItem.SameAs`), the report section's
      CONTROL operand (`ReportControlRef.Operand`), SUM addend (`ReportSumAddend.Operand`), CODE identifier and SOURCE screen;
      `DataBinder.KeyReference`, the one capture of a written data-name-n, answers it.
    - *A name a carrier holds is resolved from the carrier.* A REDEFINES clause's data-name-2 is the redefiner's own
      (`DataItem.RedefinesTargetName`), so `SymbolTable.RedefinitionCandidatesOf(redefiner, scope)` answers the preceding entries so
      named (an entry of the subject's level-number that the subject follows, §2023:13.18.44.3 SR2 and SR10; "Data-name-2
      shall not be qualified", SR6, whose NOTE 1 makes the placement the answer) and `ResolveRedefinesPass` compares nothing; the layout's overlay start
      (`BitLayout.Walk`) follows the resolved `RedefinesTarget`, not the written name matched again among the siblings.
    - *The link is made before any item is measured (correction, review R1-2).* Reading the overlay off `RedefinesTarget` made the
      layout depend on when the REDEFINES pass sets it, and `ResolveRedefinesPass` ran after `CheckStorageLimitsPass`, which measures
      every item (`RecordLayout.ExceedsSizeLimit` through `BitLayout.ExactExtentBits`, whose phase of a redefining bit group is its
      target's): the name match had answered there, the link had not yet been made. The link is now a pass of its own,
      `LinkRedefinitionsPass`, placed before the size limit (it measures nothing); the entry rules, SR8's size screen among them, stay
      in `ResolveRedefinesPass` after it (kb/Work PB5695 review R1-1), and both read the one scope, `RedefinitionScopeOf`.
    - *Link, then clear, one item at a time (correction, review round 2 R2-1).* Round 1's split left the report and the clearing of
      a clause whose data-name-2 is found nowhere in `ResolveRedefinesPass`, after every item was linked. `DataItem.RedefinedEntry`
      picks the nearest preceding same-named entry that is no redefinition by reading each candidate's written data-name-2, so a
      later redefiner was linked while an earlier unresolved one still carried its name: in `05 A. 05 A REDEFINES ZZ. 05 C
      REDEFINES A.` C was linked to the first A and SR10 then reported the cleared second A as new storage between them, the
      half-state kb/Work PB93 forbids. `LinkRedefinitionsPass` now reports the unfound data-name-2 (COBOL1577 or COBOL1654) and
      clears the clause before it links the next item, as the one loop did before the split; `ResolveRedefinesPass` screens linked
      pairs only, so no pass after the link sees an unlinked redefiner. `RedefinesTargetTests` pins the case at 85 and 2023.
    - *The CAPACITY IN data-name is no operand (correction of remainder slice 2's list).* Slice 2 listed "a DYNAMIC CAPACITY IN
      name" among the operands kept as text. `CAPACITY IN data-name-3` DECLARES the register (§2023:13.18.38.3 SR30, "Data-name-3
      shall not be defined elsewhere in the source element"), which the table already enters as a CAPACITY register; it names
      nothing to resolve.
    - *What remains: part C and part D.* 1 table and 51 sites: the declarations (`DataBinder._dataEntryLocations` and its
      comparer, `BindDeclarations`, `IsBeingDescribed`, the screen index-name uniqueness and the two report sets; `LinkageBinder` (6),
      `OoDataBinder` (4), `CorrespondingBinder` (2), `FileControlBinder` (4), `CommunicationSectionBinder` (1)), the §2023:8.3.2.2
      census (`UserWordCensus`, 4), the group tier's program and function units (`BinderDriver` (3),
      `GroupRepositoryProvider.FunctionsByWord`), the parameterized expansion (`OoExpansion`, 18) and `TurnState.Fold`; then D, the
      data-names' and condition-names' entries made where they are declared.
  - *(Step 6b, remainder slice 4, branch `listing-step-6b-r4`; kb/Work PB5932's dated file `2026-11-20-step-6b-r4`.)* Part C's
    first half, the declarations of the data division's binders: the baseline holds 24 (0 tables, 0 lookups, 24 sites), so no table
    keyed by a COBOL name exists outside the table. As built, with the corrections the code proved:
    - *The entry order is the table's: a name to the places its entries stand.* `DataBinder._dataEntryLocations` was the last rule (1)
      table. A constant's length phrase that names an item described after it needs the entries that declare the name (kb/Work
      PB1231): `SymbolTable.DeclareDataEntry(name, place)` and `DataEntriesDeclaring(word)` answer each entry's `DataEntryPlace` (its
      section run, its index there, its section: a place in the source, never a symbol, so `NameTableOwnershipDriftTests` names it
      among the shape's opaque types beside `SymbolId` and `WrittenDataName`). (Correction, measured:) a first build kept the names in
      the table and each entry's place in a second, entry-keyed dictionary of the binder, and read each entry's data-name twice; every
      data description entry of every compilation paid both, and IX113A's 1544 entries put its front end and bind 0.15 to 0.26 MB over
      its parent, over section 9's `None` budget, so the table keeps the place itself and the name is read once. "Is this name's
      description open" is the declaration question `Declarations.EntryDeclares(entry, word)`.
    - *The §2023:8.3.2.2 census is a table of the element's names, so it is nested in the table.* `SymbolTable.UserWordCensus`, keyed by
      `CobolName`, made per element as before (`OoClassTable.CensusOf`); the declaration funnel's once-per-word refusals (a letterless
      word, a REPOSITORY intrinsic, §2023:8.3.2.1 rule 5) are `SymbolTable.FirstRefusal`, one per binder as before; a specifier that
      names this unit is `SymbolTable.NamesOwnUnit`, read off the program entry `DeclareProgram` makes (§2023:12.3.8.3 SR11, SR15).
    - *A declaration question of the data division's own entries is the table's.* The screen-names' distinct first spellings
      (`DeclareScreenNames` answers them), the 1985 index-name uniqueness (`SymbolTable.RepeatedIndexNames`, §1985:VI.5.8.3 SR14), a
      file-name described twice (`DeclareFileDescriptions`, `DescribedEarlier`), a SAME clause's distinct file-names
      (`DistinctFileNamesWritten`), a CD entry's data-names (`CdEntryNamesElsewhere`, §1985:XIV.2.2.3 SR6), a PROPERTY subject that
      needs qualification (`NeedsQualificationAmong`, §2023:13.18.42.3 SR3).
    - *A name read before the element's tables are complete is resolved among the records handed in.* The procedure division header
      binds inside the data division's bind, so its USING and RETURNING operands are identified among the element's LINKAGE records
      (`SymbolTable.IdentifyHeaderOperand`, §2023:14.2.2 SR1), the formals an ADDRESS OF names are an `AddressedNames` set, and a linkage
      REDEFINES is read off the redefiner's own written word among the records (`RedefinedAmong`, `IsRedefinedAmong`: data-name-2 is never
      qualified, §2023:13.18.44.3 SR6, and shares the subject's level-number, SR2), before
      `LinkRedefinitionsPass` links it; a DATA RECORDS data-name is identified among its file description's level-01 records
      (`IdentifyDataRecord`, §1985:VII.3.5.3 SR1). Step 7b records each reference's occurrence there.
    - *CORRESPONDING pairs by the table's key.* Two identified groups' subordinates are paired by their declared names
      (`SymbolTable.CorrespondingMembers<T>`, `NamesakeCounts`, §2023:14.7.6); no written name is resolved.
    - *The externalized-name uniqueness is EXTERNAL (classified, not moved).* `LinkageBinder.CheckExternalizedNameUniqueness` compares
      the externalized names of EXTERNAL subjects as the run-unit store keys them (§2023:13.18.22.3 SR2): identity across the run unit.
    - *What remains: 24 sites.* The compilation group's units (`BinderDriver.CheckFunctionUnits` (2), `CheckDefinitionNameUniqueness`,
      `GroupRepositoryProvider.FunctionsByWord`), the parameterized expansion (`OoExpansion`, 18), a TURN directive's file operand
      (`TurnState.Fold`), the refused record-key operands (`FileControlBinder.ResolveFiles`, a set of operand names the key screen skips);
      then D, the data-names' and condition-names' entries made where they are declared.
  - *(Step 6b, remainder slice 5, branch `listing-step-6b-r5`, the last slice; kb/Work PB5932's dated file `2026-11-20-step-6b-r5`.)*
    The rest of part C and part D: the baseline is empty, so the slice deleted it (the file, `BaselineCeiling` and the code that read
    it), and `NameTableOwnershipDriftTests` is the five rules again, green with no exceptions. As built, with the corrections the code
    proved:
    - *The group's FUNCTION-ID units by word are the group tier's, `SymbolTable.UnitNames`.* A REPOSITORY `FUNCTION word` entry of
      any element that follows a function definition names it by its user-function-name (§2023:8.4.6.7), so the words are the
      group's. `UnitNames` keys the group's function units by word and answers the same-word screen
      (`EarlierOfSameWord`: COBOL1508, a definition compared only with definitions and a prototype with prototypes, §2023:10.6.2 SR3)
      and the provider's question which units carry the word (`FunctionsNamed`, the same keyed table; review round 1 lead R1-2: the
      slice first built it as a per-unit equality, `NamesFunction`, which the provider applied in a linear scan, a second mechanism
      beside the table that a change to how a unit's word is keyed would have had to follow). (Correction:) "the group tier gains the group's
      program and function units by word" said too much: the group's PROGRAM units are searched by their externalized names
      (§2023:12.3.8.4 GR10), the operating environment's names, so they are no table of COBOL words; and the word reaches the group
      through the repository's providers as text (`IRepositoryProvider.FunctionsByWord`, classified with the external repository's
      provider, whose answers are another compilation's records), so the tier's keyed answer to it takes the string and is classified
      with the same reason (rule (3); `string-parameter-members.json`, ceiling 128 -> 129). The OO pair of
      `CheckDefinitionNameUniqueness` (two class or interface definitions of one word, which §2023:8.4.6.4's uniqueness refuses and
      the tier reports as COBOL0820) is no comparison at all (correction, review round 1): the tier admits one definition per word
      (a repeated CLASS-ID or INTERFACE-ID, or a class named like an interface, is reported and not added), so `oo.Classes` and
      `oo.Interfaces` never hold such a pair and the skip that compared their words could not be reached. It and its equality
      (`ClassNames.DeclareOneWord`, a static equality of two arbitrary strings) are deleted, with the declared word the check carried
      only for it (`ExternalizedSubject.Word`).
    - *The parameterized expansion's names are the group's tier, a table of their own (the open tier question, decided).* A
      parameterized definition and each expansion a REPOSITORY specifier asks for are class and interface definitions of the GROUP:
      they share the group's one namespace of object-class-names and interface-names (§2023:8.4.6.4), and an expansion's name is its
      identity in the group (§2023:9.3.12). The tier cannot be made first: `ClassNames` is built from the definitions the expansion
      produces. So the expansion's names are `SymbolTable.ExpansionNames`, made by `OoExpansion.Expand`, handed to `OoClassTable` in
      the expansion's result (`OoExpansion.Result.Names`; `ParameterizedNames` and `ExpandedNames`, two string sets under the name
      comparer, are deleted) and asked thereafter only whether a word names a parameterized definition (§2023:12.3.8.4 GR1) or one
      some expansion re-binds. A skeleton is looked up by the written word's context (an EXPANDS target, a specifier's declared
      name), never by a string. The words of one definition, a formal parameter-name (scoped to its definition, §2023:11.3.4 GR6) and
      a specifier's declared name (scoped to its REPOSITORY paragraph), are parse-tree questions asked before any element's table
      exists, answered with the table's one equality as `EndMarkerAgrees` is: `SpecifierDeclaring`, `RepeatedFormals`, `IsFormal`,
      `Substitution` (the formal-to-actual replacement of §2023:12.3.8.4 GR5 and GR8); the end marker is renamed with the header
      when `EndMarkerAgrees`.
    - *A `>>TURN` directive's file-name is matched against the statement's IDENTIFIED file.* The fold's query names the
      `FileModel` the statement references (§2023:7.3.25.4 GR6, GR8: "checking is enabled only for exception conditions associated
      with that file-name"), never its name; the directive stands outside every source element, so its written word is matched by
      the table's equality (`SymbolTable.DirectiveNamesFile`), which reads the declared name off the `FileModel` itself, and the GR14
      implicit enable of an exception-checking PERFORM (§2023:14.9.28.4 GR14) carries the file its WHEN phrase identified, matched by
      identity.
    - *A free-standing equality of the table is the table's in use, not only in name (correction, review round 1 lead R1-2).* A
      static member of `SymbolTable` that answers `bool` and takes a `string` (`DirectiveNamesFile`, `SpellsIndexName`, `IsFormal`) is
      callable from anywhere, so rule (5) as first written, which saw only `CobolNames.Same`, `CobolNames.Comparer` and `==` on a name
      property, was green on a caller that made such an equality its own comparison. Rule (5) now also reports, outside the table, a
      call of one that is handed a binder symbol's name property for a `string` (TABLEARG: two names the caller chose) or a lambda's
      parameter or a `foreach` variable for a binder-typed parameter (TABLESEARCH: a per-symbol equality applied to each symbol of a
      collection, a lookup without a table); the planted self-test shows each kind red and the same equality asked of one symbol the
      caller holds green. An instance member is the table's own question over its own state (a declaration, a keyed lookup) and is
      neither. The sweep the two kinds made: `ClassNames.IsAccessorOf(method, property)`, applied method by method in four searches of
      `OoClassTable` and `OoDataBinder`, is `ClassNames.AccessorAmong(methods, property)`, the search inside the table; and
      `SpellsIndexName` takes the `IndexDeclaration`, not its name, so no caller hands it a declared name of its own choosing.
    - *A refused record-key operand is marked on itself, never collected by name.* `FileControlBinder.ResolveFiles` kept a set of
      the NAMES of the key operands it refused, which the key screen skipped; two operands that share a spelling were skipped
      together. Each carrier now says it was refused (`AlternateKeyClause.Refused`, `KeySourceOperand.Refused`,
      `FileModel.RecordKeyRefused`, `FileKeyOperand.Refused`), so the screen skips exactly the operand refused (kb/Work PB481, PB978).
      On the population the oracle is identical, so no program had two such operands; but it IS a user-visible change (correction,
      review round 1 R1-1): a key clause whose operand shares a refused operand's spelling now hears its own clause's rule, e.g.
      `RECORD KEY IS K1` (ambiguous, COBOL1639) beside `ALTERNATE RECORD KEY IS K1 OF NOSUCH` reports §2023:12.4.5.6.3 SR2 (COBOL0863)
      on the alternate operand, which the name set had silenced.
      `FileControlKeyRuleSpecTests.ARefusedKeyOperand_SilencesOnlyItself_NotAnotherOperandOfItsSpelling` pins both orders at every
      edition; with the screen skipping every unresolved operand it is red.
    - *Every data-name's and condition-name's entry is made where it is declared (part D).* `SymbolTable.EnterDataItem` and
      `EnterConditionName` enter each data description entry (FILLER included, a type declaration's subordinate entries, each
      level-66 entry), each entry a TYPE or SAME AS clause copies in (on the line of the entry that wrote the clause, §2023:13.18.57.4
      GR1), each level-88 and each copy of one, and the entries the standard defines implicitly (a CD's record area, §1985:XIV.2.2;
      an FD's implied record, §2023:14.9.30.4 GR6) at the position the record places it; a type declaration's root is its type-name's
      entry already. `DataEntryOf(candidate)` answers a data-name's or condition-name's entry for the occurrence step 7b records, as
      `IndexEntryOf` does. The finish requires the bind's entry for both classes (`SymbolTableFinish.DeclareEntered`): it only places
      them, in the order a record is written. The one declaration the finish still declares itself is a CAPACITY register that
      §2023:13.18.38.3 SR30 refused, which binds with an error.
    - *Proved.* `compare_oracle.py` IDENTICAL on the 22651 cases before D, and again with D and the symbol table finished for EVERY
      compilation (the listing's two gates forced on for the run, then restored): no data description entry of the population
      lacks its entry or enters twice. `SymbolEntryDriftTests` gains a data-name's, a condition-name's and a TYPE copy's reference
      answering its declaration's entry; with one declaration's `EnterConditionName` removed the planted programs fail ("the Condition
      'N-ZERO' entered no entry"), and with `TurnState`'s comparison put back rule (5) is red. The finish lists only the entries it
      writes itself, never a record's (`SymbolTableFinish.PlacedByARecord`: the measured `All` row, section 9), and the text and JSON
      listings of the four benchmark programs with every section are identical to the parent's but for the output path and the instant.
  - *(Step 6b, the entry ids, branch `listing-step-6b-ids`, a lane beside the data-name lane; kb/Work PB5932's dated file
    `2026-11-19-step-6b-entry-ids`.)* Every class whose declaration is the table's makes its ENTRY where the binder declares the name,
    and every identification hands the entry back, so step 7b joins an occurrence to its entry by id for every class, never by the
    answer (kb/Work PB5925's dated file `2026-11-19-step-7b-not-buildable-yet`, finding 2). The baseline is unchanged (this lane moves
    no table, lookup or comparison). As built, with the corrections the code proved:
    - *One store of entries per compilation group, `SymbolTable.EntryStore`.* `Enter(element, NameClass, name, at, symbol)` returns a
      `SymbolId` (an opaque 1-based number, `default` none) and records the entry: the table that made it (null for the group tier),
      its class, its name as written, where it stands and the declaration's symbol. Every element's table and the group tier enter
      into the one store the group's `OoClassTable.Entries` holds (the one object every binder of the group reaches, through
      `DataBinder.OoClasses`), so an id is unique in the group; a binder outside every group (a unit test's, OoExpansion's
      parameter-name screen) is a group of its own.
    - *The id stands beside the symbol in ONE place for every class (correction: "the class's table keeps the id beside the symbol").*
      A table per class carrying its ids would copy them down every inheritance path, and three paths hand a contained element its
      container's BARE symbol (`InheritGlobalItem`'s index-names and CAPACITY registers, `InheritFiles`, `InheritReport`), which the
      container's id would have to follow. So the store keys each entry by its declaration's symbol (reference identity), and an
      identification answers `EntryOf(symbol)`: a name an element inherits holds the container's symbol and answers the container's
      entry with nothing copied, and the next class to move (the data-names and condition-names) needs no new shape: its declaration
      enters `(NameClass.Data, name, at, item)` and its identification answers the item's entry. A class whose answer is no object of
      its own declaration enters a declaration object its table keeps: a configuration-section value (`AlphabetDef.Native` and an
      implementor-name row are shared values) is held as a `NameDeclaration<T>`, a paragraph-name as the `ParagraphDefinition` the
      table now makes once per declaration (it was made per lookup), a DEBUG-ITEM register name per member (a re-activation that
      widens DEBUG-CONTENTS makes a new item under the same name, §1985:XV.3.2.4 GR10), a VARYING counter by its declaring word (a
      counter model is made per enclosing occurrence). An object the binder makes LATER for an entered name answers the same entry
      (`EntryStore.Answer`): a constant's bound `ConstantDef`, a `ProgramPrototype` a specifier or a containing definition resolved to.
    - *Where each class enters, at the position the listing places it.* A procedure-name per declaration at the header's word
      (`DeclareParagraph`, `DeclareSection` take the header); the nine CONFIGURATION SECTION classes at the declaring word (each
      declaration takes the word's token, never its text); a constant entry at its level-number (FILLER included: the data map lists
      it; only a constant-name joins the names); a file at its file control entry (`FileModel.EntryAt`); a CD at its entry; a report at
      its RD and each report group description entry, named or FILLER, at its level-number, as a `NameClass.Data` entry (a reference to its
      data-name competes with the ordinary data-names for uniqueness, §2023:8.4.2.2.3 SR1, kb/Work PB5775), before the report section binds (a constant's length phrase names one,
      §2023:13.10.3 SR11); a program-prototype-name and a function-prototype-name at the specifier's word, the containing definition's
      name answering that program's own entry, which `DeclareProgram` makes for every unit before its data binds; an index-name and a
      CAPACITY register on their table's line and a type declaration as its type-name (section 6.6's placements); an object-class-name
      and interface-name at its CLASS-ID or INTERFACE-ID word, an imported one and the standard class BASE at no position when the group
      first names it, a parameter-name of a definition bound as itself at its word. The DEBUG-ITEM registers stand at no position (the
      standard declares them implicitly).
    - *Every identification carries its entry, and one without cannot be made.* `IdentifiedName.Id`; a candidate set
      (`ProcedureCandidates`, `ReportCandidates<T>`, `CapacityRegisterCandidates`) carries each candidate's entry
      (`ICandidateEntries.Ids`) and names none of its own; `IdentifiedName`'s constructor refuses an answer whose entry is missing, and
      `ClassNames.Lookup` sets `OoNameResult.Entry` and refuses the same, so a declaring path that enters nothing fails at its first
      reference in every compilation. `IdentifyIndexName` stays typed and unboxed (section 9): `SymbolTable.IndexEntryOf(candidate)`
      answers the entry of a set's one candidate when step 7b records it, and fails loudly for a candidate whose declaration entered
      nothing. A member of a type declaration that a TYPE clause's copy lapses to (§2023:8.4.2.2.1 SR4, made per lookup) is such a
      candidate: a type declaration's tables register no index-names (`DataBinder` declares no name of a TYPEDEF template), so it has
      no entry to answer, and it joins a set only beside a declared survivor, so it is never a set of one (review round 1 R1-1
      corrected an earlier sentence here that had it answer its table's index-name entry, which no table holds).
    - *The finish reads the entries the bind made (`SymbolTableFinish.Of(..., SymbolTable.EntryStore, ...)`).* Each element's entries are
      written with the bind's class, name and position (`SymbolTableWriter.Declare(SymbolId, ...)`); the constant entries are read off
      the bind (`ConstantEntryBinder.DeclaredConstants` is deleted), the procedure-names, configuration-section names, prototype-names,
      DEBUG-ITEM registers, VARYING counters and the group tier's names are entries of the finished table for the first time (with
      `NameAttributes`, which no view of the listing reads yet, so the data map and the program map are unchanged). The data
      description entries' DATA-NAMES and CONDITION-NAMES are still declared by the finish from the frozen forest, in the order a
      record is written, until the data-name lane enters them (step 6b's remainder slice 5: they enter where they are declared, and
      the finish only places them; section "Step 6b, remainder slice 5"); every other entry of a record (an index-name, a CAPACITY register, a
      type-name) is the bind's, placed where the record places it, and a drift between the two positions fails loudly
      (`SymbolTableFinish.DeclareEntered`), which is also where a data-name's entry is adopted the day its declaration makes one. A
      type declaration's entry is its type-name's, so the data-name lane's declaration of a typedef root must answer that entry, not
      enter a second (`EntryStore.Enter` refuses a symbol entered twice).
    - *What enters no entry.* A REPOSITORY program-specifier whose literal-3 was refused (`RefuseProgramSpecifier`) declares a word that
      answers no reference (no prototype is registered) and binds with an error, so the completeness oracle of step 7b, which binds the
      programs that bind clean, never meets it; a CAPACITY register SR30 refused (COBOL1523) likewise.
    - *Guards.* `SymbolEntryDriftTests` (Unit): an identification without an entry is refused, a candidate set with a candidate without
      one is refused; four planted programs (2023, 1985, OO, and a 2023 report whose one VARYING clause declares two counters, the only
      witness of `NameClass.ReportVarying`) declare a name of every class the table enters and reference each, and the finished table
      holds each with the class, name and line and column written from the source text; a class of `NameClass` with no witness is red,
      except the four classes a reference's position admits whose entry is a report group description entry's. Every identification
      answers the RIGHT entry: each planted reference is identified through the table's own funnels (`Identify`, `IdentifyIndexName`
      with `IndexEntryOf`, `ClassNames.Lookup`) and its id must name the entry of the declaration the rules resolve it to, by class,
      name and position: the configuration-section answers, a procedure-name and a candidate set's entries in its order, a
      function-prototype-name that goes to its containing function definition (§2023:12.3.8.3 SR11), a CAPACITY register, an
      index-name and a TYPE clause copy's index-name, a report group, a VARYING counter beside another of its clause, and the group
      tier's interface, BASE and parameter-name; a lapsed type member's `IndexEntryOf` is refused. (Shown red, review round 1 R1-2:
      `FunctionEntryOf` answering the specifier's word, the candidate ids reversed, and `VaryingCounterEntry` answering the clause's
      first word each fail it.)
- **Cost.** The table's entries exist for every compilation (they are the binder's one store of names). What only a
  listing needs is gated: `EditionContext.RecordsSymbols` (data map or cross-reference requested) gates the attribute
  records of section 6.6, and `EditionContext.RecordsReferences` (cross-reference requested) gates the occurrences
  other than `Defined` and the `with { }` copy, so a compilation without a listing allocates nothing more.
- **Guards (`tests/WiseOwl.Cobol.Tests.Conformance/Listing/ReferenceOccurrenceDriftTests.cs`).**
  - *Name slots come from the grammar, the one source of truth (R3-2, R4-3).* A SLOT is an element where the
    programmer writes a name: a reference to `cobolWord` or `reservedGatedWord`, a reference to a NAME RULE, or a
    token that is an alternative of such an element in the same block (the `INTEGERLIT` of `procedureName`: a
    paragraph-name or section-name may be written as digits). A NAME RULE is a rule each of whose
    paths through it crosses exactly one slot and nothing else: `fileName`, `className`, `reportName`, `cdName`, ...
    The set of name rules is the CLOSURE computed from the ATN, starting from `cobolWord` and `reservedGatedWord`:
    a rule joins it when every path from its start state to its stop state crosses exactly one transition into the set
    (or one alternative-of-a-slot token). A rule that holds slots and other elements is a REFERENCE RULE, classified
    inside: `procedureName` is one (`(refHead1=cobolWord | refHead2=INTEGERLIT | refHead3=reservedGatedWord)
    ((OF | IN) (qualSec1=cobolWord | qualSec2=INTEGERLIT | qualSec3=reservedGatedWord))?`), and so is
    `dataReference`. Every slot carries an ANTLR element label whose PREFIX classifies it: `ref` (a reference: one
    occurrence), `qual` (a qualifier, part of the enclosing reference's occurrence), `def` (a definition: one `Defined`
    entry), `nn` (a word that names nothing the program declares: an implementor-name, a function-name, a
    compiler-directive word), or `name` inside a name rule's own body (`fileName : name1=cobolWord ;`), which defers the
    class to each USE of the rule (`READ refFile1=fileName`, the FD's `defFile1=fileName`). **A label on the transition
    into a REFERENCE RULE classifies that occurrence's head slot (R5-2):** `sectionName : defSec1=procedureName ;` and
    `paragraphName : defPara1=procedureName ;` make the header's word a `def` (one `Defined` entry), while `PERFORM
    refProc1=procedureName` makes it a `ref`; the use-site prefix overrides the head slot's inner `ref` label, and the
    inner `qual` labels stay qualifiers. A transition into a reference rule with no classifying label is red in
    `GrammarNameSlotDriftTests` like an unlabelled slot, so a header can never be read as a reference. **Every label is
    unique within its rule**: the prefix is followed by a name and an ordinal, so no two elements share one. The labels add
    context fields and change no parse. (Step 7a:) the counter found 452 slots in 269 rules (not "the 257 `cobolWord`
    occurrences": the slots are also the `reservedGatedWord`s, the transitions into name rules and reference rules, and the
    alternative-of-a-slot tokens); they are labelled 281 `ref`, 73 `nn`, 64 `def`, 25 `name` and 9 `qual`.
  - *`GrammarNameSlotDriftTests` (Unit) counts per ELEMENT OCCURRENCE, so a reused label cannot hide a slot.* It
    deserializes the generated parser's ATN (`CobolParserCore._serializedATN` through the ANTLR runtime's
    `ATNDeserializer`), computes the name-rule closure above, and walks each rule's ATN alternative by alternative,
    counting every slot transition (rule or token) it crosses, and every transition into a reference rule (R5-2): N slots
    in rule R. It then reads R's generated context
    class and counts the DISTINCT label fields with a classifying prefix: M. ANTLR generates ONE field for a label name
    however many elements carry it (`ref1=cobolWord | ref1=cobolWord` is one field), so a label reused across two
    alternatives, or a slot with no label, makes M < N, and the test is red naming the rule and the slot's ATN state.
    The test also refuses a classified field that holds no slot (M > N). Nothing lists a slot or a name rule.
    (Correction, listing step 7 review lead R1-1, made in step 6b slice 7:) comparing M with N alone let a classified label left
    on an element that is no slot hide an unlabelled slot of the same rule (the counts agree). The test compares the two ELEMENT
    BY ELEMENT: each slot keyed by what it crosses into (the rule, or a token), each classified field by what its generated type
    holds (the rule whose context it is, or a token for `IToken`), and the two multisets must be equal; the planted grammar's
    seventh rule, `misplaced`, is that case.
  - *The planted failure.* `tests/WiseOwl.Cobol.Tests.Unit/Listing/Fixtures/NameSlotFixture.g4` is a small grammar
    generated by the same ANTLR step that generates `CobolParserCore` (`src/WiseOwl.Cobol.Frontend/Invoke-Antlr4CSharp.ps1`)
    with seven rules: one fully labelled (expected clean), one with a label REUSED across two alternatives, one with an
    unlabelled `cobolWord`, one name rule with an unlabelled `INTEGERLIT` alternative, one header rule whose
    transition into a reference rule carries no `def` label (R5-2), one whose label is not of the label's form, and one with a
    classified label on an element that is no slot beside an unlabelled slot (step 6b slice 7). The self-test runs the same
    counter over the fixture's ATN and context classes and asserts it reports exactly the last six rules, each at the right slot:
    the check is shown able to fail on each way a slot can hide.
  - *As built (step 7a), with the corrections the code proved.* `GrammarNameSlots` (Unit, `Listing/`) is the counter and
    `GrammarNameSlotDriftTests` its three facts: the COBOL parser clean, the closure and reference rules the ATN computes
    (`fileName` and `programName` name rules, `procedureName` and `dataReference` reference rules, the headers neither), and
    the fixture. The branch's first commit is the test red on the unlabelled grammar (269 rules), its witness that the check
    can fail on the real tree.
    - **A reference rule is defined by its ATN, not by "holds slots and other elements"** (correction): read literally that
      made every statement rule a reference rule, so `statement -> moveStatement` would need a label. A REFERENCE RULE is a
      rule, not a name rule, some path of which BEGINS with a direct slot (a transition into a name rule or an
      alternative-of-a-slot token): its head. That is `procedureName` (every path) and `dataReference` (its `cobolWord`
      alternative; the LINAGE-COUNTER, LINE-COUNTER and PAGE-COUNTER alternatives begin with the register's token, which
      is no slot: a register reference is covered by the planted programs, below), and 36 rules in all (beside 21 name rules). "Begins" counts a
      direct slot only, so a rule whose paths begin with a transition into a reference rule (`recordName : dataReference`)
      is not one, and its uses need no label: the closure does not climb to the statements.
    - **An alternative of a slot** is a one-element alternative of a block one of whose other alternatives is one
      transition into a name rule. Many name nothing (`FILLER` beside `cobolWord` in `dataName`, the class names `NUMERIC`
      and `ALPHABETIC` beside a class-name, the literal kinds beside a constant-name in `concatOperand`), so such a slot is
      labelled `nn` even inside a name rule's own body: `name` defers to the use site, and `nn` stays `nn`. Likewise a use-site
      label overrides only an inner `ref` head: a reference rule whose head is `nn` (`implementorSwitchEntry`'s
      implementor-name) keeps it whatever the use site says.
    - **The label's form is one pattern**, `GrammarSource.LabelPattern` (`tests/_shared/GrammarSource.cs`): a prefix, a
      capitalized name and an ordinal (`refDataReference1`, `nnBoolean1`). The counter counts only fields of that form, so a
      label with no ordinal classifies nothing (a sixth fixture rule, `malformed`, shows it red). A test that reads the
      grammar's shape from its text reads it through `GrammarSource.Read`, which removes the labels; the nine tests the
      labels broke do (kb/Work PB5925's lead `2026-11-18-grammar-text-readers`).
    - **Labels are `=`, never `+=`.** A list label is a `List` allocated in every context instance of every parse; a plain
      label is one field. One field per element is what the counter needs; for an element in a loop, step 7b's oracle reads
      the earlier iterations' nodes by the child's ATN state (`RuleContext.invokingState`), which the field's own node names.
    - **The fixture is generated at test time**, with the vendored jar the Frontend csproj names (`AntlrJar`) and the
      generator's language and encoding flags, compiled in memory with Roslyn and loaded from its bytes: not by
      `Invoke-Antlr4CSharp.ps1`, whose grammar set is the product's and whose contract `AntlrGenerationDriftTests` pins, so a
      test grammar does not enter the product build. The ATN is the generated `_ATN` (the generated class's own
      `ATNDeserializer` run over `_serializedATN`).
    - **Where the class depends on resolution**: four kinds of slot can hold either a declared name or a word of the
      implementor or the standard, and are labelled `ref`: `functionName`'s `cobolWord` (a user-defined function or an
      intrinsic), `raisingTarget`'s (an exception-name, a class or an interface), `assignTarget`'s (a device-name or a
      data-name) and RERUN ON's (a file-name or an implementor-name). Step 7b's oracle counts an occurrence there only when the
      word identified a symbol (kb/Work PB5925's lead `2026-11-18-resolution-dependent-slots`).
  - *Completeness, independent of path:* `EveryWrittenReferenceIsRecorded` binds every conformance-suite program that
    binds clean, walks its parse tree, and for every name-slot node reads its class from the label that holds it: each
    `ref` node has EXACTLY one occurrence at its first-token position, each `def` node exactly one `Defined` entry (a
    section or paragraph header included, through its use-site `def` label, R5-2), each `qual` and `nn` node none of its own. A resolve path that reaches a symbol without `Identify` (impossible by
    construction, so this guards the construction), a double record, or a position recorded wrongly is red whatever
    the path.
  - *Planted programs*, one per name class and per shape the inventory of section 6.7.2 found, each with its expected
    line, column, symbol and use written from the source text: `MOVE`/`ADD`; `SET COND-88 TO TRUE` and a condition-name
    in an IF; `SET P TO ADDRESS OF X`; `SORT TBL`, `SEARCH TBL`, `INITIALIZE TBL` and a SORT key; `FUNCTION MAX(T(ALL))`;
    `SET IX UP BY 1`; `PERFORM P THRU Q`; `READ F`; `GENERATE RPT-GROUP` and `INITIATE RPT`; a SUM counter; a CD in
    `RECEIVE`; a screen in `DISPLAY`; an alphabet-name in `SORT ... COLLATING SEQUENCE`; a mnemonic-name in
    `DISPLAY ... UPON`; `LINAGE-COUNTER OF F`; `DEBUG-ITEM`. They are examples, not the guarantee; the two tests above
    are.
  - *Pairing:* every store position `StorePositionsOf` reports for a written operand pairs with exactly one occurrence
    (fails the day a binder rebuilds a linked place by value); `ADD WS-A` / `TO WS-B` on two lines and
    `MOVE WS-X (WS-X) TO WS-X` give the lines, columns and uses written from the source.
- **Step 7b, as built (branch `listing-step-7b-2`), with the corrections the code proved.**
  - *The store.* The occurrences are the group's `SymbolTable.EntryStore`'s (`SymbolTable.Occurrences.cs`), beside the entries they
    join by `SymbolId`: one list in the global append order, an index by written position, the place links. They exist only when
    the listing asks for the cross-reference (`EditionContext.RecordsReferences`, `ListingFacts.ReadsReferences`), turned on in
    `OoClassTable.Build` before the group's first identification; `ReadsNames` and so `RecordsNames` follow it, since the
    cross-reference lists every entry. ONE OCCURRENCE PER WRITTEN POSITION: a second identification of one written word (a probe
    and its resolution both committing, a re-asked formal, a validation pass re-resolving a clause operand) answers the first's
    occurrence, so "a double record" cannot be made and "keyed by the written token" is the store's rule for every funnel, not the
    group tier's alone.
  - *Every funnel records under its `report` flag* (a candidate set only when it has one member): `Identify` (context and
    `WrittenWord`), `IdentifyIndexName`, `IdentifyDataName` (the procedure division's two and the clause operand's),
    `IdentifyConditionName`, `IdentifyWithin`, `IdentifyEntryClauseOperand`, `IdentifyRegionOperand`, `ClassNames.Lookup`, and
    the two static funnels of step 6b's remainder slice 4, `IdentifyHeaderOperand` and `IdentifyDataRecord`, made instance members
    so they record. (Correction:) a data division clause operand is resolved from the model, which holds no token, so
    `WrittenDataName` carries where it is written (`Line`, `Column`, `Length`, set by its one capture `DataBinder.KeyReference`;
    none for an operand no source wrote). The oracle found three references no funnel reached, each now recorded where its entry
    is known: a REDEFINES clause's data-name-2 when `LinkRedefinitionsPass` links it (`SymbolTable.NoteRedefinesWritten`,
    `RecordRedefinition`: the clause is resolved by its placement, after the forest is composed), and an END PROGRAM or END
    FUNCTION header's name as a reference to the unit it ends (`DeclareProgram(unit, endMarker)`).
  - *The place link is the resolver's, not `PlaceBuilder`'s (correction).* "`PlaceBuilder.PlaceForItem(IdentifiedName, ...)`" cannot
    be built: the qualifiable classes are identified as typed candidate sets, not an `IdentifiedName` (section 9's budget, step 6b's
    data-name lane), and `PlaceBuilder` is the model layer's, which names no binder type. So the one place every written data
    reference's place is built from, `ReferenceResolver` (`ResolveImplCore`, `ResolveForItem`, `ResolveByName`,
    `ResolveForAddressOf`), links the INSTANCE it built to the occurrence recorded at the reference's first token
    (`ReferenceResolver.Linked`, `EntryStore.Link`, which copies an already-linked instance with `with { }`).
  - *`StorePositionsOf` asks every place the statement holds, by instance (correction).* It is `BoundReferences.Of(s)`'s places,
    and the places linked since the statement's mark, each asked of the exhaustive visitor with a match on that instance, the
    statement alone (`direct`), a store into a window `WriteInPart`; a binder's temporary is no written operand and is skipped.
    The linked places are asked too because a node may hold an operand's place where `BoundReferences` does not enter: a
    `BoundSetConditions` holds each conditional variable in a tuple (kb/Work PB5925's dated file `2026-11-20-step-7b-2-built`,
    lead 3). `StatementBinder.BindStatement` holds both marks and classifies through `EntryStore.Classify`, which also records each
    store position that pairs with no occurrence of the statement (`UnpairedStorePositions`, the pairing test's input).
  - *The transfer kind and the verb are one fact.* An occurrence carries its statement's verb as written (`SymbolOccurrence.Verb`:
    `PERFORM`, `GO`, `ALTER`, `SORT`, `CALL`, `READ`, ...), which is the transfer kind of a procedure reference and the verb of a
    file or program reference; no enum of transfers is kept beside it.
  - *The finished table carries them.* `SymbolEntry.Occurrences` (each occurrence joined by the bind's entry id) and
    `SymbolEntry.Fold`, the name's Annex C fold the cross-reference sorts by, made by the binder (the listing's layer may not
    name the runtime's `CobolNames`, and only the binder compares names).
  - *The oracle is in the Unit assembly, over the NIST corpus (correction, as step 8's coverage test).*
    `ReferenceOccurrenceDriftTests` reads the generated parser's label fields and binds with the driver's own front end
    (`CompilerDriver.NewFrontend`), both internal to the Unit assembly; "every conformance-suite program that binds clean" would bind
    ten thousand programs once more on every gate, so its corpus is the 374 NIST programs the golden run compiles (every verb of
    every 1985 module). It reads the labels as the grammar writes them, with three rules the code proved: a reference rule's head is
    the occurrence of the label on the transition into the rule, and a reference rule writes a name only when the path it matched
    begins with its head (an `integerOperand`'s literal, a USE phrase's INPUT); a labelled node whose matched element is an `nn`
    alternative names nothing; a special register's reference (LINAGE-COUNTER, LINE-COUNTER, PAGE-COUNTER) writes no name, since
    the table enters no register entry (lead 2). A `def` slot is held to its NAME, an entry of that spelling, since its entry stands
    where section 6.6 places it. The four resolution-dependent slots admit an occurrence and never demand one. Shown able to fail:
    the data-name recording dropped turns 369 of the 374 programs red, and a broken place link turns 369 red through the pairing.
  - *What is not built, each a lead of kb/Work PB5925's dated file `2026-11-20-step-7b-2-built`:* an index-name's cell is held as
    a field name, no `Place`, so SET index-name UP BY and a VARYING index are classified `Read` (lead 1); the LINAGE-COUNTER,
    LINE-COUNTER, PAGE-COUNTER and EXCEPTION-OBJECT registers have no entry, so their references record nothing (lead 2); a whole
    table a statement stores into with no place (`BoundTableSort.Storage`, INITIALIZE's whole dynamic table) is classified `Read`
    (lead 4); a CALL whose target is a literal names no entry, so the CALL graph holds only the calls written by a
    program-prototype-name (lead 5); the statistics' counts of names and uses (section 4.1's table, lead 6).

#### 6.7.2 The mechanism, and today's inventory as evidence

The GUARANTEE is section 6.7.1's two structural halves: every name table lives privately behind `SymbolTable` (held by
`NameTableOwnershipDriftTests`), and the oracle's slots come from the grammar (held by `GrammarNameSlotDriftTests`). The
inventory below is EVIDENCE of what step 7 converts at 522468b9c8, not a list anything depends on: a path missing from
it is still caught by the tests. Rounds 1 and 2 relied on lists and both were incomplete (round 3 found the report,
CD and file tables and Identify's register arms missing).

**The tables.** A Roslyn semantic sweep over the compiled `WiseOwl.Cobol.Compiler` (MSBuildWorkspace over
`WiseOwl.Cobol.sln`; it applies rules (1) and (3) of `NameTableOwnershipDriftTests` to today's tree, with `string` as
the key since `CobolName` does not exist yet) finds 65 string-keyed tables whose value is a binder type or an open type
parameter, 31 of them non-private, and 194 non-private members that take a `string` and return a binder type (a
superset: many take a C# identifier or a message, and step 7 classifies each). Its output is committed as
`docs/rearchitecture/evidence/listing-name-tables-522468b9c8.txt`. The tables, by name class, all moving into
`SymbolTable`:

| Name class | Tables found (owner.member) |
|---|---|
| Data, type, capacity register | `ForestSlice._byName`, `_typeDecls`, `_capacityRegisters` (public views `ByName`, `TypeDecls`, `CapacityRegisters`, `CapacityRegistersSoFar`); `OoMethodDataScope.ByName`, `TypeDecls`, `CapacityRegisters`; `InheritedGlobals._typeDecls` (`TypeDecls`); `ClauseFacts.LapsedTypeMembers`; `DataBinder._dataEntryLocations` (since step 6b's remainder slice 4 `SymbolTable._dataEntries` in `SymbolTable.DataEntries.cs`, each name to the `DataEntryPlace`s of the entries that declare it, and `DataBinder` keeps no entry table; since step 6b's data-name lane, first slice, the type-name and CAPACITY register tables and the lapsed members are `SymbolTable`'s, in `SymbolTable.Types.cs`, `SymbolTable.CapacityRegisters.cs` and a method's `SymbolTable.MethodNames`; `ForestSlice.TypeDeclarations` and `OoMethodDataScope.TypeDeclarations` keep the declarations as lists; since step 6b's closing slice the data-name tables are `SymbolTable`'s too, `_dataNames` in `SymbolTable.DataNames.cs` and a method's `SymbolTable.MethodNames.DataNames`) |
| Condition | `ForestSlice._conditions` (`Conditions`); `OoMethodDataScope.Conditions` (since step 6b's closing slice `SymbolTable._conditionNames` in `SymbolTable.DataNames.cs` and a method's `SymbolTable.MethodNames.ConditionNames`) |
| Index | `IndexNameRegistry._byName` (since step 6b's data-name lane, first slice, `SymbolTable`'s private `IndexNameTable`, one per element and one per method; `IndexNameRegistry` is deleted) |
| Procedure | `ProcedureNameMap<T>._first`, `_duplicated` (since step 6b slice 2, `SymbolTable`'s private `ProcedureNames<T>`) |
| File | `FileSlice._filesByName` (`FilesByName`); `FileNameScope._own`, `_inherited` (and its public `TryGetValue`, reached as `DataBindState.VisibleFiles`); `InheritedGlobals._files` (`Files`); `FileControlBinder.SelectedFiles` (all now `SymbolTable`'s, in `SymbolTable.Files.cs`: `_ownFiles`, `_inheritedFiles`, `_hiddenFileNames`, `_selectedFiles` and, since step 6b's remainder slice 4, `_firstFileDescriptions`; `FileSlice._filesByName` and `FileNameScope` are deleted) |
| Report, report group, VARYING, sum counter | `ReportScope.Named(string)` (a search, not a table: caught by rule (3)); `DataBinder._reportEntryLocations`; `ReferenceResolver.VaryingScope`; `ReportScope._sumCounters` (`SumCounters`), `DataBindState.SumCounters` (now `SymbolTable`'s, in `SymbolTable.Reports.cs`: `_ownReports`, `_visibleReports`, `_sumCounters` and `_reportEntryLocations`; `ReportScope.Named` is deleted, and `ReferenceResolver.VaryingScope` is a list of the VARYING models in scope, handed to `SymbolTable.Identify` in its `ReportSite`) |
| CD | `CommunicationSlice._byName` (`ByName`) (now `SymbolTable._cds` in `SymbolTable.Communication.cs`) |
| Constant | `ConstantEntryBinder.ConstantScope.Bound`, `Declared`; `InheritedGlobals._constants` (`Constants`) (now `SymbolTable`'s, in `SymbolTable.Constants.cs`: the private `ConstantNames`, one per element and one per method, and `_inheritedConstants`; `ConstantScope` is deleted) |
| CONFIGURATION SECTION names (alphabet, class, symbolic character, mnemonic, locale) | `ConfigurationNames<T>._entries`; `SpecialNamesBinder.RangeCollations`, `DataBindResult.RangeCollations` (now `SymbolTable`'s private `ConfigurationNames<T>` and `_mnemonics`, in `SymbolTable.Configuration.cs`; `RangeCollations` is an unkeyed list of carriers) |
| Implicit registers | `ImplicitRegisters._debugRegisters` (`DebugRegisters`) (since step 6b's data-name lane, first slice, the DEBUG-ITEM names are keyed by `SymbolTable.DebugRegisters.cs`; `ImplicitRegisters` keeps an unkeyed list of the registers it makes) |
| Program, function, prototype, repository | `StatementBinder.ProgramPrototypes`, `NestedCallables`, `UserFunctions`; `ConfigurationSlice.ProgramSpecifiers`, `FunctionSpecifiers`; `DataBinder._repositorySpecified`; `GroupRepositoryProvider.NoFunctions` (since step 6b slice 6 the program-prototype tables are `SymbolTable`'s and `NestedCallables` is keyed by `ExternalizedProgramName`; since slice 7 the function tables are `SymbolTable`'s and `NoFunctions` is deleted) |
| OO class, interface, method | `OoClassTable._byName`, `_ifaceByName`, `_imported`, `_importedInterfaces`; `OoClassSymbol._methods`, `_factoryMethods`; `OoInterfaceSymbol._protos`, `_conformingChoice` (since step 6b's OO lane the class and interface tables and the SR5 choice are `SymbolTable.ClassNames`', and the rosters are keyed by `ExternalizedMethodName`) |
| Keyed by something that is not a COBOL user word (re-keyed by a type of their own, so rule (1) no longer sees a `string` key) | `IntrinsicCatalog.Table`, `IntrinsicArgumentRules.Verified` (intrinsic function names, a standard catalog); `ImplementorNameRegistry.ByName` (implementor-names); `FileModel.FdClauses`, `ScreenFacility.ClauseFacts` (clause keywords); `ExpansionPlacements.ByName` (CLR type names); `Polynomial._terms` (C# expression text) |

**The name comparisons (R4-2).** A second Roslyn sweep (by symbol through the `SemanticModel`, over every document of
`WiseOwl.Cobol.Compiler` at 7bc7e33469) finds 124 invocations of `CobolNames.Same`, 130 uses of `CobolNames.Comparer`
and 40 `==`, `!=` or `string.Equals` comparisons on a binder symbol's name property. Each of the 124 `Same` calls is
classified by reading its site; the result is committed, one row per site, as
`docs/rearchitecture/evidence/listing-name-equality-7bc7e33469.txt`:

| Class | `Same` calls | Where they go in step 7 |
|---|---|---|
| RESOLVE | 68 | into `Identify`: e.g. `ReportScope.Named`, `FileModel.RecordKeyNameCandidates`, `DataBinder.QualifierWalk`, `LinkageBinder.FindLinkageRoot`, `EndMarkerPass.ScreenProgram` (an END PROGRAM name names its program), `TurnState.Fold` (a `>>TURN` file operand) |
| DECLARE | 13 | into `Declarations`: e.g. `BinderDriver.CheckDefinitionNameUniqueness`, `DataBinder.NonDataDeclarationOf`, `ReportScope.Inherit` |
| KEYWORD | 19 | stay, classified: e.g. `DataBinder.BindEntry` (FILLER), `ReportWriterBinder.RawNumber` (ZERO), `OoBinder.IsStandardNew` (NEW) |
| SAMESYMBOL | 16 | stay, classified: e.g. `StrongTypeModel.EquivalentDeclarations`, `OoConformance.RaisingMismatches` |
| EXTERNAL | 6 | stay, classified: e.g. `OoClassTable.FindByExternalizedName`, `RepositoryCodec.Decoder.Root` |
| MESSAGE | 2 | stay, classified: `NonDataDeclaration.DeclaredAs` |

The 130 `Comparer` uses key name collections and move with their tables (or, for a set of fixed words, are re-keyed);
the 40 name-property comparisons are classified site by site in step 7 into the same classes. The KEYWORD, SAMESYMBOL,
EXTERNAL and MESSAGE rows seed `name-comparisons.json`, which may then only shrink.
Screen names (`ForestSlice.ScreenNames` at 522468b9c8, since step 6b's closing slice `SymbolTable._screenNames`) are a set, not a table, and resolve through the data tables. The front end's
own name tables (compilation variables of `>>DEFINE`, the `>>COBOL-WORDS` map) are text-stage names, not program
symbols, and are outside the cross-reference.

**The resolve paths.** The Roslyn reference sweep of rounds 2 and 3 (`sweep-symbol.cs`, every reference bound by the
semantic model; the LSP server had not indexed the worktree; `WiseOwl.Cobol.Runtime` did not load for want of a
restore and declares no binder member) gives the paths into those tables today:
| Lookup or entry (today) | Its callers | Converges |
|---|---|---|
| `ReferenceResolver.Resolve` / `ResolveImpl` -> `ResolveImplCore` -> `ReferenceResolver.Identify` | `ExpressionBinder.FieldOperand`, `RefExpr`, `ResolveSending`, `ResolveReceiving`; `ObjectPropertyResolver.ResolveObjectProperty`, `ViewOfQualifiedItem`; `ProcedureTableBuilder.ReportUnresolvedDebugOperand`; `ReferenceResolver.CapacityRegisterFor` | `Identify` moves into `SymbolTable`; `ResolveImplCore` calls it |
| `ReferenceResolver.ResolveForItem` (a condition-name's conditional variable) | `ConditionBinder.AnalyzeBareOperand`, `SetBinder.BindSetCondition` | the condition-name was identified by `ConditionBinder.ConditionOf` (next row), which records the use; `ResolveForItem` takes its `IdentifiedName` |
| `ConditionBinder.ConditionOf` | `ConditionBinder.AnalyzeBareOperand`, `ReportMisqualifiedCondition`; `SetBinder.BindSetCondition`; `SetAlterBinder.SwitchCondOf`; `SearchAllFormat2Rules.CheckConditionNameSide` | becomes `SymbolTable.Identify(dref, NameClass.Condition, ...)` (since step 6b's remainder slice 1, the typed `SymbolTable.IdentifyConditionName(WrittenQualifiedName, report)`, not a `NameClass.Condition` arm: section 6.7.1's correction "A qualifiable class's typed method") |
| `ReferenceResolver.ResolveForAddressOf` -> `Identify` | `PtrBinder.PtrBindAddressOf` | through `Identify` already; keeps it |
| `ReferenceResolver.ResolveTableOperand` -> `NameResolution.FindReportedItem` | `SortBinder.SortBindTable`, `SortBinder.SortAddFileKeys`, `StatementValidation.ResolveSearchTable` | `Identify(dref, NameClass.TableOperand, ...)`; `FindReportedItem` is deleted (since step 6b's remainder slice 1, `FindReportedItem(dref, WrittenQualifiedName)` is kept, over `NameResolution.DataItemNamed`: the reported lookup of a table name, which asks the LINKAGE screen) |
| `PlaceBuilder.WholeTable` (place of a whole table, after the table operand was identified) | `InitializeBinder.BindInitializeTarget`, `SearchBinder.WrittenTable`, `SortBinder.SortBindTable` | takes the `IdentifiedName` (links the place) |
| `NameResolution.FindReportedItem` | `IntrinsicBinder.TryBindAllArgument`, `ReferenceResolver.ResolveTableOperand` | deleted; both through `Identify` (since step 6b's remainder slice 1, kept: `FindReportedItem(dref, WrittenQualifiedName)` over `DataItemNamed`, as the row above says) |
| `ReferenceResolver.ResolveByName` -> `NameResolution.FindItem` | `IntrinsicBinder.TryBindAllArgument` | replaced by `PlaceBuilder.PlaceForItem(IdentifiedName, ...)` on the name `TryBindAllArgument` already identified (since step 6b's remainder slice 1, kept as `ResolveByName(WrittenQualifiedName, ...)` over `DataItemNamed`; the `IdentifiedName` comes with the entry ids, part D) |
| `NameResolution.FindItem` | `ConstantEntryBinder.LengthOperandItem`, `ReportLengthOperand`; `DataBinder.ReportEntryLocation`; `PtrBindBasedAndAddressablesPass.ResolveStorageClaimHead`; `ReferenceResolver.ProbeDeclaration`, `ResolveByName`; `NameResolution.FindReportedItem` | private to `SymbolTable`; the data-division callers (a CONSTANT LENGTH OF operand, a BASED claim) call `Identify` and record a data-division use (since step 6b's remainder slice 1, deleted: `NameResolution.DataItemNamed(WrittenQualifiedName)` over `SymbolTable.IdentifyDataName`; the CONSTANT LENGTH OF operand and the storage-claim scan hand their written name) |
| `NameResolution.ResolveQualified` / `ResolveUnqualified` | `ReferenceResolver.Identify`; `NameResolution.FindItem`, `DeclarationOf`; `SubscriptBinder.ResolveSubscriptName`, `CannotBeSubscripted`; `ObjectPropertyResolver.ViewSubjectChain`, `IsObjectPropertyReference`; `CounterResolver.SumCounterFamilyFor` | private to `SymbolTable`; `ResolveSubscriptName` (a subscript naming a data item) and `SumCounterFamilyFor` call `Identify`; the two object-property predicates and `CannotBeSubscripted` are probes (`report: false`) (since step 6b's remainder slice 1, deleted: both arms are `SymbolTable.IdentifyDataName`'s; `SumCounterFamilyFor` still asked `DataBinder.QualifiedCandidates`, part B; since step 6b's remainder slice 3 it asks `SymbolTable.IdentifyDataName`, the ordinary lookup's own method) |
| `NameResolution.ResolveIndexName` | `ExpressionBinder.IndexFieldOf`; `SubscriptBinder.SubscriptNamesIndex`, `ResolveSubscriptName`; `SearchAllFormat2Rules.CheckKeySubscript` | `Identify(..., NameClass.Index, ...)` (since step 6b's data-name lane, first slice, `SymbolTable.IdentifyIndexName`, the typed index-name identification) |
| `SymbolTable.TryResolve` | `DataBinder.ResolveClauseOperand`, `QualifiedCandidates`, `NonDataDeclarationOf`; `InspectBinder.BindPhraseOperands`; `IntrinsicBinder.TypeNameArgument`, `KeywordWordOf`, `KeywordOmittedFunction`; `PtrBinder.PtrResolveBased`; `SortBinder.SortBindTable`; `SubscriptBinder.IsFunctionBearing`; `StatementValidation.RejectProcedureName`; `NameResolution.ReportUnidentified`, `DeclarationOf`; `SymbolTable.TryResolveUnqualified`, `TryResolveCondition` | becomes private inside the grown `SymbolTable`: `DataBinder.ResolveClauseOperand` (a clause operand such as DEPENDING ON) and `InspectBinder.BindPhraseOperands` call `Identify`; `QualifiedCandidates` asks for symbols, so its callers (RECORD KEY, ASSIGN USING, the KEY phrases) call `Identify` and record; only `NonDataDeclarationOf`, a declaration question, calls `SymbolTable.Declarations` and gets a `DeclarationFact`; the word-class probes (`KeywordWordOf`, `KeywordOmittedFunction`, `TypeNameArgument`, `IsFunctionBearing`, `RejectProcedureName`) pass `report: false` (after step 6b's remainder slice 1 none of this is built: every caller named here still asks it by text, part B; since step 6b's remainder slice 3 `TryResolve` is private to the table, and the callers outside it ask `IdentifyDataName`, `IdentifyEntryClauseOperand`, `IdentifyRegionOperand`, `IdentifyWithin`, or, for a word-class probe, `DeclaresDataName`) |
| `SymbolTable.TryResolveUnqualified` | `NameResolution.ResolveUnqualified`; `InitializeBinder.BindInitializeTarget`; `IntrinsicBinder.FunctionPointerItemNamed`; `PtrBindBasedAndAddressablesPass.ResolveStorageClaimHead`; `SymbolTable.NearestDeclaringData` | as above: the two binders call `Identify` (since step 6b's remainder slice 1, `InitializeBinder.BindInitializeTarget` and `IntrinsicBinder.FunctionPointerItemNamed` ask `SymbolTable.IdentifyDataName`, `NameResolution.ResolveUnqualified` is deleted, and the table's own `IdentifyDataName` and `Declarations` ask it; the storage-claim scan's unqualified arm remains, part B; since step 6b's remainder slice 3 it is private to the table, and the storage-claim scan asks `IdentifyDataName(WrittenQualifiedName, Scope, report)`) |
| `SymbolTable.TryResolveCondition` | `DataBinder.QualifiedConditionCandidates`, `ConditionsUnderQualifiers`; `ConditionBinder.ReportMisqualifiedCondition`; `SearchAllFormat2Rules.CheckKeySide`, `CheckConditionNameSide` | through `Identify(..., NameClass.Condition, ...)`, or `Declarations` for the data-division checks (since step 6b's remainder slice 1, private to the table: `IdentifyConditionName`, `DeclaresConditionName`, `ConditionTier` and `Declarations` ask it; `DataBinder.QualifiedConditionCandidates` is deleted) |
| `SymbolTable.IndexCandidates` | `NameResolution.ResolveIndexName`; `DataBinder.NonConditionDeclarationsOf`; `IndexNameRegistry.Candidates`; `FlagConformancePass.FlagIndexSet` | `ResolveIndexName`'s lookup inside `Identify`; `FlagIndexSet` reads the occurrences instead of looking up again (since step 6b's data-name lane, first slice, `IndexCandidates` is private, asked by `SymbolTable.IdentifyIndexName` and by `Declarations.NonConditionDeclarationsOf`; the first is what `ResolveIndexName` and, until step 7b, `FlagIndexSet` ask; `IndexNameRegistry` is deleted) |
| `ProcedureTableBuilder.ResolveProcedureWord` | `ProcedureTableBuilder.Resolve`, `ResolveDebugProcedure`, `ReportUnresolvedDebugOperand` | `Identify(..., NameClass.Procedure, ...)` |
| `StatementValidation.ResolveFileWord` | `StatementValidation.ResolveFile` | `Identify(..., NameClass.File, ...)` |
| `DataBindState.VisibleFiles` (`FileNameScope.TryGetValue`), `ReportScope.Named`, `CommunicationSlice.ByName` (round 3) | the file, report and CD binders (`CounterResolver.ReportOf`, `ReportWriterBinder`, `CommunicationBinder`, `ProcedureTableBuilder`, `CheckCodeSetRecordItemsPass`, ...) | `Identify(..., NameClass.File / Report / Cd, ...)` |
| `ReferenceResolver.Identify`'s register arms (CAPACITY, EXCEPTION-OBJECT, DEBUG-ITEM, sum counters, LINAGE-/LINE-/PAGE-COUNTER) (round 3) | every `Resolve` | record like every arm (section 6.7.1) |
| Probes: `ReferenceResolver.Probe`, `ProbeDeclaration`, `DescribesOwnItem` | 39 references outside `ReferenceResolver` (31 to `Probe`, 8 to `ProbeDeclaration`), in `ArithmeticBinder`, `CallBinder`, `ConditionBinder`, `EvaluateBinder`, `OoBinder`, `SetBinder`, `SetFormatSelection`, `UdfBinder`, `ParameterConformance`, `SearchAllFormat2Rules`, `StatementValidation`, `StatementBinder`, `ProcedureTableBuilder`, `MessagingBinder`, `SubscriptBinder`, `FlagConformancePass` | through `Identify` with `report: false`: they classify before the real resolution, and the real resolution records |

The probes are why the recording is gated on `report`, not on reaching `Identify`: a statement that probes an operand
and then resolves it would otherwise record it twice. The completeness guard of section 6.7.1 counts occurrences per
written position, so a double record is red as surely as a missing one.

### 6.8 Statistics and verb counts

Lines by kind and by file (source, comment, blank, omitted, copied, suppressed), programs, statements, diagnostics by
severity, and VBREF: each verb as written, its count and every listing line. Verbs come from the parse tree
(`SourceUnitFacts`), so they are counted as the programmer wrote them (not as the binder desugared them) and exist on a
failed bind. No timings: they make two listings of one source differ for no reader's benefit.

**As built (step 5):** `ListingStatistics(Lines, Files, Diagnostics, Parse)`, built by `StatisticsSectionBuilder` from the
finished lines and the diagnostics the compilation reported:
- `ListingLineCounts(Lines, Kinds, Omitted, Copied, Unlisted)` for the whole listing and for each file (`ListingFileStatistics`,
  in the order of the files; a library text copied twice counts each copy). `Kinds` holds EVERY `ListingLineKind`, a zero
  included, in enum order, so a kind added to the enum is counted with no change here; "suppressed" is `Unlisted`, a line
  `ListingVisibility` did not list (a LISTING directive or COPY SUPPRESS).
- `Diagnostics`: every `ListingSeverity`, counted from every diagnostic reported, whether or not the diagnostics section is
  asked for.
- `Parse` (`ListingParseStatistics(Programs, Statements, Verbs)`), null when the source was not parsed. It is read off the
  tree the parse built, recovered or not; after a recovery (`ListingHeader.Parse` is `Recovered`, section 6.1) the recovery
  may have skipped statements, even every statement after the error, so the counts are of what it kept, and the text says so
  under `STATEMENTS` (review R1-1; the step-5 design said only that the counts "exist on a failed bind", which is true of a
  failed bind but not complete after a failed parse). The verbs and
  statements are `SourceUnitFacts.Statements`: each node of the grammar's `statement` rule the parse built, a statement nested
  in another included (the MOVE of `IF A MOVE B TO C END-IF` is a statement of its own); a recovered node that consumed no
  token is none. A statement's verb is its first word in upper case (`GO` of GO TO, `NEXT` of NEXT SENTENCE): the verb as
  written, never as the binder desugars it. Each `ListingVerb` holds its count and the listing line each of its statements
  begins on, placed through the one index (`ListingLineIndex.Of`), in listing order, one entry per statement; the verbs are in
  the ordinal order of their words.
- The text prints the kinds that have lines, each file's counts, the programs, statements and diagnostics, then the verbs
  (IBM's VBREF): `VERB  COUNT  LINES` (`StatisticsText.Rows`).

### 6.9 COBOL to object-code map (backend-neutral; owner 2026-11-18)

The owner, 2026-11-18: "It also makes no sense to link to C# code when we switch to the IL generation." No section,
renderer, link or schema field names, reads or points into generated source (`.g.cs`) or any Roslyn type. The map is
read from what every backend produces, the ASSEMBLY and its PORTABLE PDB:

- **Source of truth.** `ObjectCodeMapReader` (Compiler, `Listing/`) opens the produced assembly and its PDB with
  `System.Reflection.Metadata` (`MetadataReader`, `MethodDebugInformation.GetSequencePoints`, `MethodBodyBlock`). For
  each method it reads the sequence points: a document (a COBOL source or library text), a start and end line and
  column, and an IL offset; and (PB5951) the body's exception-handling clauses (`MethodBodyBlock.ExceptionRegions`, ECMA-335
  II.25.4.6), read beside the IL, since they are as much the method's object code as its instructions. Each COBOL statement's listing line is placed through its source position by the one line
  index (section 4.1); its object code is the method (its metadata token and name) and the IL offset range from its
  sequence point to the next. A copybook copied twice into one method gives two statements at one file position; they
  pair with that position's sequence points in IL order, which is statement order.
- **Both backends emit sequence points at COBOL statement granularity.** This decides open question 4. The Roslyn
  backend writes, inside the backend and nowhere else, a `#line (startLine, startColumn) - (endLine, endColumn) "file"`
  directive before each COBOL statement's C# and `#line hidden` before scaffolding, from `BoundStatement.At` through the
  source map, and emits a portable PDB beside the assembly (`<out>.pdb`, `DebugInformationFormat.PortablePdb`,
  deterministic like the assembly). The CIL backend emits the same sequence points natively. A .NET debugger then steps
  through COBOL source under either backend.
- **`SequencePointCoverageDriftTests`** (Conformance): for every executable statement of every conformance-suite program
  that compiles, the PDB holds a sequence point at the statement's COBOL position; run against every backend the
  backend factory offers.
- **The IL listing (IBM's LIST), decided: provided, off by default.** With `--listing-sections object-code` the listing
  prints, under each statement, its IL instructions decoded from the method body with `System.Reflection.Metadata` (one
  decoder, `IlDisassembler`, opcodes and resolved member and type operands). It is the true object code under both
  backends, and like IBM's LIST it is long, so it is on request only. Recommendation recorded: keep it off in
  `standard` and on in `all`.
- **Backend neutrality is a test.** `ListingBackendNeutralityDriftTests` (RepoDrift): the built `WiseOwl.Cobol.Listing`
  assembly references no `Microsoft.CodeAnalysis*` assembly, and the documents of the `WiseOwl.Cobol.Listing.Building`
  namespace in the Compiler (the builder) reference no type from `Microsoft.CodeAnalysis*` or `WiseOwl.Cobol.CodeGen*` (a Roslyn
  semantic check of every referenced symbol) and contain no `.g.cs` path. It holds because backend diagnostics reach the
  builder as the front end's `Diagnostic`, which (step 3) is the seam's own diagnostic type (section 6.3). The Roslyn backend's `#line` writing lives in
  `WiseOwl.Cobol.CodeGen`, behind the seam; the listing never sees it.

**As built (step 8), with the corrections the code proved:**

- **Where a statement is written is a fact of the bound tree (correction).** "From `BoundStatement.At` through the source map"
  inside the backend would hand the backend the front end's line map, which the seam keeps from it (`BoundCompilation` carries
  no parse context, kb/Work PB2398), and the end of a statement is not in `At` at all. So `StatementBinder.BindStatement`, the one
  funnel every statement passes through, stamps `BoundStatement.Span` (`StatementSpan`: the physical file, the 1-based line and
  column of the first character, of the position after the last, and of the position after the verb) on the OUTERMOST node it
  returns, from the statement's own first and last tokens through the one resultant-to-physical mapping. Read from `s.Start`, not
  from the node's `At`: a wrapper the funnel adds after the verb binder (`BoundActivationSite`) has no `At`, and the first build,
  which read it, left every MOVE with a sending-value temporary unmapped. A node the binder synthesised and a statement of a
  skeleton read from the repository have no span; both backends read the same span.
- **One sequence point per statement, and no other (correction of the granularity).** A `#line` before each statement's C# maps
  every following line by line delta, so a statement compiled to several lines, two statements on one line and a lambda's body
  gave points at columns no statement begins at, and a debugger stopped several times on one statement. `CodeWriter` therefore
  writes `#line hidden` once after the file's header (`HideUnmappedLines`, called by `ProgramEmitter.Emit`) and, while a statement
  is open (`MapStatement`, called by `StatementEmitter.EmitStatement`), writes the span form `#line (line, column) - (endLine,
  endColumn) offset "file"` only before the statement's first line whose code Roslyn gives a sequence point in an optimized build
  (`CodeWriter.FirstSequencePoint`, read with Roslyn's own parser: an expression statement, a jump, an if, while, switch, lock or
  using header, a for statement's declaration, a declaration whose initializer is not a constant; a line that continues a
  statement an earlier line began, `else if (...)`, `else x = y;`, a catch clause, `} while (...);`, a switch section, parsed inside
  the head that makes it a statement and read as that clause, `CodeWriter.Continuation`), with `offset` the column that code
  starts at; whatever follows that code on the line (a second statement, the body of a lambda, an arm of a switch
  expression) is moved onto a hidden line of its own; every other line is hidden. A statement nested in another maps its own
  first line; the enclosing statement's next code after it (an inline PERFORM's step and test after its body) takes ONE more
  point at the same position, spanning only the verb, so that code is the PERFORM's and not the nested ADD's. (Correction, review
  round 1:) that code is often on a line the line alone does not show to be code: an EVALUATE's second WHEN is an `else if (...)`
  after the first WHEN's body, which the first reading took for no statement, so its test fell into the first WHEN's range; and
  the brace that ends a try, catch, finally, lock or using block is a point of an optimized build (the leave, the release, the
  dispose) that no reading of the line `}` can tell from an if's. The continuation reading takes the first; the writer keeps the
  blocks its lines opened (`CodeWriter.ClosingPoint`) for the second. Both are held to what Roslyn writes, read back from a PDB
  of its own compilation (`CodeWriterTests`), and the EVALUATE on a real compilation (`ListingObjectCodeTests`). (Correction,
  review round 2:) the first reading also hid a line whose code is not certain, a local declared with a constant (an ADD ON SIZE
  ERROR's `bool __sizeErr0 = false;`, a CALL ON EXCEPTION's `bool __callErr0 = false;`, a PERFORM n TIMES's count), because an
  optimized build drops that store when nothing reads the local, and its point with it; but when the build keeps it, the store is
  code before the statement's first point, and it fell into the range of the statement before (an EVALUATE's last WHEN, a STRING's
  ON OVERFLOW DISPLAY). Roslyn keeps one sequence point per IL offset, the last one written, so the writer now gives such a line
  (`CodeWriter.PointOf`, `LinePoint.Droppable`) the statement's span too, and again the statement's next line that certainly carries
  a point: if the store was dropped, the second point stands alone at the same offset; if it was kept, both stand. At the statement's
  end the writer settles the pair (`CodeWriter.Settle`): when it can show the store kept (`CodeWriter.Kept`: the local is read, and
  used where the evaluation stack cannot carry it, inside a try, catch or finally block, a lambda or local function, an if's body or
  a switch section, or a loop, or passed by reference; Roslyn drops a store only of a local nothing reads or one it keeps on the
  stack), the second directive becomes `#line hidden` and a debugger stops once. When it cannot show it, both points stay and a
  debugger stops twice on that statement, at its span, the price of never giving its first code to the statement before it; the
  ON SIZE ERROR and ON EXCEPTION flags and an n TIMES count are shown kept whenever the code reads them. The writer's reading is held to Roslyn's PDB on each shape (kept
  and shown, kept and not shown, dropped, after a nested statement), and the ADD and CALL on a real compilation
  (`ListingObjectCodeTests.AStatementsFlagStore_IsInItsOwnRange_NotThePreviousStatements`). Text captured for
  an expression (`CodeWriter.CaptureText`) carries no directive. The file in a directive is named by the one decider
  `SourceDocuments.NameOf`, relative to the document root (next item); a path holding a quotation mark or a line end, which a
  directive cannot spell, keeps its points under the name with each replaced by `_`.
- **The PDB** is written by `RoslynBackend.Compile` in the same emit as the assembly, to a temporary of its own renamed after the
  assembly (`OutputFileProtocol`), portable and deterministic, named in the assembly by its file name alone so the assembly's
  bytes do not depend on its directory. (Correction:) it names each source document RELATIVE to the main source's directory with
  '/' (`BackendOptions.DocumentRoot`, which the driver sets from `SourceDocuments.RootOf`; `SourceDocuments.NameOf`): the PE names its PDB by a hash of the
  PDB's content, so full paths made identical source compiled in two directories two different assemblies, which the repository's
  byte-identity tests refuse (`RepositoryReaderTests.TwoCompilesOfOneGroup_WriteByteIdenticalModules`, the expansion assembly's
  determinism across groups and directories). Every reader resolves a name against the same root by the one decider
  (`SourceDocuments.PathOf`): the listing's map reader, a backend diagnostic, the tests; a debugger finds the
  files from the main source's directory (or its source search path). An expansion's own compilation writes its text under the
  expansion's name in a folder of its own (`ExpansionAssemblies.Write`), so its document name is a function of the expansion too; `BackendOptions.EmitPdb` (true by default, no longer reserved) decides it, and it is in
  `BackendArtifact.OutputFiles`, so the compiled-program cache keeps it beside the assembly. It is written for every compilation
  (it is the debugger's), and read only for a listing that asks (section 9).
- **What compiles to no code has no range.** CONTINUE, a plain EXIT, a USE statement, a NEXT SENTENCE at a sentence's end, a
  statement the backend drops because no execution reaches it, a CORRESPONDING statement with no pair, and an IF, EVALUATE or
  inline PERFORM whose condition the C# compiler folds to a constant (`if (4L == 4L)`) compile to no IL of their own, so they have
  no point and no range. The map is the truth of the assembly.
- **The model.** `ListingObjectCodeMap(Assembly, Methods, Ranges)`, `ListingMethod(Id, Token, Type, Name, CodeSize)`,
  `ListingObjectRange(Line, Column, Method, Start, End, Instructions)`, `ListingInstruction(Offset, OpCode, Operand)`. Two
  sections, as section 8's word list has them: `object-map` (`ListingSection.ObjectMap`, the ranges) and `object-code`
  (`ListingSection.ObjectCode`, the ranges with their instructions); both in `all`, neither in `standard` (section 13 item 1).
  `ObjectCodeMapReader` reads the methods and every visible sequence point with its stretch (to the method's next visible point:
  a hidden point, the code the backend writes between statements, does not end one), `IlDisassembler` decodes the stretch (the
  instruction set is the runtime's own table, `System.Reflection.Emit.OpCodes`; operands named by `MetadataNames`, ECMA-335's
  `Type::Member`), and `ObjectCodeMapBuilder` places each point on its listing line. (PB5951) Each method's exception-handling clauses ride the method,
  each stretch the lines it prints among its instructions: the next item. A copybook copied twice: the points of one
  position are told apart by their span (a statement's first point spans the statement, its later ones only the verb), the first
  points paired in compiled order with the listing lines of that position in listing order, each later point going with the first
  point before it. (Correction, review round 2:) a statement can hold two first points (the previous item), and each copy compiles
  to the same code, so when the first points divide evenly among the lines each line takes an equal share in order; when they do
  not, the k-th first point goes to the k-th line and every later one to the last. The text
  is `ObjectCodeText.MapRows` and `ObjectCodeText.CodeRows`.
- **Exception-handling regions (PB5951, correction).** The owner, 2026-11-19, in NC105A's listing: "I see endfinally and leave
  instructions, but there are no pseudo try or catch instructions, such as would appear in IL source code", and "catch also". The
  step-8 reader decoded each body's IL and never read the body's exception-handling clauses, which ECMA-335 II.25.4.6 places in the
  same method body, so a `leave`, `endfinally`, `endfilter` or `rethrow` stood with no protected block around it. As built:
  - *Read once, by the reader of the IL.* `ObjectCodeMapReader.Read` reads `MethodBodyBlock.ExceptionRegions` beside the IL into
    `ListingMethod.Regions`: one `ListingExceptionRegion(Kind, TryStart, TryEnd, HandlerStart, HandlerEnd, FilterStart, CatchType)`
    per clause in the body's order (`ListingRegionKind`: `Catch`, `Filter`, `Finally`, `Fault`; an end is the offset after the block;
    `FilterStart` only for a filter, `CatchType` only for a catch). The catch type is named by `IlDisassembler.TokenName`, the one
    naming of a token, which the instructions' operands use too, so a catch names its type as `newobj` or `isinst` would. A kind the
    metadata reader does not know fails loudly. The clauses are read whenever the map is (they cost a few objects per method); only
    their placement among the instructions waits for `object-code`.
  - *Laid out once, by the builder.* The lines are ILDASM's (ECMA-335 II.19): `.try {` and `} // end .try` around a protected
    block, then each handler of the clauses that protect it: `catch <Type> {`, `finally {`, `fault {`, or a filter clause's
    `filter {`, `} // end filter` and `{ // handler`, each closed by `} // end handler`. `ExceptionRegionLayout.Marks` makes a body's
    BLOCKS (one protected block per distinct try range, which several clauses may share, as a C# `try` with two `catch` clauses
    does; each filter block, from its start to its handler; each handler) and walks them as the tree ECMA-335 I.12.4.2.7 makes them
    (two blocks are disjoint or one contains the other): at one offset every block ending there closes before any block starting
    there opens, the innermost closing first and the outermost opening first. Each line is a `ListingRegionMark(Offset, Boundary,
    Region, Depth)`: the boundary (`ListingRegionBoundary`), the clause (1-based in `ListingMethod.Regions`; for a shared block, the
    first clause protecting it) and the nesting depth. A body whose blocks overlap, or a block covering no IL, fails loudly.
  - *Placed in one stretch.* `ExceptionRegionLayout.Place` gives each line to the stretch whose code it bounds: a line that OPENS a
    block at offset X to the stretch holding X (`Start <= X < End`), before the instruction at X; a line that CLOSES a block at X to
    the stretch holding the block's last instruction (`Start < X <= End`), after it. So a block wholly inside one statement's code (a
    `finally` of an OPEN, the handlers of an ADD ON SIZE ERROR) opens and closes in that statement's stretch, and a block ending where
    the next statement's code begins closes under the statement whose code it ends. (Correction, PB5951 review round 1: the first cut
    gave every line to the stretch holding its offset, so a closing line at a stretch's end printed at the top of the NEXT statement's
    stretch, leaving the first stretch's block unclosed and the next one starting with a close of nothing open.) A line no stretch owns
    that way (its code is before the method's first visible sequence point, or in a stretch the map leaves out because its position is
    in no file of the listing) prints in the method's next placed stretch before its first instruction, or after the last instruction
    of its last one, so every line prints exactly once. `ListingObjectRange.Regions` holds the stretch's lines in print order when the
    object-code section is asked for (null otherwise, as `Instructions` is), and `ListingObjectRange.Depth` the depth the method's
    earlier stretches' lines leave: the number of blocks containing the stretch's start whenever the stretch before it ends there.
  - *Printed by the renderers, decided by neither.* `ObjectCodeText.CodeLines` is the one merge of a stretch's instructions and its
    lines (each line before the first instruction at or after its offset, the rest after the last), giving each instruction the depth
    the lines before it leave (from the stretch's `Depth`; `ListingRegionBoundaries.Opens` is the one answer to which lines open a
    block), and `ObjectCodeText.RegionText` the one spelling of a line; the text listing indents a line two columns per level
    (`ObjectCodeText.RegionIndent`), the HTML page the same in its instruction table (a row of class `rg` per line).
  - *Witnesses.* `ExceptionRegionLayoutTests` (Unit, over hand-built clauses whose lines are written by hand from ECMA-335: nested
    regions, adjacent ones, three clauses sharing a block with a filter among them, a region inside a handler, overlap refused, the
    placement and depths, a block inside one stretch and one ending at a stretch boundary, the text rows) and
    `ListingExceptionRegionTests` (Conformance: a program whose OPEN compiles to a `finally`, whose ADD ON SIZE ERROR compiles to
    two nested protected blocks of two `catch` clauses each, and whose reference modification under `>>TURN EC-BOUND-REF-MOD
    CHECKING ON` compiles to a `filter` in a `finally`; each method's clauses are read in the test from the assembly with
    `System.Reflection.Metadata`, each line's depth from the blocks' containment, each line's stretch from the code it bounds, each
    stretch's depth from the blocks containing its start, and the text and HTML rows counted against them). Each was shown able to
    fail: the reader dropping the filter clauses, the layout opening the innermost block first, the merge dropping the lines, and
    placement giving a closing line at a stretch's end to the next stretch were each red.
- **Backend diagnostics through COBOL positions.** A Roslyn diagnostic on a statement's mapped line carries the COBOL file, line
  and column. (Correction, review round 1:) the mapped file is the directive's document name, relative to the document root, which
  no file of the listing is called and which names a file relative to the source's directory rather than the user's; so
  `RoslynBackend.Neutral` resolves it to the file's full path (`SourceDocuments.PathOf` against `BackendOptions.DocumentRoot`),
  and `DiagnosticSectionBuilder` finds a diagnostic's file by the name the compilation read it by or, failing that, by its full
  path (the listing's files by full path, which `ObjectCodeMapBuilder` reads too), so it is placed on its listing line
  (`BackendDiagnosticPlacementTests`: a source by its full path, a copybook in a sibling directory). One on a hidden line carries
  the generated file's position and is in the summary only (section 6.3).
- **`SequencePointCoverageDriftTests` (correction: Unit, over the NIST corpus).** "Every conformance-suite program" would compile
  ten thousand programs once more with a listing for the parse's statements, and a listing compilation is never stored by the
  compiled-program cache (it reads the clock), so every gate would pay it again; and the backend factory is internal. The test
  is in the Unit assembly, which builds the driver's front end (`CompilerDriver.NewFrontend`), and its corpus is every NIST program
  the golden run compiles (the green and divergent rows of `tests/nist/corpus.tsv`, 374 programs, every verb of every 1985
  module, about a minute): each statement the PARSE built has a visible point at its position, and every visible point is at a
  statement's position, the points read from the PDB in the test. Exempt, and nothing else: the statements the standard gives no
  code (CONTINUE, EXIT, USE, NEXT SENTENCE), a statement after a GO TO, STOP, EXIT, GOBACK or NEXT SENTENCE in its paragraph's
  order, a statement after (or being) an IF or PERFORM whose condition the backend's source shows as a constant, and a
  MOVE, ADD or SUBTRACT CORRESPONDING read by its own phrase, the word after its verb (correction, review round 1: the first test
  exempted every statement whose text held CORR, every move to the NIST programs' CORRECT-A among them); each exemption is sound (it leaves a statement unasked, never asks a wrong one). It runs against the
  backend the driver compiles with, the only one the factory offers. Shown able to fail: a span withheld from MOVE (431 statements
  of NC101A without a point) and `HideUnmappedLines` removed are each red.

### 6.10 DISPLAY directive output (2023)

Each `>>DISPLAY` transfer to the listing prints at its directive as `DISPLAY: <text>`, always listed (§2023:7.3.12.4
GR2), in the order the directives were processed (GR1).

### 6.11 Conformance flags

The edition flags (COBOL0903 obsolete, COBOL0902 removed under `--permissive`, COBOL2894 with `--flag-extensions`) are
diagnostics and print inline; the summary adds a table of flagged constructs by edition. (Step 9, the check of what is built:) the
table is not built, and no step of section 11 named it: kb/Work PB5947.

**As built (PB5947, branch `listing-step-pb5947`), with the corrections the code proved:**

- **The flag is a fact on the diagnostic, stamped where it is raised (correction).** "Edition flags (COBOL0903, COBOL0902, COBOL2894)"
  read as a list of codes the listing would keep beside the catalog, and a code is not the fact: a removed construct's verdict
  carries the code its registry row pins (`ConstructDialectStatus.DiagnosticCode`, a pre-band code for some rows), and four front-end
  stages spelled the codes themselves (the reference-format stage's column-7 continuation and word continuation, COPY REPLACING's
  non-pseudo-text operand, the directive-word rule). So the flag is a record, `EditionFlag(Kind, Construct, Description, Edition)`
  (`WiseOwl.Cobol.Editions`, `EditionFlagKind` Obsolete, Removed, Extension), and `Diagnostic.Flag` carries it. It is made in two
  places only: `ConstructRegistry.VerdictAt`, the one verdict of a registry row at an edition (severity, code and flag; `Check` and
  `GateErrorCodeAt` read it, and the four front-end stages report through it, `DiagnosticBag.Report(EditionVerdict, ...)`, in their
  own words), and `EditionContext.Extension`, the §2023:4.2.10 seam. `EditionDiagnostic.Flag` carries it through either sink
  (`EditionContext.Report`, `BagSink.Report`). `EditionFlagDriftTests` (Unit) holds it: every registry row at every edition on both
  axes carries the flag its row names, through `Check` and through the binder's records; every flaggable extension carries its
  row's flag at the edition compiled to; and no source of `src/` outside those funnels names a flag code by literal, constant or
  descriptor (a Roslyn scan, shown red on planted text). A flag added to either register is in the table by construction.
  (Correction, listing step pb5947's review lead R1-2, fixed by kb/Work PB5951's lead sweep:) the scan knew the three band codes
  only, so a removal its row pins a code of its own for escaped it: the MERGE in a SORT or MERGE procedure (row
  `merge-in-sort-merge-proc-removed-2023`, COBOL1572) was reported by hand in `VersionConformancePass` with no flag and was missing
  from the table. It now reports `ConstructRegistry.VerdictAt`'s code, severity and flag, and the scan reads its codes from the rows
  (every row with a removal edition) and its descriptors from `DiagnosticCatalog`, so a row added with a code of its own is scanned
  with no change to the test (`EditionFlagDriftTests.TheScannedCodes_AreTheBandsAndEveryRowsPinnedRemovalCode`;
  `ListingFlaggedConstructsTests.ARemovalWithAPinnedCode_IsInTheTable`, red with the flag withheld). The one code it does not take
  from the rows is the reserved-word code COBOL0901, which the user-word rows pin but the reserved-word list reports for every
  reserved word: a word a later edition reserved, used as a user word under `--permissive`, is a removal that carries no flag yet
  (kb/Work PB5947's dated file `2026-11-19-reserved-word-removal-flag`).
- **"By edition" is the edition of the rule that flags it.** Obsolete: the row's `ObsoleteIn`; removed: its `RemovedIn` (so a
  debugging line at `--std 2023` is a 2014 removal); an extension: the edition compiled to, whose standard does not define it
  (§2023:4.2.10, "to indicate use of a nonstandard extension in a compilation group"). Archaic (§2023:4.2.12) and obsolete
  (§2023:4.2.13) share the kind Obsolete, as they share COBOL0903. A removed construct is counted whatever its severity: strict, it is
  an error and still names the edition that removed it.
- **What is not a flagged construct (decision).** A rule a later edition ADDED that `--permissive` relaxes (`EditionContext.Removed`'s
  documented-dialect-leniency callers, the 2023 EXTERNAL file-connector rules that take the removed-construct severity) reports under
  the rule's own code; it flags no construct of a register, and carries no flag. (PB5949, correcting PB5947's review lead R1-2:) the
  MERGE placement rule is not such a rule: the registry names it (`merge-in-sort-merge-proc-removed-2023`, removed in 2023, pinning
  COBOL1572), so `VersionConformancePass` reports it through that row's `ConstructRegistry.VerdictAt` (code, severity and flag) and
  the table counts it (witness `BoundArmDiagnosticPositionTests.MergeInASortProcedure_At2023_IsAFlaggedRemovedConstruct`). The
  drift test's scan now knows every code a removal row pins (all but the user-defined-word rows' COBOL0901, below), each descriptor
  of the catalog with one of them, and the band codes, so a row-pinned code reported outside the funnel is red (shown on the pre-fix
  report). The FLAG-85 and
  FLAG-NATIVE-ARITHMETIC directives' warnings flag a change of meaning the program asked to see, not a construct by edition; neither
  is in the table. The reserved-word check (COBOL0901) refuses a spelling, not a construct.
- **The model.** `ListingDiagnostic.Flag` (`ListingFlag`, `ListingFlagKind`, mapped by `DiagnosticSectionBuilder.FlagOf`, a switch
  `FlaggedConstructTests.TheFlagKindMap_IsTotal` holds total both ways) and `ListingDocument.FlaggedConstructs`
  (`ListingFlaggedConstruct(Edition, Kind, Construct, Description, Diagnostics)`), built once by
  `DiagnosticSectionBuilder.FlaggedConstructs` from the placed diagnostics, by edition, kind and construct, each naming its
  diagnostics in listing order. It is the diagnostics section's: empty when that section is not asked for. The schema stays 1.0
  (section 7.3: no release has shipped the JSON listing); `flaggedConstructs` is required and `flag` optional.
- **Rendering.** `FlaggedConstructText` holds the words both renderers print: the title `FLAGGED CONSTRUCTS BY EDITION`, the headings,
  `KindWord`, `ConstructText` (the description, then the identifier) and `LinesText`; the text summary ends with the table
  (`FlaggedConstructText.Rows`, after the list; nothing when no diagnostic is a flag), and the HTML page's diagnostics section with
  the same table, each listing line a link to its diagnostic under its line.
- **Witnesses.** `FlaggedConstructTests` (Unit: the grouping and order over hand-built diagnostics, a flag code with no flag left out,
  the kind map, the text rows and their wrap, the page's rows and links), `EditionFlagDriftTests` (Unit, above),
  `JsonListingWriterTests` (the member in `EveryMember`) and `ListingFlaggedConstructsTests` (Conformance: one program at `--std 2023
  --permissive --flag-extensions` writing a construct of each kind in the front end and the binder; the table in the model, the text,
  the JSON and the page equals the one the source and the registers call for, and is exactly the diagnostics of the three codes).
  Each was shown red by mutation: the verdict's flag dropped, the extension's edition changed, a flag code planted in a front-end
  source, the kind map crossed, the binder's error path dropping the flag, the table re-derived from the code, a front-end stage
  dropping its flag.

## 7. The renderers

Each implements `IListingRenderer` and visits the document through `IListingSectionVisitor<T>`, one method per section
type: a section type added without a method in every renderer is a compile error.

(Step 5:) the visitor is not built yet: the JSON writer serializes the record and walks no section, so the text renderer is
the only renderer that walks sections, and an interface with one implementation would guarantee nothing a total switch does
not. The text renderer dispatches on `ListingSection` in one place (`TextListingRenderer.Render` over `SectionOrder`, with
`TitleOf`; the rows of each section other than the source and the summary are a class of their own, `OptionsText`,
`ProgramMapText` and `StatisticsText`, so a section added is a class added), every switch throws on a member it does not know, and `ListingSectionsTests` holds
`SectionOrder`, `TitleOf` and `ListingSections.Spelling` total over the enum: a section added without its place in the text
listing is red at test time, as a format without its renderer is (`ListingOutputTests`). Step 9 builds the visitor with
HTML, the second renderer that walks sections.
(Step 9:) built as `IListingSectionVisitor<T>` (one method per member of `ListingSection`, each returning the renderer's form of
one PRODUCED section) and its one dispatch, `ListingSectionVisitor.Visit`, a switch that throws on a member it does not know and
that `ListingSectionsTests.EverySection_IsVisitedByTheMethodOfItsName` calls for every member: a section added to the enum without
its arm is red at test time, and one added to the interface without its method in a renderer does not compile. The text renderer
visits through it (`TextListingRenderer.Sections`, which replaced its own switch `RowsOf`) and so does the HTML renderer
(`HtmlListingRenderer.Sections`). The order and the titles are one table for both renderers: `TextListingRenderer.SectionOrder`
and `TitleOf` moved to `ListingSections.Order` and `ListingSections.Title` (the old members deleted), so the HTML page presents the
sections in the text listing's order by construction. (Step 9, correction:) `IListingRenderer` is NOT built: the formats' one
table, `ListingOutput` (section 4.2), already dispatches each format to its renderer in one switch that `ListingOutputTests` holds
total, and the JSON writer walks no section, so an interface implemented by three classes would be a second statement of that
switch, kept in step with it by nothing.
(PB5941:) every renderer WRITES as it renders (section 4.2's `ListingOutput.Write`): `TextListingRenderer.Write(document, request,
TextWriter)`, `HtmlListingRenderer.Write(document, request, TextWriter)` and `JsonListingWriter.Write(document, Stream)`. A
visitor method of the HTML renderer appends its section's markup to the writer; one of the text renderer returns its section's rows
lazily, read by the paginator as they are made.

### 7.1 Paginated text (`TextListingRenderer`)

UTF-8, LF line ends, a form feed (U+000C) before every page after the first. `PageLength` (default 60 lines including
the heading; 0 = no pagination, so only '/' and `>>PAGE` produce form feeds) and `PageWidth` (default 132, range 100 to
255; below 132 the margin columns are not printed). `PageComposer` turns rows into pages: a page break before a row with
`PageBreakBefore`, or when the page is full. A row wider than the page wraps onto `+` rows. Sections in order: options,
source with inline diagnostics, program map, data map, cross-reference, statistics and verbs, object-code map, diagnostic
summary; each starts a page.
(Step 5:) a page outside the source section has three heading lines (the identification line, the section's title, a blank
line) and no ruler, and names the first outermost program (section 6.1). A section asked for that could not be produced
prints its title and `Not produced: <reason>` (section 4.3). The pages are numbered through the whole listing.
(PB5941:) `PageComposer.Compose(TextWriter, IEnumerable<ListingRow>, ...)` reads the rows as they are produced and writes each page
when it closes, so it holds one page of rows, never a section (`PageComposerTests.APage_IsWritten_BeforeTheRowsAfterItAreRead`); the
form feed before every page after the listing's first is its own. `ListingRow` is a value (a record struct), and an IL instruction's
row is one string, made indented, cut by `TextListingRenderer.Indented` only when it is wider than the page.

### 7.2 HTML (`HtmlListingRenderer`)

One self-contained file (inline CSS, a small inline script for collapse and filter, no external resource). Every name on
a source line links to its definition, and every definition lists its references as links; each diagnostic is anchored
on its line and listed in a summary panel that links to it; each inclusion is a collapsible block; suppressed and
omitted lines are present but collapsed and greyed; each statement opens its method, IL offset range and (when requested) IL
instructions beside it. Links come from
each line's `ListingLine.Names` (built once from the reference occurrences, section 6.7.1) and the definitions' reference
lists from `ListingCrossReference`; the renderer joins nothing and computes nothing.

(Step 9:) built as `HtmlListingRenderer` (`src/WiseOwl.Cobol.Listing/Html`, namespace `WiseOwl.Cobol.Listing.Html` in the `listing`
layer of `architecture-edges.json`), `--listing-format html`, `BASE.lst.html`. What the page holds:
- The identification (the compiler, the source, the edition, the instant, the outcome and the parse) and every file read with its
  digest; a bar of links to each section held, with the filter (text on a source line; only the lines with diagnostics) and the
  expand / collapse buttons, the page's one script. The bar is sticky, so every link target (a section heading, a line, a data
  item) scrolls to just below it: the style's `scroll-padding-top` reads `--navh`, which the script sets from the bar's rendered
  height whenever it resizes (it wraps to more lines as the window narrows; kb/Work PB5948).
- Each section the document holds, in `ListingSections.Order`, titled `ListingSections.Title`; a section asked for and not produced
  says `Not produced: <reason>` (section 4.3).
- The source section: every line of `ListingDocument.Lines` is a row (`id="L<n>"`) with its number, depth mark, marker
  (`TextListingRenderer.MarkerOf`), file line (`TextListingRenderer.FileLineColumn`: empty where it is the listing number, as
  in the text listing's section 6.2) and text (DATE-COMPILED's replacement in its place, as the text listing prints it);
  a `PageBreakBefore` line carries a rule on screen and `break-before: page` in print. Each inclusion that has lines is a `<details>`
  block (`id="I<n>"`) opened by the text listing's banner (`TextListingRenderer.Banner`), nested as the inclusions nest, collapsed
  when its COPY statement wrote SUPPRESS. Each run of consecutive lines of one inclusion that the text listing does not print
  (`ListingLine.Listed` false; a further DATE-COMPILED comment-entry line; an omitted line) is a collapsed block whose summary says
  how many lines, why, and which (`HtmlListingRenderer.HiddenReason`), its lines greyed: section 7.2's "present but collapsed and
  greyed" read for all three, since an omitted line, which the text listing prints marked 'X' (open question 5), is the kind 7.2
  names. Inside a SUPPRESS block no run is made, the block itself being the collapse. A DISPLAY transfer stands outside any
  collapsed block (§2023:7.3.12.4 GR2: the transfer takes place "irrespective of whether it is suppressed by the LISTING
  directive"), where the text listing prints it whether or not it prints its line: a run of hidden lines ends at a line with a
  transfer, and (review round 1) a SUPPRESS block, which cannot end there, is closed before the transfer from its outermost SUPPRESS
  block in; the next line of the inclusion reopens it as its continuation (its banner and "(continued)", no second id). Under a line: its compiled text when a replacing action changed it ('R' rows), each
  diagnostic placed on it (`id="D<n>"`, `TextListingRenderer.DiagnosticText`) with the character its column names marked in the line's text
  (a supplementary-plane character one position), and each stretch of its object code (`ListingObjectRange`, by its `Line`) as a
  collapsed block naming its method and IL range, its instructions when the object-code section was asked for, with (PB5951) the
  method's exception-handling lines among them as `ObjectCodeText.CodeLines` merges them (section 6.9).
- The links between the model's own records: a margin to its data map entry, a data map or file map entry's line and the entries its
  facts name, the program map's first and last lines, the object-code map's lines and methods, each verb's lines, and the
  diagnostic summary to each diagnostic placed on a line. A link is written only to an element the page holds (a line, when the
  source section is held; an entry, when the data map is), so no link dangles. The facts an entry names are one statement for both
  renderers: `DataMapText.DetailParts` and `FileDetailParts` give each fact's words and each entry it names as a `DataMapPart`; the
  text prints `#<id>`, the page links it.
- (Step 9, review round 1:) ⛔ ONE SET OF WORDS. Every word the page shares with the text listing is the text listing's, read from
  the helper that prints it there, never typed a second time: the identification (`TextListingRenderer.CompilerText`,
  `EditionText`), a section not produced (`ListingSections.NotProduced`), the diagnostic rows (`DiagnosticText`, `CountsText`,
  `PlaceText`), the DISPLAY rows (`DisplayText`), the 'R' rows (`ReplacedMarker`), each table's headings (`ProgramMapText.Headings`,
  `OptionsText.Headings`, `DataMapText.ItemHeadings` and `FileHeadings`, `ObjectCodeText.MethodHeadings` and `RangeHeadings`,
  `StatisticsText.VerbHeadings`), the notes (`ProgramMapText.RecoveredNote`, `StatisticsText.RecoveredNote` and `NotParsedNote`,
  `ObjectCodeText.NoCodeNote`), the data and file map's numbers and words (`DataMapText.NumberText`, `LevelText`,
  `OrganizationText`, `AccessText`, `FlagWords`), the object code's (`ObjectCodeText.Token`, `RangeText`, `StretchText`; the
  disassembler's branch targets spell offsets through the same `Offset`), and the statistics' labels and counts (`StatisticsText`'s
  labels, `CountsText`, `DiagnosticCountsText`). Only the page's own form is its own: the diagnostic summary's and the file table's
  column headings, the DETAILS and ATTRIBUTES columns, the summaries of its collapsed blocks. The Unit assembly's
  `HtmlListingRendererTests.EveryWordThePageSharesWithTheText_IsTheTextListings` renders one document holding every section both
  ways and finds each shared word of the page in the text listing.
- The markup is well-formed XML as well as HTML (every element closed, the five XML characters escaped by the one
  `HtmlListingRenderer.Escape`, the script in a CDATA section), so the tests read the page with `XmlReader` and find each row by the
  renderer's id functions (`LineId`, `DiagnosticId`, `InclusionId`, `DataItemId`, `FileEntryId`, `ProgramId`, `MethodId`,
  `SectionId`).
(PB5949) **Syntax colouring of the source, from the compiler's own reading (owner, 2026-11-19: "it would be great for the HTML
version to syntax color the COBOL source code").** Decided in kb/Work PB5949: no published pattern grammar (the TextMate grammar
VS Code's COBOL extensions use, highlight.js's or Prism's `cobol`): the page loads nothing, a pattern guesses where the compiler
knows, and "the renderer joins nothing and computes nothing". So each `ListingLine` carries `Spans`, `ListingSpan(Column, Length,
Class)` in 1-based character positions of `Text`, in column order and never overlapping, made once by `SyntaxSpanBuilder`
(Compiler, `Listing/`) for the source section only; `ListingSpanClass` is the front end's `SourceTextClass`, mapped by the one
switch `ListingSpanClasses.Of`. The HTML renderer wraps each span in `<span class="s-<class>">` (`HtmlListingRenderer.SpanClass`,
the member's name in lower case) and the style colours every class for a light and a dark page; a diagnostic's marked column nests
inside its span; the text listing ignores the spans; the JSON carries them. As built, with the corrections the code proved:
- **Two readers, not one (correction).** The decision said the classes come "from the token stream"; three kinds of text never reach
  the lexer, so they come from the reference format, which decides them: the fixed-form areas (`ReferenceFormatMap.RegionsAt`: the
  sequence number area, the indicator, the text past margin R), the whole program text of a comment line, a comment-entry line or a
  directive line (`KindAt`), and the inline comment or comment-entry the logical conversion discards from a line (`DiscardedAt`,
  section 4.4.1; §2023:6.5 GR3 replaces an inline comment by spaces before the lexer reads the text). The lexer's own skipped
  comments (a `*>` the text stages leave, COBOL-85's surviving floating indicator) are recorded by the lexer's comment rules
  (`CobolLexer.CommentStarts`, null without a listing).
- **The tokens are the listed pass's FINAL stream, classed by the parse that read them, at the edition in force.**
  `SourceTextClassifier` (Frontend, `Parsing/`; `Frontend.TextClasses`, built only when `RecordsListing`) classes each token after
  every retype (the §2023:8.9 reservation gate's, COBOL-WORDS', the debugging lines', the embedded blocks'): by its TYPE
  (`FixedClassOf`: the literals, the picture character-string, the separators and operators, the figurative ZERO_ARITH, the
  embedded block), refined by the rule that holds it (a `cobolWord` name slot is a user-defined word, a FUNCTION reference's name a
  function-name, a figurative constant's word and its ALL a figurative constant, a `levelNumber` a level-number, a digits
  procedure-name a user-defined word, a comment-entry's tokens comment-entry), and for a word read as a keyword by the edition's
  effective reserved words (`ReservedWordSet.Compose(group's COBOL-WORDS).RejectsAt(word, edition)`): reserved, else a context
  word. So a word §2002:8.9 reserves and the 1985 list does not, written as a name in a 1985 program, is the gate's `IDENTIFIER` in a
  name slot and is a user-defined word, and the same word read as a keyword at 2023 is reserved (`ListingSyntaxSpanTests`).
- **Placed through the one index.** Each classified stretch of the resultant text is cut at line ends and mapped through the line
  map's origin spans (`SourceLineMap.SpansAt`) to each physical line it came from, through `ListingLineIndex.Of(int, SourceOrigin)`
  (a token in the second copy of a copybook colours the second block; a continued literal colours both lines), and kept only where
  the written characters ARE the lexed ones: text a REPLACE or COPY REPLACING placed (a span with a layout of its own) or the text
  stages wrote (the period that closes an emptied comment-entry paragraph) stands at a place whose written text was not lexed.
  The units differ (correction, review round 1): a resultant column and the listed text's index are UTF-16 units, an origin span's
  source column a character position (a tab one, a supplementary-plane character one), and the reference format starts a new span at
  each unit that does not advance the position (the second unit of a surrogate pair, each space of a tab's expansion after the
  first). So a stretch is placed from the expanded index of its span's FIRST unit (`SyntaxSpanBuilder.StretchStart`: the position's
  expanded index plus the units before it on that line that map to the same position) one unit for one, which within a span is exact,
  and the stretches one token makes on one line are one piece. Placing the stretch's END at its start position plus its unit count
  dropped everything after the first supplementary-plane character of a literal.
- **The JSON version stays 1.0 (correction of the note's "a schema minor").** Section 7.3: no release has shipped the JSON listing,
  so `spans` (required, never null) completes 1.0 as every member since step 3 did.
- **Not coloured: a COPY or REPLACE statement.** The text stages read and remove it; the lexer never sees its words and no stage
  records their positions, so the listing has no reading of them but a pattern: kb/Work PB5952.
- **Drift tests.** `SourceTextClassDriftTests` (Unit): every token type of the parser's vocabulary (the lexer's and the rewriters')
  is a fixed class or a word proved a keyword from the vocabulary (shown red on a classifier that forgets STRINGLIT and on a word list
  that forgets DEFINED); `ListingSpanClasses.Of` total both ways; the style colours every class in both schemes.
  `HtmlSyntaxSpanTests` (Unit) and `ListingSyntaxSpanTests` (Conformance) are the witnesses.

(PB5941, correction:) "each statement opens its method, IL offset range and (when requested) IL instructions beside it" and the
object-code section's own instructions printed every instruction TWICE, most of the page's rows (IX113A: 52,026 table rows, of them
its 24,882 instructions twice over). Each stretch's instructions now stand ONCE: under its statement's line when the page holds that line, in the stretch's
collapsed block (`id="C<n>"`, `HtmlListingRenderer.RangeId`, n its 1-based place in `ListingObjectCodeMap.Ranges`), and the
object-code section's entry for the stretch keeps its line, its heading (now a link to that block) and its source text; a stretch
whose line the page does not hold (no source section) shows its instructions in the object-code section. The text listing is
unchanged: it has one copy (the object-code section). `HtmlListingRendererTests.EachInstruction_StandsOnce_UnderItsLine_AndTheObjectCodeSectionLinksToIt`
holds both cases. The page is written as it is rendered (section 4.2): `Escape` writes the runs between the five markup characters
whole, and a number or an IL offset (`IlOffset`, the one spelling of `IL_xxxx`, a value that formats itself) is formatted into the
writer with no string of its own.
(Step 7b, built:) each reference on a source line (`ListingLine.Names`) links its WORD (the span that starts at its column) to the
line its symbol is defined on, or to the symbol's cross-reference row when the page does not hold that line
(`HtmlListingRenderer.NameHref`), so a link holds one span whole and links never nest when one reference is written inside another;
the cross-reference section (`id="Y<n>"`, `HtmlListingRenderer.SymbolId`) lists each symbol's references as links to their lines,
and the PERFORM and CALL graphs link both ends; a statement line's margin links each defining line. The words are the text
listing's (`CrossReferenceText`).

### 7.3 JSON (`JsonListingWriter`) and its schema

- Serialized with a `System.Text.Json` source-generation context (`ListingJsonContext`): camelCase, enums as strings,
  nulls omitted, no reflection at run time. (Step 2:) also `RespectNullableAnnotations` (a null where the model has none is
  refused on reading), indented with LF line ends; enum values are the member names (`"PageEjectComment"`).
  (Step 2, review R1-1 and R1-2:) the context is internal and is only the base of THE ONE CONTRACT, `ListingJson`
  (`ListingJson.Options`, `ListingJson.Document`), through which the writer, the reader and the schema generator all
  serialize. It adds three rules, each decided once: REQUIRED, a property whose value is never null is required
  (`ListingJson.IsRequired`, applied to the metadata by a resolver modifier; `RespectNullableAnnotations` alone refused
  only an explicit null, so a document missing `header`, `lines` or a line's `text` read with null in a non-nullable
  member), and the generator's `required` lists read the same flag; NO NULL ELEMENT, a list of the model never holds null
  (`[null]` in `files` is refused); ENUM VALUES ARE NAMES, one converter (made per model enum by one factory, the only
  reflection, once per enum type) writes and reads a member's name and refuses a number (`"kind": 42` was read as an
  undefined value). The compatibility policy makes an added enum value a MINOR change, so a reader of 1.0 must read a
  1.x document holding a value it does not know: in a document of a LATER minor than `CurrentSchemaVersion` such a name
  reads as `ListingJson.UnknownEnumValue` (-1, which no model enum's member has; the converter refuses an enum that
  defines it, and a document holding it is refused on writing, since its name is not known); in a document of this
  reader's minor or an earlier one the same name is invalid and refused. `JsonListingWriter.Write` writes the document;
  `JsonListingWriter.Read` is the policy's reader: it reads `schemaVersion` first (`ListingJson.MinorOf`, the one parser
  of `1.<minor>`, which refuses `1.x` and `1.01`) to choose the contract of the document's minor, ignores a property it
  does not know, refuses a property written twice, and refuses a document whose `$schema` is not
  `ListingDocument.SchemaUrn` or whose `schemaVersion` is absent or not of major 1.
- The schema is GENERATED from the model types by `JsonSchemaExporter` (.NET 10) with the descriptions taken from the
  model's XML comments, and committed as `src/WiseOwl.Cobol.Listing/Json/listing-v1.schema.json`, an embedded resource.
  A preflight drift test regenerates it and fails on a difference: the schema cannot drift from the writer.
  (Step 2:) `ListingJsonSchema.Generate(XDocument)` takes the XML documentation file the Listing project now builds
  (`GenerateDocumentationFile`; with `TreatWarningsAsErrors` an undocumented public member, every enum member included, is
  a build error there), and refuses a property, type or enum value the file does not describe. A positional record's
  property is described by its `<param>` (the C# compiler writes no `P:` entry for it); an enum's description lists each
  value's meaning after the type's own. The exporter lists every constructor parameter as `required`; the generator
  replaces each object's `required` with the contract's required properties (`ListingJson.IsRequired`: never null, because
  the writer leaves a null out), and states each model enum as its members' names (the exporter cannot describe the
  contract's own converter). So a property ADDED in a later minor must
  be nullable (absent from older documents), which keeps it out of `required` and keeps every older document valid.
  `ListingJsonSchema.Committed()` returns the embedded file. The drift test is
  `tests/WiseOwl.Cobol.Tests.Unit/Listing/ListingJsonSchemaDriftTests.cs` (`WISEOWL_COBOL_UPDATE_LISTING_SCHEMA=1` writes the
  file). It was NOT at first declared a preflight class: `scripts/buildgraph.py`'s preflight rule admits only a class the merge
  group has MEASURED in `tests/_shared/test-weights.json`, and a new class has no measurement. (PB5951:) the weights now carry it
  (0.3 s), so it is declared in the `unit` target's `preflight` of `scripts/build_targets.json`: a stale schema ends the gate RED
  before the population runs (kb/Work PB5925's lead `2026-11-18-schema-drift-preflight`, closed).
- (PB5941:) `JsonListingWriter.Write(document, Stream)` serializes through the same contract straight to the stream and ends it with a
  line feed: the JSON text is never a string (it was serialized to one, then copied once more to append the line feed).
- The document carries `"$schema": "urn:wiseowl:cobol:listing:v1"` and `"schemaVersion": "1.<minor>"`.
- Diagnostics carry their citations as a field. (Step 3:) the field is a LIST, `citations`, of `ListingCitation(Edition,
  Locator, Rule)`: a message may cite several rules (`§2002:7.2.15.2 GR4` and a 1985 rule beside it), and keeping only the
  first would drop the rest. Each is read from the message by the one C# reader of the citation format
  `§<edition>:<locator>[ <rule>]`, `ListingCitation.Scan`, a port of `scripts/spec/citation.py`'s `scan`: a bare `§N` and
  a malformed citation are none. The two readers are pinned by one vectors file,
  `tests/WiseOwl.Cobol.Tests.Unit/Listing/citation-vectors.json` (each text's citations written by hand from the format's
  specification), held by `ListingCitationTests` and by `citation.py --self-test`; open question 3 is decided below.
- (Step 3:) the schema version stays `1.0`. The policy's minor bump is for documents a reader may already hold, and no
  release has shipped the JSON listing (`git tag --contains` of step 2's commit lists none), so steps 3 to 9 complete 1.0
  with the members required where they are never null; 1.0 freezes at the first release that ships the JSON listing, which
  is the moment kb/Work PB5925's lead `2026-11-18-schema-version-bump` names for the surface record. (PB5951:) its members
  (`ListingMethod.regions`, `ListingObjectRange.regions` and `depth`, and the three enums) join 1.0 by the same reasoning: `git tag
  --contains` of step 2's commit still lists no release, so no reader holds a 1.0 document a minor bump would protect, and a 1.1
  would name a version no document of 1.0 was ever written at.

**Versioning and compatibility policy.** The major version changes only when a property is removed, renamed, or changes
meaning; the minor version changes when a property or an enum value is added. Within a major version every addition is
optional, and a reader must ignore properties it does not know and accept an enum value it does not know in a
document of a later minor (`JsonListingWriter.Read` reads it as `ListingJson.UnknownEnumValue`). The compiler writes exactly one major version; a new
major version is announced in the release notes of the release that ships it, with the schema of the old major kept in
the repository for reference. The schema file name carries the major (`listing-v1.schema.json`, `listing-v2...`).

## 8. CLI surface

| Option | Meaning | Default |
|---|---|---|
| `--listing [on\|off]` | Produce a listing (bare `--listing` is on) | on at `--std 85`, off at 2002, 2014, 2023 |
| `--listing-format text\|html\|json` | A format to write; repeatable; implies `--listing on` | text |
| `--listing-output BASE` | Where: `BASE.lst`, `BASE.lst.html`, `BASE.lst.json`; `-` writes the one format requested to standard output | the output assembly's path without extension |
| `--listing-sections LIST` | Comma list of `source, diagnostics, options, programs, statistics, data-map, xref, object-map, object-code`, or `standard` or `all` | `standard` = source, diagnostics, options, programs, statistics |
| `--listing-page-length N` | Lines per page; 0 = unpaginated | 60 |
| `--listing-page-width N` | Columns, 100 to 255 | 132 |

Every value is validated in `Program.BuildParser` the way `--std` is (an argument error, never a COBOL diagnostic).
(Step 2:) `--listing-format` takes one format per occurrence (`AllowMultipleArgumentsPerToken` off, so a following source
path is never taken for a format) and accepts `text` and `json`; `html` is refused until step 9 builds its renderer ((step 9) it
does: `html` is accepted). A
validator on the root command applies `ListingRequest.FormatsProblem` to the two options together (`-` with two formats is
an argument error). With `-`, `RunCompile` prints `ListingOutput.Render` of the one format before the diagnostics; the
`>>DISPLAY` lines that go to standard output follow it there, so a JSON listing on standard output is followed by them
(step 4a sends DISPLAY UPON LISTING into the listing itself, so only UPON CONSOLE or SYSOUT follow it).
(PB5941:) `RunCompile` no longer prints a rendered string: it streams the one format with section 4.2's character entry,
`ListingOutput.Write(format, document, request, Console.Out)`, through the process's one standard output writer. That writer's
encoding is `StandardStreams.EnsureUtf8`'s rule, set from `Main` for every line the command prints (kb/Work PB899, PB3294): on a
redirected handle a BOM-less UTF-8 writer, so standard output receives the bytes the format's file would hold (section 7.1's text
listing is UTF-8, a JSON text exchanged is UTF-8 by RFC 8259 section 8.1, and the page declares `charset="utf-8"`); on a Windows
console the console's wide-character interface, so a listing read there shows its characters as themselves whatever the console's
code page. The `>>DISPLAY` lines that follow are written through the same writer, so they follow the listing in order.
(PB5941 review R1-1, correction:) the first PB5941 shape wrote the listing's UTF-8 bytes to `Console.OpenStandardOutput()`, past that
writer, on the claim that `Console.Out` re-encoded through the console's OEM code page. The claim was wrong (since PB899 a redirected
`Console.Out` is UTF-8), and the bypass was the defect: a Windows console whose standard output encoding `EnsureUtf8` set to UTF-16
read the bytes as UTF-16 and showed CJK characters for the whole listing, measured on a code page 437 pseudoconsole. Two witnesses
hold the path, each run as a process: `ListingCliTests.ToStandardOutput_TheListingIsUtf8_AndTheDisplayLinesFollowIt` (redirected
under a cp437 console: UTF-8 with no byte order mark, the DISPLAY line after the listing; red when the listing is written after the
DISPLAY lines) and `WindowsConsoleDeviceTests.The_cobol_commands_listing_on_the_console_shows_its_characters_as_themselves` (on a cp437
pseudoconsole; red on the bypass).
`Resolve` builds a `ListingRequest` on `CliOptions`; `RunCompile` passes it as `CompilerDriver.Options.Listing`. The
library default is null (no listing), so every caller that builds `Options` itself (the test suites, `check-batch`, an
expansion's own compilation in `ExpansionAssemblies`) does no listing work; the per-edition default is a CLI decision.
`check-batch` never writes a listing. `Options.Listing` decides what the build writes, never what a source means:
`CompileStateDriftTests` classifies it as an output selection.
(Step 5:) `--listing-sections` takes one value, a comma list of section words, `standard` and `all` (each word trimmed, case
ignored, the union of the words), read by `ListingSections.TryParse`; an empty or unknown word is an argument error. It
accepts the sections built so far (`source`, `programs`, `diagnostics`, `options`, `statistics`, (step 6) `data-map`, and (step 8)
`object-map` and `object-code`); (step 7b) `xref` joined with its builder, `CrossReferenceBuilder`, as `html` joins
`--listing-format` with its renderer. Naming it asks for a listing at any edition.

## 9. Performance budget

- **No listing requested: no listing work, by structure.** The front end passes `TextStageTrace.None`;
  `SourceUnitFacts` is not collected; `EditionContext.RecordsSymbols` and `RecordsReferences` are false, so the
  symbol table holds no listing attributes, no use occurrences and no place copy; the PDB is read only when the object-code map is requested; the driver passes a null
  collector and the wrapper returns the body's result. The one fact recorded for every compilation is the line-kind
  array of section 4.4.1, one byte per physical line, written where the converter already decides the kind. The guard
  is structural, never a wall-clock limit: `ListingOffDoesNoWorkDriftTests` compiles with `Options.Listing = null` and
  asserts `Frontend.ListingTrace` is the `TextStageTrace.None` instance, `Frontend.UnitFacts` is null, no
  occurrence was recorded and no `ListingFacts` was built. (Step 1:) the test is in the Unit assembly, because it reads
  the front end the driver builds through the internal `CompilerDriver.NewFrontend`; it also asserts that no file was
  hashed (`CompilationInputs.FileDigests` empty) and the clock was not read, and a control case with a listing shows each
  assertion able to fail. Occurrences join it in step 7b. (Step 1, review R1-3:) a recording call's ARGUMENTS are work too
  (`CopyProcessor.ExpandCopiesOneLevel` computes the COPY statement's last origin, a `LineOrigin` per statement, to pass
  to `BeginInclusion`), so every recording call into `TextStageTrace` is asked only when `TextStageTrace.Records` is true,
  and `TextStageTrace.None`'s recording methods THROW: a call site that forgets the question fails every compilation of a
  COPY statement without a listing, the no-work drift test's own program first.
- **Budgets, measured.** No listing: compile time within the run-to-run noise of the parent commit, and allocated bytes
  per compile no more than the parent's plus the line-kind arrays (one byte per physical line read), (step 7a) the
  name-slot label fields (one reference field per label in each parse-tree context of its rule, section 6.7.1), (step 8) the
  backend's sequence points: a line directive pair per statement and the portable PDB (section 6.9), and (step 6b, the entry ids) the
  symbol table's entries: one entry and one identity-index slot per declared name (section 6.7.1). Standard text
  listing: at most 10 % over the no-listing compile. Every section in every format: at most 25 %.
- **The harness.** `tests/WiseOwl.Cobol.Benchmarks/Compile/CompileBenchmarks.cs` (BenchmarkDotNet, Release, run by
  `dotnet run -c Release --project tests/WiseOwl.Cobol.Benchmarks -- --filter *CompileBenchmarks*`) gains, in step 1,
  `[Params] ListingMode` (`None`, `Text`; later steps add `All`), read by `CompileBenchmarks.Setup` into the
  `CompilerDriver.Options` of `CompileBenchmarks.FullCompile` and `CompileBenchmarks.FrontEndAndBind`, and
  `[MemoryDiagnoser]`; its `Program` corpus gains one large NIST program. The numbers, with the parent commit's `None`
  row beside them, go in the compile-baseline section of `tests/WiseOwl.Cobol.Benchmarks/README.md`. (Step 1:) the
  corpus already holds the three largest NIST sources (IX113A, NC105A, NC218A) and the most COPY-heavy (SM201A), so no
  program is added; `[MemoryDiagnoser]` is already the class's (`BenchmarkConfig`); the parameter is
  `CompileBenchmarks.Listing` of type `CompileBenchmarks.ListingMode`, and its witness checks that `Text` wrote a listing
  of every source line and `None` wrote none.
- **Who measures.** The step that adds a cost measures it and lands the numbers with it: step 1 (`None` against the
  parent commit, and `Text`), step 2 (`Text` with JSON), step 6, step 7 and step 8 (`All`, each with the section it
  adds), step 9 (`All` with HTML). A row over its budget is a kb/Work defect filed before the step lands, never a
  number left in a README. Open question 1 is decided from steps 6 to 9's rows.
  (Step 2:) the parameter gains `TextAndJson`; on a full compile it allocates 3.7-8.3 % over `None` and 0.1-3.2 % over
  `Text` (the README's step-2 table); the host's noise (StdDev 25-40 %) decided no time row, as at step 1 (kb/Work PB5927).
  (Step 3:) step 3 is not in the list above, but it changes the no-listing path (a bind diagnostic is a record, not a string;
  a located `SourceLocation` carries two more ints), so it measures the `None` budget against its parent, and the
  `TextAndJson` row with the diagnostics section; the README's step-3 table holds both. The first run was over the `None`
  budget (IX113A's front end and bind +0.85 MB); the cause was a closure per character of every token in
  `MarginRCutScreen.Warnings`, which step 3's larger `SourceLocation` made larger, and with it removed `None` allocates 5.3 MB
  (7 %) less than the parent on IX113A.
  (Step 4a:) step 4a also touches the no-listing path (the driver carries the LISTING state for PUSH / POP; DISPLAY's LISTING
  destination is a stream of its own; the replacing actions take a null match observer), so it measures `None` against its
  parent: the front end and bind allocate within 0.21 MB of the parent on all four programs (the README's step-4a table).
  (Step 4b:) step 4b is not in the list above and changes no product code (`git diff --stat` of its branch against its
  parent touches no file under `src/`), so no cost moved and nothing was measured; a benchmark run would compare identical
  assemblies.
  (Step 5:) step 5 is not in the list above, but the default request now produces three more sections, so it measures every
  row (the README's step-5 table): the standard text listing is +0.2 to +5.4 % over `None` on a full compile, inside its 10 %
  budget, and step 5's own cost, in the front end and bind with a listing, is +0.38 to +0.65 MB per program over step 4a's
  `Text` rows. It adds no no-listing work (the statements are read only under `Frontend.RecordsListing`, the options rows only
  in the driver's listing branch), and the `None` rows are within 0.23 MB of step 4a's.
  (Step 6:) the `All` row (every section, text and JSON) is +11.1 to +20.2 % over `None` on a full compile, inside its 25 %
  budget; the `None` rows of the front end and bind are within 0.6 MB of the parent commit's, measured beside them on the same
  host (the README's step-6 table). The standard listing finishes only the program entries of the symbol table
  (`EditionContext.RecordsNames`, section 6.6); SM201A's `Text` full compile reads +11.2 % over `None` on the run, from a `None`
  row 2.07 MB under the parent's for the same code, and +5.2 % against the parent's (kb/Work PB5925's lead
  `2026-11-18-full-compile-allocation-noise`).
  (Step 7a:) step 7a is not in the list above, but its labels are fields of the parser's context classes, so they cost every
  parse, with or without a listing. Measured twice on the branch and twice on its parent, alternating (the README's step-7a
  table): the front end and bind with no listing allocate a fixed amount more on both runs, growing with the program (about
  +0.3 MB, 0.4 to 0.5 %, on IX113A's 68.6 MB; +0.2 MB on NC218A), where the parent repeats itself within 0.1 MB. That is the
  label fields, which the budget's letter ("the parent's plus the line-kind arrays") did not name; like the line-kind arrays
  they are a fact the design records for every compilation so the listing can be exact (section 6.7.1's oracle reads each
  slot's class from its label), so the budget now names them. Labels are plain fields, never list labels, for this reason
  (section 6.7.1).
  (Step 8:) step 8 changes every compilation: the Roslyn backend writes a line directive pair per statement and a portable PDB
  (section 6.9), which the design decided for every compilation (a debugger needs them with no listing asked). Measured against the
  parent on a full compile (the benchmark README's step-8 table): `None` allocates +3.4 to +18.4 MB (+8.9 to +17.1 %) more, most of
  it Roslyn's handling of the directives and the rest the PDB (probe runs with each switched off); so the `None` budget now names
  the sequence-point directives and the PDB beside the line-kind arrays and the label fields. The standard text listing is +0.8 to
  +9.7 % over `None`, inside its 10 %. The `All` row, now holding the object code (every statement's IL), is +35.5 to +38.6 % over
  `None`, over its 25 %: kb/Work PB5941, filed before the step lands.
  (Step 6b:) step 6b changes every compilation's name resolution, so EACH slice measures the `None` row of the front end and
  bind against its parent, twice each, alternating, and the last slice measures the whole step against the parent of slice 1.
  Slice 1 (the cd-names and the file-names) is within the parent's run-to-run spread on all four programs, at most 0.23 MB
  above it (the README's step-6b table). Slice 2 (the tables keyed by no user word, the procedure-names) is
  within the parent's run-to-run spread on all four programs, at most 0.07 MB above it (the README's step-6b table, slice 2).
  Slice 3 (the constant-names) moves no measurable allocation: over four runs each, alternating, NC105A's `None` row is 34.90
  to 35.64 MB on the branch and 35.15 to 35.78 MB on the parent, IX113A's is 0.2 to 0.4 MB below the parent's on every run, NC218A's
  straddles it and SM201A's equals it (the README's step-6b table, slice 3).
  Slice 4 (the CONFIGURATION SECTION names) moves no measurable allocation: every branch row of the front end and bind lies inside
  the parent's two runs or within 0.04 MB of them (IX113A `None` 68.92 and 68.88 MB against 68.94 and 68.84; the README's step-6b
  table, slice 4).
  Slice 5 (the REPORT SECTION names): the first build allocated 0.6 to 1.3 MB more on every program's `None` row (IX113A 69.98 and
  69.95 MB against 68.92 and 68.95), because the moved sum-counter arm, which every data-name reference asks first, read the reference's
  word and qualifiers before asking whether any counter carries the name. The arm now asks the table first; re-measured, every
  branch `None` row lies within 0.13 MB of the parent's three runs but one NC218A run whose other rows lie inside the parent's (the
  README's step-6b table, slice 5).
  Slice 6 (the program-prototype-names) moves no measurable allocation: every branch `None` row lies inside the parent's two runs or
  within 0.06 MB of them, IX113A's below both (68.98 and 68.85 MB against 69.05 and 69.09; the README's step-6b table, slice 6).
  The OO lane (the object-class-names, interface-names, method rosters and PROPERTY specifiers, measured against its parent 84097a3e22): every branch `None` row lies inside the parent's two runs or within 0.09 MB of them (IX113A 69.07 and 69.02 MB against 69.05 and 68.99; NC105A 35.20 and 35.04 against 35.11 and 35.06; NC218A 30.94 and 31.08 against 30.96 and 31.56; SM201A 15.41 and 15.21 against 15.35 and 15.40), so the lane moves no measurable allocation; the `None` budget holds (the benchmark README's step-6b table, the OO lane).
  The data-name lane's first slice (the DEBUG-ITEM, type, CAPACITY register and index-names) was over the budget twice: its first build
  by 0.6 MB on IX113A's `None` row, then, after two causes were removed, by 0.14 to 0.26 MB (69.24 and 69.12 MB against 68.98 and
  68.98), the identification object an index-name ask received (an `IdentifiedName` around the boxed candidate set), the qualifier
  words read again at each use, and the CAPACITY register arm reading every data reference's word before asking the table. With those
  removed (section 6.7.1, "Step 6b, data-name lane, first slice"), every `None` row lies at or below the merged main's, measured
  alternating on one host: IX113A 68.46 and 68.40 MB against 69.09 and 69.06, NC105A 34.61 and 34.62 against 35.01 and 34.99 (the
  README's step-6b table, data-name lane).
  The closing slice's first session (the data-name, condition-name and screen-name tables, the file registers, the end markers),
  measured against its parent 6fda8efbac, alternating: IX113A's, NC218A's and SM201A's branch `None` rows lie inside the parent's
  two runs or within 0.04 MB of them (IX113A 68.32 and 68.29 MB against 68.33 and 68.33); NC105A's lie one below both parent runs and
  one 0.55 MB above them (34.60 and 35.26 MB against 34.61 and 34.71), inside the program's run-to-run spread on one tree (slice 3's
  four runs each). The slice moves no measurable allocation; the `None` budget holds (the README's step-6b table, the closing slice).
  Remainder slice 1 (the procedure division's data-names and condition-names through `IdentifyDataName` and `IdentifyConditionName`),
  measured against its parent 8dd07d2eab, alternating: the first build was over the budget on every program (IX113A 68.51 and 68.37 MB
  against 68.23 and 68.32), because the identification step read the head word twice and a qualified reference's qualifier words twice.
  With each word read once (`WrittenQualifiedName.OfRead`, section 6.7.1), every branch `None` row lies below all three parent runs
  (IX113A 67.61 and 67.66 MB against 68.23, 68.32 and 68.30; NC105A 34.21 and 34.22 against 34.59 to 34.66); the `None` budget holds
  (the README's step-6b table, remainder slice 1).
  The entry-id lane (every class the table declares enters its entry where it is declared, section 6.7.1 "Step 6b, the entry ids"),
  measured against its parent 8dd07d2eab (branch, branch, parent, parent; the first parent run failed to build BenchmarkDotNet's
  boilerplate in its two-minute timeout and is not counted): NC105A's `None` rows are 34.81 and 34.82 MB against 34.67 and 34.66,
  IX113A's 68.45 and 68.74 against 68.29 and 68.43, NC218A's 30.75 and 30.78 against 30.64 and 30.72, SM201A's 15.12 and 15.12 against
  15.23 and 15.28. The entries are a fixed cost per declared name, made for every compilation by design ("The table's entries exist for
  every compilation"): an entry and an identity-index slot per name, about 0.15 MB on NC105A, inside its run-to-run spread on one tree
  (0.7 MB, slice 3) and below the parent on SM201A, where a paragraph's identification no longer makes a `ParagraphDefinition` per
  lookup. Like the line-kind arrays and the label fields, they are a fact the design records for every compilation so the listing can
  be exact, so the `None` budget now names them (above). The README's step-6b table, the entry-id lane, holds the four rows of each run.
  Remainder slice 2 (the file clauses' operands kept as a `WrittenDataName` the table resolves, section 6.7.1 "Step 6b, remainder
  slice 2"), measured against its parent 9682d2f88d, four runs each, alternating: the `None` medians agree within 0.08 MB on every
  program (IX113A 67.83 MB on the parent against 67.81 on the branch, NC105A 34.43 against 34.45, NC218A 30.55 against 30.62, SM201A
  15.17 against 15.18), inside each program's run-to-run spread on one tree; the `None` budget holds (the README's step-6b table,
  remainder slice 2).
  Remainder slice 3 (the data-name candidate sets moved into the table behind the written words, section 6.7.1 "Step 6b, remainder
  slice 3"), measured against its parent 0c29eebc42, two runs each, alternating: every branch `None` row lies inside the parent's two
  runs or below them but one, NC218A's 30.50 MB against 30.44 and 30.42 (its other run 30.44); IX113A is 67.66 and 67.60 MB against
  67.78 and 67.81. No measurable allocation moved; the `None` budget holds (the README's step-6b table, remainder slice 3).
  Remainder slice 4 (the data division's declarations moved into the table, section 6.7.1 "Step 6b, remainder slice 4"), measured
  against its parent 75f9b4fd58, alternating: the slice's first build (ab52edae86) put IX113A 0.15 to 0.26 MB over the parent (67.97
  and 68.05 MB against 67.82 and 67.79), the entry order's second dictionary and second read of each of its 1544 data-names, so the
  table keeps each entry's place and the name is read once; then (75936462ff, two runs each, branch, parent, parent, branch) IX113A
  67.70 and 67.71 MB against 67.78 and 67.82, NC105A 34.39 and 34.42 against 34.40 and 34.68, NC218A 30.60 and 30.42 against 31.01
  and 30.42, SM201A 15.18 and 15.15 against 15.00 and 15.16. Every branch `None` row lies inside the parent's two runs or below them
  but SM201A's 15.18, 0.02 MB above; the `None` budget holds (the README's step-6b table, remainder slice 4).
  Remainder slice 5, the last (the group tier's function units and the expansion's names, the TURN file operand, the refused key
  operands, and every data-name's and condition-name's entry made where it is declared, section 6.7.1 "Step 6b, remainder slice 5"),
  measured against its parent 111f7fd0f5 and against the parent of slice 1, bfb8500569, two runs each: IX113A's `None` rows lie 0.29 to
  0.50 MB over the parent's (68.10 and 68.03 MB against 67.74 and 67.63), the entries of its data description entries now made for every
  compilation, which this section's `None` budget names (one entry and one identity-index slot per declared name), so the budget holds;
  NC105A's, NC218A's and SM201A's lie inside or near the parent's runs, within the programs' run-to-run spread. The first build's `All`
  row was 0.84 to 0.94 MB over the parent's on IX113A, because the finish listed every entry an element's table made, the data entries
  now among them, before choosing the few it writes itself; it lists those few only (`SymbolTableFinish.PlacedByARecord`), and `All` is
  0.13 to 0.26 MB over. The whole step against the parent of slice 1: IX113A 68.10 and 68.03 MB against 68.91 and 68.88, SM201A 15.04 and
  15.18 against 15.40 and 15.37, NC105A and NC218A within their spread of it (35.19 and 34.89 against 35.47 and 34.89; 31.04 and 30.44
  against 30.79 and 31.36), the listing work landed between the two trees in the same rows (the README's step-6b table, remainder
  slice 5).
  (Step 7b:) the occurrences touch every compilation only by a test (`EntryStore.RecordsReferences`, read at each identification and
  each statement), so step 7b measures the `None` row of the front end and bind against its parent `0c54b1d651`, two runs each,
  alternating: IX113A 68.17 and 68.20 MB against 68.10 and 68.10, NC105A 34.54 and 35.12 against 34.88 and 35.11, NC218A 30.47 and
  30.48 against 30.41 and 30.43, SM201A 15.19 and 15.05 against 15.04 and 15.01: within the parent's run-to-run spread or at most
  0.15 MB above it, so the `None` budget holds. The `All` row, now holding the cross-reference, is +24.7 % (IX113A), +31.2 % (NC105A),
  +30.8 % (NC218A) and +28.4 % (SM201A) over `None` on a full compile (one run), over its 25 % budget on three programs; the front
  end and bind's `All` grows 8.5 to 12.1 MB over the parent's: kb/Work PB5953, filed before the step lands (the benchmark README's
  step-7b table).
  (Step 9:) the `All` row with HTML, measured against the parent on a full compile (the benchmark README's step-9 table): `All` is
  +53.7 to +60.0 % over `None` (the parent's `All`, without the page, +34.4 to +40.4 %), the page adding +7.3 to +36.1 MB, three
  times its size (IX113A's page is 6.6 MB: the `StringBuilder`, the string `ListingOutput.Render` returns, the encoding of the
  file). Over its 25 % budget, as the row was since step 8: kb/Work PB5941, whose dated file `2026-11-19-step-9-html-row` records
  the rows and the fix they point to (each renderer writing to the file's stream, not to a string; the instructions shown once).
  The `None` rows lie within the full compile's run-to-run spread of the parent's (0.37 to 1.86 MB): step 9 changes nothing a
  compilation without a listing runs.
  (PB5949:) the source's syntax colouring touches every compilation a little (the conversion's per-line record object, the lexer's
  comment action that records nothing without a listing), so it measures the `None` row of the front end and bind against its parent:
  within 0.14 MB on all four programs, so the `None` budget holds. The spans are built for every listing that holds the source
  section: the standard text listing, which prints none, is +7.1 % over `None` on IX113A (+4.8 % over the parent's `Text`), inside its
  10 %; `All` grows +8.2 to +10.1 % over the parent's `All`, further over its 25 % (kb/Work PB5941's dated file
  `2026-11-19-pb5949-spans`). The benchmark README's PB5949 table holds every row.
  (PB5947:) the table of flagged constructs changes every compilation's diagnostic records (a flag reference on each, null unless the
  diagnostic is an edition flag), so it measures every row of the front end and bind against its parent `8dd07d2eab`, once each (the
  benchmark README's PB5947 table): every `None` row lies within 0.07 MB of the parent's, SM201A's 0.26 MB below it, so the `None`
  budget holds; the NIST programs flag nothing at the benchmark's edition, and the listing rows equal the parent's within 0.14 MB but
  SM201A's, which move both ways (+0.40, -0.60, +1.82 MB), that program's noise (kb/Work PB5925's lead
  `2026-11-18-full-compile-allocation-noise`).
  (PB5941:) the `all` row is back inside its 25 % budget: +16.5 to +20.7 % over `None` on a full compile, from +50.2 to +69.3 % on
  the parent `8dd07d2eab`, measured twice on each tree, alternating (the benchmark README's PB5941 table); `all` allocates 15.0 to
  73.8 MB less per compile. The measurement found the mechanism before the change: a Release probe of each part of IX113A's `all`
  listing put the document at +14.6 MB over no listing and the rendering at 99.7 MB, the text listing 33.2 MB for a 2.6 MB file,
  the JSON 27.0 MB for 6.7 MB and the page 39.5 MB for 6.6 MB, each format held whole (a buffer, a string copied from it, the
  string encoded) and the page holding every instruction twice. With each format streamed to its file (section 4.2), the paginator
  holding one page (section 7.1), the instructions once on the page (section 7.2) and the object code decoded into one array per
  method (each stretch a slice of it, `ObjectCodeMapReader`), the probe reads the text listing at 13.3 MB, the JSON at 0.16 MB and
  the page at 8.3 MB (4.9 MB, from 6.6). What remains in the text listing is one string per printed row (an IL instruction's row
  among them), which the paginator's model of a row as a string asks for; the budget holds with it, so the row stays a string.
  The text and JSON listings of the four programs are byte-identical to the parent's but for the instant (a differential of the
  CLI's `all` listing in every format); the page differs only in the stretch blocks' ids and the object-code section's entries.
  (PB5951:) the exception-handling regions, measured against the parent on a full compile, twice on the branch (the benchmark
  README's PB5951 table): the `None` rows move by up to 8.5 MB between two runs of one tree, and PB5951 changes nothing a compilation
  without a listing runs (the clauses are read only for `object-map` or `object-code`). The `All` row is +0.9 to +5.8 % over the
  parent's, inside that spread on three programs (SM201A, with no clause, moves as much as NC218A, with 218 protected blocks) and about
  5 MB beyond it on IX113A (320 protected blocks); the first build wrote a padding attribute on every instruction row of the page, and
  writing it only where a region nests the row took 6.5 MB off IX113A's `All`. `All` is +54.6 to +77.2 % over `None` (the parent's
  +43.4 to +69.8 %), over its 25 % budget as it was before PB5941
  landed (these rows were measured on PB5941's parent): kb/Work PB5941's dated file `2026-11-19-pb5951-rows`.

## 10. Test plan

Expected values come from the standard's rules, from the input files themselves, and from the schema; never from a
captured listing.

1. **1985 minimum (`tests/WiseOwl.Cobol.Tests.Conformance/Listing/Listing1985Tests.cs`).** Each test compiles a small
   program at `--std 85` and asserts on the model and on the rendered text. (Step 1:) the Conformance assembly cannot set
   the internal `IntrinsicBinder.CompileClock`, so the instant is the one the listing records, and the test proves it is
   the compilation's: it lies between two clock readings taken around the compile, and the heading, the DATE-COMPILED row
   and the program's WHEN-COMPILED (§1989:A.2.46.3 RV1's 21 positions, computed from it) all denote it:
   - every physical line of the source and of each copybook appears once per inclusion, its text equal to the file's
     own line (§1985:IV.7.1, §1985:IV.7.4; the expected text is read from the input file);
   - a '/' line is the first source row of a new page (split the text on U+000C) and a '*' line follows its predecessor
     on the same page (§1985:IV.7.2.4);
   - the DATE-COMPILED row reads `DATE-COMPILED. <fixed instant>.`, the heading carries the same instant, and running the
     program, `FUNCTION WHEN-COMPILED` returns that instant (§1985:VI.3.4.4 GR1, §1989:A.2.46.3 RV3);
   - with a nested program, each page's heading names the innermost program of its first line (§1985:VI.3.3.4 GR1,
     §1985:X.3.1.4 GR1);
   - at `--std 85` the CLI writes `<out>.lst` with no option; `--listing off` writes none (step 1: in the Unit assembly's
     `ListingCliTests`, which reaches the internal `Program`);
   - a front-end error and a bind error still list the source and the programs; a source that does not exist lists nothing.
2. **The standard hooks, one class per edition, each citing its own edition's rules.** `Listing2023Tests.cs`: LISTING
   OFF/ON with each directive line listed (§2023:7.3.18.3 GR2-GR5); PAGE ejects and is listed, and under OFF neither
   (section 2.3); COPY SUPPRESS lists the COPY statement but not the library text, and a LISTING OFF inside library text
   does not outlive it (§2023:7.2.3.4 GR4, GR5); `>>DISPLAY` with no UPON and with UPON LISTING lands in the listing under
   OFF, in order, and on standard output when no listing is produced (§2023:7.3.12.4 GR1, GR2, GR5a, GR6).
   `Listing2002Tests.cs`: the same LISTING, PAGE and COPY cases against §2002:7.2.15.2 GR2-GR5, §2002:7.2.16.3 GR2, GR3
   and §2002:7.1.2.3 GR4, GR5, the '/' line (§2002:6.2.6.1) and WHEN-COMPILED (§2002:15.80.2 RV3); they are the witnesses
   of the ten 2002 rows of section 2.2 that closed (the two DOC rows wait on kb/Work PB5930, section 2.2). `Listing2014Tests.cs`: the same against §2014:7.3.16.2 GR2-GR5, §2014:7.3.17.3
   GR2, GR3, §2014:7.2.2.3 GR4, GR5 and §2014:15.92.2 RV3. No DISPLAY case at 2002 or 2014: the directive is new in 2023.
   (Step 4b:) a hook whose rule is the same sentence in the three editions is ONE body, `ListingHookCases` (Conformance,
   parameterized by edition), and each edition's class holds a fact per hook that cites its OWN edition's rule and calls
   that body; `Listing2023Tests`' four shared cases call it too, and it gains the PAGE-with-no-listing, '/' line and
   WHEN-COMPILED cases at §2023:7.3.19.4 GR3, §2023:6.3.7.2 and §2023:15.99.3 RV3. The GR5 case covers both directions (an
   OFF in library text copied under ON, an ON in library text copied under OFF).
3. **Provenance.** A copybook copied twice with different REPLACING: two blocks, each `R` row's compiled text equal to
   the replacement the test wrote; a diagnostic in the second copy is anchored in the second block. Omitted lines of a
   false `>>IF` are marked and listed.
4. **Sections.** Data map: offsets and lengths computed by hand from the PICTURE clauses of the test's own record, every INDEXED BY index and constant present
   (step 6: `ListingDataMapTests`, section 6.6); cross-reference: a fixture with one read, one
   write, one modify (ADD TO) and one definition of the same name yields exactly those four uses at those lines and
   columns, `ADD WS-A` / `TO WS-B` on two lines puts each use on its own line, and `MOVE WS-X (WS-X) TO WS-X` yields two
   reads and one write (section 6.7.1); a line's `Margin` and `Names` equal what the data map and cross-reference say of
   that line, and the text margin and the HTML links render exactly them ((step 7b) `ListingCrossReferenceTests`, Conformance, whose
   program writes the shape as `MOVE T-N (T-N (1)) TO T-N (2)`, since a subscript names an integer item; `ReferenceOccurrenceDriftTests`
   and `CrossReferenceKindDriftTests`, Unit); object-code map: each statement's method and IL
   range equal what the PDB's sequence points say, read independently in the test with `System.Reflection.Metadata` (step 8:
   `ListingObjectCodeTests`, Conformance: each statement of the test's own source at its line and column, the PERFORM's second
   range, a copybook copied twice placed per block, the instructions, the statuses without an assembly; `IlDisassemblerTests`,
   Unit: every operand form against ECMA-335 partition III; `ListingSectionRenderingTests`' object-code rows); the
   exception-handling regions (PB5951: `ExceptionRegionLayoutTests`, Unit, and `ListingExceptionRegionTests`, Conformance, section
   6.9).
5. **Schema.** The generated schema equals the committed file (a preflight class, since a red one means a stale derived
   file); every document the tests produce round-trips through the contract (`ListingJson`) with no unknown property.
   (Step 2:) `ListingJsonSchemaDriftTests` (Unit: the schema equals the committed file and the embedded resource, every
   property at every depth is described, an undocumented member is refused); `JsonListingWriterTests` (Unit: names, enums,
   left-out nulls, the reader's policy) over a hand-built document holding every member; `ListingJsonTests` (Conformance:
   text and JSON written together, the JSON equal to the document built, each line's text and each file's digest from the
   input files, the DATE-COMPILED replacement at the header's instant, a failed compilation's JSON, a refused JSON file).
   The compile helpers of `Listing1985Tests`, `ListingOutcomeTests` and `ListingJsonTests` hold EVERY document they produce
   to `ListingJsonContract.AssertHolds`: valid against the committed schema and a strict round trip
   (`JsonUnmappedMemberHandling.Disallow`). Validity is checked by `tests/_shared/JsonSchemaSubset.cs`, a validator of the
   keywords the exporter writes (`type`, `properties`, `required`, `items`, `enum`, `format`, the annotations) that treats
   any other keyword as an error, so a schema it does not fully understand cannot pass a document by default; the
   repository takes no JSON Schema package for it.
6. **Drift tests that keep the shape.** `WiseOwl.Cobol.Listing` references no solution project (architecture edges);
   every section has a method in `IListingSectionVisitor<T>` (compile-time); the options section names every public
   property of `CompilerDriver.Options`; the no-listing path does no work (section 9); the manual's command-line page
   names every listing option (`UserManualDriftTests`).
   (PB5941:) every format is written by the one call as UTF-8 with no byte order mark to a stream left open
   (`ListingOutputTests.EveryFormat_IsWrittenAsUtf8_WithNoByteOrderMark_ToAStreamLeftOpen`), the paginator writes a page before it
   reads a row after it (`PageComposerTests.APage_IsWritten_BeforeTheRowsAfterItAreRead`), and the page holds each instruction once
   (`HtmlListingRendererTests.EachInstruction_StandsOnce_UnderItsLine_AndTheObjectCodeSectionLinksToIt`). Each was mutated red (the
   paginator materializing its rows, the object-code section printing the instructions of a held line, a byte order mark) and
   restored green. No guard is a wall-clock or allocation limit: the API holds the structure (no renderer returns a string).
   (PB5941 review R1-1:) the command line's `-` is held by the two process witnesses section 8 names, and the character entry by
   `ListingOutputTests.EveryFormat_WrittenAsCharacters_IsWhatItsFileHolds`.
7. **Unit tests** of `ListingVisibility` and `PageComposer` over hand-built line lists (one per rule above), and of
   `ListingLineIndex` over hand-built maps (step 1: `ListingLineIndexTests`, a copybook copied at two depths, framing
   lines, a file no inclusion holds, omission); `PhysicalLineKindDriftTests` (section 4.4.1). (kb/Work PB5950:)
   `TextListingRendererTests.TheFileLineColumn_IsBlankWhereItIsTheListingNumber` reads columns 1-17 of the text rows of a
   source line before an inclusion, the COPY line, a library line and a source line after it (section 6.2, columns 11-16).
7a. **Diagnostics (step 3).** The records and their rendered views, the severity map, the placement by frame over hand-built
   maps, the caret, the wrapping and the summary (Unit); a bind error placed, printed and cited, a diagnostic in the second
   copy of a copybook placed in the second block, and one record per printed diagnostic on a front-end error, a bind error and
   a success with warnings (`ListingDiagnosticsTests`, Conformance); the shared citation vectors (`ListingCitationTests` and
   `citation.py --self-test`). Section 6.3 names each.
7b. **Options, program map, statistics and sections (step 5).** `OptionsSectionDriftTests` (section 6.4); `ListingSectionsTests`
   (Unit: one spelling, title and text place per section, `standard` and `all`, the parser, a request of no section refused);
   `ListingSectionRenderingTests` (Unit: the line counts, the order and pages of the sections, the program map, the options and
   statistics rows, a section not produced); `ListingCliTests.TheListingSections_ReachTheRequest`;
   `ListingSectionsOfACompilationTests` (Conformance: the program map's nesting and lines, the verbs as written with their
   listing lines through two copies of a library text, the line counts by kind and file, the verbs of a failed bind, the options
   given and defaulted, only the sections asked for in the text and the JSON, no diagnostic printed without its section). The
   tests of one section read that section's pages (`ListingPages`), so a section printed beside it moves nothing they check.
   `JsonSchemaSubset` checks `$ref`, which the exporter writes once the statistics hold one type twice.
7c. **Name slots (step 7a).** `GrammarNameSlotDriftTests` (Unit): every rule of `CobolParserCore` has one classified label per
   name slot; the name rules and reference rules are what the ATN computes; the counter over `NameSlotFixture.g4` reports
   exactly its five planted failures, each at its slot (section 6.7.1). Its first commit on the branch is the test red on the
   unlabelled grammar.
7d. **HTML (step 9).** `HtmlListingRendererTests` (Unit, over `JsonListingWriterTests.EveryMember`, the document holding every
   member): the page is self-contained and well-formed, every id unique and every link landing on an element of the page; every line a
   row with its text and its file line (empty where it is the listing number, section 6.2), a page break where the model has one; an inclusion an open block, a suppressed one collapsed, a
   line not listed in a collapsed run inside its inclusion's block; a diagnostic under its line, its column marked (a
   supplementary-plane character one position), the summary linking to it and not linking one placed on no line; the compiled text,
   the DISPLAY transfer and the object code under their line; the margin, the data map's facts, the file map, the object-code map and
   the verbs linked; each section in `ListingSections.Order` and one not produced saying why; markup in the source printed as text.
   `ListingHtmlTests` (Conformance): a compiled program's page, every row's text the input files' lines, the library text in its
   inclusion's block, the margin and the data map entry linked both ways, the DISPLAY statement's object code; a program the binder
   refuses writes its page with the error under its line and its column marked. `ListingSectionsTests.EverySection_IsVisitedByTheMethodOfItsName`
   holds the visitor's dispatch total; `ListingOutputTests` and `ListingCliTests` take `html`. Each was mutated red (the visitor's
   `ObjectCode` arm removed, the `html` spelling removed, the `<` escape removed, the collapsed run disabled) and green restored.
7e. **Flagged constructs (PB5947).** `FlaggedConstructTests` and `EditionFlagDriftTests` (Unit), `ListingFlaggedConstructsTests`
   (Conformance); section 6.11 names each and the mutation that showed it red.
7e. **Syntax colouring (PB5949).** `SourceTextClassDriftTests` and `HtmlSyntaxSpanTests` (Unit), `ListingSyntaxSpanTests`
   (Conformance: the fixed-form areas, comment and comment-entry, a continued literal, each word's, literal's and picture
   character-string's category, the 1985 witness, a library text copied twice and a REPLACING-changed word, the page's markup),
   section 7.2.
8. **Failed compilations.** One test per kind of outcome (section 4.6): the sections present and the
   `ListingSectionStatus` of each absent one; the failed-parse test of section 4.4 (the listed pass). (Step 1:)
   `tests/WiseOwl.Cobol.Tests.Conformance/Listing/ListingOutcomeTests.cs`, beside the 1985 class's failed-compilation
   case, and `ListingOutcomeMapTests` (Unit) for the backend error no source reaches (section 4.6).

The inventory rows the listing makes testable (`1985-GR-VI.3.4.4-1`, `1985-GR-VI.3.3.4-1`, `1985-GR-X.3.1.4-1`,
PB5924's IV.7 rows, the 2023 LISTING/PAGE/COPY rows whose derivations rest on "no listing", and the 2002 rows of section 2.2
that can close, ten of the twelve) take these tests as witnesses in the records change of each step.

## 11. Implementation plan

**The owner ordered, 2026-11-18 (about 00:30 PST): "build the full listing support".** Every step below, 4b included, is
built now, in the order written, and each step lands on main before the next one branches, so each builds on the
landed model and none merges against a sibling. Each step lands on its own and leaves the compiler correct. Step 1 is
the 1985 minimum, so PB5923 closes first.

**Step 1 - the 1985 standard minimum (closes PB5923). BUILT (branch `listing-step-1`; the corrections above marked "step 1").**
- Create `src/WiseOwl.Cobol.Listing/` (`WiseOwl.Cobol.Listing.csproj`, `ListingDocument.cs` with the header, files,
  inclusions, lines and programs, `ListingRequest.cs`, `Text/TextListingRenderer.cs`, `Text/PageComposer.cs`), add it to
  `WiseOwl.Cobol.sln`, reference it from `src/WiseOwl.Cobol.Compiler/WiseOwl.Cobol.Compiler.csproj`.
- Enter the new product project in every record that covers the product projects, so the gate's foreseeable reds are
  removed before it runs: its own lowest layer in `docs/rearchitecture/architecture-edges.json` (`ArchitectureEdgesDriftTests`);
  a new census record under `docs/rearchitecture/evidence/arch-census/` written by `python scripts/arch/census.py`, which
  must cover every `src/WiseOwl.Cobol.*` project (`ArchCensusDriftTests`); a subsystem row in `scripts/arch/r2_subsystems.py`'s
  TABLE owning `src/WiseOwl.Cobol.Listing/**` and `src/WiseOwl.Cobol.Compiler/Listing/**`, with
  `python scripts/arch/r2_subsystems.py --render-design` re-rendering DESIGN-architecture-review.md section 3 and `--check`
  green (`ArchReviewFleetDriftTests`); and whatever public API baseline `python scripts/public_api_baselines.py --check`
  asks of a new product assembly.
- Front end, line kinds (section 4.4.1): `src/WiseOwl.Cobol.Frontend/Preprocessor/PhysicalLineKind.cs` (new, replacing
  `FixedFormConverter.LineKind`), recorded in `ReferenceFormatProcessor.FixedFormConverter.KindOf`,
  `ReferenceFormatProcessor.FixedFormConverter.ConvertLine`, `ReferenceFormatProcessor.FreeFormConverter.Convert` and
  `ReferenceFormatProcessor.NormalizeToFreeFormMapped`; stored by `ReferenceFormatMap.Create` and answered by
  `ReferenceFormatMap.KindAt`.
- Front end, provenance: `src/WiseOwl.Cobol.Frontend/Preprocessor/TextStageTrace.cs` (inclusion events only in this step)
  recorded in `CopyProcessor.ExpandCopiesOneLevel` (from `ResolveOneCopy`'s result) and
  `ConditionalCompilationProcessor.Run.RenderCopybook`; `src/WiseOwl.Cobol.Frontend/Pipeline/SourceUnitFacts.cs` (program
  names, nesting and ranges, the DATE-COMPILED paragraph's range, the comment-entry paragraphs) collected in
  `Frontend.Parse`; `Frontend.RunTextStages` and `Frontend.Preprocess` carry the per-pass trace, and `Frontend.Parse`
  exposes the LISTED pass's trace, format maps and unit facts from the same tuple as `LineMap` (section 4.4).
- `CompilationInputs.ReadCompilationTime` captures once and records `CompilationTime` (section 5).
- Compiler: `src/WiseOwl.Cobol.Compiler/Listing/` (`ListingFacts.cs`, `ListingBuilder.cs`, `ListingLineIndex.cs`,
  `ListingVisibility.cs`, `SourceSectionBuilder.cs`, `ListingWriter.cs`); `CompilerDriver.Options` gains `Listing`,
  `CompilerDriver.Result` gains `Listing`, `CompilerDriver.Compile` becomes the one-exit wrapper (section 4.6).
- CLI: `CliOptions` and `Program.BuildParser` gain `--listing`, `--listing-output`, `--listing-page-length`,
  `--listing-page-width` (text only); `Program.RunCompile` handles `--listing-output -`.
- Tests: `tests/WiseOwl.Cobol.Tests.Conformance/Listing/Listing1985Tests.cs` and `ListingOutcomeTests.cs` (the
  failed-compilation tests of section 10 item 8); in `tests/WiseOwl.Cobol.Tests.Unit/Listing/`: `ListingVisibilityTests.cs`,
  `PageComposerTests.cs`, `TextListingRendererTests.cs`, `ListingLineIndexTests.cs`, `ListingOutcomeMapTests.cs`,
  `PhysicalLineKindDriftTests.cs`, `ListingCliTests.cs` and `ListingOffDoesNoWorkDriftTests.cs` (Unit, because it reads the
  internal `CompilerDriver.NewFrontend`; section 9).
- Measurement (section 9): `CompileBenchmarks` gains `[Params] Listing` of type `CompileBenchmarks.ListingMode` (`None`,
  `Text`); `[MemoryDiagnoser]` is already the class's (`BenchmarkConfig`); the `None` row against the parent commit and the
  `Text` row land in `tests/WiseOwl.Cobol.Benchmarks/README.md`.
- Docs: `docs/manual/user-guide/listing.md` (new), `docs/manual/user-guide/command-line.md`,
  `docs/conformance/7/DOC-A.1-117.md` (a listing at `--std 85` by default, on request otherwise; copied text listed;
  the effect of logical conversion), DESIGN.md section 0.5's row moves to "step 1 built". The 1985 records (the GAP and
  PARTIAL re-adjudications PB5923 names, PB5924's rows) take the step's tests as witnesses.

**Step 2 - JSON and its schema. BUILT (branch `listing-step-2`; the corrections above marked "step 2").**
`src/WiseOwl.Cobol.Listing/Json/JsonListingWriter.cs`, `ListingJson.cs` (the one contract), `ListingJsonContext.cs`,
`ListingJsonSchema.cs`,
`listing-v1.schema.json` (generated), `src/WiseOwl.Cobol.Listing/ListingOutput.cs` (the one table of formats), the schema
drift test (a population class until the merge group measures it, then preflight: section 7.3), `--listing-format`; the
`Text` benchmark row re-measured with JSON (`CompileBenchmarks.ListingMode.TextAndJson`, section 9).

**Step 3 - diagnostics. BUILT (branch `listing-step-3`; the corrections above marked "step 3").** Structured bind
diagnostics: `EditionContext.Records` with `Diagnostics` and `Warnings` as rendered views; `CompilerDriver.Result.Diagnostics`
carries the records; `SourceLocation.ResultantLine` and `Frame` (stamped by `SourceLineMap.Locate`) in place of a resultant
line on `Diagnostic`; `BackendDiagnostic` deleted; `ListingDiagnostic`, `ListingLine.Diagnostics`, inline rows and the
summary; `ListingCitation` and the `citations` field (section 6.3, 7.3).

**Step 4a - the directive hooks, witnessed at 2023. BUILT (branch `listing-step-4a`; the corrections above marked "step 4a").**
`TextStageTrace` gains `ListingDirective`, `PageDirective` and
`Replaced` (recorded in `ConditionalCompilationProcessor.Run.Render`, `CopyProcessor.ApplyReplacements`,
`CopyProcessor.ApplyReplaceStatements`); SUPPRESS on inclusions; `CompileOutputStream.Listing` and
`CompileOutputLine.At` (step 4a: no `At`; the transfer is recorded into the trace with its inclusion, section 4.4),
`ConditionalCompilationProcessor.DisplayDestinations`; `ListingVisibility`'s directive rules;
`DirectiveStateRegistry`'s LISTING row stops saying "NONE HELD"; DOC-A.1-53, DOC-A.1-54, DOC-A.1-117, DOC-A.1-200; the 2023
derivations that rest on "no listing" (`DRV-GR-7.3.18.3-2..5`, `DRV-GR-7.3.19.4-2`, `DRV-GR-7.2.3.4-4/-5`) are retired and
witnessed by `Listing2023Tests`. The code is edition-neutral: the same trace and visibility rules serve 2002 and 2014.

**Step 4b - 2002 and 2014. BUILT (branch `listing-step-4b`; the corrections above marked "step 4b").** `tests/WiseOwl.Cobol.Tests.Conformance/Listing/Listing2002Tests.cs`
and `Listing2014Tests.cs` (section 10 item 2), each citing its own edition's rules (memory rule: each edition stands on
its own text); the records change adjudicates ten of the twelve 2002 rows of section 2.2 (`2002-FMT-7.2.15.1`,
`2002-FMT-7.2.16.1`, `2002-GR-7.1.2.3-4`, `-5`, `2002-GR-7.2.15.2-2` to `-5`, `2002-GR-7.2.16.3-2`, `2002-RV-15.80.2-3`) with
those tests as witnesses; DOC-A.1-117's 2002 and 2014 annex items (B.1 item 90, A.1 item 106) and the stage items (B.1 item
165, A.1 item 182) are documented. (Correction, step 4b review lead R1-1, made in step 6b slice 7:) `2002-DOC-B.1-90` and
`2002-DOC-B.1-165` are not adjudicated: a 2002 DOC row cannot close while kb/Work PB5930 is open (section 2.2), so both stay
GAP, held by that note.

**Step 5 - options, program map, statistics and verbs. BUILT (branch `listing-step-5`; the corrections above marked "step 5").**
`OptionsSectionBuilder` with its drift test; the program map's
names, nesting and ranges and the verb counts from `SourceUnitFacts` (the source); `--listing-sections`. The program
attributes and .NET types, and the statistics' counts of names and uses, join in steps 6 and 7 as views of the symbol
table when it exists.

**Step 6 - the symbol table's entries and attributes, and the data map. BUILT (branch `listing-step-6`; the corrections above
marked "step 6").** The finished table (`SymbolTableFinish.Of`, `SymbolTableWriter`, `SymbolTableSnapshot` in
`WiseOwl.Cobol.Symbols`), deposited by the driver when `EditionContext.RecordsSymbols`; `DataMapBuilder`, `LineAnnotationBuilder`
with `ListingLine.Margin` for definition lines, `DataMapText`, the program attributes, `--listing-sections data-map`; the
benchmark's `All` row. (Step 6, kb/Work PB5932:) the ONE store, below, is split out as step 6b: it is a restructuring of the
whole binder's name resolution (65 tables, 194 members, 124 + 130 + 40 comparison sites, section 6.7.2), which the data map does
not stand on (its entries and attributes are read off the frozen bound model, whose every declaring structure step 6 walks:
section 6.6 "What the table holds") and the occurrences of step 7 do. (Step 6 review round 1, correction:) the first build walked
only the program forests and claimed completeness by construction; the REPORT SECTION, the methods' own data, the type
declarations, the CAPACITY registers and the CDs were missing, and step 6 now walks each. Step 6b lands before step 7 branches.

**Step 6b - the one store of names (kb/Work PB5932). BUILT, landed by name class (the last slice, branch `listing-step-6b-r5`).** The ONE store first: `NameTableOwnershipDriftTests` is written first on
the step's branch and runs red, its failures matching the evidence of section 6.7.2 (a check that could fail); then
`src/WiseOwl.Cobol.Compiler/Binding/Model/SymbolTable.cs` grows into the table (`SymbolEntry`, `CobolName`,
`SymbolTable.Declare`, `SymbolTable.SetAttributes`, `SymbolTable.Identify` as the only lookup, `SymbolTable.Declarations`
returning only `DeclarationFact`), every name table moved into it and made private, every lookup converted to `Identify`
or classified in `string-parameter-members.json` with its reason, every `CobolNames.Same` / `Comparer` / name comparison
moved in or classified in `name-comparisons.json` (seeded from section 6.7.2's evidence), and (step 6) each attribute of
section 6.6 still read off its symbol when the table is finished, never stored a second time; `SymbolTable.Finish()` and the
snapshot handed to the collector. (Step 6b, slice 1, correction, kb/Work PB5932's dated file `2026-11-18-land-by-class`:)
this said the step lands only with `NameTableOwnershipDriftTests` green; it lands IN SLICES, by name class, behind the
temporary shrink-only baseline of section 6.7.1. The first session measured the remainder as the binder's whole name
resolution, which one branch would carry for many sessions against a moving main. Each slice moves one or more whole name
classes (each WITH its resolution code), sends every lookup of them through `Identify`, moves in or classifies every comparison,
is shown behaviour-neutral by `compare_oracle.py` (IDENTICAL) after every source change, measures its `None` row against its parent (section 9), lowers the baseline and its ceiling,
and records what moved and what remains in a dated file beside PB5932. Slice 1 is the drift test, the table's place,
`Identify`, `Declarations`, `CobolName`, the two classifications, the cd-names and the file-names, and the baseline. The LAST
slice empties the baseline and deletes it with the code that reads it (so `NameTableOwnershipDriftTests` is green with no
exceptions, the original landing condition), measures the whole step's no-listing path against the parent of slice 1 (section 9), and flips PB5932
to landed. Step 7b branches after the last slice. Then the data map and file map as views of the
snapshot (`DataMapBuilder`), `LineAnnotationBuilder` with `ListingLine.Margin` for definition lines, and the `All`
benchmark row. `Identify` records nothing yet; the table's entries and attributes are complete. (Step 6:) the data map, the
`LineAnnotationBuilder` margin and the `All` row are built in step 6; step 6b moves the finishing into the grown table, whose
`Declare` then replaces `SymbolTableWriter.Declare`, and the data map is unchanged. (Step 6b, slice 1, branch `listing-step-6b`:)
the drift test was written first and measured red (section 6.7.1, "Step 6b, as built so far"); the table moved to the binder layer
with `Identify`, `Declarations` (`DeclarationFact`) and `CobolName`; the cd-names and the file-names are its own, with every
reference to them through `Identify` and each change shown behaviour-neutral by `compare_oracle.py` (IDENTICAL over 22641 cases).
The estimate was wrong in size, not in shape (rule 5): each name class moves WITH its resolution code, because a private table can
be read only by the table's own members, so the data-name class (ForestSlice's maps, the method overlay, the index-name registry,
the inherited type declarations and the lapsed type members, with `NameResolution`, `ReferenceResolver.Identify` and
`DataBinder`'s candidate walks) moves as one unit; kb/Work PB5932's dated file `2026-11-18-step-6b-progress` holds the measured
remainder by class. (Step 6b, slice 2, branch `listing-step-6b-2`:) the tables keyed by no COBOL user word took keys of their own
and the procedure-names moved into the table with their resolution (section 6.7.1, "Step 6b, slice 2"), each change shown
behaviour-neutral by `compare_oracle.py`; the baseline is 308, and kb/Work PB5932's dated file `2026-11-19-step-6b-slice-2` holds
what remains by class. (Step 6b, slice 3, branch `listing-step-6b-3`:) the constant-names moved into the table with their
resolution, the pre-bind reading included (section 6.7.1, "Step 6b, slice 3"), each change shown behaviour-neutral by
`compare_oracle.py`; the baseline is 294, and kb/Work PB5932's dated file `2026-11-19-step-6b-slice-3` holds what remains by
class. (Step 6b, slice 4, branch `listing-step-6b-4`:) the CONFIGURATION SECTION names moved into the table with their resolution
(section 6.7.1, "Step 6b, slice 4"), `compare_oracle.py` IDENTICAL after the move; the baseline is 279, and kb/Work PB5932's dated
file `2026-11-19-step-6b-slice-4` holds what remains by class. (Step 6b, slice 5, branch `listing-step-6b-5`:) the REPORT SECTION
names moved into the table with their resolution (section 6.7.1, "Step 6b, slice 5"), `compare_oracle.py` IDENTICAL after the move;
the baseline is 234, and kb/Work PB5932's dated file `2026-11-19-step-6b-slice-5` holds what remains by class: the data-name class
(which takes with it the two text-held report operands), the program, function and repository names, and the OO names. (Step 6b, slice 6, branch `listing-step-6b-6`:) the program-prototype-names moved into the table with their resolution (section
6.7.1, "Step 6b, slice 6"), `compare_oracle.py` IDENTICAL after the move; the baseline is 223, and kb/Work PB5932's dated file
`2026-11-19-step-6b-slice-6` holds what remains by class. The progress table's program, function and repository row proved to be two
classes (§2023:8.3.2.2 lists program-prototype-name and function-prototype-name apart): what remains is the data-name class, the
function-prototype-names and the OO names. (Step 6b, slice 7, branch `listing-step-6b-7`:) the
function-prototype-names moved into the table with their resolution and the containing-definition set (section 6.7.1, "Step 6b, slice
7"), `compare_oracle.py` IDENTICAL after the move; the baseline is 201, and kb/Work PB5932's dated file `2026-11-19-step-6b-slice-7`
holds what remains by class: the data-name class and the OO names with the compilation group's tier. Slice 7 also fixed the open
review leads of listing steps 2 to 7 (kb/Work PB5932's dated file `2026-11-19-review-leads-slices-2-and-3` and the review findings of
each step), whose corrections are marked "step 6b slice 7" in sections 6.3, 6.7.1 and 11.
(Step 6b, the OO names, branch `listing-step-6b-oo`, a lane beside the data-name lane, controller 2026-11-19:) the object-class-names
and interface-names moved into the symbol table's compilation-group tier, `SymbolTable.ClassNames`, with their resolution, the method
rosters are keyed by the externalized method name, and the PROPERTY specifiers are the element tier's (section 6.7.1, "Step 6b, the
OO names"), each change shown behaviour-neutral by `compare_oracle.py`; kb/Work PB5932's dated file `2026-11-19-step-6b-oo` holds what
the OO class leaves (the parameterized definitions' expansion) and the section 9 rows. Lanes run in parallel by class; none empties
the baseline, which a closing slice deletes after every lane has landed. (Step 6b, the closing slice's first session, branch
`listing-step-6b-close`, correction:) the slice dispatched to close the step moved the data-name, condition-name and screen-name
tables, the USE and WHEN file registers and the end markers (section 6.7.1, "Step 6b, the closing slice's first session"), each change
shown behaviour-neutral by `compare_oracle.py`, and lowered the baseline 122 -> 86; it did not empty it. What remains is the
data-names' and condition-names' resolution by text (22 lookups, about 100 call sites), their declaration sites, the census, the group
tier's function units and the parameterized expansion, so the step lands one more slice at least, by the same rule: the slice that
empties the baseline deletes it with the code that reads it, measures the whole step against the parent of slice 1, and flips PB5932;
kb/Work PB5932's dated file `2026-11-19-step-6b-close` holds the remainder by class. (Step 6b, remainder slice 1, branch
`listing-step-6b-r1`:) the procedure division's data-name and condition-name references whose item is used are identified by
`SymbolTable.IdentifyDataName` and `IdentifyConditionName` (section 6.7.1, "Step 6b, remainder slice 1"), eleven text lookups
deleted, `compare_oracle.py` IDENTICAL; the baseline is 75, and kb/Work PB5932's dated files `2026-11-19-step-6b-r1` and
`2026-11-19-listing-step-6b-r1-review-r1` hold what remains in the remainder's order: the procedure division's word probes and
ambiguity counts with the data division's clause operands (B), the declarations, the census, the group tier's units and the parameterized expansion (C), and every
class's entry id (D). (Step 6b, the entry ids, branch `listing-step-6b-ids`, a
lane beside the data-name lane, controller 2026-11-19:) the classes already in the table make their entries where they are declared and
every identification carries its id (section 6.7.1, "Step 6b, the entry ids"); the lane moves no table, so the baseline is unchanged, and
kb/Work PB5932's dated file `2026-11-19-step-6b-entry-ids` holds what it built and measured: with remainder slice 1 merged the baseline is 75, and of D
the data-names' and condition-names' entries remain, which their declarations (C) make. (Step 6b, remainder slice 2, branch
`listing-step-6b-r2`:) part B's first half, the file clauses' operands kept as a `WrittenDataName` the table resolves (correction: the
model holds no parse tree, so the operand is the written words, not a `WrittenQualifiedName`), the record-key-names read off their file,
`compare_oracle.py` IDENTICAL; the baseline is 63, and kb/Work PB5932's dated file `2026-11-19-step-6b-r2` holds what remains: part B's
second half (the data-entry clause operands, the qualification walks moved into the table, the procedure division's probes and
counts), then C and D. (Step 6b, remainder slice 3, branch `listing-step-6b-r3`:) part B's second half: the data-name candidate
sets, the qualifier walk and the raw lookups moved into the table, private, every caller handing the written words (the procedure
division a `WrittenQualifiedName`, the data-entry and report clause operands a `WrittenDataName`), the unqualified and qualified
data-name answered by one rule, the REDEFINES target and the layout's overlay read off their carriers (section 6.7.1, "Step 6b,
remainder slice 3"), `compare_oracle.py` IDENTICAL; the baseline is 52 (1 table, 0 lookups, 51 sites), and kb/Work PB5932's dated
file `2026-11-20-step-6b-r3` holds what remains: C (the declarations, the census, the group tier's units, the parameterized
expansion) and D (the data-names' and condition-names' entries). (Step 6b, remainder slice 4, branch `listing-step-6b-r4`:) part C's
first half: the data division's declarations into the table (the last rule (1) table, the entry order's names; the census, nested in
the table; the declaration questions of the data division's own entries, the procedure division header's operands and the
CORRESPONDING pairing by the table's key), the externalized-name uniqueness classified EXTERNAL (section 6.7.1, "Step 6b, remainder
slice 4"), `compare_oracle.py` IDENTICAL; the baseline is 24 (0 tables, 0 lookups, 24 sites), and kb/Work PB5932's dated file
`2026-11-20-step-6b-r4` holds what remains: the group tier's units, the parameterized expansion, the TURN file operand, the refused
record-key set, then D. (Step 6b, remainder slice 5, branch `listing-step-6b-r5`, the last slice:) the rest of C and D: the group's
FUNCTION-ID units by word in the group tier (`SymbolTable.UnitNames`), the parameterized expansion's names in the group tier, a table
of their own (`SymbolTable.ExpansionNames`: the open tier question, decided), a `>>TURN` file-name matched against the statement's
identified file, a refused record-key operand marked on itself, and every data-name's and condition-name's entry made where it is
declared (section 6.7.1, "Step 6b, remainder slice 5"); `compare_oracle.py` IDENTICAL, also with the table finished for every
compilation. The baseline is empty, so the slice deleted it with the code that read it: `NameTableOwnershipDriftTests` is the five
rules, green with no exceptions, the design condition. It measured the whole step's `None` row against the parent of slice 1
(section 9) and flipped kb/Work PB5932 to landed; kb/Work PB5932's dated file `2026-11-20-step-6b-r5` holds it.

**Step 7 - occurrences and the cross-reference.** (Step 7a, kb/Work PB5932's dated file `2026-11-18-step-7-split`:) step 7
was dispatched while step 6b, which it stands on ("`Identify` appends each `SymbolOccurrence`" needs the one `Identify`), had
not been built, so it is split like step 6. **Step 7a - the grammar's name slots. BUILT (branch `listing-step-7`; the
corrections above marked "step 7a").** `GrammarNameSlotDriftTests` written first and red, with the `NameSlotFixture.g4`
self-test, then the labels on every name slot of `CobolParserCore.g4` and the `Core/*.g4` fragments (452 slots, section
6.7.1), `GrammarSource` for the tests that read the grammar's text, and the diagram generator's cleaning
(`scripts/gen-grammar-diagrams.ps1`) taught to drop a label. It changes no parse and depends on nothing step 6b changes.
**Step 7b - the occurrences and the cross-reference, after step 6b lands. BUILT (branch `listing-step-7b-2`; the corrections above
marked "step 7b", section 6.7.1 "Step 7b, as built").** `ReferenceOccurrenceDriftTests`; `Identify`
appends each `SymbolOccurrence`
to its entry (Identify's register arms included), and so does the group tier's recording point, `SymbolTable.ClassNames.Lookup`,
for each written object-class-name and interface-name (section 6.7.1, "The compilation group's tier"), `PlaceBuilder.PlaceForItem` / `PlaceBuilder.WholeTable` take
`IdentifiedName`, `StatementBinder.BindStatement` classifies the occurrences since its mark through the new
`BoundStores.StorePositionsOf`, all gated by `EditionContext.RecordsReferences`. Step 7b lands with the grammar test,
the completeness oracle and the pairing test green. Then `CrossReferenceBuilder` (a view of the entries and their
occurrences), the PERFORM and CALL graphs, `LineAnnotationBuilder`'s `ListingLine.Names` and statement-line `Margin`;
the `All` row. (Step 7b, dispatched early, branch `listing-step-7b`, correction: kb/Work PB5925's dated file
`2026-11-19-step-7b-not-buildable-yet`:) step 7b was dispatched after the closing slice's FIRST session, with step 6b's baseline at
86 and no data-name or condition-name arm in `Identify`, and the code proved a gap in the design besides: no entry exists while the
bind runs (step 6b built each class's declaration as a table of symbols and no `SymbolId`; the finish makes the entries from the
frozen model), so "`Identify` appends each `SymbolOccurrence` to its entry" has no entry to append to, and for most classes the
identified answer is not what the finish declares (a constant's `ConstantDef` against the finish's own constant symbol, an
ordering-name's string, a paragraph's per-lookup `ParagraphDefinition(Pc)`, a report group's candidates), and several classes
(procedure, CONFIGURATION SECTION, prototype, DEBUG-ITEM and OO names) have no entry at all. Step 7b therefore waits for step 6b's
remaining slices, which give each declared name its entry id as its declaration moves (section 6.7.1's `SymbolId`); step 7b then joins
every occurrence to its entry by that id, never by the answer. (Step 6b, the entry ids, branch `listing-step-6b-ids`:) every class whose
declaration was the table's then gives its entry id (section 6.7.1, "Step 6b, the entry ids"), and the finish reads the entries the bind
made; the data-names and condition-names give theirs when their declarations move, in the shape that lane adopts unchanged. (Step 6b,
remainder slice 5:) they do (`SymbolTable.EnterDataItem`, `EnterConditionName`, and `DataEntryOf` for a candidate's entry), so step 7b
finds an entry for every class, and step 6b, which it stands on, is built. (Step 9:) when step 7b adds `ListingLine.Names` and
`ListingCrossReference` to the model, it also adds their HTML (section 7.2): each name on a source line linked to its definition,
each definition's references listed as links, and the cross-reference section's method in `IListingSectionVisitor<T>`, which the
text and HTML renderers then must both implement to compile.

**Step 8 - COBOL to object-code map. BUILT (branch `listing-step-8`; the corrections above marked "step 8").** Inside the Roslyn
backend, behind the seam: the `#line` directives written by `StatementEmitter.EmitStatement` through `CodeWriter.MapStatement` and the
portable PDB (`RoslynBackend.Compile`'s `EmitOptions`), from `BoundStatement.Span`. In the listing:
`ObjectCodeMapReader`, `IlDisassembler`, `MetadataNames`, `ObjectCodeMapBuilder`, `ObjectCodeText`, the sections `object-map` and
`object-code`; `SequencePointCoverageDriftTests`, `ListingBackendNeutralityDriftTests`; backend diagnostics placed through their
COBOL positions; the `All` row.

**Step 9 - HTML. BUILT (branch `listing-step-9`; the corrections above marked "step 9").** `HtmlListingRenderer`, reading
`ListingLine.Names` and the cross-reference only; the `All` row with HTML. (Step 9:) built: `HtmlListingRenderer` (section 7.2),
`ListingFormats.Html`, `IListingSectionVisitor<T>` and `ListingSectionVisitor.Visit` with both renderers visiting through them
(section 7), `ListingSections.Order` and `Title` in place of the text renderer's own, `DataMapText.DetailParts` and
`FileDetailParts`; `--listing-format html`; the benchmark's `All` mode writes every format (section 9). The page reads what the
model holds; `ListingLine.Names` and the cross-reference do not exist yet, so the name links join with them in step 7b.

**PB5947 - the table of flagged constructs by edition. BUILT (branch `listing-step-pb5947`; section 6.11, "As built").** The edition
flag as a fact on the diagnostic (`EditionFlag`, `Diagnostic.Flag`, made by `ConstructRegistry.VerdictAt` and
`EditionContext.Extension`), `ListingDiagnostic.Flag` and `ListingDocument.FlaggedConstructs`, the table in the text summary, the
JSON and the HTML page; `EditionFlagDriftTests`, `FlaggedConstructTests`, `ListingFlaggedConstructsTests`.

**PB5949 - syntax colouring of the source. BUILT (branch `listing-step-pb5949`; section 7.2, "(PB5949)").** `SourceTextClass`,
`SourceTextSpan`, `SourceTextClassifier`, `Frontend.TextClasses`, `CobolLexer.CommentStarts`, `PhysicalLineRecord`,
`ReferenceFormatMap.RegionsAt` and `DiscardedAt`, `SourceLineMap.SpansAt`, `SyntaxSpanBuilder`, `ListingSpanClasses`, `ListingSpan`,
`ListingSpanClass`, `ListingLine.Spans`, the page's span markup and colours, the schema; the drift tests and witnesses of section 10
item 7e; the measurement in section 9.

**The file-line column (kb/Work PB5950). BUILT (branch `listing-step-pb5950`).** The owner found every source line's number
printed twice: until a program's first inclusion the listing number and the file line are the same number. Section 6.2's
columns 11-16 had specified the file line on every row; the correction prints it only where it is not the listing number,
decided once by `TextListingRenderer.FileLineColumn` and asked by both the text row and the HTML row (section 7.2). The model
and the JSON listing keep `ListingLine.FileLine` on every line. Witnesses: `TextListingRendererTests.TheFileLineColumn_IsBlankWhereItIsTheListingNumber`
and `HtmlListingRendererTests.EveryLine_IsARow_WithItsText` (section 10 items 7 and 7d). The diagnostic summary's
`file(line,column)` (`TextListingRenderer.PlaceText`) keeps its line: it is the compiler's diagnostic location, read whole
by an editor, not a column of the source row.

**PB5951 - the object code's exception-handling regions. BUILT (branch `listing-step-pb5951`; the corrections above marked
"PB5951").** The owner's finding of 2026-11-19 (section 6.9, "Exception-handling regions"): `ObjectCodeMapReader` reads each body's
clauses beside its IL into `ListingMethod.Regions`; `ExceptionRegionLayout` lays them out as ILDASM's lines and places each in one
stretch (`ListingObjectRange.Regions`, `Depth`); `ObjectCodeText.CodeLines` and `RegionText` print them in the text and HTML object
code; the JSON schema regenerated within 1.0 (section 7.3) and its drift test declared a preflight class; `ExceptionRegionLayoutTests`
and `ListingExceptionRegionTests`; the `All` row measured (section 9).

**What is not built (checked at step 9 against every section of this document).** Each part below is a kb/Work item, never a silent
gap:
- (Step 7b:) section 9's `All` budget, over 25 % with the cross-reference: kb/Work PB5953.
- (Step 7b:) the uses and references step 7b left, each a lead of kb/Work PB5925's dated file `2026-11-20-step-7b-2-built` (section
  6.7.1, "Step 7b, as built"): an index-name's store, the special registers' entries, a whole table's store, a literal CALL target
  in the CALL graph, and the statistics' counts of names and uses (section 4.1's table).
- Section 2.2's two 2002 DOC rows (`2002-DOC-B.1-90`, `-165`): documented in DOC-A.1-117 and DOC-A.1-200 but held at GAP by kb/Work
  PB5930.
- (PB5941:) section 9's `All` budget, over 25 % since step 8, is met (measured at the merged tree of PB5941 and PB5951, `0c29eebc42`,
  two runs: `All` is +13.8 to +23.5 % over `None` on a full compile, the benchmark README's "The `All` row at the merged tree"; review
  lead pb5951 R4-1): every format streams to its file and the page shows each
  instruction once (section 9, PB5941; kb/Work PB5941 landed with this branch, `listing-step-pb5941`).
- Section 7's `IListingRenderer`: not built by decision (section 7, step 9 correction).
- Section 7.2's colouring of a COPY or REPLACE statement's words, which the text stages remove before the lexer reads the text:
  kb/Work PB5952.

## 12. Documents and records that change with the listing

`docs/conformance/7/DOC-A.1-117.md` ("The compiler NEVER produces a source listing" is false from step 1);
DOC-A.1-200 (the stage, step 4a; its 2002 and 2014 items, step 4b); DOC-A.1-53 and DOC-A.1-54 (where DISPLAY UPON LISTING goes, step 4a); the inventory rows
and derivations PB5923 and section 10 name; `docs/manual/user-guide/command-line.md` and the new `listing.md`;
`DirectiveStateRegistry` (step 4a); this document's status line at each step.

## 13. Open questions, each with the recommendation taken

1. **Which sections by default?** Recommendation: `standard` (source, diagnostics, options, programs, statistics), the
   rest with `--listing-sections`. IBM's defaults are similar (NOMAP NOXREF NOLIST) and the full set costs a bound-tree
   walk. Decided again from the benchmark rows steps 6 to 9 land (section 9). (Step 9:) decided: `standard` stays the default. The
   rows of steps 6, 8 and 9 put `all` at 35 % and more over no listing (kb/Work PB5941), and `standard` inside its 10 %.
   (PB5941:) the decision stands: with every format streamed `all` is +16.5 to +20.7 % (section 9), inside its own budget, but the
   object code is IBM's LIST, long and wanted on request, and `standard` is what a 1985 listing must hold.
2. **Should JSON contain every section regardless of `--listing-sections`?** Recommendation: no; one request, one
   document, so the three formats always agree.
3. **Diagnostics carrying their citation as data.** DECIDED in step 3: NO second copy; the message is the one source of a
   diagnostic's citations and `ListingCitation.Scan` the one reader of them. The recommendation rested on a reading of
   `EditionInfo.Cite` the code refutes: it chooses among whole TEXTS per edition (`Cite(y1985, y2002, y2023)`, 38 calls), not a
   citation value, and the compiler's messages write their citations inline in about 9,000 string literals of the Compiler,
   Frontend and Editions assemblies (a count of literals holding `§<edition>:`). A structured citation carried beside the text
   would be a second statement of the same fact, kept in step with the first by nothing, and the drift test counting the rest
   to zero would be a campaign over every message with no reader's benefit the parse does not already give. Every citation in
   a message is already checked by `audit_code_citations.py`, so the text the reader parses is a valid citation.
4. **COBOL source-level debugging (`#line` and a portable PDB).** DECIDED by the owner's ruling of 2026-11-18: both
   backends emit sequence points at COBOL statement granularity and a portable PDB, because the object-code map reads
   them (section 6.9). A .NET debugger steps through COBOL source as a consequence. (Step 8:) built for the Roslyn backend.
5. **Omitted lines shown or hidden.** Recommendation: shown and marked 'X' (IBM's default NOSKIPSRC); add
   `--listing-omitted hide` only if a user asks.
6. **Publishing the schema at a URL.** Publishing is the owner's (CLAUDE.md, "Still the owner's"). Recommendation: the URN
   identifier now; ask the owner when the product's documentation site is updated for the listing.
7. **A listing for an expansion's own compilation** (`ExpansionAssemblies`). Recommendation: none; its text is the
   repository's skeleton, and its diagnostics already report at the EXPANDS entry of the user's listing.
