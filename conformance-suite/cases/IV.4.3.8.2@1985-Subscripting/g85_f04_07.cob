      *>--- conformance-case
      *> id: g85_f04_07
      *> rules: [1985-GR-IV.4.3.8.2.4-3]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:IV.4.3.8.2.4 GR3 - "The value of the index referenced by
      *> index-name-1 must be initialized before it is used as a subscript.
      *> An index may be given an initial value by either a PERFORM
      *> statement with the VARYING phrase, a SEARCH statement with the ALL
      *> phrase, or a SET statement. An index may be modified only by the
      *> PERFORM, SEARCH, and SET statements." The table holds A..E. Every
      *> use of IX as a subscript follows one of the three initializers.
      *> S: SET IX TO 3 gives C; SET IX UP BY 1 (a modification by SET)
      *> gives D. P: PERFORM VARYING IX FROM 1 BY 1 UNTIL IX > 5 visits
      *> ABCDE. A: SEARCH ALL for key B leaves IX at occurrence 2, shown
      *> through SET N TO IX (02). Q: a serial SEARCH from SET IX TO 1
      *> modifies IX until key E matches (05). A compiler that did not
      *> store into the index from any of the three fails a line.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85F0407.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  TV  PIC X(5) VALUE "ABCDE".
       01  T REDEFINES TV.
           05  E OCCURS 5 TIMES ASCENDING KEY IS EK INDEXED BY IX.
               10  EK  PIC X.
       01  BUF PIC X(5) VALUE SPACES.
       01  N   PIC 99 VALUE 0.
       PROCEDURE DIVISION.
       MAIN-PARA.
           SET IX TO 3.
           DISPLAY "S " EK (IX).
           SET IX UP BY 1.
           DISPLAY "S " EK (IX).
           MOVE 0 TO N.
           PERFORM VARYING IX FROM 1 BY 1 UNTIL IX > 5
               ADD 1 TO N
               MOVE EK (IX) TO BUF (N:1)
           END-PERFORM.
           DISPLAY "P " BUF.
           SEARCH ALL E
               AT END DISPLAY "A NOT FOUND"
               WHEN EK (IX) = "B"
                   SET N TO IX
                   DISPLAY "A " EK (IX) " " N
           END-SEARCH.
           SET IX TO 1.
           SEARCH E
               AT END DISPLAY "Q NOT FOUND"
               WHEN EK (IX) = "E"
                   SET N TO IX
                   DISPLAY "Q " EK (IX) " " N
           END-SEARCH.
           STOP RUN.
