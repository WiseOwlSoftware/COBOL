      *>--- conformance-case
      *> id: g2-value-cd-record-table
      *> rules: [2002-GR-13.16.61.3-15, 2002-GR-13.16.61.3-1, 2002-SR-13.16.61.2-12, 2002-SR-13.16.61.2-15, 2002-SR-13.16.61.2-16, 2002-SR-13.16.61.2-17]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> VALUE clauses in a record description entry under an output
      *> CD. The record is
      *> the first (only) record of the CD, so it may contain VALUE
      *> clauses
      *> (§2002:A.1.2.2 SR9); communication-section VALUEs take effect
      *> when the program
      *> is placed in its initial state (§2002:13.16.61.3 GR4).
      *> TA  §2002:13.16.61.3 GR15 - "A" "B" FROM (1) TO (4) fills A B
      *> A B (GR12, GR13);
      *>     "C" FROM (2) TO (3) is the later FROM phrase naming
      *> elements 2 and 3, so
      *>     the last specified FROM phrase wins: [ACCB]. Fails if the
      *> first phrase wins
      *>     ([ABAB]) or the second phrase runs past its TO ([ACCC]).
      *> RC  §2002:13.16.61.2 SR15/SR16/SR17 accepted form - one
      *> integer subscript per
      *>     OCCURS level, TO at or after FROM in subscripted-reference
      *> order: (1 2) to
      *>     (2 1) is two successive elements (GR12 odometer), so RC(1
      *> 2) RC(2 1) are A,
      *>     (3 1) to (3 2) are B: [AABB]. Fails if the legal form is
      *> refused or the
      *>     increment is per dimension.
      *> E   §2002:13.16.61.2 SR12 accepted form - a group-level format
      *> 2 VALUE whose
      *>     subordinates are DISPLAY with no JUSTIFIED/SYNCHRONIZED:
      *> [ABAB].
      *> F1 TF FC  §2002:13.16.61.3 GR1 (FORMATS 1, 2, 3, AND 4) - a
      *> float-long item takes
      *>     an approximation of the literal. 1.5, 2.5, 3.5 and 0.5 are
      *> exact in radix
      *>     2 and 10, and COMPUTE ROUNDED of the item times 10 into
      *> ZZ9.99 gives 15.00,
      *>     25.00, 35.00 for any approximation within 0.0005; the
      *> condition-name FC-HALF
      *>     has the characteristics of FC (GR16), so the same literal
      *> makes it true.
      *> COBOL0903 (the communication facility is obsolete) is a
      *> warning, not a refusal.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2VAL01.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  D PIC ZZ9.99.
       COMMUNICATION SECTION.
       CD  CDOUT FOR OUTPUT.
       01  CREC.
           05 TA PIC X OCCURS 4
              VALUES ARE "A" "B" FROM (1) TO (4) "C" FROM (2) TO (3).
           05 R OCCURS 3.
              10 RC PIC X OCCURS 2 VALUE "A" FROM (1 2) TO (2 1)
                 "B" FROM (3 1) TO (3 2).
           05 E OCCURS 2 VALUE "AB" FROM (1) TO (2).
              10 E1 PIC X.
              10 E2 PIC X.
           05 F1 USAGE FLOAT-LONG VALUE 1.5E0.
           05 TF USAGE FLOAT-LONG OCCURS 2
              VALUES ARE 2.5E0 3.5E0 FROM (1).
           05 FC USAGE FLOAT-LONG VALUE 0.5E0.
              88 FC-HALF VALUE 0.5E0.
       PROCEDURE DIVISION.
       MAIN-P.
           DISPLAY "TA=[" TA (1) TA (2) TA (3) TA (4) "]"
           DISPLAY "RC=[" RC (1 2) RC (2 1) RC (3 1) RC (3 2) "]"
           DISPLAY "E=[" E (1) E (2) "]"
           COMPUTE D ROUNDED = F1 * 10
           DISPLAY "F1=[" D "]"
           COMPUTE D ROUNDED = TF (1) * 10
           DISPLAY "TF1=[" D "]"
           COMPUTE D ROUNDED = TF (2) * 10
           DISPLAY "TF2=[" D "]"
           IF FC-HALF
               DISPLAY "FC=HALF"
           ELSE
               DISPLAY "FC=NOT-HALF"
           END-IF
           STOP RUN.
