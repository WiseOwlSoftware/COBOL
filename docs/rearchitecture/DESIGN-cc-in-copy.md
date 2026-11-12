# DESIGN — conditional-compilation directives inside COPY (merged text-manipulation driver, ISO §2023:7.2.1)

> **STATUS: IMPLEMENTED (DEVLOG 966).** Subsystem deep-dive for merging the conditional-compilation (CC) and
> COPY text-manipulation stages so directives INSIDE copybooks are processed (ISO §2023:7.2.1). Design SSOT; the plan
> (`REARCHITECTURE_PLAN.md` §0) points here. Keep current with the code (rule 4). The merged
> `ConditionalCompilationProcessor.ProcessWithCopy` driver is wired into `Frontend`; the legacy `Process`/COPY
> paths stay byte-identical. Gate: characterization 33/33 · CC/COPY unit 26/26 · CopyConditional 8/8 · the
> `cc_in_copy` golden · GnuCOBOL +2 fixes/0 regressions · legacy guard ALL GREEN · greenfield Conformance 3806/0.

## §1 The defect

ISO §2023:7.2.1 orders the text-manipulation stage: **Step 1** incorporate COPY library text (false-path IF/EVALUATE
lines, *including* COPY'd text, may be omitted); **Step 2** process DEFINE/IF/EVALUATE + variable substitution +
PUSH/POP + COPY REPLACING, in encounter order over the expanded group; **Step 3** REPLACE; **Step 4** COBOL-WORDS.

The current greenfield pipeline (`Frontend.Preprocess`) runs **CC (Step 2a) BEFORE COPY (Step 1)** —
deliberately, so a main-source `>>IF` can gate a `COPY` statement. Consequence: a `>>DEFINE`/`>>IF`/`>>EVALUATE`
**inside a copybook** is never processed — it survives COPY expansion and reaches the lexer as a stray token
(**verified:** a copybook `>>IF` → `COBOL0001: unexpected '>'`). Steps 3/4 (REPLACE, COBOL-WORDS) and Step 2d
(COPY REPLACING) are already correctly ordered — the ONLY defect is Steps 1/2.

## §2 What must be preserved (verified firsthand)

1. A main-source `>>IF USEIT = 0` gating a `COPY` → the COPY is skipped (prints the un-copied value).
2. A **missing copybook in a false branch** must NOT error (today it works because CC blanks the false-branch
   COPY line before `CopyProcessor` runs). **This is why a simple order-swap (COPY-first) is unacceptable** —
   it would expand the false-branch COPY and raise a spurious CBL3620.
3. Byte-identity of the conditional-compilation-only entry point: `ConditionalCompilationProcessor.Process` (the
   merged driver with no COPY processor) keeps its exact behavior, because the direct unit tests depend on it. The
   COPY-only entry point `CopyProcessor.Process` and its string-only helpers are deleted: after the legacy compiler
   went (P15 Cut 2) nothing called them (kb/Work PB2200).
4. The H3 line-count-preserving discipline: the five downstream directive-collection stages
   (TURN/PROPAGATE/REF-MOD/FLAG/COBOL-WORDS) run AFTER, on the final expanded text; the `linesBefore` baseline is
   captured after the merged driver. The merged driver itself changes line counts (COPY inserts lines) — it runs
   BEFORE the baseline snapshot, exactly where CC+COPY run today.

## §3 Mechanism — one interleaved recursive driver (owner-directed)

A new greenfield class **`CopyConditionalProcessor`** fuses the CC branch-selection state machine with COPY
expansion into ONE pass with **shared directive state**, so:
- an emitting-branch `COPY` is expanded and the copybook's incorporated text is fed through the SAME pass (so
  copybook `>>DEFINE`/`>>IF`/… are processed, with the DEFINE table shared across the copybook boundary);
- an omitted-branch `COPY` is DROPPED (never expanded → constraint §2.2 holds);
- a main-source `>>IF` still gates a `COPY` (constraint §2.1) — the driver reaches the `>>IF` first and its
  false branch omits the following COPY line.

This is the spec model (Step 1 flatten ⊕ Step 2 CC, processed in encounter order over the expanded group)
realized lazily so false-path COPYs are never incorporated.

### §3.1 Shared state (`CcState`)

Extracted from the current `ConditionalCompilationProcessor.Process` locals into a per-run object threaded
through recursion: the `defines` map (DEFINE/OFF/OVERRIDE), the `>>IF`/`>>EVALUATE` frame `stack`, the
`FlagScanState` (the frontend-inline FLAG options b/c), and the shared `CompileTimeExpressionEvaluator`. One
`CcState` per compilation group; recursion into a copybook shares it (so a copybook DEFINE is visible to
following main-source directives — spec Step-2 encounter order).

### §3.2 COPY expansion reuse

The copybook mechanics stay in `CopyProcessor` (find/read, `NormalizeCopybook` fixed→free, COPY REPLACING via
`ApplyReplacements`, the circular/`MaxCopyDepth` guards). The merged driver calls a NEW `CopyProcessor` method
that expands ONE COPY statement at a given position — parse name + REPLACING (to the terminating period, possibly
multi-line), resolve, normalize, apply REPLACING — and returns the copybook text (NOT recursively expanded; the
merged driver recurses so nested COPY *and* nested CC both process). `alreadyIncluded` + `depth` thread through
for the GR12 circular / depth-20 guards.

### §3.2b A directive never stands inside a COPY or REPLACE statement (kb/Work PB1384, §2023:7.3.3 SR8 b)

The driver ends its text block at every directive line, which is also the only place it can SEE the violation: before it
flushes, `Render` asks `CopyProcessor.OpenStatementAt` whether the text since a COPY or REPLACE keyword has reached its
separator period (the §3.2a words, pseudo-text delimiters toggling, so a period inside pseudo-text ends nothing). The open
statement's text is carried from its keyword across further directive lines, so a statement split by several is reported at
each (COBOL2697, `DirectiveDiag.WithinStatement`). The directive then takes effect like any other (superset-continue),
and a truncated COPY still draws its own COBOL2449 after it.

A `>>SOURCE FORMAT` line never reaches `Render` (kb/Work PB1353): logical conversion DISCARDS it (§2023:6.5 1)) and leaves a blank
line, with its physical line recorded in the text's `ReferenceFormatMap.DirectiveLines`. `Render` therefore asks, of each
BLANK line, `CopyProcessor.IsDiscardedFormatDirective(origin)` — the map registered under the line's file, which is the
main source's or the library text's own — and puts the same `OpenStatementAt` question over the pending block WITHOUT
flushing it (the discarded line takes nothing from the text). One question, one answer, whichever stage consumed the
directive. Inside pseudo-text the diagnostic also quotes §2023:7.2.3.3 SR10 / §2023:7.2.4.3 SR10 (`OpenStatementAt`'s `InPseudoText`).

### §3.2a Text-words — the one scanner under COPY and REPLACE (kb/Work PB1350 / PB1351 / PB1354)

Everything the text-manipulation stage does is decided over §2023:7.2.2.5 TEXT-WORDS, and ONE type forms them:
`TextWordScanner` (`src/WiseOwl.Cobol.Frontend/Preprocessor/TextWord.cs`). A space or a comment is no text-word; the
colon and parentheses are separator text-words outside literals; a period, comma or semicolon separates only when
a space (or a line end, or a closing `==`) follows (§2023:8.3.5 2)/3)), so `Z,ZZ9` and `ZZ.ZZ` are one word; a literal
is ONE word from its prefix (`X" N" NX" B" BX"`) through its closing delimiter with doubled quotes inside; `&` is a
word of its own; `==` is the pseudo-text delimiter. Its three consumers:

- **Locating COPY and REPLACE** — `FindStatementKeyword(text, pos, keyword, onGlued)` returns the next text-word
  `COPY` / `REPLACE`, so it cannot fire in a literal, a comment, a directive line or a longer word, and a REPLACE is
  found wherever it stands, not only first on a line (§2023:7.2.4.3 SR1, kb/Work PB1358); a keyword glued behind
  `.`/`,`/`;` forms no text-word and is reported (SR2: COBOL2451 for COPY, COBOL2449 for REPLACE).
- **Parsing the statements** — `ParseCopyStatement` reads the §2023:7.2.3.2 general format in order
  (`{text-name-1 | literal-1} [{OF|IN} …] [SUPPRESS [PRINTING]] [REPLACING …] .`) through a `StatementCursor`, and
  the REPLACE statement uses the same cursor and the same `ParseReplacingOperands`. A statement ends at its
  SEPARATOR period (§2023:7.2.3.4 GR6), never at a `.` inside pseudo-text or a literal; a word out of order is
  COBOL2449, SR4/SR5 literal forms COBOL2450, a COPY within a COPY statement COBOL2451 (SR1), and a literal its
  line ends inside COBOL5563, named by the cursor as it consumes the text-word and standing as the statement's one
  error (`LiteralScreen.ReportUnterminated`, the lexer's own question; kb/Work PB4176). Each operand
  pair is screened by ONE `ScreenOperandPair` against its statement's `OperandRules` row — the §2023:7.2.3.3 / §2023:7.2.4.3
  content rules are word-for-word twins (COBOL2572) — and `ReadOperand` checks the §2023:8.3.5 6) separation of each
  `==` (COBOL2573) (kb/Work PB1353). A directive line CONSUMED before the operand is read leaves only a blank
  line, so the operand screen cannot see it; §3.2b's driver check does (§2023:7.3.3 SR8 b), and names §2023:7.2.3.3 SR10 /
  §2023:7.2.4.3 SR10 when the open statement stands inside pseudo-text. The LEADING / TRAILING phrases are gated at their
  introducing edition by constructs row `replacing-partial-word-2002` — ONE check, `OperandScreen.PartialWord`, asked
  by `ParseReplacingOperands` for both statements, once per statement (kb/Work PB1670). The statement's OWN phrases
  that 1985's `COPY text-name [{OF | IN} library-name] [REPLACING …]` did not have are gated by ONE method,
  `CopyProcessor.GateCopyPhrase`: each literal written as text-name-1 / library-name-1 (`copy-literal-name-2002`,
  asked by `ReadCopyName`) and the SUPPRESS phrase (`copy-suppress-2002`, asked by `ParseCopyStatement`) —
  COBOL0900 below 2002, an error under `--permissive` too (kb/Work PB1601; VCR row 7.44).
- **The REPLACE states** — `ApplyReplaceStatements` parses format 1 `REPLACE [ALSO] …` and format 2
  `REPLACE [LAST] OFF` and drives `ReplaceStates` (§2023:7.2.4.4 GR4–GR7): the active operands plus a LIFO stack of
  inactive ones; ALSO pushes and activates current-then-pushed operands, LAST OFF pops, a plain format 1 or OFF
  cancels the queue (kb/Work PB1357). ALSO/LAST are gated by constructs row `replace-also-last-2002`.
- **Matching** — `ApplyReplacements` compares text-words with `TextWord.MatchesForReplacing`, the ONE
  implementation of §2023:7.2.3.4 9) c) / §2023:7.2.4.4 8) c): character-strings case-insensitively; literals by prefix
  (case-insensitive), content un-doubled, the quotation symbol not compared, content case-SENSITIVE in `"…"` and
  `N"…"` and insensitive in the hexadecimal and boolean formats. Separator commas/semicolons are dropped (c) 1.).
  What a match PRODUCES is screened in the same pass by the statement's `ResultRule` (`ForbiddenIn`: §2023:7.2.3.4 GR13 /
  §2023:7.2.4.4 GR9 — no COPY statement, REPLACE statement (REPLACE only), SOURCE FORMAT directive or comment, the
  comment asked of the produced TEXT by `TextWordScanner.HoldsComment` since the scanner skips a comment; a blank
  line cannot arise, §2023:6.5 2) discarded it; COBOL2574), and a COPY with REPLACING met inside library text is COBOL1640 (GR12) (kb/Work PB1356).
- **Locating library text** — `FindCopybook` over `LibraryPlaces()` (the working directory, then each configured
  search path) and `LocateInLibrary` (as spelled, then — unless the FILE NAME holds a period —
  `.CPY .CBL .COB .cpy .cbl .cob`): the DOC-A.1-40
  determination; the source file's own directory is not searched, and a text not found is CBL3620 on every path
  (kb/Work PB1355).

A fixed-form debugging line reaches the stage as `ReferenceFormatProcessor.DebugLineCarrier` (`*>` plus the Unicode
noncharacter U+FDD0) + its text; the scanner skips only the carrier, so the line's text-words take part in matching (the
COBOL-85 rule) while no comment a programmer writes can be mistaken for one. The lexer later reads the carrier as the
hidden `DEBUG_LINE` marker and `DebuggingLineRewriter` keeps or hides the line's tokens (kb/Work PB1705).

### §3.3 The `leave*` flags + the collection stages

The driver keeps the exact `leave*` behavior: an emitting-branch `>>TURN`/`>>PROPAGATE`/`>>REF-MOD-ZERO-LENGTH`/
`>>FLAG-02`/`>>FLAG-14`/`>>COBOL-WORDS` survives to its downstream stage; an omitted-branch one drops; the FLAG
options b/c update the shared `FlagScanState`. REPLACE (Step 3) runs after the whole interleaved expansion
(`CopyProcessor.ApplyReplaceStatements` over the merged text); the five collection stages + COBOL-WORDS run
unchanged on the final text.

### §3.4 Copybook reference format (§2023:7.3.24.3 GR3 / GR5 — kb/Work PB1067)

`NormalizeCopybookMapped` runs library text through the ONE §2023:6.5 walker the main source uses
(`ReferenceFormatProcessor.NormalizeToFreeFormMapped`), starting in the format in effect for the COPY statement (GR3).
Every normalization reports a `ReferenceFormatMap` (the format of each physical line); `Frontend.Preprocess` registers
the compilation group's map with `CopyProcessor.RegisterReferenceFormat`, each library text's map is registered as it
is normalized, and `ResolveOneCopy` asks the map of the text holding the COPY at the COPY's origin line
(`ReferenceFormatMap.LibraryTextDefaultAt`: a stated or inherited format, or a DETECTED fixed one, is handed on; a
text detected free hands nothing on and the library text is detected on its own — CONFORMANCE.md DOC-A.1-158). The
copybook's own `>>SOURCE FORMAT` segments are resolved within it and the COPY's text was converted on its own, so
GR5's revert holds by construction.

### §3.5 The origin map (kb/Work PB82 — landed 2026-08-18; columns kb/Work PB1801 / PB1688)

Every stage of the chain that CHANGES the line count or the columns is MAPPED, with no exception (the `--nist`
placeholder substitution, `NistPreprocessor.Process`, was the last one, kb/Work PB2986): it takes and returns a
`MappedText` (the text plus ONE `LineOrigin` per `Split('\n')` piece — the invariant the constructor asserts), and its
string overload is the mapped one's `.Text` (one implementation, never two). A `LineOrigin` is the piece's ordered list of
`OriginSpan(ResultColumn, SourceOrigin(File, Line), SourceColumn)`: the characters from a resultant column on came
from that physical line, starting at that PHYSICAL column (an index of the line as the file holds it, so a TAB counts
one, as in the user's editor). The piece is NAMED by its first span's line (`LineOrigin.Origin`, the PB82 rule: the
origin of the first content written into it), and `LineOrigin.Locate(column)` is the one place a resultant column
becomes a physical one. `OriginWriter` assembles the outputs: a slice of a mapped input keeps the input's spans,
rebased to the column it lands on; text a stage writes itself takes one stretch at the position it stands for, a character position per character (a supplementary-plane character ends the span, kb/Work PB3031)
(`MappedText.PointAt`). The stages and their splice rules —
- the reference-format conversion (`ReferenceFormatProcessor.LogicalLineBuilder`, fed a `TextSite` per physical line:
  the program text's start index in the expanded line and the line's `ColumnMap`) BEARS the spans: a fixed-form line's
  program text maps from position 8, a free-form line from position 1, a line that held a tab or a supplementary-plane
  character gets a span wherever the physical columns (character positions) stop running one for one, and a continuation join appends the continuation's own span (its line,
  and the column of its first appended character — the one after its opening quotation symbol for a literal), so the
  joined line keeps its head line's NUMBER while every fragment keeps its own line and column; a discarded
  `>>SOURCE FORMAT` directive line keeps its slot;
- the CC driver's `Render(MappedText)`: an omitted or directive line keeps its own origin (a blank output line), a
  block keeps its lines', a copybook expansion the copybook's (through `RenderCopybook(MappedText, depth,
  lineOffset)` — the offset is where `ExpandCopiesOneLevel` splices the expansion, which lets the driver name the
  output-frame line of every directive it meets, the DIRECTIVE ENCOUNTERS the §2023:14.9.28.4 GR14 fixed point keys its
  implicit PUSH ALL / POP ALL to; `DESIGN-frontend-grammar.md`, kb/Work PB1066);
- `CopyProcessor.ExpandCopiesOneLevel(MappedText)`: the text before a COPY keeps its origins, the two framing
  newlines belong to the COPY statement's own line, the incorporated text carries the copybook's path and physical
  lines (`NormalizeCopybookMapped` — a fixed-form member's continuation joins are tracked exactly like the main
  source's) — and `ResolveOneCopy`, `OnNonPseudoTextOperand` and every COPY / REPLACE statement diagnostic report at
  `MappedText.LocateAt(position)` (the file, line AND column of the position they are looking at), the CC
  `DirectiveDiag` at the directive's indicator, never at an ordinal of the text being processed. `MappedText` has no
  accessor that answers a position with a column-less `SourceOrigin` (`SourceLineMapTests`);
- `ApplyReplaceStatements(MappedText)` / `ApplyReplacements(MappedText)`: a REPLACE statement's own lines vanish
  from the resultant text; kept text keeps its origins, a replacement's text takes ONE span at the position of the
  text it replaced (the only honest answer for synthesized text), and the text after it on the line keeps its own
  columns. `ReplaceLineMap` is the same pass over line-index origins: where each input line lands in the
  resultant text (the directive encounters' resultant lines, kb/Work PB1066);
- `NistPreprocessor.Process(MappedText, testName)` (under `--nist` only, after COPY and REPLACE): every CCVS
  placeholder substitution goes through ONE loop (`RewriteMatches`) that appends the text between matches as a slice
  of the input and the substitute as text at the position of the placeholder it replaces, so a substitute longer or
  shorter than its placeholder (`XXXXX058` → `"CONTROL"`, `XXXXX083` → `DOTNET`), a deleted card (`XXXXX069`) and the
  rewrite inside a SELECT entry (`RewriteRegions`, which maps the entry's slice and splices it back) all leave every
  later character at its physical column. Placeholders in COPY library text are substituted after the copybook's
  lines are spliced in, so they carry the copybook's file and line (`DiagnosticPositionTests`).

`Frontend.Preprocess` returns the final `MappedText`, publishes `Frontend.LineMap` (`SourceLineMap`: resultant
line → origin; `Locate(line, column, fallbackFile)` is the ONE conversion to a 0-based `SourceLocation`) built from
the NIST stage's output, asserts the six directive stages are line-count preserving, and hands the map to the parser listener
(`CobolErrorListener` — which also stopped reporting every syntax error one line late), to the directive stages'
diagnostics, and (via `CompilerDriver` → `EditionContext.LineMap`) to the binder's diagnostic cursor and
`EXCEPTION-LOCATION`. The `>>TURN` / `>>FLAG` / `>>REF-MOD-ZERO-LENGTH` event lines are compared with token lines
and therefore stay in RESULTANT space — the map is consulted only at the user-facing boundary. Consumer side:
`docs/VALIDATION_DESIGN.md` §2 "Positions".

### §3.6 What text manipulation asks of the edition (kb/Work PB5085, PB5126, PB5277)

COBOL-85 and the editions from 2002 differ in three rules of this stage, each asked of the targeted edition at ONE site:

- **A REPLACE statement's place.** §1985:XII.3.3 SR1 requires a separator period before it (or the start of a separately compiled
  program); §2002:7.1.3.2 SR1 allows it wherever a character-string may stand. `ApplyReplaceStatements` asks the constructs row
  `replace-anywhere-2002` of a REPLACE that `FollowsSeparatorPeriod` does not accept. It asks the COPY-expanded text, whatever file each
  character came from (kb/Work PB5422): a COPY statement and its period are not part of the resultant text, so a REPLACE that begins
  library text follows what preceded the COPY statement. It reads the last text-word before the REPLACE, so a comment, a comment-entry carrier
  and a debugging line's carrier are spaces to it.
- **COPY in library text.** §1985:XII.2.4 GR12 forbids it; §2002:7.1.2.3 GR14 allows it without REPLACING. `ResolveOneCopy` asks the
  row `copy-nested-library-text-2002` of the library text it has read, and reports at the OUTER statement. Below 2002 the one
  report speaks for the REPLACING arm (the 2023 GR10 diagnostic) too.
- **The scope of a REPLACE.** §1985:XII.3.4 GR3 ends it with the separately compiled program, §2002:7.1.3.3 GR5 with the compilation
  group. `EditionInfo.ReplaceScopeEndsWithSeparatelyCompiledProgram` is the edition fact; below 2002 `ApplyReplaceStatements` cuts the text at each
  `END PROGRAM` header that closes no open program (`SeparatelyCompiledProgramEnds`, read as text-words so pseudo-text is never a
  header) and `ReplaceStates.EndScope` drops every statement in effect. A contained program lies inside its separately compiled program.

The word COPY as word-1 or word-2 of a COPY REPLACING phrase is refused by COBOL7006 at 85, 2002 and 2014 (§1985:XII.2.3 SR6, §2002:7.1.2.2 SR10,
§2014:7.2.2.2 SR10), and so are the compiler directive indicator `>>` (2002, 2014) and the floating debug indicator `>>D` (2002); the comment
indicator is a comment to the scanner and never a word. 2023 removed the word forms (COBOL0902); under `--permissive`, which still accepts them,
the 2014 set applies (`OperandScreen.CopyWord`).

### §3.7 The lines a replacement introduces (kb/Work PB5382)

A debugging line reaches this stage as `ReferenceFormatProcessor.DebugLineCarrier` plus its text, and `DebuggingLineRewriter` later keeps or
hides each carrier's line by the unit's WITH DEBUGGING MODE. A whole replacement whose pseudo-text-2 spans lines therefore has to write the
carrier on every line it introduces that is a debugging line, or the rewriter sees source. `ApplyReplacements` asks `IntroducedText` for
the text it writes, decided by the line the match begins on (`firstLineIsDebugging` covers a slice that begins inside a line):

- **The replaced text begins on a debugging line:** every introduced line is a debugging line (§1985:XII.3.4 GR11, §1985:XII.2.4 GR9,
  §2002:7.1.3.3 GR11, §2002:7.1.2.3 GR13); a carrier pseudo-text-2 wrote at its own start is dropped, because its first line lands on the
  replaced line.
- **It begins on a source line:** a COPY keeps the debugging lines pseudo-text-2 wrote (GR9 / GR13: "only those text words that are specified
  on debugging lines where the debugging line is within pseudo-text-2 appear on debugging lines"), and so does REPLACE from 2002; COBOL-85's
  REPLACE gives each introduced line the replaced line's indicator, so it drops them (`ResultRule.KeepsPseudoTextDebuggingLines`, asked
  with `EditionInfo.ReplacedTextIsLaidOutInFixedForm`).
- The text after the match keeps the indicator of the line the match ENDS on (GR11 decides the introduced lines only). When the line the
  replacement ends on differs in kind from that line (a match crossing between a debugging line and a source line, either way) or a kept
  directive line intervenes, the rest starts a line of its own, with the carrier when the end line is a debugging line.

At COBOL-85 (`ReplacedTextIsLaidOutInFixedForm`) the resultant text is laid out in fixed-form lines, so a literal a replacement places on a
debugging line that the program-text area cannot hold from the margin of the area it begins in (65 positions in area A, 61 in area B; a
pseudo-text-2 literal keeps the area it was written in, literal-2 takes the replaced word's, §1985:XII.2.4 GR9) would be continued on a
debugging line: COBOL7039 (§1985:XII.2.4 GR9, §1985:XII.3.4 GR11). The 2002 rules drop that error. A COPY statement written on a debugging
line writes its whole library text behind the carrier (`ExpandCopiesOneLevel`: `LineIsDebugging`, `AppendAsDebuggingLines`; §1985:XII.2.4 GR9).

## §4 Wiring (greenfield only)

`Frontend.Preprocess` makes ONE call, `ConditionalCompilationProcessor.Manipulate(normalized, copy, …)`, with a
`CopyProcessor` built from the compilation's copy search paths, and gets back the fully expanded text (COPY
incorporated, CC applied, REPLACE applied) with its source map. Everything downstream (NIST,
TURN/…/COBOL-WORDS, the H3 baseline) is unchanged. There is no second, COPY-only path.

## §5 Increments

- **Incr 1 — the merged driver, common case.** `CcState` extraction + `CopyConditionalProcessor` (CC
  branch-selection + emitting-branch COPY expansion + omitted-branch COPY drop + shared DEFINE state) +
  `CopyProcessor.ExpandOneCopy`. Wire into `Frontend`. Preserve §2.1/§2.2. Unit + conformance goldens (copybook
  `>>IF`, copybook `>>DEFINE` seen by main `>>IF`, main `>>IF` gating COPY, false-branch missing copybook).
- **Incr 2 — edges** (only if a probe/differential shows a gap): multi-line COPY REPLACING interleaved with CC;
  mid-line COPY; PUSH/POP across the copybook boundary; nested-copybook directives.

## §6 Gate (per the shared-core / high-blast-radius doctrine)

Wave-local per commit (build + characterization + the new unit/goldens + CLI probes). **Before merge / for the
driver-swap commit:** the FULL NIST guard (`guard.sh`/`guard-fast.sh` ALL GREEN — the NIST verdicts must be
byte-identical) + the FULL greenfield Conformance + **the GnuCOBOL external differential before/after** (the
owner directive: diff per-case verdicts; a divergence→agree flip is a FIX, an agree→divergence flip is a
REGRESSION, 0 tolerated). The GnuCOBOL corpus exercises COPY heavily — it is the real net for this change.

## §7 Tests

- **Unit** `CopyConditionalProcessorTests` — copybook `>>IF` (both branches), copybook `>>DEFINE` visible to a
  later main `>>IF`, main `>>IF` gating COPY (taken/omitted), false-branch missing copybook (no error), nested
  COPY with directives, the `leave*` survival (a copybook `>>TURN` reaches the TURN stage).
- **Conformance** `tests/conformance/2023/cc_in_copy_*.cob` (+ `.out`) — an observable end-to-end (a copybook
  `>>IF` selecting a VALUE, printed).
