# Ruling for kb/Work PB5785: what READ INTO must give when every record description is shorter than RECORD CONTAINS (the Mythos lane, Sunday 2026-11-15 20:05 PST)

**The question.** `FD FR RECORD CONTAINS 12 CHARACTERS` with one `01 RR PIC X(5)`, reading a file of 12-character
records with `READ FR INTO W` (W is `PIC X(12)`). The compiler prints `FS=00 W=[ABCDE       ]` at `--std 85` and at
`--std 2023` (`E:/COBOL-coord/scratch/my-20261115/contradictions-b2/src/rinto.cob`). The checker of code-read batch 2
found two results the 1985 text could support and left the choice open: (A) the 12 characters with status 00; (B)
five characters with status 04.

**The 1985 text** (each citation `cite.py --check` OK this evening).
- `§1985:VII.3.8.3 SR1`: "No record description entry for the file may specify a number of character positions
  greater than integer-1." A shorter description is legal.
- `§1985:VII.3.8.4 GR1`: "If the RECORD clause is not specified, the size of each data record is completely defined
  in the record description entry." So the record descriptions give the record's size only when there is no RECORD
  clause.
- `§1985:VII.3.8.4 GR3`: "Format 1 is used to specify fixed length records. Integer-1 specifies the number of
  character positions contained in each record in the file."
- `§1985:VII.4.4.4 GR8b`: the current record is moved to identifier-1, and "The size of the current record is
  determined by rules specified in the RECORD clause."
- `§1985:VII.4.4.4 GR13`: a record longer than "the maximum size specified by the record description entries" is
  truncated, and "an I-O status is set indicating a record length conflict has occurred".
- `§1985:VII.1.3.5`, status 04: "A READ statement is successfully executed but the length of the record being
  processed does not conform to the fixed file attributes for that file."

**Ruling: result (A). `FS=00 W=[ABCDEFGHIJKL]`.**
1. The file's records are 12 characters because the RECORD clause says so (GR3), and GR1 gives that job to the
   record descriptions only when the clause is absent.
2. GR8b names the RECORD clause as what sizes the record the INTO phrase moves. The sentence is there for this case
   and no other: where the descriptions and the clause agree it would say nothing.
3. Status 04 is defined by the file's FIXED ATTRIBUTES, and the record size the RECORD clause states is one of them.
   A 12-character record read from a file declared to hold 12-character records conforms to them, so there is no
   record length conflict and GR13 has nothing to act on. GR13's "sizes specified by the record description entries"
   are the file's record sizes as the program describes them, which with a RECORD clause present is what the clause
   says (GR1, GR3).
4. Result (B) would make every READ of such a file end in 04 with the data cut off, which empties the permission SR1
   gives and contradicts GR8b.

**What the fix owes.** At `--std 85`: status 00 and an implied move of integer-1 characters. The record area holds
the whole record; a reference to `RR` still sees its own five characters. The same question at 2002 and 2023 is
decided from each edition's own text by whoever fixes it (the checker reports that 2023's READ carries the same
truncation sentence; I did not read 2023's rules on the record's size).
