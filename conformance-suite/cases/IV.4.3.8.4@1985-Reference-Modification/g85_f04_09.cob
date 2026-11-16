      *>--- conformance-case
      *> id: g85_f04_09
      *> rules: [1985-SR-IV.4.3.8.4.3-4]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:IV.4.3.8.4.3 SR4 - "Data-name-1 may be qualified or
      *> subscripted." Each leg reference-modifies a qualified, a
      *> subscripted, or a qualified and subscripted data-name, as a sender
      *> and as a receiver (SR3: allowed wherever an alphanumeric identifier
      *> is). NM is declared in G1 ("ABCDEF") and G2 ("UVWXYZ"); CELL in G3
      *> (a two-row table "PQR","STU") and G4. Q1: NM OF G1 (2:3) is BCD.
      *> Q2: NM IN G2 (4:) is XYZ (length omitted: to the end). S1: TE (2)
      *> (3:2) is BB. QS: CELL OF G3 (2) (1:2) is ST. R1: MOVE to NM OF G2
      *> (1:2) gives **WXYZ. R2: MOVE to TE (3) (4:1) gives CCC#. A compiler
      *> that refused the qualifier or subscript before the modifier, or
      *> applied the modifier to the wrong item, fails a line.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85F0409.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  G1.
           05  NM  PIC X(6) VALUE "ABCDEF".
       01  G2.
           05  NM  PIC X(6) VALUE "UVWXYZ".
       01  TBV PIC X(12) VALUE "AAAABBBBCCCC".
       01  TB REDEFINES TBV.
           05  TE  PIC X(4) OCCURS 3 TIMES.
       01  G3V PIC X(6) VALUE "PQRSTU".
       01  G3 REDEFINES G3V.
           05  RW OCCURS 2 TIMES.
               10  CELL PIC X(3).
       01  G4.
           05  CELL PIC X(3) VALUE "ZZZ".
       PROCEDURE DIVISION.
       MAIN-PARA.
           DISPLAY "Q1 " NM OF G1 (2:3).
           DISPLAY "Q2 " NM IN G2 (4:).
           DISPLAY "S1 " TE (2) (3:2).
           DISPLAY "QS " CELL OF G3 (2) (1:2).
           MOVE "**" TO NM OF G2 (1:2).
           DISPLAY "R1 " NM OF G2.
           MOVE "#" TO TE (3) (4:1).
           DISPLAY "R2 " TE (3).
           STOP RUN.
