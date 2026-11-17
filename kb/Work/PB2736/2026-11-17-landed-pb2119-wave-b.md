**2026-11-17, landed (Delete program PB2119, wave B): PB2736 is deleted.**

Deleted: the try/catch (Exception) { return 0; } in CobolKeywordTokens.LexProbe (src/WiseOwl.Cobol.Frontend/Parsing/CobolKeywordTokens.cs); the pre-screen and the error-listener count stay.

The reference sweep was re-run on the tree of the day (Grep and git grep over src, tests, tools, scripts, docs, .github and .claude): no
caller, no string or nameof use, no emitted text, no PublicAPI entry; the only mentions left are history (DEVLOG, kb/Work) and the arch-census
evidence records. Behaviour is neutral: the solution builds with 0 warnings and the 37 Characterization snapshots pass unchanged.

Probe (the note's condition: remove the catch only if no exception can reach it). LexProbe is reached only through
CobolKeywordTokens.TryTokenType, and its pre-screen admits only a non-empty word whose first character is a letter and whose
characters are letters, digits, '-' or '_'. It runs a fresh CobolLexer with an empty >>COBOL-WORDS plan and edition 2023, so
neither NextToken step that could throw on state (the plan's Apply, the edition spelling) has input; ANTLR reports a lexer
error through the listener, never by throwing. Then, with the catch removed, a throwaway test (not committed) called
TryTokenType over more than 100000 distinct words: every lexer literal and its lower-case, "-X", "1", "X" prefix and "_" suffix
forms; the words PIC, PICTURE, IS, FUNCTION, SUM, SECTION, DIVISION, REPORT, DEFINED, OF, IN, ADDRESS, B-AND, B-NOT and END-IF
with a "-1" suffix; every BMP character that is a letter, alone, after "A", and before "-1"; and 200000 random words over the
ASCII word characters mixed with BMP letters and digits (seed 12345). None threw (Passed, 1 of 1). The catch therefore had no
reachable exception, and a future lexer defect now surfaces as an internal compiler error, as Frontend.ParsePass does.
