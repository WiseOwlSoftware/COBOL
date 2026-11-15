      *>--- conformance-case
      *> id: pb5124_release_outside_record_range_85
      *> rules: [1985-GR-VII.3.8.4-10]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5124. §1985:VII.3.8.4 GR10 sizes a RELEASE's record
      *>   from data-name-1 (a.), and (Amendment 2) "If the number of
      *>   character positions in the logical record to be written is
      *>   less than integer-2 or greater than integer-3, the output
      *>   statement is unsuccessful and, except during execution of a
      *>   RELEASE statement, the associated I-O status is set". An
      *>   SD's RECORD clause is this clause. An unsuccessful statement
      *>   "does not affect any data referenced by that statement"
      *>   (§1985:III.2, Unsuccessful Execution), and GR9: a RELEASE
      *>   "does not alter the content of the data item referenced by
      *>   data-name-1". So a record outside the range is not
      *>   released (it never reaches RETURN), SLEN keeps its value
      *>   and the record area keeps its content. RELEASE FROM is a
      *>   MOVE and then the RELEASE (§1985:XI.4.2.4 GR3), so the MOVE
      *>   stands. Only data-name-1 can name a size outside the range:
      *>   §1985:VII.3.8.3 SR2 keeps every record description inside
      *>   it, so arms b. and c. cannot leave it.
      *> Legs, and how each fails: 8 (the largest) and 3 (the
      *>   smallest, by FROM) are released; 2 (short), 9 (long), 0
      *>   (zero) and 12 (long, by FROM) are not. Fails if AAA, DDD,
      *>   BBB or FFF is RETURNed (the pre-PB5124 defect), or if a
      *>   DISPLAY shows SLEN or SR changed by a failed RELEASE.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5124RR.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT S1 ASSIGN TO "PB5124S1.TMP".
       DATA DIVISION.
       FILE SECTION.
       SD  S1
           RECORD IS VARYING IN SIZE FROM 3 TO 8 CHARACTERS
               DEPENDING ON SLEN.
       01  SR.
           05  SK PIC X(3).
           05  FILLER PIC X(5).
       WORKING-STORAGE SECTION.
       01  SLEN PIC 99 VALUE 0.
       01  WREC PIC X(8).
       01  EOF-SW PIC X VALUE "N".
       PROCEDURE DIVISION.
       MAIN-SEC SECTION.
       MAIN-PARA.
           SORT S1 ASCENDING KEY SK
               INPUT PROCEDURE IS IN1-SEC
               OUTPUT PROCEDURE IS OUT1-SEC.
           DISPLAY "DONE".
           STOP RUN.
       IN1-SEC SECTION.
       IN1-PARA.
           MOVE "EEE12345" TO SR.
           MOVE 8 TO SLEN.
           RELEASE SR.
           MOVE "AAA12345" TO SR.
           MOVE 2 TO SLEN.
           RELEASE SR.
           DISPLAY "SHORT " SLEN " " SR.
           MOVE "DDD12345" TO SR.
           MOVE 9 TO SLEN.
           RELEASE SR.
           DISPLAY "LONG " SLEN " " SR.
           MOVE "BBB12345" TO SR.
           MOVE 0 TO SLEN.
           RELEASE SR.
           DISPLAY "ZERO " SLEN " " SR.
           MOVE "CCC99999" TO WREC.
           MOVE 3 TO SLEN.
           RELEASE SR FROM WREC.
           MOVE "FFF77777" TO WREC.
           MOVE 12 TO SLEN.
           RELEASE SR FROM WREC.
           DISPLAY "FROM " SLEN " " SR.
       OUT1-SEC SECTION.
       OUT1-PARA.
           PERFORM RET1-PARA UNTIL EOF-SW = "Y".
           GO TO OUT1-EXIT.
       RET1-PARA.
           RETURN S1
               AT END MOVE "Y" TO EOF-SW
           END-RETURN.
           IF EOF-SW = "Y"
               DISPLAY "S1 AT END"
           ELSE
               DISPLAY "RETURN " SLEN " " SR (1:SLEN).
       OUT1-EXIT.
           EXIT.
