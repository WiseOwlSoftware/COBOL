      *>--- conformance-case
      *> id: g85_x36
      *> rules: [1985-GR-IV.6.4.2.1.1-1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:IV.6.4.2.1.1 GR1 (1985-GR-IV.6.4.2.1.1-1). An output
      *> test, the accepted half of the row: each listed statement
      *> (EVALUATE, IF, SEARCH, RETURN), delimited by its explicit
      *> scope terminator, is an imperative statement
      *> (§1985:IV.6.4.2.3.1) and stands as imperative-statement-1 of
      *> an inline PERFORM. Derived: N = 2 selects WHEN 2 and the IF's
      *> true branch; TE (2) = Y is found; RETURN delivers AAAA, BBBB,
      *> then takes AT END. The refusal cases g85_x04 to g85_x07 are
      *> the other half.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85X36.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT WK ASSIGN TO "G85X36W".
       DATA DIVISION.
       FILE SECTION.
       SD  WK.
       01  WREC PIC X(4).
       WORKING-STORAGE SECTION.
       01  N PIC 9 VALUE 2.
       01  W PIC X(4).
       01  TBL.
           05  TE PIC X OCCURS 3 TIMES INDEXED BY IX.
       PROCEDURE DIVISION.
       MAIN-SEC SECTION.
       P0.
           MOVE "XYZ" TO TBL.
           PERFORM
               EVALUATE N
                   WHEN 1 DISPLAY "EVALUATE 1"
                   WHEN 2 DISPLAY "EVALUATE 2"
               END-EVALUATE
               IF N = 2 DISPLAY "IF TRUE" ELSE DISPLAY "IF FALSE"
               END-IF
               SET IX TO 1
               SEARCH TE AT END DISPLAY "SEARCH AT END"
                   WHEN TE (IX) = "Y" DISPLAY "SEARCH FOUND Y"
               END-SEARCH
           END-PERFORM.
           SORT WK ON ASCENDING KEY WREC
               INPUT PROCEDURE IS IN-SEC
               OUTPUT PROCEDURE IS OUT-SEC.
           DISPLAY "END".
           STOP RUN.
       IN-SEC SECTION.
       I0.
           MOVE "BBBB" TO WREC.
           RELEASE WREC.
           MOVE "AAAA" TO WREC.
           RELEASE WREC.
       OUT-SEC SECTION.
       O0.
           PERFORM 3 TIMES
               RETURN WK INTO W AT END MOVE "*EOF" TO W
               END-RETURN
               DISPLAY "RETURN " W
           END-PERFORM.
