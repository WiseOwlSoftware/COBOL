      *>--- conformance-case
      *> id: g2_if_statement_operands_and_nesting
      *> rules: [2002-SR-14.8.18.2-1, 2002-SR-14.8.18.2-2]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:14.8.18.2 SR1 - "Statement-1 and statement-2 represent
      *>   either one or more imperative statements or a conditional
      *>   statement optionally preceded by one or more imperative
      *>   statements." The 2002 sets include CALL ... ON OVERFLOW
      *>   (conditional; §2002:14.8.4.3 GR3g1 transfers to its
      *>   imperative statement when the program cannot be made
      *>   available), RECEIVE ... NO DATA (conditional) and ENABLE,
      *>   SEND, DISABLE, PURGE (imperative) of the communication
      *>   facility (Annex A; obsolete, COBOL0903 warns). The ELSE ends
      *>   an unterminated conditional statement-1 (§2002:14.4.2.2).
      *> §2002:14.8.18.2 SR2 - "Any ELSE encountered is matched with the
      *>   nearest preceding IF that either has not been already matched
      *>   with an ELSE or has not been implicitly or explicitly
      *>   terminated. Any END-IF encountered is matched with the
      *>   nearest preceding IF that has not been implicitly or
      *>   explicitly terminated."
      *> DERIVATION (SR1, lines C1-C3).
      *>   C1-PRE, C1-OVF, C1-END - statement-1 is DISPLAY then the
      *>     conditional CALL; G2IFNONE is no program, so ON OVERFLOW
      *>     runs; the ELSE ends the CALL and is not taken.
      *>   C2-ELSE - statement-1 is ENABLE then a conditional RECEIVE
      *>     ended by ELSE; the condition is false so statement-2 runs.
      *>   C3-THEN - statement-2 is three communication imperatives; the
      *>     condition is true so they are not executed.
      *>   C4-PRE, C4-OVF, C4-END - statement-2 (the ELSE position) is
      *>     DISPLAY then a conditional CALL; WS-X is "N" so statement-2
      *>     runs; G2IFNONE cannot be located (§2002:14.8.4.3 GR3), so
      *>     ON OVERFLOW runs (GR3g1); END-IF ends the CALL
      *>     (§2002:14.4.2.2).
      *> DERIVATION (SR2, lines S1-S4; each shape has two candidate
      *>   matchings that print differently).
      *>   S1-INNER-ELSE - the dangling ELSE binds to the inner IF.
      *>   S2-OUTER-TAIL - END-IF ends the inner IF, so the ELSE binds
      *>     to the outer (true) IF.
      *>   S3-OUT-E - the inner IF was matched with an ELSE and ended,
      *>     so the second ELSE binds to the outer (false) IF.
      *>   S4-L3-E, S4-L2-TAIL - three levels, each ELSE and END-IF to
      *>     its own level.
      *>   S5-E-IN-T, S5-E-TAIL - SR2 "Statement-1 and statement-2 may
      *>     each contain an IF statement": the inner IF is in
      *>     statement-2.
      *>     The second ELSE matches the inner IF, the first END-IF the
      *>     inner IF and the second END-IF the outer IF; W-A=0, W-B=1.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2IF01.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-X  PIC X VALUE "N".
       01 W-MSG PIC X(10).
       01 W-A   PIC 9 VALUE 0.
       01 W-B   PIC 9 VALUE 0.
       01 W-C   PIC 9 VALUE 0.
       COMMUNICATION SECTION.
       CD CDIN FOR INPUT
           SYMBOLIC QUEUE IS SQ
           TEXT LENGTH IS TL.
       01 INREC PIC X(87).
       CD CDOUT FOR OUTPUT
           TEXT LENGTH IS OTL
           DESTINATION COUNT IS DC
           STATUS KEY IS OSK.
       PROCEDURE DIVISION.
       MAIN-P.
           IF WS-X = "N"
               DISPLAY "C1-PRE"
               CALL "G2IFNONE" ON OVERFLOW DISPLAY "C1-OVF"
           ELSE
               DISPLAY "C1-ELSE"
           END-IF.
           DISPLAY "C1-END".
           IF WS-X = "Y"
               ENABLE INPUT CDIN
               RECEIVE CDIN MESSAGE INTO W-MSG
                   NO DATA DISPLAY "C2-NODATA"
           ELSE
               DISPLAY "C2-ELSE"
           END-IF.
           IF WS-X = "N"
               DISPLAY "C3-THEN"
           ELSE
               SEND CDOUT FROM W-MSG WITH EMI
               DISABLE INPUT CDIN
               PURGE CDOUT
           END-IF.
           IF WS-X = "Y"
               DISPLAY "C4-THEN"
           ELSE
               DISPLAY "C4-PRE"
               CALL "G2IFNONE" ON OVERFLOW DISPLAY "C4-OVF"
           END-IF.
           DISPLAY "C4-END".
           MOVE 1 TO W-A.
           MOVE 0 TO W-B.
           IF W-A = 1
               IF W-B = 1
                   DISPLAY "S1-INNER-THEN"
               ELSE
                   DISPLAY "S1-INNER-ELSE".
           MOVE 1 TO W-A.
           MOVE 0 TO W-B.
           IF W-A = 1
               IF W-B = 1
                   DISPLAY "S2-INNER-THEN"
               END-IF
               DISPLAY "S2-OUTER-TAIL"
           ELSE
               DISPLAY "S2-OUTER-ELSE"
           END-IF.
           MOVE 0 TO W-A.
           MOVE 1 TO W-B.
           IF W-A = 1
               IF W-B = 1
                   DISPLAY "S3-IN-T"
               ELSE
                   DISPLAY "S3-IN-E"
               END-IF
           ELSE
               DISPLAY "S3-OUT-E"
           END-IF.
           MOVE 1 TO W-A.
           MOVE 1 TO W-B.
           MOVE 0 TO W-C.
           IF W-A = 1
               IF W-B = 1
                   IF W-C = 1
                       DISPLAY "S4-L3-T"
                   ELSE
                       DISPLAY "S4-L3-E"
                   END-IF
                   DISPLAY "S4-L2-TAIL"
               ELSE
                   DISPLAY "S4-L2-E"
               END-IF
           ELSE
               DISPLAY "S4-L1-E"
           END-IF.
           MOVE 0 TO W-A.
           MOVE 1 TO W-B.
           IF W-A = 1
               DISPLAY "S5-T"
           ELSE
               IF W-B = 1
                   DISPLAY "S5-E-IN-T"
               ELSE
                   DISPLAY "S5-E-IN-E"
               END-IF
               DISPLAY "S5-E-TAIL"
           END-IF.
           DISPLAY "DONE".
           STOP RUN.
       END PROGRAM G2IF01.
