      *>--- conformance-case
      *> id: g2-value-ext-based-initialize
      *> rules: [2002-GR-13.16.61.3-4]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:13.16.61.3 GR4 legs a) and b), showing ONLY content the
      *> 2002 text defines. GR4 closes: "the values of other data items
      *> and indexes are undefined", so nothing is displayed before a
      *> VALUE clause takes effect, and an occurrence the format 2
      *> clause gives no literal (XE(3), BE(3)) is not displayed after
      *> an INITIALIZE ... ALL TO VALUE.
      *> X  GR4 a) - an external item takes its VALUE "during the
      *>    execution of an INITIALIZE statement": after INITIALIZE X
      *>    ALL TO VALUE, X(1:4) is [AABB] (FROM (1) TO (2), GR13).
      *> B  GR4 b) - a based item takes its VALUE "during the execution
      *>    of an ALLOCATE or an explicit or implicit INITIALIZE".
      *>    ALLOCATE B INITIALIZED acts as INITIALIZE B WITH FILLER ALL
      *>    TO VALUE THEN TO DEFAULT (§2002:14.8.3.3 GR7), so all of B
      *>    is defined: BE(3) takes the alphanumeric default, spaces,
      *>    and B is [AABB  ]. Then MOVE ALL "." and an explicit
      *>    INITIALIZE B ALL TO VALUE restore B(1:4) to [AABB].
      *> Fails if a VALUE does not take effect on either occasion.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2VQ03.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 X EXTERNAL.
          05 XE PIC X(2) OCCURS 3 VALUES ARE "AA" "BB" FROM (1) TO (2).
       01 PTR USAGE POINTER.
       LINKAGE SECTION.
       01 B BASED.
          05 BE PIC X(2) OCCURS 3 VALUES ARE "AA" "BB" FROM (1) TO (2).
       PROCEDURE DIVISION.
           INITIALIZE X ALL TO VALUE
           DISPLAY "X-INIT=[" X(1:4) "]"
           ALLOCATE B INITIALIZED RETURNING PTR
           DISPLAY "B-ALLOC=[" B "]"
           MOVE ALL "." TO B
           INITIALIZE B ALL TO VALUE
           DISPLAY "B-INIT=[" B(1:4) "]"
           FREE PTR
           STOP RUN.
