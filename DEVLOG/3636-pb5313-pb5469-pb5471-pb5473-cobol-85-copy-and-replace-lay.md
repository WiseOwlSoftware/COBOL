## Entry 3636 — 2026-11-14 18:35 PST — PB5313, PB5469, PB5471, PB5473: COBOL-85 COPY and REPLACE lay pseudo-text-2 out where it is written, read identifier operands whole, keep identifier-2 off debugging lines, and count the libraries a compilation names; PB5501: a COBOL-85 size error in a condition continues the program and the relation is false; PB5535: the edition gate of a national or boolean literal names the literal

### PB5313, PB5469, PB5471, PB5473: COBOL-85 COPY and REPLACE lay pseudo-text-2 out where it is written, read identifier operands whole, keep identifier-2 off debugging lines, and count the libraries a compilation names

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

### PB5501: a COBOL-85 size error in a condition continues the program and the relation is false

**The defect.** At `--std 85`, `IF 5 / Z > 1` (Z = 0) ended the run unit with `abnormal run-unit termination:
EC-SIZE-ZERO-DIVIDE (fatal)`, exit 1, re-probed on this tree before the fix. PB5484 had made a COBOL-85 arithmetic statement
continue and left the condition as a lead: §1985:VI.6.4.2 GR3 governs "an arithmetic statement", and a condition has no
resultant and no statement of its own.

**The determination.** §1985:B.2.2 item 45 is general: "If a size error condition occurs and an ON SIZE ERROR phrase has not been
specified, the program will continue", and 1985 has no fatal exception condition. So the program continues. The text gives the
relation no truth value; WiseOwl COBOL's choice (DOC-A.1-70) is that a relation or sign condition whose
arithmetic expression cannot be evaluated is false, its own NOT inside it (`NOT >`, `IS NOT POSITIVE`), so a logical NOT outside it
is true and a sibling leaf of an AND or OR is evaluated as written. GnuCOBOL 3.2.0 (`cobc -std=cobol85` under WSL, run on the
golden and a probe of sixteen more relations) continues too, but its truth values follow no rule: the not-a-number quotient
compares like an unbounded value of the dividend's sign, yet `B / Z * 0 = 0` is true, `M ** 0.5 < 0` is true and `Z ** 0 = 1` is
true because it computes 0 ** 0 as 1 where §1985:VI.6.2.3 GR5a makes it a size error. DOC-A.1-70 now records that, and the
determination stands.

**The fix.** `ConditionRenderer.SizeErrorGuarded` wraps the leaf of `Visit(BoundRelational)` and `Visit(BoundSignCondition)` in
`CobolSizeError.FalseWhenRaised(() => leaf)` when `DialectBehavior.UnphrasedSizeErrorIsFatal` is inactive (the one 85-vs-2002
fact PB5484 added, VCR row 7.71) and an operand reaches an arithmetic operation, as the operand itself or inside a function
argument at any depth (`ArithmeticReach`, a visitor over the generated bound-expression and operand interfaces). The first cut
tested only the operand's own top node, so `IF FUNCTION INTEGER (B / Z) > 1` still ended the run unit (the branch review's R1-1,
re-probed and fixed). A relation none of whose operands reaches one renders as before. EVALUATE WHEN, PERFORM UNTIL and SEARCH WHEN render through the same two leaves. From 2002 nothing
is wrapped. The branch carried PB5484's branch merged in, because that fact was not yet on main; PB5484's review moved the fact from
`EditionInfo` into the `DialectBehavior` registry, and the merge of main re-pointed this consumer at it.

**Goldens.** `pb5501_size_error_in_condition_continues` (85; eight conditions, one a function argument, and a PERFORM UNTIL), `SizeErrorDispositionTests.
Cobol85_SizeErrorInACondition_ContinuesWithTheLeafFalse` (13 rows: four of them function arguments, two rows no-error controls) and its 2002 control
`Cobol2002_SizeErrorInACondition_StaysFatal`. No refusal: 85 is the lowest edition. No inventory row closed.

**Not covered (registered).** A reference modifier's arithmetic expression is evaluated into a temporary before the
statement (`StatementBinder.SubscriptSegments`), outside any condition leaf, so `IF X (B / Z : 1) = "A"` still ends the run
unit at 85, as DOC-A.1-70 says of every 1985 reference-modifier size error; that is a separate mechanism, left as a lead.

### PB5535: the edition gate of a national or boolean literal names the literal

**The defect.** At `--std 85`, `MOVE N"AB" TO X` reported `COBOL0900: national data (PICTURE symbol N / USAGE NATIONAL) requires
COBOL-2002 … the national literal N"AB"`, and `B"1"` named boolean data (PICTURE symbol 1 / USAGE BIT). The program declares no such
item. `LiteralScreen.GateFormat` gated the literal token under the data rows `national-data-2002` and `boolean-data-2002`, because
the edition introduced both at once. Re-probed on this tree before the fix with all four spellings (N, NX, B, BX).

**The fix.** Two construct catalogue rows of their own, `national-literal-2002` and `boolean-literal-2002`. Their edges are quoted:
§2023:8.3.3.5.1 and §2023:8.3.3.4.1 make each literal its own class, and §1985:IV.4.2.2.2 says "Every literal belongs to one of two
types, nonnumeric or numeric". `GateFormat` reports under them, so the message now reads `the national literal N"..." requires
COBOL-2002`. The data rows still gate data items (`VersionConformancePass.GateData`). The registry files were regenerated
(`scripts/gen-constructs.ps1`), and the hexadecimal-literal and VALUE-implied-PICTURE rows' descriptions and the `pb425` golden's
comment now name the literal rows.

**Tests.** `NationalBooleanLiteralTests.LiteralEditionGate_NamesTheLiteral` reads the diagnostic text, because the bug is only in the
words: the four spellings at 85 name the literal row and not the data construct, and they compile at 2002. Each new row's
version-matrix source runs at every edition. Inventory: six national-literal rows had their notes restated through a
`record_verdicts` batch to name the new row. Their verdicts are unchanged, and the GAP count stays 0.

