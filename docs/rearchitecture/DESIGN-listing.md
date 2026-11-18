# DESIGN - the source listing

> **Status: DESIGN, APPROVED BY THE CODEBASE OWNER (Claude, CLAUDE.md "Ownership"; decision kb/Work PB5925). STEP 1 BUILT
> (the 1985 standard minimum, kb/Work PB5923, branch `listing-step-1`); STEP 2 BUILT (the JSON listing and its schema, branch
> `listing-step-2`); STEP 3 BUILT (diagnostics, branch `listing-step-3`); steps 4a to 9 not yet built.** Where building a step
> proved this document wrong it was corrected in the same change; each correction is marked "(step 1)", "(step 2)" or "(step 3)"
> in the section it changed.
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
same code and witnessed by `Listing2014Tests`, with no rows to close. Steps 4a and 4b are built in order with the rest
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
| `>>LISTING`, `>>PAGE` | Consumed with no effect: `ConditionalCompilationProcessor.Run.Render`'s default arm (`CompilerDirectiveCatalog.IsDirective`); `DirectiveStateRegistry`'s `Constructs.ListingDirective2002` row says "NONE HELD" |
| `>>DISPLAY` | `ConditionalCompilationProcessor.ApplyDisplay` and `DisplayDestinations` (ConditionalCompilationProcessor.Display.cs): LISTING maps to `CompileOutputStream.Standard`; lines collected on `DiagnosticBag.CompileOutput` as `CompileOutputLine(Stream, Text)` with no position |
| Front-end diagnostics | `Diagnostic(Code, Severity, Message, Location, Span)` (src/WiseOwl.Cobol.Frontend/Diagnostics/Diagnostic.cs), structured, physical `SourceLocation` |
| Bind diagnostics | (before step 3) `EditionContext.Diagnostics` and `EditionContext.Warnings` were `List<string>`, rendered at report time from the resultant `EditionContext.Cursor`; `CompilerDriver.Result.Errors` and `Result.Warnings` are strings. (Step 3:) `EditionContext.Records` holds each as a `Diagnostic`; section 6.3 |
| Edition citation in a message | `EditionInfo.Cite` (src/WiseOwl.Cobol.Editions/EditionInfo.cs), chooses the edition's text when the message is built |
| Program units | `BoundUnit.Name`, `ExternalizedName`, `ClassName`, `Parent`, `Children`, `Initial`, `Common`, `Recursive`, `IsFunction`, `IsPrototype`, `SourcePosition` (src/WiseOwl.Cobol.Compiler/Binding/Model/BoundUnit.cs); only after a successful bind |
| Data items | `DataItem.Level`, `CobolName`, `ClrName`, `StructName`, `Pic`, `PictureText`, `Occurs`, `OccursSpec`, `RedefinesTarget`, `Renames`, `Storage` (`StorageForm`: `NativeInt`, `NativeFloat`, `CharImage`, ...), `DeclaredAt`, `Indexes`, `Own88s`; roots in `DataBindResult.Forest` (`ForestSlice.Roots`) |
| Offsets and lengths | `RecordLayout.OffsetInRootWithEveryIndexOne`, `RecordLayout.FirstOccurrenceOffsetOf`, `RecordLayout.OccupiedBytes`, `ItemLength.Positions` |
| Data references of a statement | `BoundReferences.Of` returns `Reach` (the `Place`s a bound statement references, in source order) and a `Place` carries NO source position; `BoundStores.StoreKindOf(BoundStatement, DataItem)` answers per ITEM per statement (`StoreKind.Write`, `ReadWrite`, `WriteInPart`), not per occurrence; a written name reaches its symbol along many paths (section 6.7.2), none of which records the use: `ReferenceResolver.ResolveImpl` is one of them, not a funnel every reference passes |
| Statement position | `BoundStatement.At` (a resultant `DiagnosticCursor`, stamped by `StatementBinder.BindStatement`) |
| The object code | The backend behind the `ICodeGenBackend` seam writes the assembly; today's Roslyn backend (`RoslynBackend.Emit`, `RoslynBackend.Compile`) emits no PDB and no `#line` directive, and DESIGN.md plans a CIL backend behind the same seam. The listing reads neither backend's internals: it reads the produced assembly and its portable PDB (section 6.9) |
| The compile timestamp | `IntrinsicBinder.CompileClock` (the clock seam), read through `CompilationInputs.ReadCompilationTime`, captured lazily once per emit in `ProgramEmitter._whenCompiledStamp` and baked as WHEN-COMPILED's constant (`IntrinsicRenderer`); `CompilationInputs.ReadsCompilationTime` records that the output depends on it. CURRENT-DATE reads the run-time clock (`WiseOwl.Cobol.Runtime.IO.Clock`), not the compile timestamp |
| The CLI | `Program.BuildParser` (System.CommandLine options), `CliOptions`, `Program.RunCompile`; the library entry `CompilerDriver.Compile(CompilerDriver.Options)` returns `CompilerDriver.Result` on every outcome |

Four facts the listing needs are missing today and are added by this design:
1. the KIND of each physical line, recorded where the normalizer already decides it and exposed through a public type
   (section 4.4.1; step 1);
2. which INCLUSION a resultant line belongs to: a copybook copied twice has two inclusions whose lines share one file
   and line number (section 4.4; step 1);
3. structured bind diagnostics (section 6.3; step 3, built);
4. ONE symbol table holding every name with its attributes and its occurrences (position, length, use), which today
   is spread over dozens of tables and recorded nowhere (section 6.7.1: the store and its attributes in step 6, the
   occurrences in step 7).

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
    ListingObjectCodeMap? ObjectCode,             // from the assembly and its PDB (section 6.9)
    IReadOnlyList<ListingSectionStatus> Sections); // each requested section: produced, or why not
// (Step 1 builds Header, Files, Inclusions, Lines, Programs and Sections; step 3 Diagnostics and ListingLine.Diagnostics;
// each later step adds its members.)

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
                                //   replaced with it and not printed); step 4a adds the REPLACE / REPLACING kind, shown
                                //   as an 'R' row UNDER the line. The two render differently, so the kind is data.
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
(Step 1:) `ListingLineKind` has every member of `PhysicalLineKind` but `CommentEntryCandidate`, which the builder settles
from the parse before it maps (section 4.4.1); the switch is `ListingLineKinds.Of` and the drift test
`PhysicalLineKindDriftTests.TheKindMap_IsTotal_BothWays`.

Every other record (`ListingInclusion`, `ListingProgram`, `ListingDiagnostic`, `ListingDataItem`, `ListingSymbol`,
`ListingReference`, `ListingObjectRange`, ...) refers to lines by `Number` and to files by id. Section 6 defines each.

`ListingRequest` (also in this assembly) is the input: `Formats` (flags: Text, Html, Json), `OutputBase` (a path without
extension, or null for the default), `Sections` (flags), `PageLength`, `PageWidth`. (Step 1 builds `OutputBase`,
`PageLength` and `PageWidth`, with the one validation of each, `ListingRequest.PageLengthProblem` and `PageWidthProblem`,
that the command line calls; `OutputBase` `-` writes no file. `Formats` arrives with JSON in step 2 and `Sections` in step 5.)
(Step 2:) `ListingFormats` has `Text` and `Json` only: a format joins the enum WITH its renderer, so `Html` is added by
step 9, and no request can name a format the compiler cannot write. ONE table, `ListingOutput`, holds each format's
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
| `Programs` (attributes, the .NET type) | the symbol table's program entries | Bind | The bind finished |
| `Diagnostics` | `CompilerDriver.Result.Diagnostics`: the front end's `Diagnostic`s, the binder's records (`EditionContext.Records`), each expansion's own compilation's records moved to its EXPANDS entry, and the backend's, which the seam reports as the same `Diagnostic` (step 3; section 6.3) | Every phase | Always (what was reported) |
| `Displays` | `CompileOutputLine` with destination Listing and its origin | Text stages | The source was read |
| `DataMap` | the symbol table snapshot's entries and their attributes (section 6.6) | Bind | The bind finished |
| `CrossReference`, `Line.Names`, `Line.Margin` | the symbol table snapshot's entries and occurrences (section 6.7.1) | Bind | The bind finished |
| `ObjectCode` | the produced assembly and its portable PDB, read with `System.Reflection.Metadata` (section 6.9) | After the backend | An assembly was written |
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
and a row `R` under each line REPLACE changed prints the compiled text. (Step 1:) the banner is text in the source
columns like any line, so a banner longer than 80 positions (a long path) wraps onto `+` rows as a long line does: no row
of a page is wider than the page. A DATE-COMPILED paragraph prints its replacement
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
  names no file, so no listing names generated source. The column is a 1-based position of the line's listed text
  (`ReferenceFormatMap.ExpandedIndexAt`, tabs expanded). Listing order: placed diagnostics by line and column, then the
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
  cited; the second copy of a copybook copied twice; one record per printed diagnostic) and the anchoring assertion of
  `ListingOutcomeTests.AFailedParse_ListsThePassWhoseDiagnosticsAreReported` (section 4.4).
### 6.4 Options

Every option of the compilation with its value and whether it is the default: each public property of
`CompilerDriver.Options`, the edition, `--permissive`, the listing request itself, the compiler version (the assembly's
informational version), the runtime version, the .NET runtime version and the host operating system. Built from one table
in `OptionsSectionBuilder`; a drift test holds it to every public property of `CompilerDriver.Options`.

### 6.5 Program map (nested programs)

Every program, function, class, factory, object and method: its name, kind, nesting depth, containing program, COMMON /
INITIAL / RECURSIVE / IS PROTOTYPE, first and last listing line, and, once bound, its externalized name and the .NET
type it became. A view of the symbol table's program entries (whose attributes the binder writes from `BoundUnit` when
it creates them) joined to the source's program ranges (`SourceUnitFacts`, from the parse tree, so names and ranges
exist on a failed bind).

### 6.6 Data map: a view of the symbol table

Every data item of every program, in definition order, including those GnuCOBOL omits (INDEXED BY indexes, level-78 and
CONSTANT entries, condition-names, level-66 entries, compiler-implicit items marked as such). The data map is a VIEW of
the symbol table's data, index, condition and constant entries (section 4.1); it does not walk `DataBindResult.Forest`
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
the connector's native field.

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
| `Identify` is the only way from a written name to a symbol | rules (2) and (3) |
| A declaration question cannot answer a reference (`DeclarationFact`) | rule (4) |
| Name equality is the table's alone | rule (5) |
| Every name the programmer wrote to USE something is one occurrence | `GrammarNameSlotDriftTests` (the slots, from the grammar) and `EveryWrittenReferenceIsRecorded` (each slot, one occurrence) |
| Each occurrence carries its true store kind | the pairing test (`StorePositionsOf`) |

- **ONE recording point, made structural in both halves (rounds 2 and 3, R2-1, R3-1, R3-2).** A list of resolve paths
  was incomplete twice, so no list is the guarantee. The occurrence is recorded where a written name becomes a symbol,
  `SymbolTable.Identify`, and two mechanisms, each checked by a drift test that needs no list, make that the only way:
  - **(a) Every name table is private to `SymbolTable`.** `src/WiseOwl.Cobol.Compiler/Binding/Model/SymbolTable.cs`
    OWNS every table keyed by a COBOL name whose value is a symbol, of every name class: data, condition, index,
    procedure (`ProcedureNameMap<T>`), file (`FileSlice`, `FileNameScope`, `InheritedGlobals.Files`, the `SELECT` table),
    report, report group, report VARYING, sum counter, CD (`CommunicationSlice`), screen, the CONFIGURATION SECTION names
    (alphabet, class, symbolic-character, mnemonic, locale, through `ConfigurationNames<T>`), type and constant names,
    the special registers that are implicitly defined names (CAPACITY, DEBUG-ITEM), the REPOSITORY specifiers and the
    OO class, interface and method tables. Its one public LOOKUP is
    `SymbolTable.Identify(ParserRuleContext written, NameClass wanted, bool report) -> IdentifiedName`, where `written`
    is the name as the parser read it and `NameClass` says which classes the position admits. Every other lookup the
    tables serve today becomes a private member of `SymbolTable`. The declaring binders create the entries
    (`SymbolTable.Declare`, which records the `Defined` occurrence and returns a `SymbolId`, an opaque number, never an
    entry or a symbol) and write each entry's attributes where they decide them (`SymbolTable.SetAttributes(SymbolId,
    ...)`, section 6.6); those two and `Identify` are the table's only writers.
  - **Declarations cannot answer a reference (R4-1).** The data division's declaration questions (does this name exist
    in this scope, does it conflict with another declaration, where was it declared, is its description still open) go
    to `SymbolTable.Declarations`, a nested class whose members return only `DeclarationFact` values: a
    `readonly record struct DeclarationFact(bool Exists, DeclarationConflict Conflict, NameClass Class,
    SourceLocation DeclaredAt)`. The fact names a place in the source, never a `DataItem`, `FileModel`, `Condition88`
    or any other symbol, so no caller can turn it into an operand: a reference needs a symbol, and only
    `Identify` hands one out (inside an `IdentifiedName`). Every position the programmer wrote a name in to USE it
    goes through `Identify` and records, whatever division it is in: a RECORD KEY or ALTERNATE RECORD KEY name
    (`FileModel.RecordKeyNameCandidates` today), an ASSIGN USING data-name, a KEY phrase of SORT, MERGE or READ, an
    OCCURS DEPENDING ON or INDEXED BY operand, a REDEFINES or RENAMES object, a CONSTANT FROM or LENGTH OF operand, and
    every caller of today's `DataBinder.QualifiedCandidates` (which asks for symbols, so it is a resolution).
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
    name property or a `CobolNames.Fold` or `UpperFold` call, anywhere in `WiseOwl.Cobol.Compiler` outside `SymbolTable`,
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
    context fields and change no parse. Step 7 labels the 257 `cobolWord` occurrences of the grammar files and every
    other slot.
  - *`GrammarNameSlotDriftTests` (Unit) counts per ELEMENT OCCURRENCE, so a reused label cannot hide a slot.* It
    deserializes the generated parser's ATN (`CobolParserCore._serializedATN` through the ANTLR runtime's
    `ATNDeserializer`), computes the name-rule closure above, and walks each rule's ATN alternative by alternative,
    counting every slot transition (rule or token) it crosses, and every transition into a reference rule (R5-2): N slots
    in rule R. It then reads R's generated context
    class and counts the DISTINCT label fields with a classifying prefix: M. ANTLR generates ONE field for a label name
    however many elements carry it (`ref1=cobolWord | ref1=cobolWord` is one field), so a label reused across two
    alternatives, or a slot with no label, makes M < N, and the test is red naming the rule and the slot's ATN state.
    The test also refuses a classified field that holds no slot (M > N). Nothing lists a slot or a name rule.
  - *The planted failure.* `tests/WiseOwl.Cobol.Tests.Unit/Listing/Fixtures/NameSlotFixture.g4` is a small grammar
    generated by the same ANTLR step that generates `CobolParserCore` (`src/WiseOwl.Cobol.Frontend/Invoke-Antlr4CSharp.ps1`)
    with five rules: one fully labelled (expected clean), one with a label REUSED across two alternatives, one with an
    unlabelled `cobolWord`, one name rule with an unlabelled `INTEGERLIT` alternative, and one header rule whose
    transition into a reference rule carries no `def` label (R5-2). The self-test runs the same counter over the fixture's
    ATN and context classes and asserts it reports exactly the last four rules, each at the right slot: the check is shown
    able to fail on each way a slot can hide.
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
| Data, type, capacity register | `ForestSlice._byName`, `_typeDecls`, `_capacityRegisters` (public views `ByName`, `TypeDecls`, `CapacityRegisters`, `CapacityRegistersSoFar`); `OoMethodDataScope.ByName`, `TypeDecls`, `CapacityRegisters`; `InheritedGlobals._typeDecls` (`TypeDecls`); `ClauseFacts.LapsedTypeMembers`; `DataBinder._dataEntryLocations` |
| Condition | `ForestSlice._conditions` (`Conditions`); `OoMethodDataScope.Conditions` |
| Index | `IndexNameRegistry._byName` |
| Procedure | `ProcedureNameMap<T>._first`, `_duplicated` |
| File | `FileSlice._filesByName` (`FilesByName`); `FileNameScope._own`, `_inherited` (and its public `TryGetValue`, reached as `DataBindState.VisibleFiles`); `InheritedGlobals._files` (`Files`); `FileControlBinder.SelectedFiles` |
| Report, report group, VARYING, sum counter | `ReportScope.Named(string)` (a search, not a table: caught by rule (3)); `DataBinder._reportEntryLocations`; `ReferenceResolver.VaryingScope`; `ReportScope._sumCounters` (`SumCounters`), `DataBindState.SumCounters` |
| CD | `CommunicationSlice._byName` (`ByName`) |
| Constant | `ConstantEntryBinder.ConstantScope.Bound`, `Declared`; `InheritedGlobals._constants` (`Constants`) |
| CONFIGURATION SECTION names (alphabet, class, symbolic character, mnemonic, locale) | `ConfigurationNames<T>._entries`; `SpecialNamesBinder.RangeCollations`, `DataBindResult.RangeCollations` |
| Implicit registers | `ImplicitRegisters._debugRegisters` (`DebugRegisters`) |
| Program, function, prototype, repository | `StatementBinder.ProgramPrototypes`, `NestedCallables`, `UserFunctions`; `ConfigurationSlice.ProgramSpecifiers`, `FunctionSpecifiers`; `DataBinder._repositorySpecified`; `GroupRepositoryProvider.NoFunctions` |
| OO class, interface, method | `OoClassTable._byName`, `_ifaceByName`, `_imported`, `_importedInterfaces`; `OoClassSymbol._methods`, `_factoryMethods`; `OoInterfaceSymbol._protos`, `_conformingChoice` |
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
Screen names (`ForestSlice.ScreenNames`) are a set, not a table, and resolve through the data tables. The front end's
own name tables (compilation variables of `>>DEFINE`, the `>>COBOL-WORDS` map) are text-stage names, not program
symbols, and are outside the cross-reference.

**The resolve paths.** The Roslyn reference sweep of rounds 2 and 3 (`sweep-symbol.cs`, every reference bound by the
semantic model; the LSP server had not indexed the worktree; `WiseOwl.Cobol.Runtime` did not load for want of a
restore and declares no binder member) gives the paths into those tables today:
| Lookup or entry (today) | Its callers | Converges |
|---|---|---|
| `ReferenceResolver.Resolve` / `ResolveImpl` -> `ResolveImplCore` -> `ReferenceResolver.Identify` | `ExpressionBinder.FieldOperand`, `RefExpr`, `ResolveSending`, `ResolveReceiving`; `ObjectPropertyResolver.ResolveObjectProperty`, `ViewOfQualifiedItem`; `ProcedureTableBuilder.ReportUnresolvedDebugOperand`; `ReferenceResolver.CapacityRegisterFor` | `Identify` moves into `SymbolTable`; `ResolveImplCore` calls it |
| `ReferenceResolver.ResolveForItem` (a condition-name's conditional variable) | `ConditionBinder.AnalyzeBareOperand`, `SetBinder.BindSetCondition` | the condition-name was identified by `ConditionBinder.ConditionOf` (next row), which records the use; `ResolveForItem` takes its `IdentifiedName` |
| `ConditionBinder.ConditionOf` | `ConditionBinder.AnalyzeBareOperand`, `ReportMisqualifiedCondition`; `SetBinder.BindSetCondition`; `SetAlterBinder.SwitchCondOf`; `SearchAllFormat2Rules.CheckConditionNameSide` | becomes `SymbolTable.Identify(dref, NameClass.Condition, ...)` |
| `ReferenceResolver.ResolveForAddressOf` -> `Identify` | `PtrBinder.PtrBindAddressOf` | through `Identify` already; keeps it |
| `ReferenceResolver.ResolveTableOperand` -> `NameResolution.FindReportedItem` | `SortBinder.SortBindTable`, `SortBinder.SortAddFileKeys`, `StatementValidation.ResolveSearchTable` | `Identify(dref, NameClass.TableOperand, ...)`; `FindReportedItem` is deleted |
| `PlaceBuilder.WholeTable` (place of a whole table, after the table operand was identified) | `InitializeBinder.BindInitializeTarget`, `SearchBinder.WrittenTable`, `SortBinder.SortBindTable` | takes the `IdentifiedName` (links the place) |
| `NameResolution.FindReportedItem` | `IntrinsicBinder.TryBindAllArgument`, `ReferenceResolver.ResolveTableOperand` | deleted; both through `Identify` |
| `ReferenceResolver.ResolveByName` -> `NameResolution.FindItem` | `IntrinsicBinder.TryBindAllArgument` | replaced by `PlaceBuilder.PlaceForItem(IdentifiedName, ...)` on the name `TryBindAllArgument` already identified |
| `NameResolution.FindItem` | `ConstantEntryBinder.LengthOperandItem`, `ReportLengthOperand`; `DataBinder.ReportEntryLocation`; `PtrBindBasedAndAddressablesPass.ResolveStorageClaimHead`; `ReferenceResolver.ProbeDeclaration`, `ResolveByName`; `NameResolution.FindReportedItem` | private to `SymbolTable`; the data-division callers (a CONSTANT LENGTH OF operand, a BASED claim) call `Identify` and record a data-division use |
| `NameResolution.ResolveQualified` / `ResolveUnqualified` | `ReferenceResolver.Identify`; `NameResolution.FindItem`, `DeclarationOf`; `SubscriptBinder.ResolveSubscriptName`, `CannotBeSubscripted`; `ObjectPropertyResolver.ViewSubjectChain`, `IsObjectPropertyReference`; `CounterResolver.SumCounterFamilyFor` | private to `SymbolTable`; `ResolveSubscriptName` (a subscript naming a data item) and `SumCounterFamilyFor` call `Identify`; the two object-property predicates and `CannotBeSubscripted` are probes (`report: false`) |
| `NameResolution.ResolveIndexName` | `ExpressionBinder.IndexFieldOf`; `SubscriptBinder.SubscriptNamesIndex`, `ResolveSubscriptName`; `SearchAllFormat2Rules.CheckKeySubscript` | `Identify(..., NameClass.Index, ...)` |
| `SymbolTable.TryResolve` | `DataBinder.ResolveClauseOperand`, `QualifiedCandidates`, `NonDataDeclarationOf`; `InspectBinder.BindPhraseOperands`; `IntrinsicBinder.TypeNameArgument`, `KeywordWordOf`, `KeywordOmittedFunction`; `PtrBinder.PtrResolveBased`; `SortBinder.SortBindTable`; `SubscriptBinder.IsFunctionBearing`; `StatementValidation.RejectProcedureName`; `NameResolution.ReportUnidentified`, `DeclarationOf`; `SymbolTable.TryResolveUnqualified`, `TryResolveCondition` | becomes private inside the grown `SymbolTable`: `DataBinder.ResolveClauseOperand` (a clause operand such as DEPENDING ON) and `InspectBinder.BindPhraseOperands` call `Identify`; `QualifiedCandidates` asks for symbols, so its callers (RECORD KEY, ASSIGN USING, the KEY phrases) call `Identify` and record; only `NonDataDeclarationOf`, a declaration question, calls `SymbolTable.Declarations` and gets a `DeclarationFact`; the word-class probes (`KeywordWordOf`, `KeywordOmittedFunction`, `TypeNameArgument`, `IsFunctionBearing`, `RejectProcedureName`) pass `report: false` |
| `SymbolTable.TryResolveUnqualified` | `NameResolution.ResolveUnqualified`; `InitializeBinder.BindInitializeTarget`; `IntrinsicBinder.FunctionPointerItemNamed`; `PtrBindBasedAndAddressablesPass.ResolveStorageClaimHead`; `SymbolTable.NearestDeclaringData` | as above: the two binders call `Identify` |
| `SymbolTable.TryResolveCondition` | `DataBinder.QualifiedConditionCandidates`, `ConditionsUnderQualifiers`; `ConditionBinder.ReportMisqualifiedCondition`; `SearchAllFormat2Rules.CheckKeySide`, `CheckConditionNameSide` | through `Identify(..., NameClass.Condition, ...)`, or `Declarations` for the data-division checks |
| `SymbolTable.IndexCandidates` | `NameResolution.ResolveIndexName`; `DataBinder.NonConditionDeclarationsOf`; `IndexNameRegistry.Candidates`; `FlagConformancePass.FlagIndexSet` | `ResolveIndexName`'s lookup inside `Identify`; `FlagIndexSet` reads the occurrences instead of looking up again |
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

### 6.9 COBOL to object-code map (backend-neutral; owner 2026-11-18)

The owner, 2026-11-18: "It also makes no sense to link to C# code when we switch to the IL generation." No section,
renderer, link or schema field names, reads or points into generated source (`.g.cs`) or any Roslyn type. The map is
read from what every backend produces, the ASSEMBLY and its PORTABLE PDB:

- **Source of truth.** `ObjectCodeMapReader` (Compiler, `Listing/`) opens the produced assembly and its PDB with
  `System.Reflection.Metadata` (`MetadataReader`, `MethodDebugInformation.GetSequencePoints`, `MethodBodyBlock`). For
  each method it reads the sequence points: a document (a COBOL source or library text), a start and end line and
  column, and an IL offset. Each COBOL statement's listing line is placed through its source position by the one line
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
source with inline diagnostics, program map, data map, cross-reference, statistics and verbs, object-code map, diagnostic
summary; each starts a page.

### 7.2 HTML (`HtmlListingRenderer`)

One self-contained file (inline CSS, a small inline script for collapse and filter, no external resource). Every name on
a source line links to its definition, and every definition lists its references as links; each diagnostic is anchored
on its line and listed in a summary panel that links to it; each inclusion is a collapsible block; suppressed and
omitted lines are present but collapsed and greyed; each statement opens its method, IL offset range and (when requested) IL
instructions beside it. Links come from
each line's `ListingLine.Names` (built once from the reference occurrences, section 6.7.1) and the definitions' reference
lists from `ListingCrossReference`; the renderer joins nothing and computes nothing.

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
  file). It is NOT yet declared a preflight class: `scripts/buildgraph.py`'s preflight rule admits only a class the merge
  group has MEASURED in `tests/_shared/test-weights.json`, and a new class has no measurement, so it runs in the population
  until the weights carry it; declaring it then is kb/Work PB5925's lead `2026-11-18-schema-drift-preflight`.
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
  is the moment kb/Work PB5925's lead `2026-11-18-schema-version-bump` names for the surface record.

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
path is never taken for a format) and accepts `text` and `json`; `html` is refused until step 9 builds its renderer. A
validator on the root command applies `ListingRequest.FormatsProblem` to the two options together (`-` with two formats is
an argument error). With `-`, `RunCompile` prints `ListingOutput.Render` of the one format before the diagnostics; the
`>>DISPLAY` lines that go to standard output follow it there, so a JSON listing on standard output is followed by them
(step 4a sends DISPLAY UPON LISTING into the listing itself).
`Resolve` builds a `ListingRequest` on `CliOptions`; `RunCompile` passes it as `CompilerDriver.Options.Listing`. The
library default is null (no listing), so every caller that builds `Options` itself (the test suites, `check-batch`, an
expansion's own compilation in `ExpansionAssemblies`) does no listing work; the per-edition default is a CLI decision.
`check-batch` never writes a listing. `Options.Listing` decides what the build writes, never what a source means:
`CompileStateDriftTests` classifies it as an output selection.

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
  assertion able to fail. Occurrences join it in step 7. (Step 1, review R1-3:) a recording call's ARGUMENTS are work too
  (`CopyProcessor.ExpandCopiesOneLevel` computes the COPY statement's last origin, a `LineOrigin` per statement, to pass
  to `BeginInclusion`), so every recording call into `TextStageTrace` is asked only when `TextStageTrace.Records` is true,
  and `TextStageTrace.None`'s recording methods THROW: a call site that forgets the question fails every compilation of a
  COPY statement without a listing, the no-work drift test's own program first.
- **Budgets, measured.** No listing: compile time within the run-to-run noise of the parent commit, and allocated bytes
  per compile no more than the parent's plus the line-kind arrays (one byte per physical line read). Standard text
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
   of the twelve 2002 rows of section 2.2. `Listing2014Tests.cs`: the same against §2014:7.3.16.2 GR2-GR5, §2014:7.3.17.3
   GR2, GR3, §2014:7.2.2.3 GR4, GR5 and §2014:15.92.2 RV3. No DISPLAY case at 2002 or 2014: the directive is new in 2023.
3. **Provenance.** A copybook copied twice with different REPLACING: two blocks, each `R` row's compiled text equal to
   the replacement the test wrote; a diagnostic in the second copy is anchored in the second block. Omitted lines of a
   false `>>IF` are marked and listed.
4. **Sections.** Data map: offsets and lengths computed by hand from the PICTURE clauses of the test's own record, every INDEXED BY index and constant present; cross-reference: a fixture with one read, one
   write, one modify (ADD TO) and one definition of the same name yields exactly those four uses at those lines and
   columns, `ADD WS-A` / `TO WS-B` on two lines puts each use on its own line, and `MOVE WS-X (WS-X) TO WS-X` yields two
   reads and one write (section 6.7.1); a line's `Margin` and `Names` equal what the data map and cross-reference say of
   that line, and the text margin and the HTML links render exactly them; object-code map: each statement's method and IL
   range equal what the PDB's sequence points say, read independently in the test with `System.Reflection.Metadata`.
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
7. **Unit tests** of `ListingVisibility` and `PageComposer` over hand-built line lists (one per rule above), and of
   `ListingLineIndex` over hand-built maps (step 1: `ListingLineIndexTests`, a copybook copied at two depths, framing
   lines, a file no inclusion holds, omission); `PhysicalLineKindDriftTests` (section 4.4.1).
7a. **Diagnostics (step 3).** The records and their rendered views, the severity map, the placement by frame over hand-built
   maps, the caret, the wrapping and the summary (Unit); a bind error placed, printed and cited, a diagnostic in the second
   copy of a copybook placed in the second block, and one record per printed diagnostic on a front-end error, a bind error and
   a success with warnings (`ListingDiagnosticsTests`, Conformance); the shared citation vectors (`ListingCitationTests` and
   `citation.py --self-test`). Section 6.3 names each.
8. **Failed compilations.** One test per kind of outcome (section 4.6): the sections present and the
   `ListingSectionStatus` of each absent one; the failed-parse test of section 4.4 (the listed pass). (Step 1:)
   `tests/WiseOwl.Cobol.Tests.Conformance/Listing/ListingOutcomeTests.cs`, beside the 1985 class's failed-compilation
   case, and `ListingOutcomeMapTests` (Unit) for the backend error no source reaches (section 4.6).

The inventory rows the listing makes testable (`1985-GR-VI.3.4.4-1`, `1985-GR-VI.3.3.4-1`, `1985-GR-X.3.1.4-1`,
PB5924's IV.7 rows, the 2023 LISTING/PAGE/COPY rows whose derivations rest on "no listing", and the twelve 2002 rows of
section 2.2) take these tests as witnesses in the records change of each step.

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

**Step 4a - the directive hooks, witnessed at 2023.** `TextStageTrace` gains `ListingDirective`, `PageDirective` and
`Replaced` (recorded in `ConditionalCompilationProcessor.Run.Render`, `CopyProcessor.ApplyReplacements`,
`CopyProcessor.ApplyReplaceStatements`); SUPPRESS on inclusions; `CompileOutputStream.Listing` and
`CompileOutputLine.At`, `ConditionalCompilationProcessor.DisplayDestinations`; `ListingVisibility`'s directive rules;
`DirectiveStateRegistry`'s LISTING row stops saying "NONE HELD"; DOC-A.1-53, DOC-A.1-54, DOC-A.1-117, DOC-A.1-200; the 2023
derivations that rest on "no listing" (`DRV-GR-7.3.18.3-2..5`, `DRV-GR-7.3.19.4-2`, `DRV-GR-7.2.3.4-4/-5`) are retired and
witnessed by `Listing2023Tests`. The code is edition-neutral: the same trace and visibility rules serve 2002 and 2014.

**Step 4b - 2002 and 2014.** `tests/WiseOwl.Cobol.Tests.Conformance/Listing/Listing2002Tests.cs`
and `Listing2014Tests.cs` (section 10 item 2), each citing its own edition's rules (memory rule: each edition stands on
its own text); the records change adjudicates the twelve 2002 rows of section 2.2 (`2002-FMT-7.2.15.1`,
`2002-FMT-7.2.16.1`, `2002-GR-7.1.2.3-4`, `-5`, `2002-GR-7.2.15.2-2` to `-5`, `2002-GR-7.2.16.3-2`, `2002-RV-15.80.2-3`,
`2002-DOC-B.1-90`, `2002-DOC-B.1-165`) with those tests as witnesses; DOC-A.1-117's 2002 and 2014 annex items (B.1 item
90, A.1 item 106) and the stage items (B.1 item 165, A.1 item 182) are documented.

**Step 5 - options, program map, statistics and verbs.** `OptionsSectionBuilder` with its drift test; the program map's
names, nesting and ranges and the verb counts from `SourceUnitFacts` (the source); `--listing-sections`. The program
attributes and .NET types, and the statistics' counts of names and uses, join in steps 6 and 7 as views of the symbol
table when it exists.

**Step 6 - the symbol table and the data map.** The ONE store first: `NameTableOwnershipDriftTests` is written first on
the step's branch and runs red, its failures matching the evidence of section 6.7.2 (a check that could fail); then
`src/WiseOwl.Cobol.Compiler/Binding/Model/SymbolTable.cs` grows into the table (`SymbolEntry`, `CobolName`,
`SymbolTable.Declare`, `SymbolTable.SetAttributes`, `SymbolTable.Identify` as the only lookup, `SymbolTable.Declarations`
returning only `DeclarationFact`), every name table moved into it and made private, every lookup converted to `Identify`
or classified in `string-parameter-members.json` with its reason, every `CobolNames.Same` / `Comparer` / name comparison
moved in or classified in `name-comparisons.json` (seeded from section 6.7.2's evidence), and each attribute of section
6.6 written into its entry where the binder decides it (the declaring binders, the redefinition pass, the layout step,
the storage-form decision with its one .NET-type spelling); `SymbolTable.Finish()` and the snapshot handed to the
collector. The step lands only with `NameTableOwnershipDriftTests` green. Then the data map and file map as views of the
snapshot (`DataMapBuilder`), `LineAnnotationBuilder` with `ListingLine.Margin` for definition lines, and the `All`
benchmark row. `Identify` records nothing yet; the table's entries and attributes are complete.

**Step 7 - occurrences and the cross-reference.** The table's second half: `GrammarNameSlotDriftTests` written first
and red (with the `NameSlotFixture.g4` self-test), then the grammar labels on every name slot (`CobolParserCore.g4` and
the `Core/*.g4` fragments, regenerated) and `ReferenceOccurrenceDriftTests`; `Identify` appends each `SymbolOccurrence`
to its entry (Identify's register arms included), `PlaceBuilder.PlaceForItem` / `PlaceBuilder.WholeTable` take
`IdentifiedName`, `StatementBinder.BindStatement` classifies the occurrences since its mark through the new
`BoundStores.StorePositionsOf`, all gated by `EditionContext.RecordsReferences`. The step lands with the grammar test,
the completeness oracle and the pairing test green. Then `CrossReferenceBuilder` (a view of the entries and their
occurrences), the PERFORM and CALL graphs, `LineAnnotationBuilder`'s `ListingLine.Names` and statement-line `Margin`;
the `All` row.

**Step 8 - COBOL to object-code map.** Inside the Roslyn backend, behind the seam: the `#line` directives written by
`StatementEmitter.EmitStatement` and the portable PDB (`RoslynBackend.Compile`'s `EmitOptions`). In the listing:
`ObjectCodeMapReader`, `IlDisassembler`, `ObjectCodeMapBuilder`; `SequencePointCoverageDriftTests`,
`ListingBackendNeutralityDriftTests`; backend diagnostics placed through their COBOL positions; the `All` row.

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
   them (section 6.9). A .NET debugger steps through COBOL source as a consequence.
5. **Omitted lines shown or hidden.** Recommendation: shown and marked 'X' (IBM's default NOSKIPSRC); add
   `--listing-omitted hide` only if a user asks.
6. **Publishing the schema at a URL.** Publishing is the owner's (CLAUDE.md, "Still the owner's"). Recommendation: the URN
   identifier now; ask the owner when the product's documentation site is updated for the listing.
7. **A listing for an expansion's own compilation** (`ExpansionAssemblies`). Recommendation: none; its text is the
   repository's skeleton, and its diagnostics already report at the EXPANDS entry of the user's listing.
