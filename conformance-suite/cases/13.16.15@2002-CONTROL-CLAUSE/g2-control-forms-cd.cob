      *>--- conformance-case
      *> id: g2-control-forms-cd
      *> rules: [2002-FMT-13.16.15.1, 2002-SR-13.16.15.2-3,
      *>    2002-SR-13.16.15.2-4, 2002-GR-13.16.15.3-4]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:13.16.15.1 - the CONTROL clause format (IS and ARE
      *>   are optional words, the 2002 page 277 diagram): R-A writes
      *>   CONTROL {data-name-1}... (IS omitted), R-B CONTROLS ARE
      *>   FINAL [data-name-1]..., R-C CONTROLS data-name-1 (ARE
      *>   omitted). CONTROL IS FINAL alone is g2sum01 (13.16.52).
      *>   cite.py: OK §2002:13.16.15.2 SR1 "CONTROL and CONTROLS are
      *>   synonyms."
      *> §2002:13.16.15.2 SR3 - "Data-name-1 may be qualified." (cite.py
      *>   OK): R-A names K OF REC-A and K OF REC-B, two items both
      *>   called K; R-B names CD-K OF CDIN-REC, an item of a record
      *>   under a CD (only the report section is excluded, SR2).
      *> §2002:13.16.15.2 SR4 - "Data-name-1 may be reference modified.
      *>   If it is, leftmost-position and length shall be integer
      *>   literals." (cite.py OK): K OF REC-B (1:2), CD-K OF
      *>   CDIN-REC (1:2). The control data item is the two leftmost
      *>   characters only.
      *> §2002:13.16.15.3 GR4a - "the corresponding prior controls are
      *>   stored in the control data items before any control footing
      *>   is printed" and then "the control data items are restored
      *>   from the prior controls to their new current values"
      *>   (cite.py OK); GR4b - "If no control footing is defined for
      *>   the report, the new current values of the control data
      *>   items are stored in the corresponding prior controls"
      *>   (cite.py OK). GR3 saves the priors at the first GENERATE
      *>   and compares major to minor; GR5 stores the priors before
      *>   each footing at TERMINATE (cite.py OK). Footings print
      *>   lowest level up (§2002:13.16.55.3 GR6e, cite.py OK);
      *>   control headings print at the first GENERATE and before
      *>   the detail of a break (§2002:13.16.55.3 GR6c, cite.py OK).
      *> The communication facility is obsolete at 2002 (COBOL0903
      *> warning), which does not fail the case.
      *>
      *> DERIVATION (lines A:, B:, C: are the non-blank report lines).
      *> R-A: major K OF REC-A, minor K OF REC-B (1:2).
      *>  G1 AAAA XX11  priors AAAA, XX             -> AAAA XX11
      *>  G2 AAAA XX22  XX = XX: no break (SR4: only 1:2 is a control)
      *>                                            -> AAAA XX22
      *>  G3 AAAA YY33  minor break: K OF REC-B(1:2) holds prior XX
      *>                while CF-B prints           -> CF-B XX33
      *>                restored, then detail       -> AAAA YY33
      *>  G4 BBBB YY33  major break: CF-B then CF-A with priors
      *>                                            -> CF-B YY33
      *>                                            -> CF-A AAAA
      *>                                            -> BBBB YY33
      *>  TERMINATE     GR5                         -> CF-B YY33
      *>                                            -> CF-A BBBB
      *> R-B: FINAL, then CD-K OF CDIN-REC (1:2) in the CD record.
      *>  G1 AA11 -> AA11; G2 AA22 (no break) -> AA22
      *>  G3 BB33 break: prior AA in (1:2) -> CF AA33, restored
      *>          -> BB33
      *>  G4 BB44 no break -> BB44
      *>  CC55 moved, TERMINATE: prior BB in (1:2) before each footing
      *>          -> CF BB55, then FIN BB55
      *> R-C: CD-J, no control footing (GR4b), a control heading.
      *>  G1 P  first GENERATE: CH P, D P
      *>  G2 Q  break, no footing: prior becomes Q -> CH Q, D Q
      *>  G3 Q  prior is Q: no break                -> D Q
      *>  G4 R  break                               -> CH R, D R
      *> Each leg can fail: a compiler that compared all of K OF REC-B
      *> breaks at G2; one that skipped GR4a prints CF-B YY33 / CF BB33
      *> at G3; one that skipped GR5 prints CF CC55; one that kept the
      *> old prior under GR4b prints a second CH Q at G3; one that
      *> resolved K without its qualifier refuses or reads REC-A's K.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2CTL03.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPA ASSIGN TO "g2ctl01a.rpt".
           SELECT RPB ASSIGN TO "g2ctl01b.rpt".
           SELECT RPC ASSIGN TO "g2ctl01c.rpt".
           SELECT BKA ASSIGN TO "g2ctl01a.rpt".
           SELECT BKB ASSIGN TO "g2ctl01b.rpt".
           SELECT BKC ASSIGN TO "g2ctl01c.rpt".
       DATA DIVISION.
       FILE SECTION.
       FD  RPA REPORT IS R-A.
       FD  RPB REPORT IS R-B.
       FD  RPC REPORT IS R-C.
       FD  BKA.
       01  BKA-REC PIC X.
       FD  BKB.
       01  BKB-REC PIC X.
       FD  BKC.
       01  BKC-REC PIC X.
       WORKING-STORAGE SECTION.
       01  REC-A.
           02  K       PIC X(4).
       01  REC-B.
           02  K       PIC X(4).
       01  WS-EOF      PIC 9 VALUE 0.
       01  WS-N        PIC 99 VALUE 0.
       01  WS-TAG      PIC XX VALUE SPACES.
       01  WS-BYTE     PIC X.
       01  WS-LINE     PIC X(20) VALUE SPACES.
       COMMUNICATION SECTION.
       CD  CDIN FOR INPUT.
       01  CDIN-REC.
           02  CD-Q    PIC X(12).
           02  CD-K    PIC X(4).
           02  CD-J    PIC X.
           02  FILLER  PIC X(70).
       REPORT SECTION.
       RD  R-A CONTROL K OF REC-A K OF REC-B (1:2).
       01  DT-A TYPE DETAIL LINE PLUS 1.
           05  COLUMN 1 PIC X(4) SOURCE K OF REC-A.
           05  COLUMN 6 PIC X(4) SOURCE K OF REC-B.
       01  TYPE CONTROL FOOTING K OF REC-A LINE PLUS 1.
           05  COLUMN 1 PIC X(5) VALUE "CF-A ".
           05  COLUMN 6 PIC X(4) SOURCE K OF REC-A.
       01  TYPE CONTROL FOOTING K OF REC-B (1:2) LINE PLUS 1.
           05  COLUMN 1 PIC X(5) VALUE "CF-B ".
           05  COLUMN 6 PIC X(4) SOURCE K OF REC-B.
       RD  R-B CONTROLS ARE FINAL CD-K OF CDIN-REC (1:2).
       01  DT-B TYPE DETAIL LINE PLUS 1.
           05  COLUMN 1 PIC X(4) SOURCE CD-K.
       01  TYPE CONTROL FOOTING CD-K OF CDIN-REC (1:2) LINE PLUS 1.
           05  COLUMN 1 PIC X(3) VALUE "CF ".
           05  COLUMN 4 PIC X(4) SOURCE CD-K.
       01  TYPE CONTROL FOOTING FINAL LINE PLUS 1.
           05  COLUMN 1 PIC X(4) VALUE "FIN ".
           05  COLUMN 5 PIC X(4) SOURCE CD-K.
       RD  R-C CONTROLS CD-J.
       01  TYPE CONTROL HEADING CD-J LINE PLUS 1.
           05  COLUMN 1 PIC X(3) VALUE "CH ".
           05  COLUMN 4 PIC X SOURCE CD-J.
       01  DT-C TYPE DETAIL LINE PLUS 1.
           05  COLUMN 1 PIC X(2) VALUE "D ".
           05  COLUMN 3 PIC X SOURCE CD-J.
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT RPA
           INITIATE R-A
           MOVE "AAAA" TO K OF REC-A
           MOVE "XX11" TO K OF REC-B
           GENERATE DT-A
           MOVE "XX22" TO K OF REC-B
           GENERATE DT-A
           MOVE "YY33" TO K OF REC-B
           GENERATE DT-A
           MOVE "BBBB" TO K OF REC-A
           GENERATE DT-A
           TERMINATE R-A
           CLOSE RPA
           OPEN OUTPUT RPB
           INITIATE R-B
           MOVE "AA11" TO CD-K
           GENERATE DT-B
           MOVE "AA22" TO CD-K
           GENERATE DT-B
           MOVE "BB33" TO CD-K
           GENERATE DT-B
           MOVE "BB44" TO CD-K
           GENERATE DT-B
           MOVE "CC55" TO CD-K
           TERMINATE R-B
           CLOSE RPB
           OPEN OUTPUT RPC
           INITIATE R-C
           MOVE "P" TO CD-J
           GENERATE DT-C
           MOVE "Q" TO CD-J
           GENERATE DT-C
           GENERATE DT-C
           MOVE "R" TO CD-J
           GENERATE DT-C
           TERMINATE R-C
           CLOSE RPC
           MOVE "A:" TO WS-TAG
           MOVE 0 TO WS-EOF
           OPEN INPUT BKA
           PERFORM UNTIL WS-EOF = 1
               READ BKA
                   AT END MOVE 1 TO WS-EOF
                   NOT AT END MOVE BKA-REC TO WS-BYTE
                              PERFORM TAKE-BYTE
               END-READ
           END-PERFORM
           CLOSE BKA
           IF WS-N > 0 PERFORM SHOW-LINE END-IF
           MOVE "B:" TO WS-TAG
           MOVE 0 TO WS-EOF
           OPEN INPUT BKB
           PERFORM UNTIL WS-EOF = 1
               READ BKB
                   AT END MOVE 1 TO WS-EOF
                   NOT AT END MOVE BKB-REC TO WS-BYTE
                              PERFORM TAKE-BYTE
               END-READ
           END-PERFORM
           CLOSE BKB
           IF WS-N > 0 PERFORM SHOW-LINE END-IF
           MOVE "C:" TO WS-TAG
           MOVE 0 TO WS-EOF
           OPEN INPUT BKC
           PERFORM UNTIL WS-EOF = 1
               READ BKC
                   AT END MOVE 1 TO WS-EOF
                   NOT AT END MOVE BKC-REC TO WS-BYTE
                              PERFORM TAKE-BYTE
               END-READ
           END-PERFORM
           CLOSE BKC
           IF WS-N > 0 PERFORM SHOW-LINE END-IF
           STOP RUN.
       TAKE-BYTE.
           EVALUATE WS-BYTE
               WHEN X"0A" PERFORM SHOW-LINE
               WHEN X"0D" CONTINUE
               WHEN X"0C" CONTINUE
               WHEN OTHER
                   ADD 1 TO WS-N
                   MOVE WS-BYTE TO WS-LINE(WS-N:1)
           END-EVALUATE.
       SHOW-LINE.
           IF WS-LINE NOT = SPACES DISPLAY WS-TAG WS-LINE END-IF
           MOVE SPACES TO WS-LINE
           MOVE 0 TO WS-N.
