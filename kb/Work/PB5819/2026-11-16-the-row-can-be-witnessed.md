# 1985-GR-XIII.4.5.4-18 can be witnessed: it waits on kb/Work PB5815

`1985-GR-XIII.4.5.4-18` is no derivation. The second refuter (`E:/COBOL-coord/scratch/my-20261115/unwitnessable/refute2/refute2.json`,
outcome OVERTURNED) wrote a program that witnesses the rule, and it FAILS on this compiler today by the mechanism of
kb/Work PB5815 (a report file read back as fixed blocks). When PB5815 is fixed, the program becomes the row's suite case.

The refuter's check of the derivation's facts: The argument's facts: FixedAttributeConflict compares no record size on any sequential file (TRUE); a report file records no size (TRUE: it is written as host-newline text lines, DOC-A.1-159). But the obligation is not empty: the created file must hold the record size fixed at creation, and it does not

The refuter's check of the arm: MISFIT: the first half's subject is the attribute the implementation fixes when its OPEN OUTPUT creates the file, observable by status 04 against 'the fixed file attributes for that file' (§1985:VII.1.3.5); definitional is never open to the implementation's obligation

## The program (`refute2/probes/g18rd2.cob`, in full)

```cobol
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G18RD2.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT-FILE ASSIGN TO "G18RPT2"
               FILE STATUS IS ST-R.
           SELECT IN-FILE ASSIGN TO "G18RPT2"
               ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS ST-I.
       DATA DIVISION.
       FILE SECTION.
       FD  RPT-FILE
           RECORD CONTAINS 20 CHARACTERS
           REPORT IS RPT.
       FD  IN-FILE
           RECORD CONTAINS 20 CHARACTERS.
       01  IN-REC PIC X(20).
       WORKING-STORAGE SECTION.
       01  ST-R   PIC XX.
       01  ST-I   PIC XX.
       01  WS-N   PIC 9.
       01  WS-EOF PIC X VALUE "N".
       REPORT SECTION.
       RD  RPT.
       01  DL TYPE IS DETAIL LINE NUMBER IS PLUS 1.
           05  COLUMN 1  PIC X(3) VALUE "ROW".
           05  COLUMN 5  PIC 9    SOURCE WS-N.
           05  COLUMN 18 PIC X(3) VALUE "END".
       PROCEDURE DIVISION.
       P0.
           OPEN OUTPUT RPT-FILE.
           DISPLAY "OPEN-OUT " ST-R.
           INITIATE RPT.
           MOVE 1 TO WS-N.
           GENERATE DL.
           MOVE 2 TO WS-N.
           GENERATE DL.
           TERMINATE RPT.
           CLOSE RPT-FILE.
           OPEN INPUT IN-FILE.
           DISPLAY "OPEN-IN " ST-I.
           PERFORM UNTIL WS-EOF = "Y"
               READ IN-FILE
                   AT END MOVE "Y" TO WS-EOF
                          DISPLAY "READ " ST-I " AT END"
                   NOT AT END
                          DISPLAY "READ " ST-I " [" IN-REC "]"
               END-READ
           END-PERFORM.
           CLOSE IN-FILE.
           STOP RUN.
```

## What the refuter derived from the text

```
OPEN-OUT 00
OPEN-IN 00
READ 00 [ROW 1            END]
READ 00 [ROW 2            END]
READ 10 AT END
```

## What the run printed (`refute2/run/g18rd2-001`)

stdout:

```
OPEN-OUT 00
OPEN-IN 00
READ 00 [ROW 1            END]
READ 00 [
ROW 2            E]
READ 04 [ND
                ]
READ 10 AT END
```

stderr:

```
WiseOwl COBOL notice: file 'E:\COBOL-coord\scratch\my-20261115\unwitnessable\refute2\run\g18rd2-001\g18rpt2.txt' is 44 bytes, which is not a whole multiple of the 20-byte record described for it (2 whole records + 4 trailing bytes). The record description and the file's layout disagree; reads after the first record will be misaligned. If this file was written by an earlier build, note that a BINARY or PACKED field now occupies its true bytes (COMP 1-2-4-8-16, COMP-3 packed BCD) rather than one byte per digit.
```

The refuter's correction: File a kb/Work defect: at --std 85 a report file under a Format 1 RECORD clause is written as delimited text lines (and with trailing spaces cut, g18rd), so the same description cannot read it back; this also contradicts DOC-A.1-159's 'integer-1 bytes in Format 1'. The row closes on g18rd2 (expected lines above) once that is fixed, or on a split: the second half ('must not subsequently be changed') alone is definitional of the user's programs.
