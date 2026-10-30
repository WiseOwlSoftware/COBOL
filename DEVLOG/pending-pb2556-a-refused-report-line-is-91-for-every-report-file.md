## PB2556: a refused wide report line is `'91'` for every report file, and DOC-A.1-159 says so

Wave 20261777 A, kb/Work PB2556 (one mechanism, documentation plus one witness; no behavior change).

**Re-probe.** The adjudication's `rptwide.cob` (report file ORGANIZATION LINE SEQUENTIAL, FILE STATUS, a USE AFTER STANDARD
ERROR procedure, the second of three GENERATEs carrying U+0100 through a PIC N SOURCE), `--std 2023`, on the wave build:
`OPEN FS=00`, `GEN1 FS=00`, `GEN2 FS=00`, `GEN3 FS=00`, `CLOSE FS=00`, no USE output, and the file holds `DD A`, an empty
line, `DD B`. The note held: `FileRegistry.RegisterReport` registers every report connector `lineSequential: false`
(PB1664 made the line end the host newline whatever the ORGANIZATION), the print arm's refusal answers
`CharacterWithoutByteImage` ('91', §9.1.13.11), and `FileCharacterSet` documented exactly that. Only DOC-A.1-159 said
`'71'` for a LINE SEQUENTIAL report file, and said it twice.

**Fix.** The row now says `'91'` for every report file, names why (a report file is never a line sequential file to its
connector, and §14.9.51.4 GR23's `'71'` is a line sequential file's WRITE rule), and records the facts a program can
see (a blank line, FILE STATUS `'00'`, no USE). The same edit carries the PB2510 sentence: GnuCOBOL 3.2.0 writes no
page-advance representation in a report file (its intermediate pages run straight into the next heading; only the last
page is padded at TERMINATE), so there is none to follow, and where it does represent a page advance on a print stream
it writes the form feed this compiler writes. `RegisterReport` carries a comment saying the flag is deliberate.

**Witness.** `conformance-suite/cases/A.1-Implementor-defined-language-element-list/pb2556_report_line_refusal_line_sequential`
(accept 2023: the LINE SEQUENTIAL phrase is a 2023 element and COBOL0900 refuses it below). It pins the observable facts
on a LINE SEQUENTIAL report file with a FILE STATUS item and a USE procedure. It is the second witness of DOC-A.1-159
(inventory batch: GAP 20 to 20).

PB2556 is landed with `closes_rows: []` (a determination restated; no row changed verdict). PB1253, which rests on the
same row, may now proceed.
