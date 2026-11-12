      *>--- conformance-case
      *> id: pb5119_odo_object_changed_by_statement
      *> rules: [GR-14.6.4-6, GR-14.9.25.4-1, GR-14.9.43.4-8, GR-14.9.48.4-11, GR-14.9.37.4-4]
      *> accept: [85, 2002, 2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5119 review R1-1, the sibling sweep - a statement that stores into the OCCURS DEPENDING ON object of
      *> one of its own operands.  Every answer below is the same at every edition.
      *>
      *> §2023:14.6.4 (checked with scripts/spec/cite.py --check): item identification, step 6 "length evaluation for an
      *> occurs-depending group item", is done "as the first operation of the execution of that statement" unless
      *> otherwise specified; §2023:13.18.38.4 GR8 a (checked) uses data-name-1's value "at the start of the operation",
      *> as §1985:VI.5.8.4 GR3 a (checked) does at COBOL-85.
      *>
      *> L1 STRING "XYZ" INTO G WITH POINTER N, N = 1: G is identified at length 1 (§2023:14.9.43.4 GR8, checked: a
      *>    pointer that "exceeds the number of character positions in the data item referenced by identifier-3" stops
      *>    the transfer).  "X" moves to position 1, N becomes 2, 2 exceeds 1: overflow.  N 2, record HX.....
      *> L2 UNSTRING G DELIMITED "," INTO N W, N = 6, G = "2,XYZW": G is examined at the length it was identified with
      *>    (§2023:14.9.48.4 GR11, checked), so storing 2 into N does not shorten it: N 2, W XYZW.
      *> L3 MOVE G TO N W, N = 6, G = "2ABCDE": "The length of the data item referenced by identifier-1 is evaluated only
      *>    once, immediately before the data is moved to the first of the receiving operands" (§2023:14.9.25.4 GR1,
      *>    checked; §1985:VI.6.19.4 GR2, checked, says the same): N 2, W 2ABCDE.
      *> L4 MOVE S TO N G, S = "000003": "Item identification for identifier-2 is performed immediately before the data is
      *>    moved to the respective data item" (§2023:14.9.25.4 GR1; §1985:VI.6.19.4 GR2: "Any length evaluation ... associated
      *>    with identifier-2 is evaluated immediately before the data is moved to the respective data item"), so G receives
      *>    at N = 3: "000" and the rest unchanged: H000...
      *> L5 SEARCH TE VARYING N, N = 2, the search index at 1: N is incremented with the index (§2023:14.9.37.4 GR3 b 2,
      *>    checked), and each new index setting is tested against "the permissible range of occurrence values"
      *>    (§2023:14.9.37.4 GR4; §1985:VI.6.22.4 GR2 b, both checked), the current value of data-name-1
      *>    (§2023:13.18.38.4 GR7, checked).  TE is a table element, not an occurs-depending group, so step 6 of 14.6.4
      *>    does not fix it.  Index 5 is within N = 6 when "E" is tested: FOUND N 6.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5119SIBS.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  N   PIC 9.
       01  W   PIC X(6).
       01  S   PIC X(6).
       01  R.
           05  H  PIC X.
           05  G.
               10  E  PIC X OCCURS 1 TO 6 TIMES DEPENDING ON N.
       01  T.
           05  TE PIC X OCCURS 1 TO 6 TIMES DEPENDING ON N
                  INDEXED BY IX.
       PROCEDURE DIVISION.
       MAIN-PARA.
           MOVE 6 TO N. MOVE "H......" TO R. MOVE 1 TO N.
           STRING "XYZ" DELIMITED SIZE INTO G WITH POINTER N
               ON OVERFLOW DISPLAY "L1 OVERFLOW"
           END-STRING.
           DISPLAY "L1 " N.
           MOVE 6 TO N. DISPLAY "L1 " R.
           MOVE "H2,XYZW" TO R. MOVE SPACES TO W.
           UNSTRING G DELIMITED "," INTO N W.
           DISPLAY "L2 " N " " W.
           MOVE 6 TO N. MOVE "H2ABCDE" TO R.
           MOVE G TO N W.
           DISPLAY "L3 " N " " W.
           MOVE 6 TO N. MOVE "H......" TO R. MOVE "000003" TO S.
           MOVE S TO N G.
           MOVE 6 TO N. DISPLAY "L4 " R.
           MOVE "ABCDEF" TO T. MOVE 2 TO N.
           SET IX TO 1.
           SEARCH TE VARYING N
               AT END DISPLAY "L5 END N " N
               WHEN TE (IX) = "E" DISPLAY "L5 FOUND N " N
           END-SEARCH.
           STOP RUN.
