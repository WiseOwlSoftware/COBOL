# DESIGN - the source listing

> **Status: DESIGN, APPROVED BY THE CODEBASE OWNER (Claude, CLAUDE.md "Ownership"; decision kb/Work PB5925). NOT YET BUILT.**
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
| L-1 | Listings outside the standard's scope: "The form and content of listings produced during compilation, if any"; so are "The form or content of error, flagging, or warning messages" | §2002:1 | (same) | §2023:1 | The form is ours; this document is its definition |
| L-2 | "Whether the compiler produces a source listing is implementor defined" | §2002:7.2.15.2 GR1 | §2014:7.3.16.2 GR1 | §2023:7.3.18.3 GR1 | Produced on request (`--listing`); documented in DOC-A.1-117 |
| L-3 | Annex item: "Listings (whether and when produced by the compiler, effect of logical conversion). This item is required" | §2002:B.1 | | §2023:A.1 | DOC-A.1-117 rewritten (section 12) |
| L-4 | Logical conversion: "The implementor shall define the effect on the source listing, if any, of logical conversion" | §2002:6.4 | | §2023:6.5 | The listing shows the physical lines (before conversion); continuation lines are listed as written; DOC-A.1-117 says so |
| L-5 | Stage: "The implementor defines the stage during which actions associated with listings, if any, take place"; the item "is conditioned on the implementor producing listings" | §2002:7 | | §2023:7.1, §2023:A.1 | The text-manipulation stage records the actions; the listing applies them after compilation (section 4.4); DOC-A.1-200 |
| L-6 | Comment lines: '/' "causes page ejection followed by printing of the comment line"; '*' at the next line position | §2002:6.2.6.1 | §2014:6.3.6.1 | §2023:6.3.7.2 | As 85-3 |
| L-7 | COPY SUPPRESS: library text not listed; "If a listing is being produced, the COPY statement itself is listed" | §2002:7.1.2.3 GR4 | §2014:7.2.2.3 GR4 | §2023:7.2.3.4 GR4 | `ListingInclusion.Suppress`; the visibility pass (section 4.5) |
| L-8 | After COPY, "the LISTING directive that is in effect for the COPY statement itself is considered to be in effect" | §2002:7.1.2.3 GR5 | | §2023:7.2.3.4 GR5 | The visibility pass restores the state at the end of each inclusion |
| L-9 | The NOTE: "It is recommended that the implementor provide a listing of the original compilation group" (2023: "The implementor can provide a listing of the original compilation group") | §2002:7.2.15 | | §2023:7.3.18.1 | The source section IS the original compilation group; the manipulated text is shown as annotations |
| L-10 | "The default LISTING directive is '>>LISTING ON'" | | | §2023:7.3.18.3 GR2 | Visibility starts ON |
| L-11 | "Each LISTING directive shall be listed, even if the listing is being suppressed by a LISTING directive" | §2002:7.2.15.2 GR3 | | §2023:7.3.18.3 GR3 | A LISTING directive line is always listed |
| L-12 | OFF: "source lines shall not be listed until a LISTING directive specifying or implying the ON phrase is encountered"; ON: "source lines shall be listed until either a LISTING OFF directive is encountered" or the end | | | §2023:7.3.18.3 GR4, GR5 | The visibility pass |
| L-13 | "a PAGE directive shall cause page ejection followed by listing of the PAGE directive"; with no listing it has no effect | §2002:7.2.16.3 GR2 | §2014:7.3.17.3 GR2 | §2023:7.3.19.4 GR2, GR3 | `PageBreakBefore` on a listed PAGE line |
| L-14 | DISPLAY directive: operands transferred "to the source listing or compile-time-device-1 or both"; "Any conversion of data required is defined by the implementor"; with a listing, "the data transfer shall take place irrespective of whether it is suppressed by the LISTING directive"; UPON LISTING: "data is transferred to the same device as that used for source listings"; no UPON: "the default is as if UPON LISTING were specified" | | | §2023:7.3.12.4 GR1, GR2, GR5a, GR6 | A `ListingDisplayLine` at the directive's place, always listed; to standard output when no listing is produced (unchanged) |
| L-15 | WHEN-COMPILED: the same time as "the compilation date and time if provided in the listing" | §2002:15.80.2 RV3 | §2014:15.92.2 RV3 | §2023:15.99.3 RV3 | One compilation timestamp (section 5) |
| L-16 | The warning mechanism: "An implementation shall provide a warning mechanism that optionally may be invoked by the user at compile time" | | | §2023:4.2.10 | Not a listing rule; its warnings (COBOL2894) are diagnostics and appear inline |

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
| The physical text of every file read, tabs expanded | `ReferenceFormatMap.PhysicalText` (src/WiseOwl.Cobol.Frontend/Preprocessor/ReferenceFormatMap.cs), one map per file in `CopyProcessor.ReferenceFormats`, the converged pass's maps returned by `Frontend.RunTextStages` |
| Fixed or free form of each physical line | `ReferenceFormatMap.AreaAt`, `ReferenceFormatMap.Create` (format changes by line) |
| The indicator area of a fixed-form line | `FixedFormLine.Indicator`, `FixedFormLine.SequenceArea`, `FixedFormLine.ProgramText` (src/WiseOwl.Cobol.Frontend/Preprocessor/FixedFormLine.cs) |
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
| Data references of a statement | `BoundReferences.Of` (every place a bound statement references, in source order); `BoundStores.StoreKindOf` (`StoreKind.Write`, `ReadWrite`, `WriteInPart`) |
| Statement position | `BoundStatement.At` (a resultant `DiagnosticCursor`, stamped by `StatementBinder.BindStatement`) |
| Emitted C# | `CodeWriter.Line` (src/WiseOwl.Cobol.Compiler/CodeGen/CodeWriter.cs) writes it; `StatementEmitter.EmitStatement` is the one statement funnel; no `#line` directive and no PDB is emitted; the text is written to `<out>.g.cs` by `RoslynBackend.Emit` |
| The compile timestamp | `IntrinsicBinder.CompileClock` (the clock seam), read through `CompilationInputs.ReadCompilationTime`, captured lazily once per emit in `ProgramEmitter._whenCompiledStamp` and baked as WHEN-COMPILED's constant (`IntrinsicRenderer`); `CompilationInputs.ReadsCompilationTime` records that the output depends on it. CURRENT-DATE reads the run-time clock (`WiseOwl.Cobol.Runtime.IO.Clock`), not the compile timestamp |
| The CLI | `Program.BuildParser` (System.CommandLine options), `CliOptions`, `Program.RunCompile`; the library entry `CompilerDriver.Compile(CompilerDriver.Options)` returns `CompilerDriver.Result` on every outcome |

Two facts the listing needs are missing today and are added by this design: which INCLUSION a resultant line belongs
to (a copybook copied twice has two inclusions whose lines share one file and line number), and structured bind
diagnostics.

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
                                //   | CommentEntry | Blank
    string Text,                // the physical text as the file holds it, tabs expanded (ReferenceFormatMap.PhysicalText)
    bool Omitted,               // a false >>IF / >>WHEN branch omitted it (LineOrigin.Omitted)
    bool Listed,                // the visibility pass's answer (section 4.5): the text and HTML renderers print it
    bool PageBreakBefore,       // the visibility pass's answer: '/' comment, >>PAGE, new outermost program
    int? Program,               // ListingProgram id of the innermost program containing the line
    string? Replaced,           // what the compiler compiled in place of Text, when different: DATE-COMPILED's
                                //   replacement, or the line's resultant text after REPLACE / REPLACING
    IReadOnlyList<int> Diagnostics); // ListingDiagnostic ids anchored on this line
```

Every other record (`ListingInclusion`, `ListingProgram`, `ListingDiagnostic`, `ListingDataItem`, `ListingSymbol`,
`ListingReference`, `ListingEmitRange`, ...) refers to lines by `Number` and to files by id. Section 6 defines each.

`ListingRequest` (also in this assembly) is the input: `Formats` (flags: Text, Html, Json), `OutputBase` (a path without
extension, or null for the default), `Sections` (flags), `PageLength`, `PageWidth`. `CompilerDriver.Options.Listing` is a
`ListingRequest?`; null means no listing and no listing work (section 9).

### 4.3 Who fills what, at which stage

| Part | Filled from | Stage | Producible when |
|---|---|---|---|
| `Files`, `Lines` (text, form, kind) | `ReferenceFormatMap.PhysicalText` and the format map of every file of the converged pass; `FixedFormLine.Indicator` | Text stages (Phase 0) | The source was read |
| `Inclusions`, `Line.Inclusion`, `Line.Depth` | `TextStageTrace` inclusion events | Text stages | The source was read |
| `Line.Omitted` | `SourceLineMap.IsOmitted` | Text stages | The source was read |
| `Line.Replaced` (REPLACE) | `TextStageTrace` replacement events | Text stages | The source was read |
| `Line.Listed`, `PageBreakBefore` | `ListingVisibility` over the directive events and inclusions | Listing build | Always |
| `Programs` (names, nesting, ranges), DATE-COMPILED, verb counts | `SourceUnitFacts`, read off the parse tree the parser built, recovered or not | Parse (Phase 1) | The text stages finished |
| `Programs` (C# class, attributes) | `BoundUnit` | Bind | The bind finished |
| `Diagnostics` | Front-end `Diagnostic`s, the binder's structured diagnostics, backend `BackendDiagnostic`s | Every phase | Always (what was reported) |
| `Displays` | `CompileOutputLine` with destination Listing and its origin | Text stages | The source was read |
| `DataMap` | `DataBindResult.Forest`, `RecordLayout`, `ItemLength`, `StorageForm` | Bind | The bind finished |
| `CrossReference` | `BoundReferences.Of`, `BoundStores.StoreKindOf`, declarations | Bind | The bind finished |
| `EmitMap` | `EmitMap` recorded by `StatementEmitter.EmitStatement` | Emit | The emit ran |
| `Options`, `Header` | `CompilerDriver.Options`, the compilation timestamp | Driver | Always |

A listing is produced on EVERY outcome once the source text was read: frontend error, bind error, backend error and
success. A section that cannot be built at the outcome reached is not omitted silently: its `ListingSectionStatus` says
`NotProducible` and names the phase that stopped (for example `DataMap`: "the bind refused the program"). This matters
most when the compile fails, which is when a listing is most read.

### 4.4 Provenance through COPY, REPLACE and conditional compilation: `TextStageTrace`

The text stages run several times per compilation (the §2023:14.9.28.4 GR14 fixed point and the comment-entry fixed point
in `Frontend.Parse` and `Frontend.RunTextStages`); only the converged pass's facts are the compilation's. The trace
follows the discipline the per-pass `DiagnosticBag` already follows: one `TextStageTrace` per pass, created in
`Frontend.RunTextStages` beside `passDiagnostics`, handed to `CopyProcessor` and to `ConditionalCompilationProcessor`, and
the converged pass's trace exposed as `Frontend.ListingTrace` (beside `Frontend.CopyNames`). When no listing is
requested the front end passes `TextStageTrace.None`, whose methods do nothing.

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
- `PageBreakBefore = true` for a listed fixed-form line whose indicator is '/' (§1985:IV.7.2.4, §2023:6.3.7.2), a listed
  `PageDirective` line (§2023:7.3.19.4 GR2), and the first line of each outermost program after the first.
- At `--std 85` no directive exists, so every line is listed and only '/' and program starts break pages.

### 4.6 The builder

`ListingBuilder.Build(ListingFacts facts, ListingRequest request)` in `src/WiseOwl.Cobol.Compiler/Listing/` is the one
constructor of a `ListingDocument`. `ListingFacts` is what the driver gathered at the outcome reached (the frontend's
maps, trace and unit facts; the diagnostics; the bound compilation or null; the emit map or null; the options; the
timestamp). Each section has one builder class (`SourceSectionBuilder`, `DataMapBuilder`, `CrossReferenceBuilder`,
`EmitMapBuilder`, ...) that reads only `ListingFacts` and `ListingLineIndex`; no builder class is a god class and no two
build one fact.

The driver gets ONE exit for the listing: `CompilerDriver.Compile` becomes a thin wrapper over the existing body
(`CompileCore`, the current method with its seven return points unchanged) that, when `Options.Listing` is set, builds
and writes the listing from the facts the body left on a `ListingFacts` collector, then returns the result with
`Result.Listing` set and the listing files added to `Result.OutputFiles`. No return point of the body can forget the
listing, and the no-listing path is the body alone.

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
cols 100-132 the embedded margin: on a data definition line its offset, length and usage; on a statement line the
            listing line numbers where the names it uses are defined (from the cross-reference, when requested)
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
Line, Column, Use)` with `Use` one of:

- `Defined` - the declaration (`DataItem.DeclaredAt`, a paragraph header, an FD, a PROGRAM-ID, a COPY statement);
- `Read` - a place `BoundReferences.Of` yields that the statement does not store;
- `Written` - a place whose `BoundStores.StoreKindOf` is `Write` or `WriteInPart`;
- `Modified` - a place whose store kind is `ReadWrite` (ADD TO, SUBTRACT FROM, INSPECT REPLACING, a STRING POINTER, ...).

`BoundStores` is already the exhaustive store taxonomy over every bound statement leaf (a generated visitor, so a new
statement leaf is a compile error there); the collector adds no second taxonomy. Procedure references carry the
transfer kind (PERFORM, GO TO, ALTER, USE, SORT INPUT/OUTPUT PROCEDURE) and build the PERFORM graph; CALL references
build the call graph. Sorted by name (Annex C fold of the name, `CobolNames.UpperFold`), then by line. A symbol defined
and never referenced is flagged.

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
the cross-reference's `(Line, Column, Symbol)` entries; the renderer computes nothing.

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

- **No listing requested: no listing work.** The front end passes `TextStageTrace.None`; `SourceUnitFacts` is not
  collected; `EmitMap` is not recorded (`StatementEmitter` tests a null map once per statement); the driver's wrapper
  returns the body's result. Budget: no measurable difference in compile time and zero added allocations per compile.
- **Listing requested.** Text listing with the standard sections: at most 10 % of the compile's time. All sections and
  all three formats: at most 25 %.
- **How it is measured.** `tests/WiseOwl.Cobol.Benchmarks/Compile/CompileBenchmarks.cs` gains a `[Params]` over no
  listing / standard text / everything, with `MemoryDiagnoser`, over its existing programs and a large NIST program; the
  numbers go in the benchmarks README. The guard that keeps the no-listing path free is structural, never a wall-clock
  limit: a drift test compiles with `Options.Listing = null` and asserts the front end used `TextStageTrace.None` (the
  instance `Frontend.ListingTrace` returns) and the driver built no `ListingFacts`.

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
2. **The standard hooks (`Listing2023Tests.cs`, and the same at 2002 and 2014).** LISTING OFF/ON with each directive
   line listed (§2023:7.3.18.3 GR2-GR5); PAGE ejects and is listed, and under OFF neither (section 2.3); COPY SUPPRESS
   lists the COPY statement but not the library text, and a LISTING OFF inside library text does not outlive it
   (§2023:7.2.3.4 GR4, GR5); `>>DISPLAY` with no UPON and with UPON LISTING lands in the listing under OFF, in order, and
   on standard output when no listing is produced (§2023:7.3.12.4 GR1, GR2, GR5a, GR6).
3. **Provenance.** A copybook copied twice with different REPLACING: two blocks, each `R` row's compiled text equal to
   the replacement the test wrote; a diagnostic in the second copy is anchored in the second block. Omitted lines of a
   false `>>IF` are marked and listed.
4. **Sections.** Data map: offsets and lengths computed by hand from the PICTURE clauses of the test's own record, every INDEXED BY index and constant present; cross-reference: a fixture with one read, one
   write, one modify (ADD TO) and one definition of the same name yields exactly those four uses at those lines; C# map:
   every C# range named lies inside the `.g.cs` and starts at a line the emitter wrote for that statement.
5. **Schema.** The generated schema equals the committed file (a preflight class, since a red one means a stale derived
   file); every document the tests produce round-trips through `ListingJsonContext` with no unknown property.
6. **Drift tests that keep the shape.** `WiseOwl.Cobol.Listing` references no solution project (architecture edges);
   every section has a method in `IListingSectionVisitor<T>` (compile-time); the options section names every public
   property of `CompilerDriver.Options`; the no-listing path does no work (section 9); the manual's command-line page
   names every listing option (`UserManualDriftTests`).
7. **Unit tests** of `ListingVisibility` and `PageComposer` over hand-built line lists (one per rule above), and of
   `ListingLineIndex` over hand-built maps.

The inventory rows the listing makes testable (`1985-GR-VI.3.4.4-1`, `1985-GR-VI.3.3.4-1`, `1985-GR-X.3.1.4-1`,
PB5924's IV.7 rows, the 2023 LISTING/PAGE/COPY rows whose derivations rest on "no listing") take these tests as
witnesses in the records change of each step.

## 11. Implementation plan

Each step lands on its own and leaves the compiler correct. Step 1 is the 1985 minimum, so PB5923 closes first.

**Step 1 - the 1985 standard minimum (closes PB5923).**
- Create `src/WiseOwl.Cobol.Listing/` (`WiseOwl.Cobol.Listing.csproj`, `ListingDocument.cs` with the header, files,
  inclusions, lines and programs, `ListingRequest.cs`, `Text/TextListingRenderer.cs`, `Text/PageComposer.cs`), add it to
  `WiseOwl.Cobol.sln`, reference it from `src/WiseOwl.Cobol.Compiler/WiseOwl.Cobol.Compiler.csproj`, give it a layer in
  `docs/rearchitecture/architecture-edges.json` and a public API baseline.
- Front end: `src/WiseOwl.Cobol.Frontend/Preprocessor/TextStageTrace.cs` (inclusion events only in this step) recorded in
  `CopyProcessor.ResolveOneCopy`, `CopyProcessor.ExpandCopiesOneLevel` and
  `ConditionalCompilationProcessor.Run.RenderCopybook`; `src/WiseOwl.Cobol.Frontend/Pipeline/SourceUnitFacts.cs` (program
  names, nesting and ranges, the DATE-COMPILED paragraph's range) collected in `Frontend.Parse`; `Frontend.RunTextStages`
  and `Frontend.Preprocess` carry the per-pass trace and expose the converged pass's trace and format maps.
- `CompilationInputs.ReadCompilationTime` captures once and records `CompilationTime` (section 5).
- Compiler: `src/WiseOwl.Cobol.Compiler/Listing/` (`ListingFacts.cs`, `ListingBuilder.cs`, `ListingLineIndex.cs`,
  `ListingVisibility.cs`, `SourceSectionBuilder.cs`, `ListingWriter.cs`); `CompilerDriver.Options` gains `Listing`,
  `CompilerDriver.Result` gains `Listing`, `CompilerDriver.Compile` becomes the one-exit wrapper (section 4.6).
- CLI: `CliOptions` and `Program.BuildParser` gain `--listing`, `--listing-output`, `--listing-page-length`,
  `--listing-page-width` (text only); `Program.RunCompile` handles `--listing-output -`.
- Tests: `tests/WiseOwl.Cobol.Tests.Conformance/Listing/Listing1985Tests.cs`,
  `tests/WiseOwl.Cobol.Tests.Unit/Listing/ListingVisibilityTests.cs`, `PageComposerTests.cs`, the no-work drift test.
- Docs: `docs/manual/user-guide/listing.md` (new), `docs/manual/user-guide/command-line.md`,
  `docs/conformance/7/DOC-A.1-117.md` (a listing at `--std 85` by default, on request otherwise; copied text listed;
  the effect of logical conversion), DESIGN.md section 0.5's row moves to "step 1 built". The 1985 records (the GAP and
  PARTIAL re-adjudications PB5923 names, PB5924's rows) take the step's tests as witnesses.

**Step 2 - JSON and its schema.** `src/WiseOwl.Cobol.Listing/Json/JsonListingWriter.cs`, `ListingJsonContext.cs`,
`listing-v1.schema.json` (generated), the schema drift test (preflight), `--listing-format`.

**Step 3 - diagnostics.** Structured bind diagnostics: `EditionContext.Error`, `EditionContext.Warning`, `EditionContext.Render`,
`EditionContext.Diagnostics`, `EditionContext.Warnings` become records plus rendered views; `CompilerDriver.Result` carries
the records; `Diagnostic` gains the resultant line; `ListingDiagnostic`, inline rows and the summary; the citation field.

**Step 4 - the standard hooks of 2002, 2014 and 2023.** `TextStageTrace` gains `ListingDirective`, `PageDirective` and
`Replaced` (recorded in `ConditionalCompilationProcessor.Run.Render`, `CopyProcessor.ApplyReplacements`,
`CopyProcessor.ApplyReplaceStatements`); SUPPRESS on inclusions; `CompileOutputStream.Listing` and
`CompileOutputLine.At`, `ConditionalCompilationProcessor.DisplayDestinations`; `ListingVisibility`'s directive rules;
`DirectiveStateRegistry`'s LISTING row stops saying "NONE HELD"; DOC-A.1-53, DOC-A.1-54, DOC-A.1-117, DOC-A.1-200; the 2023
derivations that rest on "no listing" (`DRV-GR-7.3.18.3-2..5`, `DRV-GR-7.3.19.4-2`, `DRV-GR-7.2.3.4-4/-5`) are retired and
witnessed by `Listing2023Tests` and its 2002 and 2014 twins.

**Step 5 - options, program map, statistics and verbs.** `OptionsSectionBuilder` with its drift test; program attributes
from `BoundUnit`; `SourceUnitFacts` verbs; `--listing-sections`.

**Step 6 - data map.** `DataMapBuilder` over `DataBindResult.Forest`, `RecordLayout`, `ItemLength`, `StorageForm`, the
emitter's type-name function; the embedded margin on definition lines.

**Step 7 - cross-reference.** `CrossReferenceBuilder` over `BoundReferences.Of`, `BoundStores.StoreKindOf`, procedure, file,
program and copy-member symbols; the PERFORM and CALL graphs; the margin on statement lines.

**Step 8 - COBOL to C# map.** `CodeWriter` line count, `StatementEmitter.EmitStatement` records into `EmitMap`;
`EmitMapBuilder`; backend diagnostics placed through it.

**Step 9 - HTML.** `HtmlListingRenderer`.

## 12. Documents and records that change with the listing

`docs/conformance/7/DOC-A.1-117.md` ("The compiler NEVER produces a source listing" is false from step 1);
DOC-A.1-200 (the stage, step 4); DOC-A.1-53 and DOC-A.1-54 (where DISPLAY UPON LISTING goes, step 4); the inventory rows
and derivations PB5923 and section 10 name; `docs/manual/user-guide/command-line.md` and the new `listing.md`;
`DirectiveStateRegistry` (step 4); this document's status line at each step.

## 13. Open questions, each with the recommendation taken

1. **Which sections by default?** Recommendation: `standard` (source, diagnostics, options, programs, statistics), the
   rest with `--listing-sections`. IBM's defaults are similar (NOMAP NOXREF NOLIST) and the full set costs a bound-tree
   walk. Revisit once step 9's measurements exist.
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
