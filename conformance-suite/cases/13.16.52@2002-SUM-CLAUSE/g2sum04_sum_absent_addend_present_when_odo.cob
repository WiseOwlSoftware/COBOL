      *>--- conformance-case
      *> id: g2sum04_sum_absent_addend_present_when_odo
      *> rules: [2002-GR-13.16.52.3-11, 2002-GR-13.16.52.3-8]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:13.16.52.3 GR11 - "If the operand is data-name-1 and is
      *>   declared to be absent as a result of a PRESENT WHEN clause or
      *>   an OCCURS clause with the DEPENDING phrase, data-name-1 is not
      *>   added into the sum counter during the processing that
      *>   instance of the report group in which data-name-1 is defined."
      *>   BOTH branches: DP (PRESENT WHEN) and DX (OCCURS DEPENDING).
      *> §2002:13.16.52.3 GR8a - "each occurrence of the addend is added
      *>   into the corresponding occurrence of the sum counter" (DX and
      *>   TX are each subject to one level of repetition, SR3 c), d)).
      *> §2002:13.16.39.3 GR2b - "If condition-1 is false, the
      *>   corresponding data item is declared to be absent".
      *> §2002:13.16.36.3 GR13 - with data-name-1 in integer-1 to
      *>   (integer-2 - 1) "the OCCURS clause has the same effect as an
      *>   OCCURS clause with no TO or DEPENDING phrases and with an
      *>   integer-2 equal to the current value of data-name-1"; outside
      *>   that range, as though written without TO and DEPENDING.
      *> §2002:13.16.52.3 GR7a - "adding takes place when the report
      *>   group description containing data-name-1 is processed".
      *>
      *> DERIVATION. Three GENERATE DET, then TERMINATE (FINAL footing):
      *>  (AMT,N) = (2,3): N=3 is outside 1..2, so 3 occurrences; DP
      *>   absent (2 > 5 false). Line: DA 02, DX 02 02 02.
      *>  (7,1): DP present; one DX occurrence.  Line: 07 07 07
      *>  (4,2): DP absent; two DX occurrences.  Line: 04, DX 04 04
      *>  TA = 2+7+4 = 013 (no absence; the control leg).
      *>  TP = 7 = 007: GR11, DP not added when absent (13 if it were).
      *>  TX(1) = 2+7+4 = 013, TX(2) = 2+4 = 006, TX(3) = 2 = 002:
      *>   GR8 a) per occurrence, GR11 an absent occurrence adds nothing
      *>   (an implementation summing every declared occurrence answers
      *>   013 013 013).
      *>  Columns: DET DA 1, DP 4, DX 7 STEP 3; CF TA 1, TP 5, TX 9 STEP 4.
      *> cite.py --check: OK §2002:13.16.52.3 GR7, GR8, GR11;
      *>   §2002:13.16.52.2 SR3; §2002:13.16.39.3 GR2;
      *>   §2002:13.16.36.3 GR13.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2SUM06.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRT ASSIGN TO "g2sum04.txt".
           SELECT CHK ASSIGN TO "g2sum04.txt".
       DATA DIVISION.
       FILE SECTION.
       FD  PRT REPORT IS R.
       FD  CHK.
       01  CHK-REC PIC X.
       WORKING-STORAGE SECTION.
       01  WS-AMT  PIC 99    VALUE 0.
       01  WS-N    PIC 9     VALUE 0.
       01  WS-EOF  PIC X     VALUE "N".
       01  WS-BYTE PIC X.
       01  WS-I    PIC 99    VALUE 0.
       01  WS-LN   PIC 99    VALUE 0.
       01  WS-LINE PIC X(30) VALUE SPACES.
       REPORT SECTION.
       RD  R CONTROLS ARE FINAL.
       01  DET TYPE DE LINE PLUS 1.
           05  DA COLUMN 1 PIC 99 SOURCE WS-AMT.
           05  DP COLUMN 4 PIC 99 SOURCE WS-AMT
                   PRESENT WHEN WS-AMT > 5.
           05  DX COLUMN 7 PIC 99 SOURCE WS-AMT
                   OCCURS 1 TO 3 DEPENDING ON WS-N STEP 3.
       01  CFF TYPE CF FINAL LINE PLUS 1.
           05  TA COLUMN 1 PIC 999 SUM DA.
           05  TP COLUMN 5 PIC 999 SUM DP.
           05  TX COLUMN 9 PIC 999 SUM DX OCCURS 3 STEP 4.
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT PRT.
           INITIATE R.
           MOVE 2 TO WS-AMT. MOVE 3 TO WS-N.
           GENERATE DET.
           MOVE 7 TO WS-AMT. MOVE 1 TO WS-N.
           GENERATE DET.
           MOVE 4 TO WS-AMT. MOVE 2 TO WS-N.
           GENERATE DET.
           TERMINATE R.
           CLOSE PRT.
           OPEN INPUT CHK.
           PERFORM UNTIL WS-EOF = "Y"
               READ CHK
                   AT END MOVE "Y" TO WS-EOF
                   NOT AT END
                       MOVE CHK-REC TO WS-BYTE
                       PERFORM TAKE-BYTE
               END-READ
           END-PERFORM.
           CLOSE CHK.
           IF WS-I > 0
               PERFORM SHOW-LINE
           END-IF.
           STOP RUN.
       TAKE-BYTE.
           EVALUATE TRUE
               WHEN WS-BYTE = X"0A"
                   PERFORM SHOW-LINE
               WHEN WS-BYTE = X"0C"
                   IF WS-I > 0
                       PERFORM SHOW-LINE
                   END-IF
               WHEN WS-BYTE = X"0D"
                   CONTINUE
               WHEN OTHER
                   ADD 1 TO WS-I
                   MOVE WS-BYTE TO WS-LINE(WS-I:1)
           END-EVALUATE.
       SHOW-LINE.
           ADD 1 TO WS-LN.
           DISPLAY "LINE " WS-LN " [" WS-LINE(1:20) "]".
           MOVE SPACES TO WS-LINE.
           MOVE 0 TO WS-I.
