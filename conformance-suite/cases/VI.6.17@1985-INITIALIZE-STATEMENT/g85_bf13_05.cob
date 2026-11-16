      *>--- conformance-case
      *> id: g85_bf13_05
      *> rules: [1985-SR-VI.6.17.3-1, 1985-GR-VI.6.17.4-1, 1985-GR-VI.6.17.4-2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.6.17.4 GR1 - each REPLACING key word names a category
      *> of data: NUMERIC reaches only C-NUM and the table C-T, never the
      *> numeric edited C-NEDIT (line 1); ALPHANUMERIC reaches only C-ALNUM,
      *> not the alphabetic, alphanumeric edited or group items (line 2);
      *> ALPHABETIC, NUMERIC-EDITED and ALPHANUMERIC-EDITED each reach only
      *> their own item (line 3).
      *> §1985:VI.6.17.4 GR2 - each affected item receives an elementary
      *> MOVE from the sender, every occurrence of the table included
      *> (77 into PIC 9 truncates to 7 in each of C-T(1..3): [777]); an
      *> elementary identifier-1 of another category is left alone
      *> (line 4: C-NUM stays 77) and one of the named category is set
      *> (line 5: 05).
      *> §1985:VI.6.17.3 SR1 - identifier-2 and literal-1 are the
      *> sending area and identifier-1 the receiving area: SRC-N and
      *> SRC-X keep their values (line 6) while G2's items change.
      *> A leg fails if an item of another category is changed, if an
      *> occurrence is skipped or moved as a group, or if the sender is
      *> overwritten.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85BF1305.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  G2.
           05  C-ALPHA         PIC A(2)  VALUE "AB".
           05  C-ALNUM         PIC X(2)  VALUE "CD".
           05  C-NUM           PIC 9(2)  VALUE 12.
           05  C-NEDIT         PIC Z9.
           05  C-AEDIT         PIC X/X.
           05  C-TAB.
               10  C-T         PIC 9 OCCURS 3 TIMES.
       01  SRC-X               PIC X(2)  VALUE "ZZ".
       01  SRC-N               PIC 9(2)  VALUE 77.
       PROCEDURE DIVISION.
       P-MAIN.
           MOVE 5 TO C-NEDIT.
           MOVE "PQ" TO C-AEDIT.
           MOVE "123" TO C-TAB.
           INITIALIZE G2 REPLACING NUMERIC DATA BY SRC-N.
           DISPLAY "[" C-ALPHA "][" C-ALNUM "][" C-NUM "]["
               C-NEDIT "][" C-AEDIT "][" C-TAB "]".
           INITIALIZE G2 REPLACING ALPHANUMERIC BY SRC-X.
           DISPLAY "[" C-ALPHA "][" C-ALNUM "][" C-NUM "]["
               C-NEDIT "][" C-AEDIT "][" C-TAB "]".
           INITIALIZE G2 REPLACING ALPHABETIC DATA BY "XY"
               NUMERIC-EDITED BY 3
               ALPHANUMERIC-EDITED BY "MN".
           DISPLAY "[" C-ALPHA "][" C-ALNUM "][" C-NUM "]["
               C-NEDIT "][" C-AEDIT "][" C-TAB "]".
           INITIALIZE C-NUM REPLACING ALPHANUMERIC BY SRC-X.
           DISPLAY "[" C-NUM "]".
           INITIALIZE C-NUM REPLACING NUMERIC BY 5.
           DISPLAY "[" C-NUM "]".
           DISPLAY "[" SRC-N "][" SRC-X "]".
           STOP RUN.
