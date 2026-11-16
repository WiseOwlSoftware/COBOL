# Rulings on read pass 16 of the 1985 code reads (slices 1985-231 to 1985-236; the Mythos lane, 2026-11-16)

Read pass 16 read 35 rows again on main `7a32e1dfd2227042dea69db82f70d1e0085a50cc`: 31 whose contradiction's fix
had landed since their read and 4 that held no read. The readers found all 35 to conform. The refuters upheld 31,
overturned 2 and doubted 2 (`E:/COBOL-coord/scratch/coderead-1985/non-agreeing-231-236.json`). This file rules
the four. I ran the three programs behind rows 1 to 3 again myself on that build at `--std 85`
(`E:/COBOL-coord/scratch/my-20261115/b16-check/`); what each printed is given below. Every citation was run through
`python scripts/spec/cite.py --check` and answered OK.

| # | Row | Ruling | Harm | Note |
|---|---|---|---|---|
| 1 | `1985-GR-X.5.1-4` | UPHELD against the code (the text decides) | wrong_answer, under_rejects | PB5876 (new) |
| 2 | `1985-SR-IX.4.5.3-2` | UPHELD against the code (the text decides) | under_rejects | PB5877 (new) |
| 3 | `1985-GR-XIII.4.8.4-5` | UPHELD against the code (the precedence decides) | wrong_answer | PB5878 (new) |
| 4 | `1985-SR-VI.6.13.3-4` | REJECTED: the code conforms, a recorded determination | none | none |

The other 31 rows are confirmed by reader and refuter and need nothing; the slices' own files name them
(`E:/COBOL-coord/scratch/coderead-1985/out/1985-231.json` to `1985-236.json` with their `-refute.json`). Rows 1
and 3 had been contradicted before under kb/Work PB5846 and PB5825, row 2 under PB5794 and row 4 under PB5796:
each of those fixes holds for the mechanism it fixed, and what is ruled here is a second mechanism on the same
row (rows 1 to 3) or a reading the first fix left unstated (row 4).

## Mechanism 1, ruled from the text: a Linkage item that is not addressable is referenced implicitly, through OCCURS DEPENDING ON, and the program compiles and reads no storage

`§1985:X.5.1 GR4` lets a Linkage Section item be "referenced within the Procedure Division of that program if, and
only if," it is an operand of the header's USING phrase, subordinate to one, or one of the three related cases the
rule lists. `§1985:IV.4.4.1` says what a reference is: it may be explicit or implicit, and "Such an implicit
reference occurs if and only if the data item contributes to the execution of the statement." The object of an
OCCURS DEPENDING ON phrase contributes to every statement that references the table's record: it fixes its
length. So when the operand `L1` holds a table DEPENDING ON `LN`, and `LN` lies in a Linkage record that is NOT an
operand, `DISPLAY L1` references `LN` within the Procedure Division, and GR4 forbids it.

The compiler refuses every WRITTEN reference to a non-operand Linkage item (`COBOL2746`; the refuter's
`lk_callarg.cob`). It does not ask the question of the implicit one. My run of
`E:/COBOL-coord/scratch/coderead-1985/out/refute-1985-233/src/lk_odo.cob` at `--std 85`: exit 0, `S L1E1=A`, then
`S L1=` and nothing after it, though the caller passed five characters: `LN` has no storage and is read as zero.
The refuter reports the same output at `--std 2023`; what 2023 requires is judged from 2023's own text by the fix
(kb/Work PB1249, landed, read the 2023 rule as reaching written references only; the fix reads 2023's definition
of an implicit reference before it keeps or changes that).

UPHELD. Harm: wrong_answer (silent: the length is taken from storage that does not exist) and under_rejects.
Severity MAJOR. Site (the refuter's reading): `src/WiseOwl.Cobol.Compiler/Binding/LinkageBinder.cs#UnreferenceableLinkageRecord`
is asked for written references only. The fix refuses a statement whose operand's size depends on a Linkage item
GR4 does not admit; a table that no statement references is not refused.

## Mechanism 2, ruled from the text: READ ... KEY IS a REDEFINES of the record key, in the key's own record, compiles

`§1985:IX.4.5.3 SR2`: data-name-1 of the KEY phrase "must be the name of a data item specified as a record key
associated with file-name-1". `§1985:IX.2.7.4 GR3` extends the key to other records only: the key's character
positions "are implicitly referenced as keys for all other record description entries of that file". A level-05
item that REDEFINES the key lies in the key's OWN record description entry ("The total set of data description
entries associated with a particular record", `§1985:III.2`); it is not specified in a RECORD KEY or ALTERNATE
RECORD KEY clause, and GR3 does not reach it. So `READ IXF KEY IS IX-KR`, where `05 IX-KR REDEFINES IX-KEY`, must
be refused.

My run of `E:/COBOL-coord/scratch/coderead-1985/out/refute-1985-235/r3-redef.cob` at `--std 85`: exit 0 (it
compiles). Site (the refuter's reading): `RecordLayout.KeyIndexOfKeyItem`, whose positional arm admits any item of
the key's width at the key's positions in a record of the file, the key's own record included.

UPHELD. Harm: under_rejects. Severity MINOR. The refuter reports the same at `--std 2023`; the fix judges 2023
from its own rule. NOT ruled here: the refuter's side observation that an item of another CLASS at the key's
positions in another record is refused (`COBOL0864`, the choice of kb/Work PB5794's fix): the reader and the
refuter both left that row's verdict standing, and nothing new is claimed against it.

## Mechanism 3, ruled by the precedence: when an OPEN fails on a file that is open in another mode, the procedure of the ATTEMPTED mode runs, where GnuCOBOL runs the procedure of the mode the file is open in

`§1985:XIII.4.8.4 GR5b` runs the OUTPUT procedure "for any file open in the output mode or in the process of being
opened in the output mode", and GR5c says the same of the extend mode. An OPEN EXTEND that fails with 41 on a
file open in the output mode meets both sentences, and the text neither orders the two procedures nor says that
one alone runs. When only one of the two is declared, it runs, by its own sentence (kb/Work PB5825, landed, made
that so). When BOTH are declared the text leaves the choice open, and CLAUDE.md rule 1's order makes it:
GnuCOBOL (3.x branch) runs the procedure of the mode the file IS open in: `libcob/fileio.c` `cob_open` returns
status 41 at line 6342, before line 6350 stores the attempted mode in `last_open_mode`, and `cobc/codegen.c`
`output_error_handler` selects the mode procedure by `last_open_mode` (line 11583).

The compiler runs the attempted mode's (`FileConnector.UseModeView`: "the attempted mode's runs", the choice
PB5825's fix made without the precedence). My run of
`E:/COBOL-coord/scratch/coderead-1985/out/refute-1985-235/r1-modes.cob` at `--std 85`: `OPEN-E2 41` (OPEN EXTEND of
a file open OUTPUT) runs `USE-EXTEND`; GnuCOBOL's order gives `USE-OUTPUT`. `OPEN-E4 41` (OPEN OUTPUT of a file
open EXTEND) runs `USE-OUTPUT`; GnuCOBOL's order gives `USE-EXTEND`.

UPHELD. Harm: wrong_answer (a different procedure runs from the one the precedence gives; the 1985 text itself is
met by either). Severity MINOR. The fix changes the one arm where both modes have a procedure, keeps the arm
PB5825 fixed (only one declared: it runs), and writes the register sentence in `docs/conformance/`: "When an OPEN
fails on a file that is already open in another mode and a USE procedure is declared for each of the two modes,
the procedure of the mode the file is open in is executed, as GnuCOBOL does; the 1985 text names no order
(`§1985:XIII.4.8.4 GR5b`, GR5c)." The sequential, relative and indexed modules have the same two sentences: the
fix sweeps them.

## Mechanism 4, rejected: SPACE in a THROUGH range of EVALUATE is of class alphanumeric; a recorded determination

`§1985:VI.6.13.3 SR4`: "Two operands connected by a THROUGH phrase must be of the same class". The compiler takes
the figurative constant SPACE as class alphanumeric there: it accepts `SPACE THRU "Z"` and refuses SPACE THRU an
alphabetic item. The refuter doubts it because the only 1985 sentence that gives SPACE a category is in the MOVE
statement: "The figurative constant SPACE (SPACES) belongs to the category alphabetic" (`§1985:VI.6.19.4 GR3`).
Under that sentence taken generally the two results would be the other way round.

The text does not decide. That sentence stands in the rule that defines an elementary MOVE, beside a sentence
about ZERO that is expressly about a move; no rule gives a figurative constant a class anywhere else. What the
text says in general is that "A figurative constant may be used whenever 'literal' appears in a format"
(`§1985:IV.4.2.2.2.3`), and where it says what a figurative constant then is, it says a nonnumeric literal: "When
a figurative constant is used as the delimiter, it stands for a single character nonnumeric literal"
(`§1985:VI.6.27.4 GR8`). Both readings are honest. The precedence: GnuCOBOL has no rule here (`cobc` makes no
class test of the operands of an EVALUATE THROUGH: `typeck.c` and `parser.y` hold none), so the vendor manuals
decide, and IBM's Enterprise COBOL Language Reference (6.4, the figurative constants) treats SPACE as an
alphanumeric literal where an alphanumeric character is required. That is the compiler's reading.

REJECTED: the read's CONFORMS stands. **Register sentence owed (1985, `docs/conformance/`, key
`D-85-SPACE-CLASS`):** "In a context that requires a class and is not a MOVE, the figurative constant SPACE is of
class alphanumeric, as a nonnumeric literal is: `SPACE THRU "Z"` is one range of an EVALUATE and SPACE THRU an
alphabetic item is refused (`§1985:VI.6.13.3 SR4`). The 1985 text gives SPACE the category alphabetic in the MOVE
statement only (`§1985:VI.6.19.4 GR3`); GnuCOBOL makes no class test there; IBM Enterprise COBOL treats SPACE as
an alphanumeric literal." The witness is `pb5796`'s 1985 case (the reader names it).

## Leads the readers left (not rulings; each is checked before it is filed as anything)
- Slice 1985-231's reader: "at 85 a second OPEN OUTPUT on the same file succeeds and truncates it". It names no
  program. If one file connector is opened twice the rule is status 41; if two connectors share one physical
  file the 1985 text may say nothing. To be probed before anything is claimed.
- Wording at `--std 85`, each a 2023 citation or a 2023 remedy in a message: `COBOL0834` (cites a 2023 syntax
  rule), `COBOL2746` (cites `§2023` and suggests RETURNING or BASED, which 1985 does not have), `COBOL5506` and
  others (slices 233, 235, 236). misleads_diagnosis; they join kb/Work PB5875's mechanism (a message's citation is
  chosen by edition) rather than a note each.
