      *>--- conformance-case
      *> id: pb3510_depending_counters_admitted
      *> rules: [SR-13.18.38.3-17, SR-13.18.38.3-20, GR-13.18.38.4-8]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB3510 - the OCCURS DEPENDING ON objects ISO 13.18.38.3
      *> ADMITS, beside the four negatives pb3510-depending-on-*. Each
      *> counter is an integer (SR17, "Data-name-1 shall describe an
      *> integer") lying before its table (SR20): N is a sibling of the group
      *> H that holds T, inside the record G; C is a record of its own. The
      *> compiler records only such a counter as the table's DEPENDING item,
      *> and every group operand below is sliced by it (13.18.38.4 GR8).
      *> Expected values: MOVE "ABCDE" TO H - N lies outside H, so GR8 a)
      *> uses N = 3 occurrences: T holds ABC. DISPLAY G - N lies inside G and
      *> G is a sending operand, so GR8 b) uses N = 3: [3ABC]. MOVE "XYZ" TO
      *> G2 - C = 2 (GR8 a)): [XY]. MOVE "4WXYZ" TO G - G is a receiving
      *> operand holding N, so GR8 b) uses its maximum length (6): N = 4 and
      *> T holds WXYZ and a space; DISPLAY G then sends N = 4: [4WXYZ].
      *> Fails if an admitted counter is refused, or a group is not sliced.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3510OK.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 G.
           05 N PIC 9 VALUE 3.
           05 H.
               10 T PIC X OCCURS 1 TO 5 TIMES DEPENDING ON N.
       01 C PIC 9 VALUE 2.
       01 G2.
           05 T2 PIC X OCCURS 1 TO 5 TIMES DEPENDING ON C.
       PROCEDURE DIVISION.
       MAIN-PARA.
           MOVE "ABCDE" TO H
           DISPLAY "[" G "]"
           MOVE "XYZ" TO G2
           DISPLAY "[" G2 "]"
           MOVE "4WXYZ" TO G
           DISPLAY "[" G "]"
           STOP RUN.
