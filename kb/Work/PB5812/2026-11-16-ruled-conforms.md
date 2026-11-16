**2026-11-16, ruled CONFORMS by the 1985 text (the Mythos lane's rulings on the ten open readings of the 1985 code reads; rows `1985-GR-IX.4.8.4-4` and `1985-GR-VII.4.6.4-4`; tracked copy `kb/Work/PB5683/2026-11-16-rulings-open-readings.md`, sections 1 and 2).** The section follows unchanged. The note stays open until the store's records are adjudicated on the next records branch.

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
