## PB2119 Delete wave B: nine dead members, scripts and tools removed

The Delete program's second Frontend, Editions and Runtime wave removed code a re-measure against main of 2026-11-17 found
unreferenced, with no change to any compiled program.

Members removed: `ContextSensitiveWords.Count` (PB2185), `CtNumeric.IsInteger` (PB2195), the private `CobolErrorStrategy.CaptureSink`
(PB2229), `SourceLocation.None` (PB4365), and `Pow10.AsLong` with its table `L` and `BuildLong` (PB2225; the design sketch in
DESIGN-runtime-library section 2.3 lost the same two lines). Files removed with `git rm`: `scripts/spec/repairs/latex_diagrams_to_house_style.py`,
`scripts/spec/repairs/onoff_directive_notes.py` (PB2235), `scripts/spec/tag_cobol_blocks.py` (PB2236), `tools/extract-nist.sh` and
`tools/extract-nist-copylib.sh` (PB2237). No doc, skill, workflow or CI file named a removed script or tool.

PB2736 is the one that was not a pure deletion: it removed the `catch (Exception) { return 0; }` in `CobolKeywordTokens.LexProbe`. The note
allowed that only if no exception can reach it. The pre-screen limits the input to letters, digits, `-` and `_`, the probe's lexer has an
empty `>>COBOL-WORDS` plan, and ANTLR reports lexer errors through the listener. A throwaway test with the catch already removed ran
`TryTokenType` over more than 100000 distinct words (every lexer literal and variants, every BMP letter, 200000 random mixed words) and
nothing threw. The test is not kept. A lexer defect now surfaces as an internal compiler error, as `Frontend.ParsePass` already does.

Proof: `dotnet build WiseOwl.Cobol.sln -c Debug` 0 warnings, 0 errors; the 37 Characterization snapshots pass unchanged.
