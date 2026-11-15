      *>--- conformance-case
      *> id: pb5386_perform_varying_identified_as_set
      *> rules: [GR-14.9.28.4-12]
      *> accept: [85, 2002, 2014, 2023]
      *> source-format: fixed
      *>---
      *> kb/Work PB5386. "Item identification for identifier-2 ... is
      *> done each time the content of the data item referenced by the
      *> identifier ... is set or augmented" (ISO 14.9.28.4 GR12). I
      *> is in a record a REDEFINES shares, the subscript form that
      *> was evaluated once before the statement ran (every operation
      *> went to WE (1), WT[300]). The induction variable WE (I) is
      *> set as WE (1), and the body advances I before each augment,
      *> so the augments reach WE (2) and WE (3): each becomes 0 + 1.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5386PV.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  A.
           05 I PIC 9 VALUE 1.
       01  B REDEFINES A PIC X.
       01  N PIC 9 VALUE 0.
       01  WT.
           05 WE PIC 9 OCCURS 3 VALUE 0.
       PROCEDURE DIVISION.
       P1.
           PERFORM P2 VARYING WE (I) FROM 1 BY 1 UNTIL N = 2.
           DISPLAY "WT[" WT "] I=" A.
           STOP RUN.
       P2.
           ADD 1 TO N.
           ADD 1 TO I.
