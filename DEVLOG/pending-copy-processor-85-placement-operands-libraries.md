## PB5313, PB5469, PB5471, PB5473: COBOL-85 COPY and REPLACE lay pseudo-text-2 out where it is written, read identifier operands whole, keep identifier-2 off debugging lines, and count the libraries a compilation names

**PB5313 (landed, MAJOR).** At `--std 85` a paragraph header that REPLACE or COPY REPLACING pseudo-text-2 writes in area A
of a line of its own was refused (COBOL3734), and one written in area B that replaces a library word in area A was accepted.
The source map gave every line of a replacement the position of the replaced text, and `AreaPlacementPass` judged the areas
there. The fix gives the two facts two sources. Whether a token begins its line now comes from the RESULTANT token stream,
because the program is judged only after COPY and REPLACE (§1985:XII.2.4 GR8). The area comes from the place the token was
laid out at. A stretch of resultant text now carries an optional `Layout` beside its origin (`OriginSpan.Layout`,
`SourcePoint`). `ApplyReplacements` writes pseudo-text-2 with the new `OriginWriter.AppendLaidOut`, so each word stands at
the replaced text, which every diagnostic and EXCEPTION-LOCATION still names. Each word is also laid out where pseudo-text-2
wrote it (§1985:XII.2.4 GR9). `ReferenceFormatMap.AreaAt` replaces `PlacementAt`. REPLACE follows COPY's rule. Its GR10
sentence also stands in COPY's GR10 beside that area rule, so RA5 (a single `P2.` written in area B) stays refused. The
message now names the line pseudo-text-2 was written on. Before and after: RA3 COBOL3734 / A B D C; p9v and p9iii COBOL3734
/ IN Q; p9i IN P / COBOL3734.

**PB5469 (landed).** `ReadOperand` read identifier-1 and identifier-2 as a word, its qualifiers and ONE parenthesized group.
So `TBL (2) (1:1)` and `FUNCTION UPPER-CASE ("ab")` drew COBOL2449. The reader now takes the qualifiers or a
function-identifier's name (§1985:IV.4.3.8.3.2), then every balanced parenthesized group (§1985:IV.4.3.8.5.1). No operand
begins with a left parenthesis. p3c now prints D twice, p3h `cd2026`, and b02 and b03 run.

**PB5471 (landed).** An identifier-2 that the COPY statement writes as `A1` / `D OF B1 OF C2` kept the D on its second line,
so without WITH DEBUGGING MODE its qualifiers vanished (COBOL1639). Only debugging lines within pseudo-text-2 stay debugging
lines (§1985:XII.2.4 GR9, §2002:7.1.2.3 GR13). An operand that is not pseudo-text now gives every line it introduces the
replaced line's indicator (GR11), at every edition. q15 prints `[Z]` at 85 and 2002. The word-2 controls still print `[Q]`.

**PB5473 (landed).** At 85 an unqualified COPY resolved silently in the default library when another library was available,
which §1985:XII.2.3 SR1 forbids. DOC-A.1-40 now states which libraries are available: the default library and each library
a COPY statement of the group names with OF or IN that a library place holds. A subdirectory nobody names is no library.
Counting every subfolder of the working directory would make most 85 programs wrong. `CountLibraries` refuses each
unqualified COPY of such a group at 85, including one written before the qualified COPY. It uses a new constructs row,
`copy-unqualified-text-name-2002` (COBOL0900; 2002 Annex F.2 item 38). `pb1355_library_text_located` mixed the two forms
while claiming 85, so it is now accepted from 2002.

**Goldens.** `pb5313-replace-introduced-line-in-area-a`, `pb5313-copy-pseudo-text-area-a`, `pb5313-copy-pseudo-text-area-b`
(refused), `pb5469-replacing-identifier-operands`, `pb5471-identifier-2-over-debugging-line`, and
`wiseowl/.../pb5473-copy-every-text-qualified` and `pb5473-copy-unqualified-text-name` (refused). There is also a
`SourceLineMapTests` case for the laid-out writer. The 1985 store records 1985-GR-XII.3.4-10, 1985-GR-XII.2.4-10,
1985-GR-XII.2.4-9, 1985-GR-XII.2.4-11, 1985-FMT-XII.2.2, 1985-GR-XII.2.4-3, 1985-GR-XII.2.4-4 and 1985-SR-XII.2.3-1 move
from PARTIAL to CONFORMS, and the 1985 GAP falls from 232 to 224. No diagnostic code was used. COBOL7132-7134 are returned.

**Replayed onto main (wave 20262038).** The gate of the replay found one test PB5473's rule now refuses:
`CopyNameUserWordTests.TheSameName_WrittenTwice_IsNoConflict` (PB5482, landed on main after this branch was cut) wrote
`COPY PB5482NOP.` beside `COPY PB5482TXT OF PB5482LIB.` at 85, which §1985:XII.2.3 SR1 refuses. Its program now names the
library on every COPY, which keeps what it tests: one text-name written twice and one library-name written three times.
