# Two adjudications by the Mythos lane, Sunday 2026-11-15 about 18:45 PST

Both were derived from the 1985 text first and then run on a build of main 5e49d46678
(`E:/COBOL-wt/gl1985-tree-a/src/WiseOwl.Cobol.Cli/bin/Debug/net10.0/cobol.exe`). Every citation below was run through
`python scripts/spec/cite.py --check` this evening with verdict OK.

## 1. UPHELD, a wrong answer at `--std 85`: a relation whose operand is a BLANK WHEN ZERO item holding spaces is evaluated as if the item were zero

**How it surfaced.** An adjudicator checking which cases really witness `1985-GR-VI.5.4.4-2` noticed that
`conformance-suite/cases/.../pb509_blank_when_zero_relation` (accepted at 85 since 369468ccc6, 2026-10-16) expects
TRUE for three relations; a refuter confirmed it with a minimal program
(`E:/COBOL-coord/scratch/my-20261115/about-row-refute/runs/bwzmin/bwzmin.cob`,
`E:/COBOL-coord/scratch/my-20261115/about-row-refute/refute.json`). I re-derived it and ran it.

**The 1985 text.**
- `§1985:VI.5.4.4 GR1`: "When the BLANK WHEN ZERO clause is used, the item will contain nothing but spaces if the
  value of the item is zero."
- `§1985:VI.5.4.4 GR2`: "When the BLANK WHEN ZERO clause is used for an item whose PICTURE is numeric, the category
  of the item is considered to be numeric edited."
- `§1985:IV.4.3.3`: "The alphanumeric class includes the categories of alphanumeric edited, numeric edited, and
  alphanumeric (without editing)." So the item is of class alphanumeric, a nonnumeric operand.
- `§1985:VI.6.3.1.1.2 GR1` (one numeric and one nonnumeric operand): "the numeric operand is treated as though it
  were moved to an elementary alphanumeric data item of the same size as the numeric data item (in terms of standard
  data format characters), and the content of this alphanumeric data item were then compared to the nonnumeric
  operand."
- Clause VI.5.4.4 has two general rules and no other. 1985 has no rule that an all-spaces item counts as zero. 2023
  has one, for a sending item of an operation (`§2023:13.18.8.4 GR3`: "the content of the sending data item is all
  spaces, the value of the sending data item is considered to be zero"); whether that rule reaches a relation
  condition at 2023 is 2023's question and is not decided here.

**Derived for the program** (`01 WP PIC 9(5) BLANK WHEN ZERO.`, `01 WZ PIC 9(5) VALUE 0.`, `MOVE 0 TO WP`,
`IF WP = WZ`): WP holds five spaces (GR1) and is numeric edited (GR2), so the relation is the nonnumeric comparison;
WZ is an integer item, so the relation is legal, and it is compared as the characters `00000`. Five spaces are not
equal to `00000` in any collating sequence. **1985 requires the false branch: `NE`.**

**Observed** (`run_case.py`, `--std 85`, exit 0): `WP=[     ] WX=[     ]`, then **`EQ`**, then `X-NE`. The same at
`--std 2023`. The control `IF WX = WZ` (WX a PIC X(5) holding the same five spaces) prints `X-NE`, so the compiler
does compare characters for an alphanumeric item and treats the BLANK WHEN ZERO item differently.

**The site** (the refuter's reading, which I have not re-read line by line):
`src/WiseOwl.Cobol.Compiler/CodeGen/Emit/ConditionRenderer.cs#BlankWhenZeroSenderTest`, called from
`#RenderRelational` with no test of the edition; other 1985 differences in the same file are gated on
`ctx.Data.DialectLevel < 2002`.

**Verdict.** A defect at `--std 85`, harm `wrong_answer`: a program that tests a blanked item against zero takes the
wrong branch. It needs a kb/Work note of its own (none covers it: the open row `1985-GR-VI.6.3.1.1.2-1` is PARTIAL
for another reason). What the fix must settle from each edition's own text: which edition first states the
all-spaces rule (read the 2002 text of the clause; I did not), and whether that rule covers a relation condition at
all, at 2002 and at 2023.

**Rows.** `1985-GR-VI.6.3.1.1.2-1` (the comparison rule the compiler does not apply to this operand) and
`1985-GR-VI.5.4.4-2` (the item is numeric edited): the note claims both. `pb509_blank_when_zero_relation` is not an
85 witness of anything: its band loses 85 until the fix lands and its 85 expectation is derived from the 1985 text
(EQ-NUMITEM, EQ-EDITED and GT-NEG all FALSE by the refuter's derivation; GT-NEG because `§1985:VI.6.3.1.1.2` compares
the unsigned characters `00003`).

## 2. DECIDED, the refused fix of wave 4: `1985-GR-XIII.4.5.4-9` has no witness on this implementation; the compiler's status 07 conforms; no golden and no defect

**The disagreement** (`E:/COBOL-coord/scratch/gl1985/lane1/reports/g1985-xiii-4-5-open.refute.json` and `.fix.json`).
The writer called the row not closable. The refuter wanted a golden that expects I-O status 00 for
`OPEN OUTPUT ... WITH NO REWIND` on a disk report file, and a defect because the compiler gives 07. The fixer
refused both.

**The text.** `§1985:XIII.4.5.4 GR9` begins "If the storage medium for the file permits rewinding, the following
rules apply". `§1985:VII.1.3.5` defines status 07: "for an OPEN statement with the NO REWIND phrase, the referenced
file is on a non-reel/unit medium", a successful completion. Which media permit rewinding is the hardware's and the
implementor's to say; the fixer cites `§1985:C.3` for that.

**Decision.** The fixer is right. WiseOwl's files are on a medium it classes as non-reel/unit, so GR9's condition
is not met, its two sub-rules do not apply, and an OPEN with NO REWIND completes with 07 as `§1985:VII.1.3.5` says.
There is nothing for a program to observe of GR9 here, so the row gets no golden, and 07 is no defect. The refuter's
sentence from the CLOSE preamble (sequential mass storage treated as tape) governs CLOSE's reel and unit phrases; it
does not make a disk a medium that "permits rewinding" for OPEN, and it cannot outrank the status definition that
names this very case.

**What is owed (kb/Work PB5779).** One `kind: decision` note for the row with this reasoning, and an entry in `docs/conformance/` for
1985 stating the classification (the 2023 register has it as `DOC-A.1-24`; an edition-specific statement names its
edition). Until the entry exists the row stays open: a row that rests on an implementor's classification closes on
the documented determination, not on silence.

## 3. UPHELD, legal source refused at `--std 85`: a CORRESPONDING operand whose group ancestor is USAGE IS INDEX (kb/Work PB5778)

**How it surfaced.** The refuter of the adjudication of `1985-GR-VI.6.4.3-3`
(`E:/COBOL-coord/scratch/my-20261115/open20-refute/refute.json`) overturned its CONFORMS. I checked the citations and
ran its program (`open20-refute/probes/vi-inherit.cob`).

**The 1985 text.** `§1985:VI.6.4.3 GR3`: "The description of D1 and D2 must not contain level-number 66, 77, or 88
or the USAGE IS INDEX clause." `§1985:VI.6.4.3 GR4` uses the same verb for subordinate items ("contains a
REDEFINES, RENAMES, OCCURS, or USAGE IS INDEX clause is ignored"). `§1985:VI.5.14.4 GR1`: "If the USAGE clause is
written at a group level, it applies to each elementary item in the group." A level-number is something an entry
has, not something it inherits, so "the description of D1 contains" speaks of D1's own entry. `§1985:VI.6.4.3 GR2`
then keeps index data items out of the move by its own words: a pair corresponds only where "the resulting move is
legal according to the move rules".

**Derived for the program** (`01 G USAGE IS INDEX.` / `05 D1.` / `10 A.` / `10 B.`, and
`MOVE CORRESPONDING D1 TO D2`, D2 holding `A PIC 9 VALUE 7` and `B PIC 9 VALUE 8`): D1's entry holds no USAGE
clause, so GR3 is met; A and B of D1 are index data items, no move from them is legal, so no pair corresponds (GR2)
and nothing is moved. **1985 requires the program to be accepted and to print `78`.**

**Observed.** `--std 85`: exit 65, `COBOL1757: MOVE CORRESPONDING operand 'D1' is described with the USAGE IS INDEX
clause (inherited from its group 'G')`, citing `§1985:VI.6.4.3 GR3`. `--std 2023`: exit 0, prints `78`.

**Verdict.** A defect at `--std 85`, harm `rejects_legal_source`, small in reach: the 85 arm
(`src/WiseOwl.Cobol.Compiler/Binding/Validation/StatementValidation.cs#CheckCorrespondingGroupOperand`, its
`Level85Forbidden`) walks up the operand's ancestors where the rule is about the operand's own entry. The row
`1985-GR-VI.6.4.3-3` is PARTIAL, not CONFORMS: the direct cases (a level 66 operand, an operand whose own entry says
USAGE IS INDEX) are refused as the rule requires.
