*>--- conformance-case
*> id: pb1652_group_indicate_suppressed_generate
*> rules: [GR-13.18.28.4-1]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> A GENERATE THAT PRINTS NOTHING STILL CONSUMES ITS GROUP'S GROUP INDICATE CONDITION (kb/Work PB1652).
      *> ISO/IEC 1989:2023 §13.18.28.4 GR1: "The GROUP INDICATE clause has the same effect as a PRESENT
      *> WHEN clause where the associated condition is true only on the first occasion that a GENERATE is
      *> issued for the current detail group after any of the following events." a) an INITIATE, b) a
      *> page advance, c) a control break.
      *>   cite.py --check 13.18.28.4 "true only on the first occasion that a GENERATE is issued for the
      *>     current detail group"  -> OK  §13.18.28.4 1)  (General rule)
      *> The rule counts GENERATEs ISSUED for the group, not lines printed: a GENERATE whose group is
      *> inhibited by SUPPRESS (§14.9.45.4 GR3: the statement inhibits "b) Any page advance associated
      *> with the report group" and the printing and LINE-COUNTER changes) or whose every line is absent
      *> under PRESENT WHEN (§13.18.41.4 GR2 b: "the corresponding data item is declared to be absent and
      *> the effect on processing is as though the entry were omitted") is still a GENERATE issued for the
      *> group, so it is the "first occasion" and the next GENERATE of the same group finds the condition
      *> false.
      *>   cite.py --check 14.9.45.4 "Any page advance associated with the report group"  -> OK  §14.9.45.4 3) b)
      *>   cite.py --check 13.18.41.4 "the corresponding data item is declared to be absent and the effect
      *>     on processing is as though the entry were omitted"  -> OK  §13.18.41.4 2) b)
      *> §13.18.14.4 GR7: "If integer-2 is specified for an item that is the first printable item in the
      *> current line, it specifies the leftmost column of that item" -- so with the indicated item absent
      *> the relative item after it lands in column 2.
      *>   cite.py --check 13.18.14.4 "If integer-2 is specified for an item that is the first printable
      *>     item in the current line, it specifies the leftmost column of that item"  -> OK  8)
      *>
      *> WHY EACH LEG CAN FAIL. D1's first GENERATE is suppressed; if SUPPRESS left the condition armed, the
      *> second GENERATE would print the indicated item ("GI 2"). D2's first GENERATE finds its only LINE
      *> absent; if an all-absent GENERATE left it armed, the second would print "HI 5". Each detail group
      *> keeps its own condition (kb/Work PB1244), so D1's GENERATEs do not touch D2's.
      *>
      *> DERIVATION (PAGE LIMIT 20, FIRST DETAIL 1, LAST DETAIL 20; no control break and no page advance
      *> occurs after INITIATE, so event a) is the only arming; the report file is read back byte by byte,
      *> each line DISPLAYed between brackets; the first printed line is line 1 by §13.18.35.4 GR5 b 3):
      *>   INITIATE                      : a) arms D1 and D2
      *>   WN=1 GENERATE D1 (hidden)     : the USE BEFORE REPORTING procedure executes SUPPRESS PRINTING;
      *>                                   nothing prints; the GENERATE is the first occasion -> D1 consumed
      *>   WN=2 GENERATE D1              : not armed: "GI" absent, WN is the line's first printable item,
      *>                                   PLUS 2 is column 2                        => [ 2]
      *>   WN=3 GENERATE D1              : still not armed                         => [ 3]
      *>   WN=4 GENERATE D2 (LINE absent): WS-SHOW is 0, the LINE's PRESENT WHEN is false; nothing prints;
      *>                                   the GENERATE is the first occasion -> D2 consumed (D1's GENERATEs
      *>                                   did not read or consume it, but INITIATE armed it)
      *>   WN=5 GENERATE D2              : LINE present; not armed: "HI" absent    => [ 5]
      *>   WN=6 GENERATE D2              : still not armed                         => [ 6]
      *>   TERMINATE (no heading or footing groups, so nothing else prints).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1652GI.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRT ASSIGN TO "PB1652GI.RPT".
           SELECT CHK ASSIGN TO "PB1652GI.RPT".
       DATA DIVISION.
       FILE SECTION.
       FD  PRT REPORT IS R.
       FD  CHK.
       01  CHK-REC PIC X.
       WORKING-STORAGE SECTION.
       01  WN      PIC 9     VALUE 0.
       01  WS-HIDE PIC 9     VALUE 0.
       01  WS-SHOW PIC 9     VALUE 0.
       01  WS-EOF  PIC X     VALUE "N".
       01  WS-I    PIC 99    VALUE 0.
       01  WS-LINE PIC X(30) VALUE SPACES.
       REPORT SECTION.
       RD  R PAGE LIMIT IS 20 LINES FIRST DETAIL 1 LAST DETAIL 20.
       01  D1 TYPE DETAIL LINE PLUS 1.
           03  COLUMN 1 PIC X(2) VALUE "GI" GROUP INDICATE.
           03  COLUMN PLUS 2 PIC 9 SOURCE WN.
       01  D2 TYPE DETAIL.
           02  LINE PLUS 1 PRESENT WHEN WS-SHOW = 1.
               03  COLUMN 1 PIC X(2) VALUE "HI" GROUP INDICATE.
               03  COLUMN PLUS 2 PIC 9 SOURCE WN.
       PROCEDURE DIVISION.
       DECLARATIVES.
       SUP-SECTION SECTION.
           USE BEFORE REPORTING D1.
       SUP-PARA.
           IF WS-HIDE = 1
               SUPPRESS PRINTING
           END-IF.
       END DECLARATIVES.
       MAIN SECTION.
       MAIN-P.
           OPEN OUTPUT PRT.
           INITIATE R.
           MOVE 1 TO WN. MOVE 1 TO WS-HIDE. GENERATE D1.
           MOVE 2 TO WN. MOVE 0 TO WS-HIDE. GENERATE D1.
           MOVE 3 TO WN. GENERATE D1.
           MOVE 4 TO WN. GENERATE D2.
           MOVE 5 TO WN. MOVE 1 TO WS-SHOW. GENERATE D2.
           MOVE 6 TO WN. GENERATE D2.
           TERMINATE R.
           CLOSE PRT.
           OPEN INPUT CHK.
           PERFORM UNTIL WS-EOF = "Y"
               READ CHK
                   AT END MOVE "Y" TO WS-EOF
                   NOT AT END PERFORM TAKE-BYTE
               END-READ
           END-PERFORM.
           CLOSE CHK.
           PERFORM SHOW-LINE.
           STOP RUN.
       TAKE-BYTE.
           EVALUATE TRUE
               WHEN CHK-REC = X"0A"
                   PERFORM SHOW-LINE
               WHEN CHK-REC = X"0C"
                   PERFORM SHOW-LINE
                   DISPLAY "[PAGE]"
               WHEN CHK-REC = X"0D"
                   CONTINUE
               WHEN OTHER
                   ADD 1 TO WS-I
                   MOVE CHK-REC TO WS-LINE(WS-I:1)
           END-EVALUATE.
       SHOW-LINE.
           IF WS-I > 0
               DISPLAY "[" WS-LINE(1:WS-I) "]"
               MOVE SPACES TO WS-LINE
               MOVE 0 TO WS-I
           END-IF.
