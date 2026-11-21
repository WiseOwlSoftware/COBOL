## Entry 3887 — 2026-11-21 00:33 PST — Every 1985 requirement the catalog had no row for, probed at --std 85: 114 conform, 9 defects, 7 undecided; PB5993: the wrong-edition citation class re-measured; eight 1985 arms fixed

### Every 1985 requirement the catalog had no row for, probed at --std 85: 114 conform, 9 defects, 7 undecided

The 1985 rule catalog takes only numbered rules, so normative prose was never checked against the compiler (kb/Work PB5924). A
mechanical scan measured 198 such sentences in 134 paragraphs (Appendix C excluded: it restates the body). At the owner's word
each one got a program compiled at `--std 85` and a status: 14 slices, a reader and a refuter each, on main at 8244f1aa4b.

45 sentences restate a row the catalog already has (the refuter sent 4 back as needing their own test; all 4 conform), 30 are
not normative, 103 were tested (95 conform, 8 do not) and 20 cannot be observed by a program (19 conform, 1 does not). The refuter
upheld 176 calls, overturned 6 and doubted 15. 271 golden files are drafted; they land once the catalog gives this text rows of
its own.

The defects, by mechanism: PB5986, the 1985 obsolete elements are neither flagged nor documented; PB5987, a misplaced
DECLARATIVES draws a reserved-word message; PB5988, the PICTURE precedence refusal cites the wrong rule; PB5989, the 1985 spacing
rules for unary, relation and logical operators are not enforced (`IF (A = 1)AND(B = 2)` compiles); PB5990, debugging lines are
not checked without the debugging mode; PB5991, a section header in area B compiles (reproduced: `S1 SECTION.` at column 11,
exit 0). PB5992 keeps two unruled leads. Seven sentences wait on the 1985 conformance statement (PB4997).

Beside it, a measured class: compiling every suite case refused at 85 and reading the messages found 20 diagnostic codes, 42
places, that cite only a later edition's rule where 1985 has its own; seven more turned up by hand. PB5993 holds the list and
the fix, one citation table keyed by edition with a drift test over the 85 refusal corpus.

### PB5993: the wrong-edition citation class re-measured; eight 1985 arms fixed

The note counted 20 codes citing only a later edition at `--std 85`. Re-run on this tree with the net's own rules (an edition
gate's "(targeting COBOL-85)" words, a descriptor's `No1985Rule`), 18 of the 27 named codes were never offenders. Real ones:
COBOL2403 now cites §1985:VI.5.3.4 GR1, 0854 §1985:VI.5.8.3 SR1b, the report arm of 2422 §1985:XIII.3.17.3, 2198
§1985:XIII.3.21.3 SR3 (and no longer says NATIONAL or prints the enum name), 1858 names the omitted ORGANIZATION as §1985:VII.2.6.3
GR3 through `FileModel.OrganizationFace(EditionInfo)`, 1641 cites §1985:XII.3.2, 2894 names ANSI X3.23-1985 and §1985:I.1.5.2.5.2,
and 2691 records `No1985Rule` (1985 has no `>>` directive). COBOL1927 was not an edition defect: BINARY is a 1985 word.

Compiling every suite case and refusal not refused at 85 at `--std 85` shows the class is wider: 336 codes. That work, and the
register's `Standard` column citing §2023 at every edition, stay open in PB5993. Six goldens `pb5993-*-85` pin the citations. Before and after: of the note's 27 named codes 9 were real offenders and 8 are fixed; the wider class stands at 336 codes, listed with their sites in `kb/Work/PB5993/2026-11-21-split-codes-left.md`.

