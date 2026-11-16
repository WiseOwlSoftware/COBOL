      *>--- conformance-case
      *> id: g2-value-ws-float-table
      *> rules: [2002-GR-13.16.61.3-1, 2002-SR-13.16.61.2-17]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> Working-storage legs of two 2002 rules the 2023 twin reaches
      *> only through
      *> cross-reference rules.
      *> FS FX  §2002:13.16.61.3 GR1 format 1 on FLOAT-SHORT and FLOAT-
      *> EXTENDED: 0.25
      *>     and 125 are exact in radix 2 and 10; COMPUTE ROUNDED into
      *> ZZ9.99 gives
      *>     25.00 (FS*100) and 125.00 (FX) for any approximation
      *> within 0.00005.
      *> FT  GR1 format 2 on a FLOAT-LONG table: 1.5E0 2.5E0 FROM (1)
      *> TO (3) reuses the
      *>     literals (GR12, GR13): 1.5 2.5 1.5, times 10 = 15.00 25.00
      *> 15.00.
      *> FR  GR1 format 3: 88 FR-RANGE VALUE 1.0E0 THRU 2.0E0 on a
      *> FLOAT-LONG item whose
      *>     VALUE is 1.5E0 is true: RANGE.
      *> TE C  §2002:13.16.61.2 SR17 accepted form at 2002 (the 2023
      *> twin's case
      *>     pb505_table_value_geometry does not run at 2002): "P" FROM
      *> (1) TO (2) "Q"
      *>     FROM (3) gives [PPQ]; in two dimensions FROM (1 2) TO (2
      *> 1) is two
      *>     successive elements and FROM (3 1) TO (3 2) two more:
      *> [AABB].
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2VAL02.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  FS USAGE FLOAT-SHORT VALUE 2.5E-1.
       01  FX USAGE FLOAT-EXTENDED VALUE 1.25E2.
       01  FT.
           05 FTE USAGE FLOAT-LONG OCCURS 3
              VALUES ARE 1.5E0 2.5E0 FROM (1) TO (3).
       01  FR USAGE FLOAT-LONG VALUE 1.5E0.
           88 FR-RANGE VALUE 1.0E0 THRU 2.0E0.
       01  T.
           05 TE PIC X OCCURS 3 VALUE "P" FROM (1) TO (2) "Q" FROM (3).
       01  T2.
           05 R OCCURS 3.
              10 C PIC X OCCURS 2 VALUE "A" FROM (1 2) TO (2 1)
                 "B" FROM (3 1) TO (3 2).
       01  D PIC ZZ9.99.
       PROCEDURE DIVISION.
       MAIN-P.
           COMPUTE D ROUNDED = FS * 100
           DISPLAY "FS=[" D "]"
           COMPUTE D ROUNDED = FX
           DISPLAY "FX=[" D "]"
           COMPUTE D ROUNDED = FTE (1) * 10
           DISPLAY "FT1=[" D "]"
           COMPUTE D ROUNDED = FTE (2) * 10
           DISPLAY "FT2=[" D "]"
           COMPUTE D ROUNDED = FTE (3) * 10
           DISPLAY "FT3=[" D "]"
           IF FR-RANGE
               DISPLAY "FR=RANGE"
           ELSE
               DISPLAY "FR=NOT-RANGE"
           END-IF
           DISPLAY "TE=[" T "]"
           DISPLAY "C=[" C (1 2) C (2 1) C (3 1) C (3 2) "]"
           STOP RUN.
