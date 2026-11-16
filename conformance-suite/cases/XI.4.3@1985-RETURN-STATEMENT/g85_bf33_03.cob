      *>--- conformance-case
      *> id: g85_bf33_03
      *> rules: [1985-GR-XI.4.3.4-3]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XI.4.3.4 GR3 - when no at end condition occurs, the
      *> record is made available and the INTO move is executed BEFORE
      *> control goes to imperative-statement-2 (leg A: the NOT AT END
      *> phrase already sees "A1" in both the receiver, which held "..",
      *> and the record area, §1985:XI.4.3.4 GR6); with no NOT AT END
      *> phrase control goes to the end of the RETURN statement (leg B:
      *> the AT END imperative does not run and the next statement sees
      *> "B2" moved INTO the receiver). Leg C is the at end control
      *> (§1985:XI.4.3.4 GR2): its NOT AT END phrase is ignored. A leg
      *> fails if the move follows imperative-statement-2 (A shows
      *> ".."), if the AT END imperative runs on success (A-ATEND or
      *> B-ATEND), or if control does not reach the end of RETURN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85BF3303.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SRT ASSIGN TO "bf3303s.tmp".
       DATA DIVISION.
       FILE SECTION.
       SD  SRT.
       01  SRT-REC.
           05  SRT-K PIC X.
           05  SRT-T PIC X.
       WORKING-STORAGE SECTION.
       01  W-X PIC X(2).
       PROCEDURE DIVISION.
       MAIN-PARA.
           SORT SRT ON ASCENDING KEY SRT-K
               INPUT PROCEDURE IS REL-P
               OUTPUT PROCEDURE IS RET-P.
           DISPLAY "DONE".
           STOP RUN.
       REL-P.
           MOVE "B2" TO SRT-REC.
           RELEASE SRT-REC.
           MOVE "A1" TO SRT-REC.
           RELEASE SRT-REC.
       RET-P.
           MOVE ".." TO W-X.
           RETURN SRT INTO W-X
               AT END DISPLAY "A-ATEND"
               NOT AT END DISPLAY "A-NAE[" W-X "][" SRT-REC "]"
           END-RETURN.
           DISPLAY "A-END".
           MOVE ".." TO W-X.
           RETURN SRT INTO W-X
               AT END DISPLAY "B-ATEND"
           END-RETURN.
           DISPLAY "B-END[" W-X "]".
           RETURN SRT
               AT END DISPLAY "C-ATEND"
               NOT AT END DISPLAY "C-NAE"
           END-RETURN.
           DISPLAY "C-END".
