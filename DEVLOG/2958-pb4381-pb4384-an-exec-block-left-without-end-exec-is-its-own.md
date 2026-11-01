## Entry 2958 — 2026-11-01 14:43 PST — PB4381, PB4384: an EXEC block left without END-EXEC is its own fault, and a semicolon inside a block is SQL text

Two defects of `EmbeddedBlockReading` (PB4341's reading that makes each `EXEC … END-EXEC` block one token before the parse), one
file.

**PB4381.** `Read` returned an empty reading at the first EXEC that opened no block, so a block left without its END-EXEC (a
plain fault in the SQL text) made the whole source read as COBOL and every well-formed block drew `COBOL0001: cannot parse
construct near 'SQL'` again. Re-probe on this build: a source with a closed block and `EXEC SQL SELECT B INTO :W-H FROM T`
before `DISPLAY W-H.` drew the one COBOL0001 and nothing named either block. The rewrite is `EmbeddedBlockReading.Scan`, which
classifies each EXEC: a word and an END-EXEC before a separator period (ISO §8.3.5 rule 3, cite.py OK) is a closed block
(COBOL5818); a user-defined word (an IDENTIFIER: SQL, CICS and DLI are reserved in no edition, §8.9) and the period first is a
block left unterminated, new COBOL5842, taken through the text before the period and made one token like any block, so the
parse goes on and the blocks that do close stay blocks; anything else is a COBOL word, and one such EXEC still reads the whole
source as COBOL. After the fix the probe draws COBOL5818, COBOL5842 and nothing else.

**Review round 1, R1-1 (blocking), fixed.** The first cut called the user-defined-word shape an unterminated block on the claim
that every declaration of a word is followed by a reserved word or the period. False: a declaration that takes a list of
user-defined words (`INDEXED BY EXEC IDX.`, `SYMBOLIC CHARACTERS EXEC BEL ARE 66 8.`) puts one after a declared EXEC, so a legal
program naming an index EXEC drew COBOL5842 where main compiled and ran it (a regression, rule 3). No lexical rule can tell the two
shapes apart, and a list of declaration positions would be a hand-kept list. The shape is now a candidate: `Read` takes a
`parsesAsCobol` question, asked only when the scan found a candidate and no EXEC that is certainly a word, which
`Frontend.ParsesCleanly` answers with one whole parse of a copy of the stream with every EXEC a COBOL word. A clean parse means
legal COBOL, EXEC a word, and the source is read as COBOL; otherwise the EXEC opens the unterminated block. Cost: no parse for a
source with no candidate or with closed blocks alone, one extra parse for a candidate. Residual: a source that writes EXEC as an
index-name AND holds a genuinely unterminated block fails the clean parse, so its legal `INDEXED BY EXEC IDX` is also reported as a
block. Goldens: `pb4381-exec-is-an-index-name` (runs at all four editions); unit rows for the index-name and symbolic-character forms.

**PB4384.** `Read` moved only default-channel tokens to `EMBEDDED_BLOCK_TEXT`; the lexer puts a semicolon with no space after
it on the HIDDEN channel so the separator screen can judge it (§8.3.5 rule 2), so `EXEC SQL SELECT A;B … END-EXEC.` drew
COBOL2631 beside COBOL5818 (probed, reproduced). `Read` now moves every token of a block that `ProgramText.Holds` (the program's
COBOL text) whatever its channel; a token already off the COBOL text, an absent debugging line, stays where it is. Now the one
COBOL5818.

**Goldens.** `wiseowl/cases/4.2.10-Nonstandard-extensions/pb4381-exec-sql-block-unterminated` (COBOL5842, refused at all four
editions) and `pb4384-exec-sql-semicolon-in-block` (COBOL5818 only); unit rows in `EmbeddedPrecompilerBlockTests` pin the counts
(unterminated block among closed ones and the fault after them, four shapes of one unterminated block, a declared EXEC keeping
the source COBOL, four semicolon placements). Both notes close no inventory row: ISO/IEC 1989 defines no EXEC construct, so the
work is diagnostic attribution. Docs: CONFORMANCE §3.2 item 4, DESIGN-frontend-grammar §3.9, migrating.md.

