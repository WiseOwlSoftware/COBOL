## PB2726, PB2735, PB2760: the lexer gets a C# superclass, every reading of COBOL text shares one token-stream preparation, and the collation start-up hook moves to the control layer

Wave 20262135, group E (the PB1754 campaign, three behaviour-neutral R2 restructurings).

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

The predecessor's re-gate at 204b037f44 was GREEN; the replay onto current main re-gates, and its verdict is in the
branch report.
