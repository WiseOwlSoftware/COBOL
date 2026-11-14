**What is not a row, and why.** (1) Behaviours that the standard leaves to the implementor and that have a standard
default and a selector — `--sign-encoding ibm|ascii` (§2023:13.18.52.4 GR4/GR5, Annex A.1 items 177/178) — are implementor
latitude, not an extension; they are documented where the latitude is (§7). (2) Constructs `--permissive` accepts — a
removed ISO construct, or a violation of a syntax rule the user opts into (COBOL0844, COBOL0819,
`CallByValueOperandClass`, …) — are flagged unconditionally, by their own named warning through `EditionContext.Removed`,
which IS the §2023:4.2.10 warning mechanism for them; they are never accepted in silence, so they need no second warning. (3)
Extensions of **class 3** (§2023:4.2.10: a required element with different functionality, "provided that standard-conforming
behavior is also implemented and that an implementor-defined mechanism exists for selection of the nonstandard behavior")
are the options themselves: `--source-format` selects its nonstandard auto mode, and the standard behavior is the default.
The reserved words added for these extensions are D-RW1. (4) **Embedded precompiler blocks** (`EXEC SQL … END-EXEC`,
`EXEC CICS … END-EXEC`, `EXEC DLI …`; kb/Work PB4341): no support is claimed, and no word is reserved for them. ISO/IEC
1989 defines no EXEC construct; the block's text is another language that a vendor's precompiler translates to COBOL
before compilation, the arrangement §2023:4.2.14 permits (`cite.py --check 4.2.14 "This permits an implementation to require
components outside the COBOL implementation, such as precompilers"` → OK), and this implementation has no precompiler.
Each block (EXEC, a word naming the language, and the text through the first END-EXEC with no separator period between)
is refused by **COBOL5818**, once, at its EXEC; the parse reads it as the statements the precompiler writes in its place
(or as nothing, where no statement may stand), so every block and every fault of the program's own is reported, and the
block's text, another language, draws no COBOL diagnostic of its own. EXEC and END-EXEC stay user-defined words (no
edition's §2023:8.9 list holds them): a source that writes EXEC as a word anywhere (a declaration, a reference) is read as
COBOL throughout, so `01 EXEC PIC X.` … `DISPLAY EXEC SQL END-EXEC.` names three data items and compiles
(`wiseowl/cases/4.2.10-Nonstandard-extensions/pb4341-*`). A block left without its END-EXEC is not such a word (kb/Work
PB4381): an EXEC followed by a user-defined word whose sentence ends at a separator period (§2023:8.3.5 rule 3) before any
END-EXEC is **COBOL5842**, once, at its EXEC, taken through the text before that period, and the blocks that do close
stay one **COBOL5818** each. That shape is a candidate only: a declaration that takes a list of user-defined words
(`INDEXED BY EXEC IDX.`, `SYMBOLIC CHARACTERS EXEC BEL ARE …`) has the same one, so only the grammar settles it: the
source is read as COBOL, EXEC a word, when it parses with no syntax error that way
(`wiseowl/cases/4.2.10-Nonstandard-extensions/pb4381-exec-is-an-index-name`). The block's text, whatever the
channel its tokens were lexed on (a semicolon with no space after it is hidden-channel, §2023:8.3.5 rule 2), is another
language and draws no separator diagnostic (kb/Work PB4384; `wiseowl/cases/4.2.10-Nonstandard-extensions/pb4384-*`).
