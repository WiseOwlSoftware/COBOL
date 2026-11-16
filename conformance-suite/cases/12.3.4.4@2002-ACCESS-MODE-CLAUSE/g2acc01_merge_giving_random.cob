      *>--- conformance-case
      *> id: g2acc01_merge_giving_random
      *> rules: [2002-SR-12.3.4.4.2-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:12.3.4.4.2 SR1 - the MERGE GIVING arm: "The RANDOM
      *> clause shall not be specified for file-names specified in the
      *> USING or GIVING phrase of a SORT or MERGE statement."
      *>   cite.py --check "§2002:12.3.4.4.2 SR1" "The RANDOM clause
      *>   shall not be specified for file-names specified in the USING
      *>   or GIVING phrase of a SORT or MERGE statement" -> OK
      *> OUTF is a relative file whose entry says ACCESS MODE IS RANDOM
      *> and it is named in the GIVING phrase of the MERGE: the
      *> program violates SR1 and is refused at the MERGE (line 47).
      *> (The same program with ACCESS MODE IS SEQUENTIAL compiles
      *> clean at --std 2002.)
      *> The other three arms (SORT USING, SORT GIVING, MERGE USING)
      *> are the pb994-* refusals, which refuse at 2002 too. Every
      *> other line is legal 2002 source, so the one diagnosis is SR1.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2ACC01.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT IN1 ASSIGN TO "G2ACC01A.DAT".
           SELECT IN2 ASSIGN TO "G2ACC01B.DAT".
           SELECT SW ASSIGN TO "G2ACC01W.DAT".
           SELECT OUTF ASSIGN TO "G2ACC01O.DAT"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS RANDOM
               RELATIVE KEY IS RKY.
       DATA DIVISION.
       FILE SECTION.
       FD  IN1.
       01  I1          PIC X(4).
       FD  IN2.
       01  I2          PIC X(4).
       SD  SW.
       01  SWR         PIC X(4).
       FD  OUTF.
       01  OREC        PIC X(4).
       WORKING-STORAGE SECTION.
       01  RKY         PIC 9(4).
       PROCEDURE DIVISION.
       MAIN-P.
           MERGE SW ON ASCENDING KEY SWR
               USING IN1 IN2 GIVING OUTF.
           STOP RUN.
