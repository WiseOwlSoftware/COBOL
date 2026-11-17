# Rulings on read pass 17 of the 1985 code reads (slices 1985-237 to 1985-242)

Ruled 2026-11-16 about 18:25 PST by the Mythos lane session (Claude Mythos 5.2). The pass read 34 KEPT rows again on
a compiler built at main 946daf5a4d (32 whose fix had landed since their read, 2 never read): 23 agree (reader
CONFORMS, refuter UPHELD) and 11 do not (`E:/COBOL-coord/scratch/coderead-1985/non-agreeing-237-242.json`). I ran
every program named below myself, with the tracked probe runner on the same compiler; "MEASURED" is my run and its
directory is given. A landed note is not reopened: each residue of a landed fix is a new note.

| row | reader | refuter | my ruling | note |
|---|---|---|---|---|
| `1985-GR-IV.4.3.8.4.4-5` | PARTIAL (a) | not in scope | UPHELD (a), under_rejects | PB5881 |
| `1985-GR-VII.2.8.4-3` | CONFORMS | DOUBTFUL | the doubt is REJECTED for this row; a separate defect | PB5882 |
| `1985-SR-IX.4.5.3-2` | CONFORMS | OVERTURNED | UPHELD (b), under_rejects | PB5883 |
| `1985-SR-VI.4.3.3-1` | CONFORMS | OVERTURNED | UPHELD (c), under_rejects | PB5884 |
| `1985-SR-VI.4.4.3-1` | CONFORMS | OVERTURNED | UPHELD (c), under_rejects | PB5884 |
| `1985-SR-VII.2.3.3-3` | CONFORMS | DOUBTFUL | the doubt is REJECTED; a register sentence is owed | none |
| `1985-SR-XIII.4.9.3-4` | PARTIAL (b) | not in scope | UPHELD (b), under_rejects | PB5885 |
| `1989-AR-A.2.9.3-1` | CONFORMS | OVERTURNED | UPHELD (b), under_rejects and wrong_answer | PB5886 |
| `1989-AR-A.2.14.3-1` | CONFORMS | OVERTURNED | UPHELD (b), under_rejects and wrong_answer | PB5886 |
| `1989-AR-A.2.28.3-1` | CONFORMS | OVERTURNED | UPHELD (b), under_rejects and wrong_answer | PB5886 |
| `1989-AR-A.2.35.3-1` | CONFORMS | OVERTURNED | UPHELD (b), under_rejects | PB5886 |

Cause letters as in the earlier batches: (a) wrong at 1985 only, (b) wrong the same way at 2023, (c) the 1985 arm
exists and is incomplete. What 2002 requires is not ruled anywhere here (the 2002 inventory is frozen).

## Mechanism 1 (PB5881): at `--std 85` the NUMERIC class test of an alphabetic reference-modified item compiles

**Ruling: UPHELD (a), harm under_rejects, MINOR.** `IF AB(1:2) NUMERIC` with `01 AB PIC A(5)` compiles at `--std 85`;
the unsliced `IF AB NUMERIC` is refused (COBOL6917). The row `1985-GR-IV.4.3.8.4.4-5` does not stand as confirmed.

**The text.** A reference-modified alphabetic item stays alphabetic, and the NUMERIC test may not be used on it:
- `§1985:IV.4.3.8.4.4 GR5`: "the same class and category as that defined for the data item referenced by data-name-1"
  `OK  §1985:IV.4.3.8.4.4 GR5  (4.3.8.4.4 General Rules)  the text is in GR5`
- `§1985:VI.6.3.1.2 GR5`: "The NUMERIC test cannot be used with an item whose data description describes the item as alphabetic"
  `OK  §1985:VI.6.3.1.2 GR5  (6.3.1.2 Class Condition)  the text is in GR5`

**MEASURED.** `E:/COBOL-coord/scratch/coderead-1985/out/work-1985-237/src/rm5d.cob`, compile only, `--std 85`: exit 0, no diagnostic (`E:/COBOL-coord/scratch/my-20261115/b17-check/run/rm5d-001`).
**Where (the reader's reading of the code, a lead):** `ConditionBinder.CheckClassConditionOperand` reads the
description through `MoveTable16.OperandItem`, which is null for a reference-modified place, and its comment says a
slice is never alphabetic. kb/Work PB5813 (landed) fixed the same mechanism for INITIALIZE.
**Sweep the fix owes:** every rule keyed on the class or category of an operand that may be reference-modified
(the class conditions ALPHABETIC, ALPHABETIC-LOWER, ALPHABETIC-UPPER and a class-name; the relation condition's
operand classes; INSPECT, STRING, UNSTRING operands; the sibling leads PB5813 names at `MoveOverlap.cs` and
`ExpressionBinder.cs`): ONE answer to "the category of this operand", asked by all of them.
**2023.** The reader reports the form is right at 2023 by 2023's own class condition rules; not ruled here.

## Mechanism 2 (PB5882): at `--std 85` an I-O status value the 1985 text does not define is stored (71)

**Ruling for the row: the refuter's doubt is REJECTED.** `1985-GR-VII.2.8.4-3` is confirmed: the rule leaves the
method to the implementor, and a method that cannot hold its own delimiter in a record is a method.
- `§1985:VII.2.8.4 GR3`: "the method used for determining the length of a variable length record is that associated with implementor-name-1 by the implementor"
  `OK  §1985:VII.2.8.4 GR3  (║2.8.4 General Rules║)  the text is in GR3`

**A separate defect, UPHELD (a), harm wrong_answer, MINOR.** Under `RECORD DELIMITER IS LINE-SEQUENTIAL` a WRITE of
a record that holds the line delimiter is unsuccessful with I-O status **71** at `--std 85`. 1985 lists the values
an I-O statement places in the I-O status and gives every condition the implementor adds the form 9x:
- `§1985:VII.1.3.5`: "The following is a list of the values placed in the I-O status for the previously named conditions"
  `OK  §1985:VII.1.3.5  (1.3.5 I-O Status)  the text is in the clause; it lies in GR5`
- `§1985:VII.1.3.5`: "An implementor-defined condition exists. This condition must not duplicate any condition specified by the I-O status values 00 through 49"
  `OK  §1985:VII.1.3.5  (1.3.5 I-O Status)  the text is in the clause; it lies in GRL2.5a`
71 is not in the list and is not of the form 9x. At 2023 the value is the standard's own.

**MEASURED.** `E:/COBOL-coord/scratch/coderead-1985/out/refute-1985-238/rdely.cob` at `--std 85`: `LFW 71` (`E:/COBOL-coord/scratch/my-20261115/b17-check/run/rdely-001`; the other lines print a
status and a record length, `00 06`, `00 03`: the second number is the length, not a status).
**DECIDED (the design is mine).** At `--std 85` the FILE STATUS item never receives a value the 1985 text does not
list. A condition 1985 does not name is an implementor-defined condition at that edition and is reported 9x, with
x fixed one-to-one per condition and documented in the conformance register. It is built at the ONE place a status
is stored for the program (an edition behaviour beside the others), never in each statement; a test enumerates
every status value the runtime can produce and requires each to be a 1985 value or to have its documented 9x at
1985, so a status added later cannot leak. USE procedures and the at end and invalid key tests see the 1985 value.
**Leads for the sweep (the refuter's reading of the code, not measured by me):** the same line sequential arm can
return 06 and 09 on a READ; the sharing and locking values (5x, 6x) and 2023's other additions belong to the same
enumeration.
**Where (the refuter's reading):** the name LINE-SEQUENTIAL sets `FileModel.LineSequentialByDelimiter` at every
edition and the runtime takes the line sequential arm whole.

## Mechanism 3 (PB5883): READ ... KEY IS a subscripted table element that lies over the record key compiles (every edition)

**Ruling: UPHELD (b), harm under_rejects, MINOR.** `READ IXF KEY IS IX-T (1)`, where `IX-T PIC X(4) OCCURS 2` lies
at the prime record key's positions in a second record description of the file, compiles and runs at `--std 85` and
at `--std 2023`. The row `1985-SR-IX.4.5.3-2` does not stand as confirmed. kb/Work PB5877 (landed) refused the
REDEFINES form; this is another form of the same phrase.

**The text.** The KEY phrase takes a data-name that may be qualified and nothing more; a table element must be
subscripted, so it is never a bare data-name and never "a data item specified as a record key":
- `§1985:IX.4.5.3 SR2`: "Data-name-1 must be the name of a data item specified as a record key associated with file-name-1"
  `OK  §1985:IX.4.5.3 SR2  (4.5.3 Syntax Rules)  the text is in SR2`
- `§1985:IX.4.5.3 SR3`: "Data-name-1 may be qualified"
  `OK  §1985:IX.4.5.3 SR3  (4.5.3 Syntax Rules)  the text is in SR3`

**MEASURED.** `E:/COBOL-coord/scratch/coderead-1985/out/refute-1985-240/src/r1-sub-run.cob`: `R1 TWO` at `--std 85` (`E:/COBOL-coord/scratch/my-20261115/b17-check/run/r1-sub-run-001`) and at
`--std 2023` (`E:/COBOL-coord/scratch/my-20261115/b17-check/run/r1-sub-run-002`), exit 0 both.
**Where (the refuter's reading):** `KeyedIoBinder.BindRead` resolves the operand through `ResolveSending` and
`KeyIndexOfKeyItem` and never calls `DataBinder.ScreenDataNameShape` (COBOL2024), which the record-key-name arm
(`RecordKeyNameOf`) does call.
**Sweep the fix owes:** every phrase whose operand is a data-name that "may be qualified" and no more, on keyed
files (READ KEY IS, START KEY, the RELATIVE KEY and RECORD KEY clauses, ALTERNATE RECORD KEY): one screen of the
operand's SHAPE, asked by every arm. **2023:** the fix judges it from 2023's own text.

## Mechanism 4 (PB5884): at `--std 85` an implementor-name of this implementation is accepted as a computer-name

**Ruling: UPHELD (c), harm under_rejects, MINOR.** `SOURCE-COMPUTER. DISK.`, `OBJECT-COMPUTER. PRINTER.`,
`SOURCE-COMPUTER. LINE-SEQUENTIAL.` and `OBJECT-COMPUTER. BINARY-SEQUENTIAL.` compile at `--std 85`. DISK and
PRINTER are this implementation's ASSIGN device classes (`docs/conformance/7/DOC-A.1-71.md`) and LINE-SEQUENTIAL
and BINARY-SEQUENTIAL its RECORD DELIMITER names (`DOC-A.1-150.md`): each is an implementor-name, so none is a
computer-name. The rows `1985-SR-VI.4.3.3-1` and `1985-SR-VI.4.4.3-1` do not stand as confirmed. kb/Work PB5795
(landed) built the gate and its landing file names this as an open lead.

**The text.**
- `§1985:IV.4.2.2.1.2`: "Within a given implementation these three types of system-names form disjoint sets; a given system-name may belong to one and only one of them"
  `OK  §1985:IV.4.2.2.1.2  (4.2.2.1.2 System-Names)  the text is in the clause`

**MEASURED.** `E:/COBOL-coord/scratch/coderead-1985/out/refute-1985-240/src/r2-disk.cob` and `r2-rdnames.cob` at `--std 85`: both print `RAN`, exit 0
(`E:/COBOL-coord/scratch/my-20261115/b17-check/run/r2-disk-001`, `E:/COBOL-coord/scratch/my-20261115/b17-check/run/r2-rdnames-001`).
**Where (the refuter's reading):** `GateComputerName` consults `ImplementorNames` (the SPECIAL-NAMES device, feature
and switch names) and `ImplementorCodeNames`, two of the four tables that hold this implementation's
implementor-names.
**The root, and what the fix owes:** the implementor-names live in four tables and the gate reads two. ONE registry
of every implementor-name the implementation specifies (the register items 71, 150, 183, 184, 189 to 191 and 198
name them), read by the gate and by each clause that takes one, with a test that a name added to any clause is in
the registry. The language-names are the third disjoint set: say what the gate does with them.

## Mechanism 5: `ASSIGN TO` a word the program declares as a symbolic-character (no defect; a register sentence)

**Ruling: the refuter's doubt is REJECTED; `1985-SR-VII.2.3.3-3` is confirmed.** With
`SYMBOLIC CHARACTERS SYMA IS 66`, `ASSIGN TO SYMA` compiles and the word is taken as the name of the file, not as
the figurative constant. The 1985 text does not decide: the ASSIGN clause offers a word (implementor-name-1) and a
literal in the same place, and a word that is in two classes takes its class from the context. CLAUDE.md rule 1 then
gives GnuCOBOL's answer, which is the same: MEASURED, GnuCOBOL 3.2.0 at `-std=cobol85` compiles the program and
creates a file named `SYMA` (`E:/COBOL-coord/scratch/my-20261115/b17-check/gc/`). WiseOwl COBOL: `RAN`, exit 0
(`E:/COBOL-coord/scratch/my-20261115/b17-check/run/r5-sym-001`), the file is `syma.txt`.
**Register sentence owed** (`docs/conformance/7/DOC-A.1-71.md`, by the next records branch): in the ASSIGN clause a
word that the program also declares as a symbolic-character is the implementor-name (the file's external name), at
every edition that has symbolic-characters; GnuCOBOL 3.2 reads it the same way. The entry's sentence on a
constant-name (the program's declaration wins) is a different case and is not changed by this one; the registrar
quotes both and says so.

## Mechanism 6 (PB5885): a READ in a USE BEFORE REPORTING procedure whose record area holds a control data item compiles (every edition)

**Ruling: UPHELD (b), harm under_rejects, MINOR.** The procedure's `READ INF`, where `IN-KEY` of the file's record
is a control data item of the report, compiles at `--std 85` (the reader reports the same at 2023 and that the run
changes the control data item before the group is presented). The row `1985-SR-XIII.4.9.3-4` does not stand as
confirmed. kb/Work PB5808 (landed) covers the REDEFINES overlap; this is the store a READ makes into its record
area.

**The text.**
- `§1985:XIII.4.9.3 SR4`: "A USE BEFORE REPORTING procedure must not alter the value of any control data item"
  `OK  §1985:XIII.4.9.3 SR4  (4.9.3 Syntax Rules)  the text is in SR4`

**MEASURED.** `E:/COBOL-coord/scratch/coderead-1985/out/work-1985-241/ubr_read.cob`, compile only, `--std 85`: exit 0, no diagnostic
(`E:/COBOL-coord/scratch/my-20261115/b17-check/run/ubr_read-001`). Expected: refused, as a MOVE to the control data item is (COBOL2378).
**Where (the reader's reading):** the visitor that collects what a statement stores (`BoundStores`,
`Visit(BoundRead)`) counts the INTO operand and not the record area.
**Sweep the fix owes:** every statement that stores without naming its receiver: READ and RETURN (the record area,
every record description of the file), RECEIVE, ACCEPT, the implicit stores of STRING, UNSTRING (INTO, DELIMITER
IN, COUNT IN, POINTER, TALLYING), INSPECT, SEARCH and PERFORM VARYING, SET, INITIALIZE, and a CALL BY REFERENCE
(say what the rule can and cannot know there). ONE answer to "what does this statement store into".

## Mechanism 7 (PB5886): an integer argument that is a quotient with an integer FUNCTION term is accepted and truncated (every edition)

**Ruling: UPHELD (b), harm under_rejects, wrong_answer and silent, MAJOR.** `FUNCTION FACTORIAL(FUNCTION ORD(A) / 2)`
compiles at `--std 85` and returns 33! for 33.5; so do `MOD(7, FUNCTION ORD(A) / 4)`, `MOD(N * N * N / 2, 7)`,
`CHAR(FUNCTION ORD(A) / 2)` and `RANDOM(FUNCTION ORD(A) / 2)`. The rows `1989-AR-A.2.9.3-1`, `1989-AR-A.2.14.3-1`,
`1989-AR-A.2.28.3-1` and `1989-AR-A.2.35.3-1` do not stand as confirmed. kb/Work PB5832 (landed) built the screen;
its list of shapes has no term that is a function.

**The text.**
- `§1989:A.2.2 AR4`: "An arithmetic expression which will always result in an integer value must be specified"
  `OK  §1989:A.2.2 AR4  (2.2 ARGUMENTS**)  the text is in AR4`

**MEASURED.** `E:/COBOL-coord/scratch/coderead-1985/out/refute-1985-242/open3.cob` at `--std 85`, exit 0 (`E:/COBOL-coord/scratch/my-20261115/b17-check/run/open3-001`): `ORDHALF
518194401280000000` (33!), `MODHALF 1`, `INTQUOT 1`, `CUBEHALF 0`, `MODARG2 7`, `CHARHALF` blank, `RANDHALF 966`.
The refuter reports the same at 2023 and that the screen refuses the same quotients written with data items
(`N / M`, `(N + 1) / 2`): that is the refuter's run, not mine.
**Where (the refuter's reading):** the default arm of `IntrinsicArgumentRules` (`CollectAdditive`, `WitnessOf`)
lets a term it cannot classify pass.
**The root, and what the fix owes:** the screen fails OPEN on a term it does not know. A term that is a function
is classified by the function's own type (an integer function is an integer term; any other is not), and a term
the classifier cannot classify is NOT "always an integer". Legal forms must stay legal: an integer function alone,
sums, differences and products of integer terms, and a division only where both operands are literals whose
quotient is an integer. The expected result of each form is derived from the rule before it is run.

## A lead that is a defect (PB5887): a debugging line in REPLACE pseudo-text is executed without WITH DEBUGGING MODE

**Ruling: UPHELD, harm wrong_answer, MINOR; 1985 row not named by a read (the slice-241 refuter found it beside its
rows).** With no WITH DEBUGGING MODE clause, a `D` line written directly in the Procedure Division is a comment
(right), and the same `D` line brought in by `REPLACE ==DBG-ONE== BY == ... ==` is executed.
- `§1985:VI.7.3`: "After all COPY and REPLACE statements have been processed, a debugging line will be considered to have all the characteristics of a comment line, if the WITH DEBUGGING MODE clause is not specified"
  `OK  §1985:VI.7.3  (7.3 DEBUGGING LINES)  the text is in the clause`
**MEASURED.** `E:/COBOL-coord/scratch/coderead-1985/out/refute-1985-241/rep_dbg_ctl.cob` at `--std 85`: prints `VIA-REPLACE` then `END`, exit 0
(`E:/COBOL-coord/scratch/my-20261115/b17-check/run/rep_dbg_ctl-001`); `DIRECT-DLINE` is not printed. Expected: `END` alone. The refuter reports that the COPY
REPLACING twin hides its line as it should, and that kb/Work PB5382 (landed) claims both twins: that is the
refuter's reading. What the later editions require is not ruled.

## Leads that are not yet anything
- Diagnostics that cite a 2023 clause at `--std 85` (COBOL0864, COBOL2645, the refusals of ADD CORRESPONDING and of
  SET, COBOL1627): they join kb/Work PB5875.
- A computer-name that is a literal or all digits draws COBOL3734 beside its own refusal (the slice-240 reader).
- COBOL0863 says a file that is not visible "lacks LINAGE" (the slice-239 reader).
