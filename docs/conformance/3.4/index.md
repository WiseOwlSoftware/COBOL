---
heading: "### 3.4 The MULTIPLE FILE TAPE clause at COBOL-85 (kb/Work PB5086)"
---
The clause (§1985:VII.2.11) is an obsolete element, deleted from 2002 (COBOL0902, VCR Table 7 row 7.6), and it depends on a reel type of device (§1985:p.XVII-94 item 7).
WiseOwl COBOL claims no reel device, so the clause's function, the placement of files on a reel, is a null facility (§1985:I.1.5.2.5.1: "Language elements that pertain to
specific hardware components for which support is not claimed need not be implemented."). The syntax rules of the statements that read it are not the clause's function, and
they are enforced: the clause is the source program's statement that its files share a reel (§1985:VII.2.11.3 GR1: "required when more than one file shares the same
physical reel of tape"), and every file it names resides on a multiple file reel for the rules below.

| Item | Determination |
|---|---|
| OPEN … EXTEND of a file in the clause (§1985:VII.4.3.3 SR2) | COBOL6814, at the OPEN statement. The rule holds for a clause that names one file: the clause states that the file shares a reel with files the program does not use (§1985:VII.2.11.3 GR1) |
| A MERGE naming two files of one clause (§1985:XI.4.1.3 SR6) | COBOL6815, at the MERGE statement. SORT has the opposite rule (§1985:XI.4.4.3 SR7) and is not checked |
| A file of the clause that is not sequential (§1985:VII.4.3.4 GR20 d: "Each of the files must be a sequential file.") | COBOL6816, at any OPEN that references a file of the set. The rule is a general rule and decidable from the source text, so it is refused at compile time |
| The other general rules of §1985:VII.4.3.4 GR20 (a, c: one file open at a time; the position numbers) | A null facility: no file is placed on a reel, so no order of opening or position constrains a disk file. GR20 applies only "Whenever a set of files resides on a multiple file reel", and §1985:C.3 lists the clause as "dependent upon a reel type of device", which WiseOwl COBOL does not claim (§9.2 states the absence, as §1985:I.1.5.2.5.1 requires: "The absence of such elements from an implementation of Standard COBOL must be specified"). Opening a second file of the clause while the first is open answers '00' (kb/Work PB5143) |
| A report file in the clause (§1985:XIII.2.4.4 GR1: "The MULTIPLE FILE TAPE clause is presented on page VII-16 in the Sequential I-O module"; §1985:XIII.2.4.3 SR2 lets a report file appear in it) | The placement on a reel and GR20 a and c are a null facility, as for any file of the clause, and GR20 d (COBOL6816) is enforced. The Sequential I-O OPEN rule (COBOL6814) is not: the Report Writer's OPEN states its own phrases, "The OPEN statement for a report file must contain only the OUTPUT phrase or the EXTEND phrase" (§1985:XIII.4.5.3 SR1), and does not restate the reel rule (kb/Work PB5479). Row 1985-GR-XIII.2.4.4-1, kb/Work PB5143 |

Basis, CLAUDE.md rule 1: the ISO text decides (the 1985 text, held in `specs/ANSI_COBOL_1985.md`), and it states the three rules as rules of the OPEN and MERGE statements, whose
elements the compiler fully supports. The hardware exemption lets an implementation omit the clause's function; it does not suspend rules stated of other statements.
