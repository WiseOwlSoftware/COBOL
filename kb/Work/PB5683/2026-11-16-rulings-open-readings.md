# Rulings on the ten open readings of the 1985 code reads (the Mythos lane, 2026-11-16)

Ten stored code reads of 1985 rules were `undecided`: the reader said the code conforms, the refuter doubted or
overturned it, and each time the earlier ruling found that the 1985 words admit two readings. An open reading does
not close by waiting. This file rules each one, by CLAUDE.md rule 1's order: the 1985 text where it controls, then
what GnuCOBOL does, then a recorded determination. Nine are ruled here; one waits for a fix that decides it.

Every citation below was run through `python scripts/spec/cite.py --check` on main `c96e8fc9b0` and answered OK
(`E:/COBOL-coord/scratch/my-20261115/b17-check/cites.py`: 32 OK, 0 not OK; the two SET and subscripting citations
were checked beside it). The GnuCOBOL readings are of the 3.3-dev sources, `cobc/parser.y`, `cobc/field.c` and
`libcob/reportio.c` (the 3.x branch), by line.

| # | Row | Note | Ruling | Decided by |
|---|---|---|---|---|
| 1 | `1985-GR-IX.4.8.4-4` | PB5812 | CONFORMS | the 1985 text |
| 2 | `1985-GR-VII.4.6.4-4` | PB5812 | CONFORMS | the 1985 text |
| 3 | `1985-SR-VI.5.8.3-1` | PB5737 | CONFORMS | the 1985 text |
| 4 | `1985-SR-VI.5.10.3-5` | PB5799 | CONFORMS | the 1985 text |
| 5 | `1989-FMT-A.2.34.2` | PB5834 | CONFORMS | the 1989 text |
| 6 | `1985-SR-VII.4.7.3-5` | PB5805 | CONTRADICTED: a defect, under_rejects | the 1985 text, and GnuCOBOL agrees |
| 7 | `1985-SR-IV.4.3.8.1-L2.9` | PB5800 | CONFORMS, a recorded determination | GnuCOBOL |
| 8 | `1985-GR-XIII.3.16.4-3` | PB5824 | CONFORMS, a recorded determination | the lane (GnuCOBOL does not model the rule) |
| 9 | `1985-GR-XIII.3.10.7-3` | PB5830 | CONFORMS against the placeholder text | the owner's decision of 2026-11-09 (kb/Work PB4952) |
| 10 | `1985-GR-VII.4.7.4-13` | PB5815 | NOT RULED: it waits for the fix of PB5815 | the fix |

## 1 and 2. A contained program's own USE ON INPUT runs, not the container's USE GLOBAL ON the file (PB5812)

**Ruling: CONFORMS, both rows. The 1985 text controls.**

The question was whether `§1985:VII.4.6.4 GR4` ("When file-name-1 is specified explicitly, no other USE statement
applies to file-name-1") and GR5b's exception ("except those files referenced by file-name-1 in another USE
statement specifying the same condition") reach across programs, so that a containing program's
`USE GLOBAL ... ON FA` disqualifies the contained program's own `USE ... ON INPUT`.

They do not, and the text shows it in three steps.
1. The syntax rules of the same paragraph are scoped to one Procedure Division: `§1985:VII.4.6.3 SR3`, as
   Amendment 2 corrected it, "The same file-name must not appear in more than one USE AFTER EXCEPTION statement
   within the same Procedure Division", and `§1985:VII.4.6.3 SR6`, "may each be specified only once in the
   declaratives portion of a given Procedure Division". So a contained program may name in its own USE statement a
   global file that its container's USE statement also names.
2. Read across programs, GR4 would then make each of those two statements exclude the other, and no declarative
   would apply to the file. But `§1985:X.5.5.4 GR1` exists to choose between declaratives of different programs
   that both qualify: "only the first qualifying declarative will be selected for execution". GR4 can therefore
   only speak of the USE statements of one Procedure Division, and GR5b's "another USE statement", the same words
   on the same subject, has the same reach.
3. `§1985:X.5.5.4 GR1` then orders the programs: first (GR1a) "The declarative within the program that contains
   the statement which caused the qualifying condition", and only then (GR1b) "The declarative in which the GLOBAL
   phrase is specified" of the containing program. The contained program's INPUT declarative satisfies "the rules
   for execution of that declarative" (the file is open in the input mode and no other USE statement of that
   Procedure Division names it), so it is the first qualifying declarative.

The other reading also gives a result the rule cannot mean: a container's USE on the file WITHOUT GLOBAL, which
can never run in the contained program, would still silence the contained program's own declarative and leave
none. The Indexed I-O text is word for word the same (`§1985:IX.4.8.3 SR3`, `§1985:IX.4.8.4 GR4`, GR5b).

Probes (unchanged): `contradictions-b5/src/useglob.cob` prints `INNER-INPUT 23`, `contradictions-b7/src/p1rg.cob`
prints `INNER-INPUT 35`, each as GR1a requires. No register entry is owed: the text decides it. PB5812 closes.

## 3 and 4. An item below an OCCURS DEPENDING ON entry is not itself "a variable occurrence data item" in the rules (PB5737, PB5799)

**Ruling: CONFORMS, both rows. The 1985 text controls.**

The glossary sentence (`§1985:III.2`, "A variable occurrence data item is a table element which is repeated a
variable number of times", and such an item must contain the DEPENDING ON clause "or be subordinate to such an
item") can be read to make every item below an OCCURS DEPENDING ON entry a variable occurrence data item. The
rules do not use the term that way, and one of them cannot:

- `§1985:VI.5.8.3 SR1b` forbids the OCCURS clause in an entry that "Has a variable occurrence data item
  subordinate to it". If every item below an OCCURS DEPENDING ON entry were one, the entry
  `05 T OCCURS 1 TO 4 TIMES DEPENDING ON N.` followed by `10 A PIC X.` would have a variable occurrence data item
  subordinate to it, and SR1b would forbid the very OCCURS clause that makes the table. Format 2 on a group entry
  is plainly provided (`§1985:VI.5.8.3 SR3` speaks of a key that is "an entry subordinate to the entry containing
  the OCCURS clause"). So in SR1b the term means the entry that bears the DEPENDING ON phrase.
- `§1985:VI.5.15.6 GR5` uses it the same way: it speaks of "the DEPENDING ON phrase in the OCCURS clause specified
  for the variable occurrence data item", and it lists apart (GR5b) "It is a variable occurrence data item" and
  (GR5c) "It is a data item that is subordinate to a variable occurrence data item", which would be one case if
  the subordinate item were one.

With that meaning, `§1985:VI.5.8.3 SR1b` forbids an OCCURS entry ABOVE an OCCURS DEPENDING ON entry, which the
compiler refuses (`COBOL0854`), and leaves a fixed OCCURS inside such a table legal: `g1.cob` is legal and row 3
conforms. `§1985:VI.5.10.3 SR5` ("Neither the original definition nor the redefinition can include a variable
occurrence data item") forbids a REDEFINES pair that CONTAINS an OCCURS DEPENDING ON entry, which the compiler
refuses (`COBOL0855`, the control `r2.cob`), and leaves a pair inside such a table legal, as the same rule's
"data-name-2 may be subordinate to an item whose data description entry contains an OCCURS clause" says without
excepting format 2: `r1.cob` is legal and row 4 conforms.

GnuCOBOL does the same, which the ruling does not rest on: `cobc/field.c` lines 1422 to 1426 refuse a REDEFINES
only when `cb_field_variable_size` finds an OCCURS DEPENDING ON inside the redefinition or the original. No
register entry is owed. PB5737 and PB5799 close.

## 5. One ALL table may supply argument-1 and the argument-2 series of PRESENT-VALUE (PB5834)

**Ruling: CONFORMS. The 1989 text controls.**

`§1989:A.2.2 AR4` gates the ALL subscript on the function, not on a position ("When the definition of a function
permits an argument to be repeated a variable number of times"), and it defines what the reference means: "the
effect is as if each table element associated with that subscript position were specified". A reference is
therefore legal when the reference it stands for is legal. `PRESENT-VALUE (WS-E (ALL))` over four elements stands
for `PRESENT-VALUE (WS-E (1) WS-E (2) WS-E (3) WS-E (4))`, which the general format admits and which the compiler
evaluates to the same 60. The reading that ALL may stand only where the repeated argument stands needs words the
rule does not have. GnuCOBOL has nothing to say: its parser has no ALL subscript. No register entry is owed.
PB5834 closes.

## 6. A written sign on the integer of WRITE ADVANCING must be refused at 1985 (PB5805): a DEFECT

**Ruling: CONTRADICTED. Harm: under_rejects. PB5805 becomes the defect.**

`§1985:III.2` sets the default for every "integer" of a general format: "it must be neither signed nor zero unless
explicitly allowed by the rules for that format". `§1985:VII.4.7.3 SR5`, "Integer-1 may be positive or zero, but
must not be negative", allows zero in so many words. It does not allow a sign in so many words, and where the
standard does allow one it says so: `§1985:IV.4.3.8.2.3 SR6`, "Integer-1 may be signed and, if signed, it must be
positive"; `§1985:VI.6.23.3 SR4`, "Integer-1 and integer-2 may be signed. Integer-1 must be positive". "Must not
be negative" forbids; it is not an explicit allowance. So integer-1 of ADVANCING is unsigned at 1985.

GnuCOBOL reads it the same way: `cobc/parser.y` line 17834 takes the count as `num_id_or_lit`, whose literal is
the `integer` of line 20033, and that refuses every literal with a sign, plus or minus ("unsigned integer value
expected", lines 20036 to 20040); `ZERO` is admitted beside it (line 19675).

What the compiler does (`contradictions-b4/src/adv_*.cob`, `--std 85`): `+3` and `+0` compile (WRONG: each must be
refused); `-0` and `-1` are refused (right); `0`, `3` and `ZERO` compile (right). Site:
`src/WiseOwl.Cobol.Compiler/Binding/Procedure/Verbs/SequentialIoBinder.cs#ScreenAdvancingCount`, which admits any
integer literal that does not start with `-`. The fix refuses a signed literal there at `--std 85`. Each other
edition's arm is decided from that edition's own text by whoever fixes it (2023's row is `SR-14.9.51.3-15`); 2002
is frozen, so the fix adds no 2002 case and no 2002 verdict.

## 7. An unqualified LINE-COUNTER counts the report descriptions of the program it is written in (PB5800): a recorded determination

**Ruling: CONFORMS. The text does not decide it; GnuCOBOL does; the choice is recorded.**

`§1985:IV.4.3.8.1 SRL2.9` requires qualification "if more than one report description entry is specified in the
source program", and `§1985:XIII.3.5.6 SR3` "If more than one LINE-COUNTER exists in a program". Neither says
whether a program's count includes the report descriptions of a program it contains, or a containing program's
GLOBAL report (`§1985:X.4.4.4 GR1`: "the special registers LINE-COUNTER and PAGE-COUNTER are global names"). Both
readings are honest, as the earlier ruling found.

GnuCOBOL counts per program: `cobc/parser.y` keeps `report_count` (line 216), clears it in
`clear_initial_values` (line 1267), which `setup_program` calls for every program after the first, contained or
not (line 1462), and refuses an unqualified LINE-COUNTER only when that count is above one (lines 18998 to 19012).
So `lc1.cob` (one report description in the container, a second in the contained program, the container's
reference unqualified) and `lc2.cob` (a GLOBAL report in the container, the contained program's own report, the
contained program's reference unqualified) both compile there, and the second means the contained program's own
report. The compiler does the same in both (`OUTER 01`; `IN1 03`): `CounterResolver.ReportOf` takes the one
report of the nearest program that describes a report, and refuses (`COBOL2729`) when that program describes more
than one.

**Register sentence owed (1985, `docs/conformance/`):** "An unqualified LINE-COUNTER or PAGE-COUNTER in the
Procedure Division is resolved among the reports of the nearest program that describes a report visible at the
reference: the program the reference is written in or, when that program describes none, the nearest containing
program with a GLOBAL report. It refers to that program's report when there is exactly one and must be qualified
when there is more than one. The report descriptions of a contained program are not counted against its
container, and a container's GLOBAL report is not counted against a contained program that describes a report of
its own. The 1985 text leaves the count open (`§1985:IV.4.3.8.1 SRL2.9`, `§1985:XIII.3.5.6 SR3`); GnuCOBOL counts
per program." The sentence is what `CounterResolver.ReportOf` does (read on main `c96e8fc9b0`: the enclosing
report description first, then `Reports.NearestInScope` over the visible reports, exactly one or `COBOL2729`).
The GLOBAL case stands on `§1985:X.4.4.4 GR1` alone: GnuCOBOL gives a contained program no counter of a
container's report. PB5800 closes when the entry is in the register.

## 8. TERMINATE's control break is at FINAL when FINAL is a control (PB5824): a recorded determination

**Ruling: CONFORMS. The text admits two readings, GnuCOBOL does not model the rule, the lane decides and records it.**

`§1985:XIII.3.16.4 GR3` ignores NEXT GROUP on a control footing "that is at a level other than the highest level
at which a control break is detected". At TERMINATE no break is detected; `§1985:XIII.4.7.4 GR1` has the report
writer "produce all the control footing report groups beginning with the minor control footing report group". The
compiler takes that as the break at the top of the hierarchy, which is FINAL when FINAL is written
(`§1985:XIII.3.7.4 GR1`: "FINAL, if specified, is the highest control, data-name-1 is the major control"), so with
`CONTROLS ARE FINAL MAJ` the footing of MAJ is below the highest level and its NEXT GROUP is ignored. The other
reading takes the level from the same rule's last clause, "as though a control break has been sensed in the most
major control data-name", so that the footing of MAJ keeps its NEXT GROUP whether or not FINAL is written.

Reasons for the compiler's reading. That clause is the end of the sentence about the values SOURCE clauses and USE
procedures see ("makes the prior set of control data item values available"), and FINAL has no value to make
available; it does not say at which level the footings are produced. GR3 keeps the NEXT GROUP of the LAST footing
of a sequence, the one at the highest level; at TERMINATE with FINAL a control the last footing is FINAL's, and
under the other reading a NEXT GROUP written on the FINAL footing could never take effect. Neither reason makes
the other reading impossible, so this is a determination and not a reading the text forces.

GnuCOBOL cannot arbitrate: `libcob/reportio.c` saves a footing line's NEXT GROUP (`set_next_info`, line 247) and
applies it only when a later line is printed (`report_line`, lines 958 to 988), and `cob_report_terminate` (line
1419) prints every control footing with no level test. It has no rule 3 at any break, so its agreement with
either reading on one program would be an accident.

**Register sentence owed (1985, `docs/conformance/`):** "At TERMINATE the control break is at the highest level of
the control hierarchy: FINAL when FINAL is written in the CONTROL clause, otherwise the major control. The NEXT
GROUP clause of a control footing below that level is ignored (`§1985:XIII.3.16.4 GR3`). The 1985 text also
supports taking the level from the most major control data-name (`§1985:XIII.4.7.4 GR1`); WiseOwl does not."
PB5824 closes when the entry is in the register.

## 9. The first line of a page heading after a report heading (PB5830): confirmed against the placeholder text

**Ruling: CONFORMS against rule 3 of `§1985:XIII.3.10.7` as the tree holds it.**

Folio XIII-30 is one of the four printed pages the scan lacks. The owner decided on 2026-11-09 that placeholders
stand for those pages until ANSI supplies them (kb/Work PB4952, landed; `cite.py` marks the rule RECONSTRUCTED).
The rule the tree holds is therefore the rule a 1985 row is read against, and the code does what it says in both
arms of 3b (`contradictions-b8/src/m2phrel.cob`: line 5 after a report heading that ends on line 4; line 2 on a
page with none). The first arm also stands on printed text: `§1985:XIII.3.16.4 GR4`, the NEXT GROUP of a report
heading "can affect the location at which the page heading report group is presented".

This is not a claim about the printed folio. When ANSI's pages arrive, PB4952's reopening re-reads every row whose
rule is a reconstruction, this one among them; the record's adjudication says so. PB5830 closes with that
adjudication. Its sibling PB5789 (`1985-GR-XIII.3.10.7-1`, contradicted against the same placeholder) is a defect
and is not touched here.

## 10. CLOSE, OPEN EXTEND and one more line (PB5815's row): not ruled

`1985-GR-VII.4.7.4-13` stays `undecided`. The fix of kb/Work PB5815 decides it, as the ruling of batch 7 says: if
a print file then reads back by its lines, the added line is a record no WRITE released and the row is
contradicted under both readings. The row is read again on the fixed code when PB5815 lands.

## What follows
- The store: on the next records branch each of rows 1 to 5 and 7 to 9 is adjudicated to the reader's verdict with
  this file as its reference, and row 6 to the refuter's with kb/Work PB5805 as its work note.
- The register: PB5805 becomes a defect (under_rejects) for the fix lane. PB5812, PB5737, PB5799, PB5834 and
  PB5830 close with their adjudications; PB5800 and PB5824 close when their register sentences are filed (with the
  sentences kb/Work PB5870 already lists).
