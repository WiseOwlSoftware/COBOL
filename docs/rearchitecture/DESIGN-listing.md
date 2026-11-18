# DESIGN - the source listing

> **Status: DESIGN, APPROVED BY THE CODEBASE OWNER (Claude, CLAUDE.md "Ownership"; decision kb/Work PB5925). NOT YET BUILT.**
> Architecture review round 1 (2026-11-17, `review-w20262103-listing.jsonl`): R1-1 to R1-4 and the leads R1-6 to R1-9 are
> answered in sections 2.2, 3, 4.2, 4.4, 4.4.1, 4.6, 6.7.1, 9 and 11; R1-5 is a finding against the citation audit.
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
| Object code | LIST / OFFSET (assembler) | The COBOL statement to emitted C# line map, both directions |
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
| L-1 | Listings outside the standard's scope: "The form and content of listings produced during compilation, if any"; so are (2023) "The form or content of error, flagging, or warning messages" | §2002:1 | §2014:1 | §2023:1 | The form is ours; this document is its definition |
| L-2 | "Whether the compiler produces a source listing is implementor defined" | §2002:7.2.15.2 GR1 | §2014:7.3.16.2 GR1 | §2023:7.3.18.3 GR1 | Produced on request (`--listing`); documented in DOC-A.1-117 |
| L-3 | Annex item: "Listings (whether and when produced by the compiler, effect of logical conversion). This item is required" (2002 item 90, 2014 item 106, 2023 item 117) | §2002:B.1 | §2014:A.1 | §2023:A.1 | DOC-A.1-117 rewritten (section 12) |
| L-4 | Logical conversion: "The implementor shall define the effect on the source listing, if any, of logical conversion" | §2002:6.4 | §2014:6.5 | §2023:6.5 | The listing shows the physical lines (before conversion); continuation lines are listed as written; DOC-A.1-117 says so |
| L-5 | Stage: "The implementor defines the stage during which actions associated with listings, if any, take place"; the annex item of the stage (2002 item 165, 2014 item 182, 2023 item 200) "is conditioned on the implementor producing listings" | §2002:7, §2002:B.1 | §2014:7.1, §2014:A.1 | §2023:7.1, §2023:A.1 | The text-manipulation stage records the actions; the listing applies them after compilation (section 4.4); DOC-A.1-200 |
| L-6 | Comment lines: '/' "causes page ejection followed by printing of the comment line"; '*' at the next line position | §2002:6.2.6.1 | §2014:6.3.6.1 | §2023:6.3.7.2 | As 85-3 |
| L-7 | COPY SUPPRESS: library text not listed; "If a listing is being produced, the COPY statement itself is listed" | §2002:7.1.2.3 GR4 | §2014:7.2.2.3 GR4 | §2023:7.2.3.4 GR4 | `ListingInclusion.Suppress`; the visibility pass (section 4.5) |
| L-8 | After COPY, "the LISTING directive that is in effect for the COPY statement itself is considered to be in effect" | §2002:7.1.2.3 GR5 | §2014:7.2.2.3 GR5 | §2023:7.2.3.4 GR5 | The visibility pass restores the state at the end of each inclusion |
| L-9 | The NOTE: "It is recommended that the implementor provide a listing of the original compilation group" (2023: "The implementor can provide a listing of the original compilation group") | §2002:7.2.15 | §2014:7.3.16 | §2023:7.3.18.1 | The source section IS the original compilation group; the manipulated text is shown as annotations |
| L-10 | "The default LISTING directive is '>>LISTING ON'" | §2002:7.2.15.2 GR2 | §2014:7.3.16.2 GR2 | §2023:7.3.18.3 GR2 | Visibility starts ON |
| L-11 | "Each LISTING directive shall be listed, even if the listing is being suppressed" (2023 adds "by a LISTING directive") | §2002:7.2.15.2 GR3 | §2014:7.3.16.2 GR3 | §2023:7.3.18.3 GR3 | A LISTING directive line is always listed |
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
same code and witnessed by `Listing2014Tests`, with no rows to close. **2002 work is frozen until 1985 completes (owner
order, 2026-11-15)**: steps 4a and 4b are planned here and start only after it.

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
| `>>LISTING`, `>>PAGE` | Consumed with no effect: `ConditionalCompilationProcessor.Run.Render`'s default arm (`CompilerDirectiveCatalog.IsDirective`); `DirectiveStateRegistry`'s `Constructs.ListingDirective2002` row says "NONE HELD" |
| `>>DISPLAY` | `ConditionalCompilationProcessor.ApplyDisplay` and `DisplayDestinations` (ConditionalCompilationProcessor.Display.cs): LISTING maps to `CompileOutputStream.Standard`; lines collected on `DiagnosticBag.CompileOutput` as `CompileOutputLine(Stream, Text)` with no position |
| Front-end diagnostics | `Diagnostic(Code, Severity, Message, Location, Span)` (src/WiseOwl.Cobol.Frontend/Diagnostics/Diagnostic.cs), structured, physical `SourceLocation` |
| Bind diagnostics | `EditionContext.Diagnostics` and `EditionContext.Warnings` are `List<string>`, rendered at report time by `EditionContext.Render` from the resultant `EditionContext.Cursor`; `CompilerDriver.Result.Errors` and `Result.Warnings` are strings |
| Edition citation in a message | `EditionInfo.Cite` (src/WiseOwl.Cobol.Editions/EditionInfo.cs), chooses the edition's text when the message is built |
| Program units | `BoundUnit.Name`, `ExternalizedName`, `ClassName`, `Parent`, `Children`, `Initial`, `Common`, `Recursive`, `IsFunction`, `IsPrototype`, `SourcePosition` (src/WiseOwl.Cobol.Compiler/Binding/Model/BoundUnit.cs); only after a successful bind |
| Data items | `DataItem.Level`, `CobolName`, `ClrName`, `StructName`, `Pic`, `PictureText`, `Occurs`, `OccursSpec`, `RedefinesTarget`, `Renames`, `Storage` (`StorageForm`: `NativeInt`, `NativeFloat`, `CharImage`, ...), `DeclaredAt`, `Indexes`, `Own88s`; roots in `DataBindResult.Forest` (`ForestSlice.Roots`) |
| Offsets and lengths | `RecordLayout.OffsetInRootWithEveryIndexOne`, `RecordLayout.FirstOccurrenceOffsetOf`, `RecordLayout.OccupiedBytes`, `ItemLength.Positions` |
| Data references of a statement | `BoundReferences.Of` returns `Reach` (the `Place`s a bound statement references, in source order) and a `Place` carries NO source position; `BoundStores.StoreKindOf(BoundStatement, DataItem)` answers per ITEM per statement (`StoreKind.Write`, `ReadWrite`, `WriteInPart`), not per occurrence; the one funnel every data reference passes, `ReferenceResolver.ResolveImpl`, already positions the diagnostic cursor at the reference's first token (`data.Edition.At(dref)`, kb/Work PB4383) and records nothing |
| Statement position | `BoundStatement.At` (a resultant `DiagnosticCursor`, stamped by `StatementBinder.BindStatement`) |
| Emitted C# | `CodeWriter.Line` (src/WiseOwl.Cobol.Compiler/CodeGen/CodeWriter.cs) writes it; `StatementEmitter.EmitStatement` is the one statement funnel; no `#line` directive and no PDB is emitted; the text is written to `<out>.g.cs` by `RoslynBackend.Emit` |
| The compile timestamp | `IntrinsicBinder.CompileClock` (the clock seam), read through `CompilationInputs.ReadCompilationTime`, captured lazily once per emit in `ProgramEmitter._whenCompiledStamp` and baked as WHEN-COMPILED's constant (`IntrinsicRenderer`); `CompilationInputs.ReadsCompilationTime` records that the output depends on it. CURRENT-DATE reads the run-time clock (`WiseOwl.Cobol.Runtime.IO.Clock`), not the compile timestamp |
| The CLI | `Program.BuildParser` (System.CommandLine options), `CliOptions`, `Program.RunCompile`; the library entry `CompilerDriver.Compile(CompilerDriver.Options)` returns `CompilerDriver.Result` on every outcome |

Four facts the listing needs are missing today and are added by this design:
1. the KIND of each physical line, recorded where the normalizer already decides it and exposed through a public type
   (section 4.4.1; step 1);
2. which INCLUSION a resultant line belongs to: a copybook copied twice has two inclusions whose lines share one file
   and line number (section 4.4; step 1);
3. structured bind diagnostics (section 6.3; step 3);
4. the POSITION and STORE KIND of each data, procedure and file reference, per occurrence (section 6.7.1; step 7).

## 4. The ONE listing model

### 4.1 Shape: every fact is placed by one index

Every compiler fact that has a position already has it in RESULTANT space: a token's line, `BoundStatement.At`,
`DataItem.DeclaredAt`, `EditionContext.Cursor`. The listing turns a resultant position into a listing line through ONE
index, `ListingLineIndex`, built once from the line map and the inclusion trace. A section is then a list of facts keyed
by resultant position; the index places each. A new section (or a new fact in an old one) is placed by construction,
never by a second mapping (rule 5). The model itself never stores a resultant position: it stores listing line numbers
and physical `(file, line, column)` coordinates, which are what readers and tools use.

```
Frontend (text stages)            Binder / emitter                     Driver
  ReferenceFormatMap per file       BoundCompilation (if reached)         CompilerDriver.Options
  SourceLineMap (resultant->phys)   structured diagnostics                the compilation timestamp
  TextStageTrace (inclusions,       EmitMap (if reached)
    replacements, directives)
  SourceUnitFacts (programs,
    DATE-COMPILED, verbs)
            \__________________________|________________________________/
                                       v
                         ListingBuilder (WiseOwl.Cobol.Compiler/Listing)
                           ListingLineIndex  ->  ListingVisibility  ->  sections
                                       v
                         ListingDocument (WiseOwl.Cobol.Listing, immutable)
                            /                 |                  \
              TextListingRenderer    HtmlListingRenderer    JsonListingWriter
```

### 4.2 The assembly: `WiseOwl.Cobol.Listing`

The model and the three renderers live in a new project, `src/WiseOwl.Cobol.Listing`, which references NO project of
the solution. A renderer therefore cannot reach a binder, parser or emitter type: "renderers read only the model" is
enforced by the compiler, and `docs/rearchitecture/architecture-edges.json` gives the project its own lowest layer so
`ArchitectureEdgesDriftTests` keeps it so. The Compiler references it and fills the model. A tool written in .NET can
reference the same assembly to read the JSON into the same types.

The model's root (all types are immutable `sealed record`s with `IReadOnlyList` members):

```csharp
namespace WiseOwl.Cobol.Listing;

public sealed record ListingDocument(
    ListingHeader Header,                         // compiler version, edition, timestamp, main source, outcome
    IReadOnlyList<ListingFile> Files,             // every file read: id, path, role (source | library), SHA-256
    IReadOnlyList<ListingInclusion> Inclusions,   // every COPY, in text order, with parent, depth, outcome, SUPPRESS
    IReadOnlyList<ListingLine> Lines,             // the source section, in listing order
    IReadOnlyList<ListingProgram> Programs,       // every program, function, class, method; nesting; line ranges
    IReadOnlyList<ListingDiagnostic> Diagnostics, // in listing order
    IReadOnlyList<ListingDisplayLine> Displays,   // >>DISPLAY UPON LISTING transfers
    IReadOnlyList<ListingOption> Options,
    ListingStatistics Statistics,
    ListingDataMap? DataMap,                      // null when not requested or not producible
    ListingCrossReference? CrossReference,
    ListingEmitMap? EmitMap,
    IReadOnlyList<ListingSectionStatus> Sections); // each requested section: produced, or why not

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
    string? Replaced,           // what the compiler compiled in place of Text, when different: DATE-COMPILED's
                                //   replacement, or the line's resultant text after REPLACE / REPLACING
    IReadOnlyList<int> Diagnostics,  // ListingDiagnostic ids anchored on this line
    ListingMargin? Margin,      // the embedded margin, computed ONCE by the builder (section 6.2): a definition line's
                                //   offset, length and usage, or a statement line's defining listing lines
    IReadOnlyList<ListingNameUse> Names); // each name occurrence on the line: (Column, Length, Symbol, Use), the text
                                //   margin's and the HTML links' one input
```

`ListingMargin` and `ListingNameUse` are joins of the data map and the cross-reference onto lines. They are made ONCE,
by `LineAnnotationBuilder` in the Compiler, and stored on the line; the text renderer prints `Margin` and the HTML
renderer turns each `ListingNameUse` into a link. No renderer joins two sections (PB5925 decision 1), so the text and
the HTML cannot disagree about a line. `PhysicalLineKind` is the front end's public enum: the Listing assembly
references nothing, so the model declares its own `ListingLineKind` with the same members and the builder maps one to
the other in one `switch` that a drift test holds total (a member added to either enum without the other is red).

Every other record (`ListingInclusion`, `ListingProgram`, `ListingDiagnostic`, `ListingDataItem`, `ListingSymbol`,
`ListingReference`, `ListingEmitRange`, ...) refers to lines by `Number` and to files by id. Section 6 defines each.

`ListingRequest` (also in this assembly) is the input: `Formats` (flags: Text, Html, Json), `OutputBase` (a path without
extension, or null for the default), `Sections` (flags), `PageLength`, `PageWidth`. `CompilerDriver.Options.Listing` is a
`ListingRequest?`; null means no listing and no listing work (section 9).

### 4.3 Who fills what, at which stage

| Part | Filled from | Stage | Producible when |
|---|---|---|---|
| `Files`, `Lines` (text, form, kind) | `ReferenceFormatMap.PhysicalText`, `ReferenceFormatMap.AreaAt` and `ReferenceFormatMap.KindAt` (section 4.4.1) of every file of the listed pass (section 4.4) | Text stages (Phase 0) | The source was read |
| `Inclusions`, `Line.Inclusion`, `Line.Depth` | `TextStageTrace` inclusion events | Text stages | The source was read |
| `Line.Omitted` | `SourceLineMap.IsOmitted` | Text stages | The source was read |
| `Line.Replaced` (REPLACE) | `TextStageTrace` replacement events | Text stages | The source was read |
| `Line.Listed`, `PageBreakBefore` | `ListingVisibility` over the directive events and inclusions | Listing build | Always |
| `Programs` (names, nesting, ranges), DATE-COMPILED, verb counts | `SourceUnitFacts`, read off the parse tree the parser built, recovered or not | Parse (Phase 1) | The text stages finished |
| `Programs` (C# class, attributes) | `BoundUnit` | Bind | The bind finished |
| `Diagnostics` | Front-end `Diagnostic`s, the binder's structured diagnostics, backend `BackendDiagnostic`s | Every phase | Always (what was reported) |
| `Displays` | `CompileOutputLine` with destination Listing and its origin | Text stages | The source was read |
| `DataMap` | `DataBindResult.Forest`, `RecordLayout`, `ItemLength`, `StorageForm` | Bind | The bind finished |
| `CrossReference`, `Line.Names`, `Line.Margin` | The reference occurrences (section 6.7.1), declarations, the data map | Bind | The bind finished |
| `EmitMap` | `EmitMap` recorded by `StatementEmitter.EmitStatement` | Emit | The emit ran |
| `Options`, `Header` | `CompilerDriver.Options`, the compilation timestamp | Driver | Always |

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
which forces the speculative re-run, and the listing anchors each syntax error on the line `diagnostics` names.

Events, each recorded at the one place the stage acts:

| Event | Recorded in | Carries |
|---|---|---|
| `InclusionBegun` | `CopyProcessor.ResolveOneCopy` (outcome, path, SUPPRESS from `ParseCopyStatement`) and `ConditionalCompilationProcessor.Run.RenderCopybook` (the resultant line range it renders into) | id, parent id, depth, the COPY statement's first and last origin, text-name, library-name, the file read (or none), `CopyOutcome`, SUPPRESS, the RESULTANT line range the library text occupies in the final frame |
| `Replaced` | `CopyProcessor.ApplyReplacements` (COPY REPLACING) and `CopyProcessor.ApplyReplaceStatements` (REPLACE) | the inclusion or REPLACE statement that acted, the physical span of the matched text, the matched text and the replacement, the resultant line |
| `ListingDirective` | `ConditionalCompilationProcessor.Run.Render`, emitting branch only | origin, ON or OFF |
| `PageDirective` | `ConditionalCompilationProcessor.Run.Render`, emitting branch only | origin |

The inclusion's RESULTANT range is what makes a copybook copied twice two blocks of listing lines with a right answer
for "which one": `ListingLineIndex` maps a resultant line to the innermost inclusion whose range holds it, then the
line's origin (`SourceLineMap.Origin`) to the physical line within that inclusion. The merged driver already places a
directive met inside library text in the final frame (`_renderBase + output.Count`, kb/Work PB1066); the inclusion range
uses the same arithmetic. A test pins it: a copybook copied twice with different REPLACING operands, a diagnostic in the
second copy, and the listing anchors the diagnostic in the second block.

Listing order: the main source's physical lines in order; after the last physical line of a COPY statement, that
inclusion's lines (recursively), each at `Depth + 1`. An inclusion that found no library text (`NotFound`, `Circular`)
lists no lines; its diagnostic is anchored on the COPY statement.

`DISPLAY` transfers: `CompileOutputStream` gains a `Listing` member, and `CompileOutputLine` gains the directive's origin
(`SourceOrigin At`). `DisplayDestinations` returns `Listing` for LISTING and for no UPON phrase (GR6). When the
compilation produces a listing, the builder places each `Listing` line at its directive; when it does not, the CLI
writes `Listing` lines to standard output exactly as `Standard` lines are written today, which is the documented
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
  `ReferenceFormatMap` (one byte per physical line).
- `ReferenceFormatMap.Create` takes that array; `ReferenceFormatMap.KindAt(int line)` (public) answers it. Copybooks
  pass through the same `NormalizeToFreeFormMapped`, so every file read gets its kinds with no second path.
- A `CommentEntryCandidate` (a COBOL-85 paragraph the conversion could not place, decided after COPY by
  `CommentEntryResolver.Resolve`) is settled by the builder from `SourceUnitFacts`: lines inside a comment-entry
  paragraph the parse read are `CommentEntry`, the rest `Source`.
- `PhysicalLineKindDriftTests` (Unit, which sees the internals): for a corpus of fixed and free lines, each kind equals
  the classification `FixedFormLine.Indicator` gives, so the recorded kind and the one reader cannot drift.

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
- `PageBreakBefore = true` for a listed line of kind `PageEjectComment` (§1985:IV.7.2.4, §2023:6.3.7.2), a listed
  `PageDirective` line (§2023:7.3.19.4 GR2), and the first line of each outermost program after the first.
- At `--std 85` no directive exists, so every line is listed and only '/' and program starts break pages.

### 4.6 The builder

`ListingBuilder.Build(ListingFacts facts, ListingRequest request)` in `src/WiseOwl.Cobol.Compiler/Listing/` is the one
constructor of a `ListingDocument`. `ListingFacts` is what the driver gathered at the outcome reached (the frontend's
maps, trace and unit facts; the diagnostics; the bound compilation or null; the emit map or null; the options; the
timestamp). Each section has one builder class (`SourceSectionBuilder`, `DataMapBuilder`, `CrossReferenceBuilder`,
`EmitMapBuilder`, ...) that reads only `ListingFacts` and `ListingLineIndex`; no builder class is a god class and no two
build one fact.

The driver gets ONE exit for the listing. `CompilerDriver.Compile` has ten return points today (`CompilerDriver.cs`
lines 169, 175, 251, 271, 275, 284, 306, 312, 316 and 321 at 16cf618813), and the hook must not depend on that count:
`Compile` becomes a thin wrapper over the existing body (`CompileCore`, the current method with every return point
unchanged). The body takes a `ListingFacts` collector and DEPOSITS each fact as its phase finishes: the front end's
listed pass (after `Frontend.Parse`), the diagnostics (as each phase reports), the bound compilation (after
`BinderDriver.Bind`), the emit map (after `backend.Emit`). The wrapper, when `Options.Listing` is set, builds and writes
the listing from whatever the collector holds and returns the result with `Result.Listing` set and the listing files
added to `Result.OutputFiles`.

What is listed is decided by the collector's STATE, never by which return was taken:
- no source text deposited (the two refusals before the source is read, `SourceNotFound` and the `OutputNotWritable` of
  a path with no directory): no listing;
- source deposited: a listing, each section present or `NotProducible` with the phase that stopped it. So a front-end
  error lists source and diagnostics; a bind error adds the program names from the parse; `CheckOnly` (a library caller
  may ask for both) has no emit map ("check-only compile: no code generated"); an expansion's failure (return 306) and a
  backend error have the bound sections and no emit map; a host that refuses the assembly write (return 321) gets the
  listing written if its own path is writable, and a listing write that fails adds one error to `Result.Errors` without
  changing the outcome.

A return point added to the body later needs no listing code. The no-listing path is the body alone, with a null
collector.

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

### 6.2 Source

One row per listed line, at the default width 132:

```
cols 1-6    listing line number
col  8      inclusion depth (blank at 0, 1-9, '+' beyond)
col  9      marker: '/' page-eject comment, '*' comment, 'D' debugging line, 'X' omitted by conditional compilation,
            'R' changed by REPLACE or REPLACING, 'E' DATE-COMPILED replaced, '>' directive, '-' continuation
cols 11-16  the physical line number in its file
cols 18-97  the line as written: a fixed-form line's 80 positions (sequence 1-6, indicator 7, area A 8-11, area B 12-72,
            identification 73-80); a free-form line's text, wrapped at col 97 onto rows marked '+'
cols 100-132 the embedded margin: the line's `ListingLine.Margin`, printed as the builder computed it (on a data
            definition line its offset, length and usage; on a statement line the listing line numbers where the names
            it uses are defined), present when the data map or the cross-reference was requested
```

A ruler (`----+----1----+----2 ...`) under each page heading marks the source columns. Each inclusion opens with a
banner row naming the text-name, library, the file read and the depth (`COPY PAYREC OF LIB1 -> lib/PAYREC.cpy (depth 1)`),
and a row `R` under each line REPLACE changed prints the compiled text. A DATE-COMPILED paragraph prints its replacement
in place of its first line, marked 'E', and its comment-entry continuation lines are not printed (the paragraph "is
replaced"); the JSON keeps the written text.

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
(a position in the `.g.cs`) is placed at the COBOL statement the emit map gives for that C# line, or in the summary when
none does.

### 6.4 Options

Every option of the compilation with its value and whether it is the default: each public property of
`CompilerDriver.Options`, the edition, `--permissive`, the listing request itself, the compiler version (the assembly's
informational version), the runtime version, the .NET runtime version and the host operating system. Built from one table
in `OptionsSectionBuilder`; a drift test holds it to every public property of `CompilerDriver.Options`.

### 6.5 Program map (nested programs)

Every program, function, class, factory, object and method: its name, kind, nesting depth, containing program, COMMON /
INITIAL / RECURSIVE / IS PROTOTYPE, first and last listing line, and, once bound, the C# class it became
(`BoundUnit.ClassName`) and its externalized name. Names and ranges come from `SourceUnitFacts` (the parse tree, so they
exist on a failed bind); attributes from the bound units.

### 6.6 Data map

Every data item of every program, in definition order, including those GnuCOBOL omits (INDEXED BY indexes, level-78 and
CONSTANT entries, condition-names, level-66 entries, compiler-implicit items marked as such):

| Column | Source |
|---|---|
| level, name, definition line | `DataItem.Level`, `CobolName`, `DeclaredAt` through the index |
| section | `DataItem.Section` (`EntrySection`) |
| offset in its record (first occurrence) | `RecordLayout.OffsetInRootWithEveryIndexOne`; blank for an item with no fixed offset |
| length (character positions and bytes) | `ItemLength.Positions`, `RecordLayout.OccupiedBytes` |
| usage, picture, category | `PicInfo`, `DataItem.PictureText` |
| OCCURS / DEPENDING ON / INDEXED BY | `DataItem.Occurs`, `OccursSpec`, `Indexes` |
| REDEFINES / RENAMES | `DataItem.RedefinesTarget`, `Renames` |
| GLOBAL / EXTERNAL / BASED | `HasGlobalClause`, `HasExternalClause`, `IsBased` |
| the .NET field and its type | `DataItem.ClrName`, `StructName` (the record struct a group became), and `StorageForm` (`NativeInt` -> `long`/`Int128` of n digits, `CharImage` -> character image of width n, `NativeFloat`, `DynamicTable`, `ObjectRef`, `PointerRef`, `TierBWindow` -> a window of a redefinition class) |

The rendering of `StorageForm` to a C# type name is one function the emitter and the map share (the emitter's own),
never a second spelling. Files get a file map: the file-name, organization, access, record keys, the record area and
the connector's C# field.

### 6.7 Cross-reference

Symbols: data items, condition-names, index-names, procedure-names (sections and paragraphs, `ProcedureNameMap`), files,
programs (CALL targets and units), and copy members (from the inclusions). Each use is one `ListingReference(Symbol,
Line, Column, Length, Use)` with `Use` one of:

- `Defined` - the declaration (`DataItem.DeclaredAt`, a paragraph header, an FD, a PROGRAM-ID, a COPY statement);
- `Read` - an occurrence at no store position of its statement;
- `Written` - an occurrence at a store position of kind `Write` or `WriteInPart`;
- `Modified` - an occurrence at a store position of kind `ReadWrite` (ADD TO, SUBTRACT FROM, INSPECT REPLACING, a STRING
  POINTER, ...).

Procedure references carry the transfer kind (PERFORM, GO TO, ALTER, USE, SORT INPUT/OUTPUT PROCEDURE) and build the
PERFORM graph; CALL references build the call graph; a file reference carries its verb. Sorted by name (Annex C fold of
the name, `CobolNames.UpperFold`), then by line. A symbol defined and never referenced is flagged.

#### 6.7.1 The missing fact: each reference's position and store kind, per occurrence (step 7)

The bound tree cannot answer "where, and how": a `Place` carries no position, `BoundReferences.Of` yields places, and
`BoundStores.StoreKindOf(BoundStatement, DataItem)` answers once per item. For `ADD WS-A` / `TO WS-B` written on two
lines both uses would get the statement's line, and in `MOVE WS-X (WS-X) TO WS-X` the three uses of one item would get
one kind. The fact is recorded where the binder already creates each reference, as ONE structure:

```csharp
// src/WiseOwl.Cobol.Compiler/Binding/Model/ReferenceOccurrence.cs (new)
internal sealed record ReferenceOccurrence(
    int Id,
    ReferenceTarget Target,          // DataItem | procedure (PcRange + name) | FileModel | program unit
    DiagnosticCursor At,             // resultant line and column of the reference's first token
    int Length,                      // characters of the reference as written (qualifiers, subscripts, ref-mod included)
    ReferenceUse Use,                // Read | Written | Modified for data; the transfer kind or verb otherwise
    DiagnosticCursor Statement);     // the BoundStatement.At of the statement it is written in
```

- **Data references.** `ReferenceResolver.ResolveImpl`, the one funnel every data reference passes (it already
  positions the cursor at `dref`'s first token, kb/Work PB4383), appends an occurrence for each non-probe resolution to
  the statement's `ReferenceLedger` (on `ProcedureBindState`), with `At` and `Length` from `dref`. The ledger keys the
  occurrence on the `Place` INSTANCE the resolver returns; to make that instance unique by construction,
  `ResolveImpl` returns `place with { }` when the instance is already in the ledger. A record copy is a new reference
  and EQUAL by value, so every value comparison of places elsewhere is unchanged.
- **Store kind.** `BoundStores`' exhaustive visitor gains one query, `BoundStores.StorePositionsOf(BoundStatement)`,
  that returns `(Place, StoreKind)` for each store position, the place being the instance the bound node holds (the
  visitor's `TargetHit`, `IntoHit`, `ReceiversHit` and `ConnectorHit` already see it). `StatementBinder.BindStatement`,
  the one statement funnel, classifies the statement's ledger when the statement is bound: an occurrence whose instance
  stands at a store position (found by walking from the stored place inward through the places it wraps, to the first
  instance the ledger holds) takes that position's kind; every other occurrence is `Read`. A subscript or
  reference-modifier operand is its own `ResolveImpl` call, so it is its own occurrence and is `Read`.
- **Procedure and file references.** `ProcedureTableBuilder.ResolveProcedureOperand` and `StatementValidation.ResolveFile`
  append occurrences with the operand context's position and the transfer kind or verb.
- **Where the result lives.** The classified occurrences of a unit are `BoundUnit.References`, read by
  `CrossReferenceBuilder` (the cross-reference section and the JSON), and by `LineAnnotationBuilder` (`ListingLine.Names`
  for the HTML links, `ListingLine.Margin` for the text margin). Nothing else derives a reference's position or use.
- **Cost.** Recorded only when the listing asks for the cross-reference: `EditionContext.RecordsReferences` gates the
  ledger and the `with { }` copy, so a compilation without it allocates nothing more.
- **Guards.** `ReferenceOccurrenceDriftTests`: every store position of every statement of a corpus (the conformance
  programs) pairs with exactly one occurrence, which fails the day a binder rebuilds a resolved place by value; the
  two examples above, with expected lines, columns and uses written from the source text.

### 6.8 Statistics and verb counts

Lines by kind and by file (source, comment, blank, omitted, copied, suppressed), programs, statements, diagnostics by
severity, and VBREF: each verb as written, its count and every listing line. Verbs come from the parse tree
(`SourceUnitFacts`), so they are counted as the programmer wrote them (not as the binder desugared them) and exist on a
failed bind. No timings: they make two listings of one source differ for no reader's benefit.

### 6.9 COBOL to C# map

`CodeWriter` gains a running line number; `StatementEmitter.EmitStatement` (the one statement funnel) records, when the
listing asks for the map, the resultant position of `BoundStatement.At` and the first and last C# line it wrote into an
`EmitMap`. Text captured by `CodeWriter.CaptureText` (an expression-position statement) belongs to the enclosing
statement. The section lists each COBOL statement's listing line beside its `.g.cs` line range; the HTML shows the two
side by side. The same map is the one input a later `#line` or portable-PDB feature would read (open question 4).

### 6.10 DISPLAY directive output (2023)

Each `>>DISPLAY` transfer to the listing prints at its directive as `DISPLAY: <text>`, always listed (§2023:7.3.12.4
GR2), in the order the directives were processed (GR1).

### 6.11 Conformance flags

The edition flags (COBOL0903 obsolete, COBOL0902 removed under `--permissive`, COBOL2894 with `--flag-extensions`) are
diagnostics and print inline; the summary adds a table of flagged constructs by edition.

## 7. The renderers

Each implements `IListingRenderer` and visits the document through `IListingSectionVisitor<T>`, one method per section
type: a section type added without a method in every renderer is a compile error.

### 7.1 Paginated text (`TextListingRenderer`)

UTF-8, LF line ends, a form feed (U+000C) before every page after the first. `PageLength` (default 60 lines including
the heading; 0 = no pagination, so only '/' and `>>PAGE` produce form feeds) and `PageWidth` (default 132, range 100 to
255; below 132 the margin columns are not printed). `PageComposer` turns rows into pages: a page break before a row with
`PageBreakBefore`, or when the page is full. A row wider than the page wraps onto `+` rows. Sections in order: options,
source with inline diagnostics, program map, data map, cross-reference, statistics and verbs, C# map, diagnostic
summary; each starts a page.

### 7.2 HTML (`HtmlListingRenderer`)

One self-contained file (inline CSS, a small inline script for collapse and filter, no external resource). Every name on
a source line links to its definition, and every definition lists its references as links; each diagnostic is anchored
on its line and listed in a summary panel that links to it; each inclusion is a collapsible block; suppressed and
omitted lines are present but collapsed and greyed; the C# map opens the statement's C# lines beside it. Links come from
each line's `ListingLine.Names` (built once from the reference occurrences, section 6.7.1) and the definitions' reference
lists from `ListingCrossReference`; the renderer joins nothing and computes nothing.

### 7.3 JSON (`JsonListingWriter`) and its schema

- Serialized with a `System.Text.Json` source-generation context (`ListingJsonContext`): camelCase, enums as strings,
  nulls omitted, no reflection at run time.
- The schema is GENERATED from the model types by `JsonSchemaExporter` (.NET 10) with the descriptions taken from the
  model's XML comments, and committed as `src/WiseOwl.Cobol.Listing/Json/listing-v1.schema.json`, an embedded resource.
  A preflight drift test regenerates it and fails on a difference: the schema cannot drift from the writer.
- The document carries `"$schema": "urn:wiseowl:cobol:listing:v1"` and `"schemaVersion": "1.<minor>"`.
- Diagnostics carry `citation` as a field: the edition and locator parsed from the message by the one C# reader of the
  citation format `§<edition>:<locator>[ <rule>]` (scripts/spec/citation.py's grammar, pinned by a shared vectors file),
  until diagnostics carry their citation as data (open question 3).

**Versioning and compatibility policy.** The major version changes only when a property is removed, renamed, or changes
meaning; the minor version changes when a property or an enum value is added. Within a major version every addition is
optional, and a reader must ignore properties it does not know. The compiler writes exactly one major version; a new
major version is announced in the release notes of the release that ships it, with the schema of the old major kept in
the repository for reference. The schema file name carries the major (`listing-v1.schema.json`, `listing-v2...`).

## 8. CLI surface

| Option | Meaning | Default |
|---|---|---|
| `--listing [on\|off]` | Produce a listing (bare `--listing` is on) | on at `--std 85`, off at 2002, 2014, 2023 |
| `--listing-format text\|html\|json` | A format to write; repeatable; implies `--listing on` | text |
| `--listing-output BASE` | Where: `BASE.lst`, `BASE.lst.html`, `BASE.lst.json`; `-` writes the text listing to standard output | the output assembly's path without extension (beside `<out>.g.cs`) |
| `--listing-sections LIST` | Comma list of `source, diagnostics, options, programs, statistics, data-map, xref, csharp-map`, or `standard` or `all` | `standard` = source, diagnostics, options, programs, statistics |
| `--listing-page-length N` | Lines per page; 0 = unpaginated | 60 |
| `--listing-page-width N` | Columns, 100 to 255 | 132 |

Every value is validated in `Program.BuildParser` the way `--std` is (an argument error, never a COBOL diagnostic).
`Resolve` builds a `ListingRequest` on `CliOptions`; `RunCompile` passes it as `CompilerDriver.Options.Listing`. The
library default is null (no listing), so every caller that builds `Options` itself (the test suites, `check-batch`, an
expansion's own compilation in `ExpansionAssemblies`) does no listing work; the per-edition default is a CLI decision.
`check-batch` never writes a listing. `Options.Listing` decides what the build writes, never what a source means:
`CompileStateDriftTests` classifies it as an output selection.

## 9. Performance budget

- **No listing requested: no listing work, by structure.** The front end passes `TextStageTrace.None`;
  `SourceUnitFacts` is not collected; `EditionContext.RecordsReferences` is false, so no reference ledger and no place
  copy; `EmitMap` is not recorded (`StatementEmitter` tests a null map once per statement); the driver passes a null
  collector and the wrapper returns the body's result. The one fact recorded for every compilation is the line-kind
  array of section 4.4.1, one byte per physical line, written where the converter already decides the kind. The guard
  is structural, never a wall-clock limit: `ListingOffDoesNoWorkDriftTests` compiles with `Options.Listing = null` and
  asserts `Frontend.ListingTrace` is the `TextStageTrace.None` instance, `Frontend.UnitFacts` is null, no
  `BoundUnit.References` was recorded and no `ListingFacts` was built.
- **Budgets, measured.** No listing: compile time within the run-to-run noise of the parent commit, and allocated bytes
  per compile no more than the parent's plus the line-kind arrays (one byte per physical line read). Standard text
  listing: at most 10 % over the no-listing compile. Every section in every format: at most 25 %.
- **The harness.** `tests/WiseOwl.Cobol.Benchmarks/Compile/CompileBenchmarks.cs` (BenchmarkDotNet, Release, run by
  `dotnet run -c Release --project tests/WiseOwl.Cobol.Benchmarks -- --filter *CompileBenchmarks*`) gains, in step 1,
  `[Params] ListingMode` (`None`, `Text`; later steps add `All`), read by `CompileBenchmarks.Setup` into the
  `CompilerDriver.Options` of `CompileBenchmarks.FullCompile` and `CompileBenchmarks.FrontEndAndBind`, and
  `[MemoryDiagnoser]`; its `Program` corpus gains one large NIST program. The numbers, with the parent commit's `None`
  row beside them, go in the compile-baseline section of `tests/WiseOwl.Cobol.Benchmarks/README.md`.
- **Who measures.** The step that adds a cost measures it and lands the numbers with it: step 1 (`None` against the
  parent commit, and `Text`), step 2 (`Text` with JSON), step 6, step 7 and step 8 (`All`, each with the section it
  adds), step 9 (`All` with HTML). A row over its budget is a kb/Work defect filed before the step lands, never a
  number left in a README. Open question 1 is decided from steps 6 to 9's rows.

## 10. Test plan

Expected values come from the standard's rules, from the input files themselves, and from the schema; never from a
captured listing.

1. **1985 minimum (`tests/WiseOwl.Cobol.Tests.Conformance/Listing/Listing1985Tests.cs`).** Each test compiles a small
   program at `--std 85` with `IntrinsicBinder.CompileClock` fixed, and asserts on the model and on the rendered text:
   - every physical line of the source and of each copybook appears once per inclusion, its text equal to the file's
     own line (§1985:IV.7.1, §1985:IV.7.4; the expected text is read from the input file);
   - a '/' line is the first source row of a new page (split the text on U+000C) and a '*' line follows its predecessor
     on the same page (§1985:IV.7.2.4);
   - the DATE-COMPILED row reads `DATE-COMPILED. <fixed instant>.`, the heading carries the same instant, and running the
     program, `FUNCTION WHEN-COMPILED` returns that instant (§1985:VI.3.4.4 GR1, §1989:A.2.46.3 RV3);
   - with a nested program, each page's heading names the innermost program of its first line (§1985:VI.3.3.4 GR1,
     §1985:X.3.1.4 GR1);
   - at `--std 85` the CLI writes `<out>.lst` with no option; `--listing off` writes none.
2. **The standard hooks, one class per edition, each citing its own edition's rules.** `Listing2023Tests.cs`: LISTING
   OFF/ON with each directive line listed (§2023:7.3.18.3 GR2-GR5); PAGE ejects and is listed, and under OFF neither
   (section 2.3); COPY SUPPRESS lists the COPY statement but not the library text, and a LISTING OFF inside library text
   does not outlive it (§2023:7.2.3.4 GR4, GR5); `>>DISPLAY` with no UPON and with UPON LISTING lands in the listing under
   OFF, in order, and on standard output when no listing is produced (§2023:7.3.12.4 GR1, GR2, GR5a, GR6).
   `Listing2002Tests.cs`: the same LISTING, PAGE and COPY cases against §2002:7.2.15.2 GR2-GR5, §2002:7.2.16.3 GR2, GR3
   and §2002:7.1.2.3 GR4, GR5, the '/' line (§2002:6.2.6.1) and WHEN-COMPILED (§2002:15.80.2 RV3); they are the witnesses
   of the twelve 2002 rows of section 2.2. `Listing2014Tests.cs`: the same against §2014:7.3.16.2 GR2-GR5, §2014:7.3.17.3
   GR2, GR3, §2014:7.2.2.3 GR4, GR5 and §2014:15.92.2 RV3. No DISPLAY case at 2002 or 2014: the directive is new in 2023.
3. **Provenance.** A copybook copied twice with different REPLACING: two blocks, each `R` row's compiled text equal to
   the replacement the test wrote; a diagnostic in the second copy is anchored in the second block. Omitted lines of a
   false `>>IF` are marked and listed.
4. **Sections.** Data map: offsets and lengths computed by hand from the PICTURE clauses of the test's own record, every INDEXED BY index and constant present; cross-reference: a fixture with one read, one
   write, one modify (ADD TO) and one definition of the same name yields exactly those four uses at those lines and
   columns, `ADD WS-A` / `TO WS-B` on two lines puts each use on its own line, and `MOVE WS-X (WS-X) TO WS-X` yields two
   reads and one write (section 6.7.1); a line's `Margin` and `Names` equal what the data map and cross-reference say of
   that line, and the text margin and the HTML links render exactly them; C# map:
   every C# range named lies inside the `.g.cs` and starts at a line the emitter wrote for that statement.
5. **Schema.** The generated schema equals the committed file (a preflight class, since a red one means a stale derived
   file); every document the tests produce round-trips through `ListingJsonContext` with no unknown property.
6. **Drift tests that keep the shape.** `WiseOwl.Cobol.Listing` references no solution project (architecture edges);
   every section has a method in `IListingSectionVisitor<T>` (compile-time); the options section names every public
   property of `CompilerDriver.Options`; the no-listing path does no work (section 9); the manual's command-line page
   names every listing option (`UserManualDriftTests`).
7. **Unit tests** of `ListingVisibility` and `PageComposer` over hand-built line lists (one per rule above), and of
   `ListingLineIndex` over hand-built maps; `PhysicalLineKindDriftTests` (section 4.4.1).
8. **Failed compilations.** One test per kind of outcome (section 4.6): the sections present and the
   `ListingSectionStatus` of each absent one; the failed-parse test of section 4.4 (the listed pass).

The inventory rows the listing makes testable (`1985-GR-VI.3.4.4-1`, `1985-GR-VI.3.3.4-1`, `1985-GR-X.3.1.4-1`,
PB5924's IV.7 rows, the 2023 LISTING/PAGE/COPY rows whose derivations rest on "no listing", and the twelve 2002 rows of
section 2.2) take these tests as witnesses in the records change of each step.

## 11. Implementation plan

Each step lands on its own and leaves the compiler correct. Step 1 is the 1985 minimum, so PB5923 closes first. **Steps 2
to 9 start only after the 1985 work is complete (owner order, 2026-11-15: 1985 first, every other body of work frozen);
step 4b is 2002 and 2014 work and is planned here for that reason, not started.**

**Step 1 - the 1985 standard minimum (closes PB5923).**
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
  recorded in `CopyProcessor.ResolveOneCopy`, `CopyProcessor.ExpandCopiesOneLevel` and
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
- Tests: `tests/WiseOwl.Cobol.Tests.Conformance/Listing/Listing1985Tests.cs`,
  `tests/WiseOwl.Cobol.Tests.Unit/Listing/ListingVisibilityTests.cs`, `PageComposerTests.cs`,
  `tests/WiseOwl.Cobol.Tests.Unit/Listing/PhysicalLineKindDriftTests.cs`, the failed-compilation tests of section 10 item 8,
  and `tests/WiseOwl.Cobol.Tests.Conformance/Listing/ListingOffDoesNoWorkDriftTests.cs`.
- Measurement (section 9): `CompileBenchmarks` gains `ListingMode` (`None`, `Text`) and `[MemoryDiagnoser]`; the `None`
  row against the parent commit and the `Text` row land in `tests/WiseOwl.Cobol.Benchmarks/README.md`.
- Docs: `docs/manual/user-guide/listing.md` (new), `docs/manual/user-guide/command-line.md`,
  `docs/conformance/7/DOC-A.1-117.md` (a listing at `--std 85` by default, on request otherwise; copied text listed;
  the effect of logical conversion), DESIGN.md section 0.5's row moves to "step 1 built". The 1985 records (the GAP and
  PARTIAL re-adjudications PB5923 names, PB5924's rows) take the step's tests as witnesses.

**Step 2 - JSON and its schema.** `src/WiseOwl.Cobol.Listing/Json/JsonListingWriter.cs`, `ListingJsonContext.cs`,
`listing-v1.schema.json` (generated), the schema drift test (preflight), `--listing-format`; the `Text` benchmark row
re-measured with JSON.

**Step 3 - diagnostics.** Structured bind diagnostics: `EditionContext.Error`, `EditionContext.Warning`, `EditionContext.Render`,
`EditionContext.Diagnostics`, `EditionContext.Warnings` become records plus rendered views; `CompilerDriver.Result` carries
the records; `Diagnostic` gains the resultant line; `ListingDiagnostic`, inline rows and the summary; the citation field.

**Step 4a - the directive hooks, witnessed at 2023.** `TextStageTrace` gains `ListingDirective`, `PageDirective` and
`Replaced` (recorded in `ConditionalCompilationProcessor.Run.Render`, `CopyProcessor.ApplyReplacements`,
`CopyProcessor.ApplyReplaceStatements`); SUPPRESS on inclusions; `CompileOutputStream.Listing` and
`CompileOutputLine.At`, `ConditionalCompilationProcessor.DisplayDestinations`; `ListingVisibility`'s directive rules;
`DirectiveStateRegistry`'s LISTING row stops saying "NONE HELD"; DOC-A.1-53, DOC-A.1-54, DOC-A.1-117, DOC-A.1-200; the 2023
derivations that rest on "no listing" (`DRV-GR-7.3.18.3-2..5`, `DRV-GR-7.3.19.4-2`, `DRV-GR-7.2.3.4-4/-5`) are retired and
witnessed by `Listing2023Tests`. The code is edition-neutral: the same trace and visibility rules serve 2002 and 2014.

**Step 4b - 2002 and 2014 (2002 work: after 1985 completes).** `tests/WiseOwl.Cobol.Tests.Conformance/Listing/Listing2002Tests.cs`
and `Listing2014Tests.cs` (section 10 item 2), each citing its own edition's rules (memory rule: each edition stands on
its own text); the records change adjudicates the twelve 2002 rows of section 2.2 (`2002-FMT-7.2.15.1`,
`2002-FMT-7.2.16.1`, `2002-GR-7.1.2.3-4`, `-5`, `2002-GR-7.2.15.2-2` to `-5`, `2002-GR-7.2.16.3-2`, `2002-RV-15.80.2-3`,
`2002-DOC-B.1-90`, `2002-DOC-B.1-165`) with those tests as witnesses; DOC-A.1-117's 2002 and 2014 annex items (B.1 item
90, A.1 item 106) and the stage items (B.1 item 165, A.1 item 182) are documented.

**Step 5 - options, program map, statistics and verbs.** `OptionsSectionBuilder` with its drift test; program attributes
from `BoundUnit`; `SourceUnitFacts` verbs; `--listing-sections`.

**Step 6 - data map.** `DataMapBuilder` over `DataBindResult.Forest`, `RecordLayout`, `ItemLength`, `StorageForm`, the
emitter's type-name function; `LineAnnotationBuilder` with `ListingLine.Margin` for definition lines; the `All`
benchmark row.

**Step 7 - cross-reference and the reference occurrences.** The missing fact first (section 6.7.1):
`ReferenceOccurrence`, the `ReferenceLedger` on `ProcedureBindState`, recorded in `ReferenceResolver.ResolveImpl`,
`ProcedureTableBuilder.ResolveProcedureOperand` and `StatementValidation.ResolveFile`, classified by
`StatementBinder.BindStatement` through the new `BoundStores.StorePositionsOf`, kept on `BoundUnit.References`, gated by
`EditionContext.RecordsReferences`, guarded by `ReferenceOccurrenceDriftTests`. Then `CrossReferenceBuilder`, the
PERFORM and CALL graphs, and `LineAnnotationBuilder`'s `ListingLine.Names` and statement-line `Margin`; the `All` row.

**Step 8 - COBOL to C# map.** `CodeWriter` line count, `StatementEmitter.EmitStatement` records into `EmitMap`;
`EmitMapBuilder`; backend diagnostics placed through it; the `All` row.

**Step 9 - HTML.** `HtmlListingRenderer`, reading `ListingLine.Names` and the cross-reference only; the `All` row with HTML.

## 12. Documents and records that change with the listing

`docs/conformance/7/DOC-A.1-117.md` ("The compiler NEVER produces a source listing" is false from step 1);
DOC-A.1-200 (the stage, step 4a; its 2002 and 2014 items, step 4b); DOC-A.1-53 and DOC-A.1-54 (where DISPLAY UPON LISTING goes, step 4a); the inventory rows
and derivations PB5923 and section 10 name; `docs/manual/user-guide/command-line.md` and the new `listing.md`;
`DirectiveStateRegistry` (step 4a); this document's status line at each step.

## 13. Open questions, each with the recommendation taken

1. **Which sections by default?** Recommendation: `standard` (source, diagnostics, options, programs, statistics), the
   rest with `--listing-sections`. IBM's defaults are similar (NOMAP NOXREF NOLIST) and the full set costs a bound-tree
   walk. Decided again from the benchmark rows steps 6 to 9 land (section 9).
2. **Should JSON contain every section regardless of `--listing-sections`?** Recommendation: no; one request, one
   document, so the three formats always agree.
3. **Diagnostics carrying their citation as data.** `EditionInfo.Cite` returns a string spliced into the message, so the
   JSON's `citation` field is parsed back out of the text. Recommendation: in step 3, make `EditionInfo.Cite` return a
   structured value (edition, locator, rule) whose `ToString` is today's text, and carry it on the diagnostic; the parser
   is then needed only for messages built elsewhere, and a drift test counts those to zero.
4. **`#line` directives or a portable PDB from the emit map.** That would let a .NET debugger step through COBOL source.
   Recommendation: a separate decision after step 8; the emit map is designed to be its one input.
5. **Omitted lines shown or hidden.** Recommendation: shown and marked 'X' (IBM's default NOSKIPSRC); add
   `--listing-omitted hide` only if a user asks.
6. **Publishing the schema at a URL.** Publishing is the owner's (CLAUDE.md, "Still the owner's"). Recommendation: the URN
   identifier now; ask the owner when the product's documentation site is updated for the listing.
7. **A listing for an expansion's own compilation** (`ExpansionAssemblies`). Recommendation: none; its text is the
   repository's skeleton, and its diagnostics already report at the EXPANDS entry of the user's listing.
