      *>--- conformance-case
      *> id: g2-value-file-table-initialize
      *> rules: [2002-GR-13.16.61.3-2, 2002-GR-13.16.61.3-15]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:13.16.61.3 GR2 (banded FORMATS 1 AND 2) - "In the file
      *> section, VALUE
      *> clauses take effect only during the execution of an INITIALIZE
      *> statement. The
      *> initial value of the data items in the file section is
      *> undefined." The record
      *> is given known content first (MOVE "zzzzz") because its
      *> initial value is
      *> undefined and may not be asserted. INITIALIZE FREC ALL TO
      *> VALUE
      *> (§2002:14.8.19.3 GR5 c) 1. b., GR6 a) 3.) applies the format 2
      *> VALUE of T1 and
      *> the format 1 VALUE of E1. T1: "A" "B" FROM (1) with no TO runs
      *> to the
      *> maximum (GR14) giving A B A B; "C" FROM (2) runs 2..4 and, as
      *> the last
      *> FROM phrase (GR15), wins there: A C C C. E1 is E. Expected
      *> [ACCCE]; every
      *> occurrence is keyed. Fails if the file-section VALUE is
      *> ignored by INITIALIZE
      *> ([zzzzz]) or the first FROM phrase wins.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2VAL05.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "g2val05.tmp".
       DATA DIVISION.
       FILE SECTION.
       FD  F.
       01  FREC.
           05 T1 PIC X OCCURS 4
              VALUES ARE "A" "B" FROM (1) "C" FROM (2).
           05 E1 PIC X VALUE "E".
       PROCEDURE DIVISION.
       MAIN-P.
           OPEN OUTPUT F
           MOVE "zzzzz" TO FREC
           DISPLAY "BEFORE=[" FREC "]"
           INITIALIZE FREC ALL TO VALUE
           DISPLAY "AFTER=[" FREC "]"
           CLOSE F
           STOP RUN.
