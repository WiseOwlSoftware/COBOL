## PB2726, PB2735, PB2760, PB2648, PB2646, PB2620, PB2636, PB2628, PB2637: the lexer gets a C# superclass, every reading of COBOL text shares one token-stream preparation, the collation start-up hook moves to the control layer, the subscript rule is raised in one place, the national codec leaves the BIT codec, every catalog shares one edition window, no variadic intrinsic allocates its argument list, the operator ** leaves the intrinsic class with one rule-6 screen, and a date/time format literal is parsed once

Wave 20262135, group E, replayed onto main and continued by wave 20262139, group C, then replayed again and continued by
wave 20262143, group B (the PB1754 campaign, seven behaviour-neutral R2 restructurings).

**PB2760: `CollationRuntime` moves from `Collation/` to `Control/`.** The run unit's text-subsystem start-up hook
(`Initialize`, `Warmup`, `Status`) sat in `WiseOwl.Cobol.Runtime.Collation`. It was the only reason that namespace
named the root namespace (`RunUnit`), `.Unicode`, `.Unicode.Segmentation` and `.Collation.Locale`. It now lives in
`Control/CollationRuntime.cs` (namespace `WiseOwl.Cobol.Runtime`), beside `RunUnit` and its `RuntimeConfig` row, and keeps
its name. Every caller and test changed in the same commit, with no forwarder. The namespace change removes shipped
surface, so the runtime major was bumped (`abi_pin.py bump --runtime-major`). `DOC-A.1-120`'s witness was re-sited by a
`record_verdicts` batch (GAP 4 → 4). A measurement showed one cycle the finding had not counted: `.Collation.Locale`
reads the run unit (`LocaleManager` → `RunUnit.Current.Locale`), so `Runtime ↔ Runtime.Collation.Locale` remains.
DESIGN-architecture-review §8.1 records it as one control layer by placement until the Move waves give `Control/` its
namespace.

**PB2735: one token-stream preparation.** The re-probe found the sequence lex → prime → debugging lines → ZERO →
§2023:8.9 retype → parser written five times, not twice: in `Frontend.LexAndParseOnce`, in
`Frontend.ReportDebuggingLinesBreakingTheProgramAsSource`, in `FragmentParse.Parse`, in `FragmentParse.ParseUnit` (with
its own copy of the PB1669 re-lex loop), and in the unprimed `CobolKeywordTokens.LexProbe`, which also had a third error
listener. `Parsing/CobolTokenStream.cs` now holds all of it:
- `Lexer` builds and primes every lexer and attaches only the reading's error listener, never ANTLR's console listener.
- `Prepare` runs the steps in one order. Each reading passes its debugging-line step, ZERO choice and retype scope
  explicitly, never by default.
- `Parser` hands every parser the edition and the words map.
- `UntilLexStable` is the one re-lex loop.

`SyntaxErrorFlag` is its own file. `CountingErrorListener`, `LexerErrorListener.AttachTo` and `Frontend.NewLexer` are
deleted. A new pin, `TokenStreamPreparationDriftTests`, holds that no product source outside `CobolTokenStream.cs`
constructs a `CobolLexer` or a `CobolParserCore`. `EditionReadDriftTests` was re-recorded: the carriers and the re-lex
decision are now `CobolTokenStream`'s.

**PB2726: the lexer has the parser's shape.** `CobolLexer.g4` names `superClass = WiseOwl.Cobol.Frontend.Parsing.CobolLexerBase`
and has no `@members` block. The roughly 450 lines of C# that ANTLR used to copy into the warning-suppressed generated
file are now in `Parsing/CobolLexerBase.cs`, where the analyzers check them. The two token actions that read lexer
state inline became one-line calls (`OnIdentifier`, `OnListSeparator`). `gen-cobol-words.ps1` now emits the word sets
as `CobolLexerWordSet`, a generated data class in `Parsing` (`DataNameTokens`, `IsReservationGated`,
`IsKeywordWhereFree`), in place of a `partial class CobolLexer` filed under the wrong folder with a stale SUBSCRIPT-mode
header. Every caller changed, and the regenerated `CobolWords.g4` is byte-identical. `EditionReadDriftTests` reads the
lexer stage from the superclass as C# syntax. Three tests that reflected into lexer private fields now read the
generated set through its API.

**Neutrality and checks.** The rows re-verdicted: none (`closes_rows: []` on all three; DOC-A.1-120 only re-sited). The
Unit project ran 42,820 of 42,821 green before the last fix and the one red then passed. `semgrep/verify.py` PASS, with
no rule count increased. The arch oracle (`compare_oracle.py`, baseline 608ccf507c57) printed `DIFFERENT — 8998 of 23370
cases differ`. Every difference is a C# difference of exactly four lines per case (35,992 lines in all), and every
one of those lines is the runtime version string (`"32.0.0.0"` → `"33.0.0.0"` in `CobolRepositoryAttribute` and
`RegisterModule`). That is the expected result of PB2760's major bump. Replayed onto a main that had meanwhile taken major 33 for
another change, the branch re-applied its own bump with `abi_pin.py bump --runtime-major` (33 → 34, the hash measured). No diagnostic stream differs, and no case was
added or removed. The first gate was RED in RepoDrift on three sweeps. Each failure came from code that moved out of
a `.g4` file, which those sweeps do not read as C#, into `CobolLexerBase.cs`, which they do:
- `ApplyEditionSpelling` wrote the Annex C edge inline as `>= 2023`. It now asks `CobolNames.HasEditionSpelling`, the
  rule's one predicate.
- 16 inventory rows cited `CobolLexer.g4#<member>`. `record_verdicts` batches re-sited them for 2023, 1985 and 2002,
  with the GAP unchanged.
- A retirement-prose sweep treated a moved member as deleted. The sweep now reads the two recognizer superclasses.

The predecessor's re-gate at 204b037f44 was GREEN. The replay onto main 9bfe9e1c44 was gated GREEN at 259c616ffe
(run 000001-20261122T205303Z: Conformance 29,080/29,080, Unit 42,697/42,697, RepoDrift 231/231, Characterization 37/37).

**PB2648: one subscript-range rule, one OCCURS DEPENDING bound test.** The §2023:8.4.2.3.4 GR2 test and its detail text
were written four times: in `CobolTable.At`, in `CobolDynTable.RefSending` and `RefReceiving`, and in
`ReportWriter.SumOccurrence`. The §2023:13.18.38.4 GR7 test was written twice in `CobolTable`. Now
`CobolTable.Permissible` is the one range test and `CobolTable.SubscriptOutOfRange` the one raise and detail composer;
all four arms call both. `CobolTable.TestOdoBounds` is the one GR7 test. Every detail text is byte-identical
(`WideSubscriptDiagnosticTests` pins them), and `SubscriptRangeRuleDriftTests` refuses a second raise of either rule.
The checking-off continuation stays per arm, as the note requires of a behaviour-neutral wave. The re-probe confirmed
the arms disagree: `F(0)=[]` for a fixed `PIC X(5)` table, `D(0)=[BBBBB]` for an OCCURS DYNAMIC one. That divergence
is reported as a lead for the fix lane.

**PB2646: the national storage codec leaves `CobolBits`.** `CobolBits`, the USAGE BIT codec, also held the national
UTF-16BE codec: `NatBytes`, `PutNat`, `BytesPerNational`, `NatReadWindow`, `NatWriteWindow` and `NatFromStorage`. These
now form `NationalImage` (`Bytes`, `Put`, `BytesPerNational`, `ReadWindow`, `WriteWindow`, `FromStorage`).
`NativeCollatingSequence` moves out of `StorageByte.cs` into its own file. Every caller changed, with no forwarder. The
compiler's emit helpers (`RuntimeApi.NatBytes` and its siblings) keep their names and emit `NationalImage.*`. The
branch's major bump covers the surface change, and `abi_pin.py repin` measured the hashes. Six inventory rows were
re-sited (2023 GAP 4 → 4, 1985 GAP 0 → 0).

**Oracle.** `compare_oracle.py --base 9bfe9e1c44` printed `DIFFERENT — 8998 of 23370 cases differ`. No diagnostic
stream differs, and no case was added or removed. In every differing case, the base's C# with two substitutions applied
equals the new C#: the runtime version string `"33.0.0.0"` → `"34.0.0.0"`, and `CobolBits.Nat*` → `NationalImage.*`.

The gate verdict for PB2648 and PB2646 is in the branch report.

**Second replay (wave 20262143, group B).** The ten commits were cherry-picked onto main a23ed881c2. Three conflicts:
`FragmentParse.Parse` keeps main's PB5736 trailing-semicolon check on the shared `CobolTokenStream` stream; main's PB5652
change to the lexer's report-SUM rule (`ReportOperandFollows`, the narrowed `IsReportSumClauseKeyword`) moved into
`CobolLexerBase.cs` with the rest of the `@members`; and main's SR-13.18.54.3-9 row was re-sited there. The first gate was RED
in RepoDrift (two 2002 rows cited `CobolLexer.g4#IsReportSumClauseKeyword` and `#ReportOperandFollows`); a `record_verdicts`
batch re-sited them (2002 GAP 1161 → 1161). `code_read_landing.py` then refused the branch: the stored 1985 read of
1985-SR-IV.4.2.1-2 named `CobolLexer.g4#ListItemBoundary`. Its probes (sep1-sep5 and the refuter's s1-s5) were re-run at the
pin and printed what they printed at the read, and the re-read was imported (`code_reads.py import --replace`, 1 confirmed).

**PB2620: one edition window.** The availability rule was written in `ConstructDialectStatus.StatusAt`, in
`IntrinsicSig.IsDefinedAt` with the intrinsic binder's COBOL1502/COBOL1503 arms, and in `ExceptionNameRule.Window`. The sweep
found two more: `CompilerDirectiveWords.IsReserved`'s exception-name test and `FileDescriptionFormats.Admitted`'s 85/2002
filter. The intrinsic arm also chose its own severity, so a removed function would have been an error under `--permissive`.
`EditionWindow` (Editions) now holds `StatusAt`, `IsInForceAt` and `VerdictAt`, whose severity is
`EditionSeverityPolicy.For`'s; each catalog keeps its codes as data (`EditionGateCodes`). `EditionFlag.Of(ConstructDialectStatus, …)`
is deleted, the window is the flag's one funnel, and `EditionWindowDriftTests` refuses a window-column comparison anywhere
else in `src/`. No row carries an intrinsic `RemovedIn`, so no output changes (COBOL1502 at `--std 85` re-probed identical).

**PB2636: no variadic intrinsic allocates its argument list.** 71 runtime bodies took `params T[]`, and the MAX/MIN/ORD-MAX/
ORD-MIN arm emitted `new Int128[] { … }, new int[] { … }` per evaluation. Every body now takes `params ReadOnlySpan<T>`, the
selection orders are `ref struct`s over the span, `RawArgPairs` emits collection expressions (`MaxAt(0, true, [WS_A, WS_B],
[0, 0])`), and `LeadThenTail` passes `xs.AsSpan(1)` instead of copying `xs[1..]`. `PublicAPI.Unshipped.txt` records the 74
signatures; `VariadicIntrinsicSpanDriftTests` pins the shape, and three reflection drift tests read the span types.

**Oracle and gates (wave 20262143).** `compare_oracle.py --base a23ed881c2` printed `DIFFERENT — 9024 of 23455 cases differ`,
every one a C# difference: no diagnostic stream or run output differs and no case was added or removed. A script showed 9,007
of the differing files equal the base with the version string (`"33.0.0.0"` → `"34.0.0.0"`), the `CobolBits.Nat*` →
`NationalImage.*` rename and the selection arm's collection expressions applied; the other 17 were read and differ only by the
same collection expressions around a nested selection call. Gates: run 000001 RED (the 2002 rows), run 000002 GREEN at
550a6c4380, run 000003 RED (`RENDERED VIEWS`: a one-folder drift test carried the tree-sweep trait), run 000004-20261123T090634Z
GREEN at a4133fbd3b (Conformance 29,183/29,183 · Unit 42,803/42,803 · RepoDrift 241/241 · Characterization 37/37). Linux
`--legs unit,conformance`: unit 42,935 and conformance 29,183 GREEN; the one red leg is selftests'
`test_forbidden_commands.py`, which reads the Linux clone's local `origin` as not this repository, on any branch.

**Wave 20262147, group A** replayed the branch onto main unchanged (16 commits, fast-forward from a23ed881c2; gate run
000001-20261123T101602Z GREEN, every population up to date at its key) and continued with two notes.

**PB2628: the operator `**` leaves the intrinsic class.** The re-probe found the §2023:8.8.1.2 rule-6 screen in two places:
`CobolIntrinsics.CheckPowRule6` (RealArgs.cs) for the native arms and `CobolExact.Pow`, and an inline copy in
`CobolDec.PowDeveloped`, each with its own text, so `0 ** 0` ended the run unit with "…not 0 (ISO §2023:8.8.1.2 rule 6a)" natively
and "exponentiation of zero with a non-positive exponent (…)" under ARITHMETIC IS STANDARD-DECIMAL. `NumericRenderer.Power`
emitted the binary64 power through `RuntimeApi.Intrinsic`. Now `Values/Numeric/CobolPower.cs` owns `Binary64`, `Binary64Checked`
(the 2002+ and the COBOL-85 exact-exponent overloads) and the one `ScreenRule6`, which `CobolDec.PowDeveloped`, `CobolExact.Pow`
and `CobolExact.PowReal` ask; `RuntimeApi.Binary64Pow` is the emitted handle. Both probes print `SE1`, `SE2`, `+0004.0000`, then
the same fatal text. `ExponentiationOwnerDriftTests` pins one raise of EC-SIZE-EXPONENTIATION and no power on `CobolIntrinsics`.
Rows GR-14.7.5-1, GR-14.7.5-L3.1, GR-8.8.1.2-6 and 1985-GR-VI.6.2.3-5 re-sited (GAP unchanged); the 1985 code read
1985-GR-VI.6.4.2-1 re-read at its moved members (its stored probes and the refuter's re-run print what they printed;
`code_reads.py import --replace`: 1 confirmed).

**PB2637: a date/time format literal is parsed once.** `CobolDate.EmitFormatted` and `Analyze` tokenized argument-1 at every
evaluation and the emitter built one string per field. `CobolDate.FormatOf` now holds each literal's `DateTimeFormat` (segments,
facts, the image's length) in a bounded cache; `EmitFormatted` writes one `string.Create` buffer; `Analyze` and the compile-time
`FormatFractionDigits` read the same parse. The emitted C# does not change. The probe prints the values §2023:15.3 gives;
`DateFormatParseOnceDriftTests` pins it.

**PB2655 and PB2747 not started**: each note sequences itself after open notes (PB2655 after PB2327's CobolEdit extract; PB2747
inside the PB2309/PB2349 group), so each would re-baseline the emitted text a second time; their `blocked_by` now say so.
