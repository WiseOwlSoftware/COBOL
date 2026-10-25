## COPY's literal names and SUPPRESS are COBOL-2002 introductions, and the literal length bound is the edition's: 160 below COBOL-2014, 8,191 from it (PB1601, PB1615)

Two edition edges the compiler had never drawn, found by the analyses sweep of 2026-10-23 and confirmed on this build
before any change: `COPY "CB1.cpy".` and `COPY CB1 SUPPRESS PRINTING.` compiled clean at `--std 85` (PB1601), and a
200-position alphanumeric literal compiled clean at `--std 85` and `--std 2002` (PB1615), both under-rejects. Both are
DERIVED edges (the repository holds no 1985, 2002 or 2014 text), so each carries its derivation in a version change
reference row that can be overturned in one line.

**PB1601, the COPY statement.** ISO/IEC 1989:2023 §7.2.3.2 prints `COPY { text-name-1 | literal-1 } [ { OF | IN } {
library-name-1 | literal-2 } ] [ SUPPRESS PRINTING ] [ REPLACING … ]`; ANSI X3.23-1985's COPY names its text by a word
alone and has no SUPPRESS. The evidence: no NIST CCVS-85 program or copy library writes either phrase, Micro Focus marks
both as dialect extensions rather than ANS85, Annex E lists neither (so both predate 2023), and 2002 is the edition that
rewrote COPY as a compiler-directing statement (the partial-word phrases of row 7.33 are of the same rewrite). Both take
the 2002 edge, the one that never refuses a legal 2002 program. One gate method, `CopyProcessor.GateCopyPhrase`, asks the
registry for the new rows `copy-literal-name-2002` (from `ReadCopyName`, for every literal written as a text-name or
library-name) and `copy-suppress-2002` (from `ParseCopyStatement`); COBOLNET0900 below 2002, an error under
`--permissive` too. A name written as a word is never gated. The sweep found one sibling in our own tests: the WiseOwl
case `pb1355_library_text_located` (accepted at 85) wrote `COPY "pb1355c.txt"`; its literal leg moved to the new 2002
case `pb1355_library_text_literal` and the 85 case keeps its three word legs. VCR row 7.42.

**PB1615, the literal length.** §8.3.3.2.3 SR1, §8.3.3.4.3 SR1, §8.3.3.5.3 SR1 and §8.8.3.2 SR2–SR4 print 8,191
positions; the older editions bound a literal at 160. The edge follows kb/Work R61's method: GnuCOBOL 3.2's per-standard
dialect files carry `literal-length` 160 in `cobol85.conf` and `cobol2002.conf` and 8191 in `cobol2014.conf`, and Annex E
lists no change, so 2014 = 2023. The bound is now one function, `CobolLiteral.MaxPositions(dialectLevel)`, whose edge is
READ from the new constructs row `literal-length-8191-2014` (`ConstructRegistry.IsAvailable`), so the number and the
version matrix that samples it cannot name different editions, and no `>= 2014` copy exists to drift. The literal screen
(`LiteralScreen.Screen` → `CobolLiteral.SyntaxViolation(raw, dialectLevel)` → `LengthViolation`) and the concatenation
fold (`ConcatFolder.Fold` → `ResultLengthViolation(folded, dialectLevel)`) both read it, so the `&` expression, the
concatenated ALL literal-1 and the literal they equal agree. A relaxation, not an introduction (the word-length
ceiling's posture): the longer literal is COBOLNET0814 / COBOLNET1545 with a message naming the edition and where 8,191
begins, an error also under `--permissive`; above 8,191 every edition refuses. The old `MaxLiteralPositionsText`
property is replaced by `MaxPositionsText(dialectLevel)` with every caller changed. VCR row 7.41.

**Goldens.** `pb1615_literal_160_positions` (the boundary, legal at 85–2023), `pb1615_literal_over_160_positions`
(2014–2023) and the negative `pb1615-literal-above-160-below-2014` (85 and 2002); `pb1354_copy_header_2002` now names
both COPY constructs, `pb1355_library_text_literal` is the literal-name positive, `pb1601-copy-literal-name-below-2002`
and `pb1601-copy-suppress-below-2002` the negatives. Unit tests pin the COPY gates (every literal and SUPPRESS spelling
at 85, 2002, 2014) and `CobolLiteral.LengthViolation` at both sides of the boundary for every class, a doubled delimiter
(one position) and a `BX` digit (four); `LiteralScreenTests` compiles every class, the `&` expression and the
concatenated ALL literal-1 at 85, 2002 and 2014. The 85 tests needed the 1985 hyphen-in-column-7 continuation, so
`FixedFormLayout.Source` gained a `classic` mode (the floating continuation it used is itself a 2002 introduction).
Inventory: the bands of SR-7.2.3.3-4, SR-7.2.3.3-5 and GR-7.2.3.4-4 (guessed as 2023 or all editions) moved to 2002, and
the length and COPY-format rows gained witnesses; no GAP row closed (41 before and after).

**Self-review.** Silent-failure lens: a `default(EditionInfo)` (year 0) asks the registry and gets the stricter 160, never
the looser; the registry lookup is skipped for any literal under 40 characters; the BX exception (four positions per
digit) keeps the fast path honest. Test lens: every new case has a negative twin or a boundary pair and an expected value
derived from the cited rule. Comment lens: every citation re-run through `cite.py --check`; one inherited section number
(§8.3.3.1 for the doubled delimiter, really §8.3.3.2.3 SR3) and one §6.3.5-as-1985 claim were corrected. Tooling: the
`LiteralRule_HasOneReportingSite` drift test now exempts `ConstructRegistry.g.cs`, which is data that names COBOLNET0814
as the new row's diagnostic.

Gate: pending.
