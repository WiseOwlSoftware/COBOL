## PB5045: the 1985 transcription is kept current, both amendments applied in place and evident misprints corrected, each attributed

The owner ruled on 2026-11-09 that the markdown transcription is the most accurate version of the specification: it
corrects evident misprints, applies what later revisions correct or change, attributes every modification, and never
keeps incorrect text because the printed page carried it; and the document says so. Until now
`specs/ANSI_COBOL_1985.md` did the opposite: "an amendment is never merged into the text it amends", and a misprint
that the sense showed (PB5031's `index-name-1`) stayed as printed.

**Misprints.** The leads PB4984 left as printed were re-judged, each on its rendered page. Five are corrected (C31-C35):
REDEFINES syntax rule 6 prints `the data-name referenced by data-name-2` where its own first sentence says `data item`;
SET general rule 2 prints `index-name-1` for index-name-2's own table (PB5031: general rule 3a provides for the two
indexing different tables); and the REWRITE rule of all three I-O modules copies the WRITE rule's `associated output
file`, though a REWRITE file is open in the I-O mode. Six stay, because they read right (`multiplier` names the BY
operand). The MOVE category list without alphanumeric is Addendum 1's own correction (row VI-104 (T)).

**Amendment 1, Addendum 1.** Every change row of its pages A-1 to A-24 (PDF 831-854) is applied where it belongs, in
its order, each read on its rendered page first: 141 Errata entries (C36-C176) for the table of contents, Section I's
module counts and diagram, the element summaries (the Intrinsic Function list, the 318 lines of I-54 to I-63
indented), the glossary, Section IV's function-names, classes, reference modification and identifier, the new 4.3.8.3
Function-Identifier with 4.3.8.3-4.3.8.5 renumbered behind it, the Section V and VI formats (subscripting with ALL,
SYMBOLIC CHARACTERS, the OCCURS box, the CD formats), the function-identifier syntax rules of the I-O statements, the
level-2 boxes the Addendum adds, and Appendix A to C. Three rows change only what the transcription does not draw; the
index rows give way to Amendment 2's index; Section A stays where Amendment 1 prints it, its one home (C176). The tool
that applies a batch (find, replace, flag, Errata row and entry) and the ledger and figure-verdict maintenance live in
the wave's scratch; the reconciliation ledger gains the added boxes, and every figure an amendment changed carries a
`drawn` verdict naming its Errata entry.

**The text says so.** The Preface, the About note and the Errata introduction now state the rule; specs/README.md and the
2002 About note follow; design 10.1 and 10.2 record the one-home decision (an amended rule is read from the body, with
the amended number; the amendment's change row only records `amended_by`), and 10.3 the misprints pass's correction rule.
