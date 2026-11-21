      *>--- conformance-case
      *> id: pb5389-report-file-same-area-omitted-from-sort-area-85
      *> rules: [1985-SR-XI.2.5.3-5]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5389. A report file may be named in a SAME clause
      *> without the RECORD phrase (§1985:XIII.2.4.3 SR2, "A file-name
      *> that represents a report file can appear in a MULTIPLE FILE TAPE
      *> clause or in a SAME clause", cite.py: OK), and it is not a sort
      *> or merge file, so §1985:XI.2.5.3 SR5c governs it: "all of the
      *> files named in that SAME clause must be named in that SAME SORT
      *> AREA" clause (cite.py: OK). RPF is in SAME AREA with OF1 and in
      *> SAME SORT AREA without it: refused, citing SR5c. The accepted
      *> twin is pb5389-report-file-same-area-in-sort-area-85.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5389N.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPF ASSIGN TO "pb5389r.txt".
           SELECT OF1 ASSIGN TO "pb5389o.dat".
           SELECT SF ASSIGN TO "pb5389s.tmp".
       I-O-CONTROL.
           SAME AREA FOR RPF OF1
           SAME SORT AREA FOR RPF SF.
       DATA DIVISION.
       FILE SECTION.
       FD RPF REPORT IS R1.
       FD OF1.
       01 O1 PIC X(10).
       SD SF.
       01 S1 PIC X(10).
       REPORT SECTION.
       RD R1.
       01 D1 TYPE DE LINE PLUS 1.
          02 COLUMN 1 PIC X(5) VALUE "HELLO".
       PROCEDURE DIVISION.
       P0.
           OPEN OUTPUT RPF.
           INITIATE R1.
           GENERATE D1.
           TERMINATE R1.
           CLOSE RPF.
           DISPLAY "RAN".
           STOP RUN.
