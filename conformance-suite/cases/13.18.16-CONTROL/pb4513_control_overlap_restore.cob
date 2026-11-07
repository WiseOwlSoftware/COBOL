      *>--- conformance-case
      *> id: pb4513_control_overlap_restore
      *> rules: [SR-13.18.16.3-6, GR-13.18.16.4-4, GR-14.9.46.4-3]
      *> accept: [85, 2002, 2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4513 - CONTROLS ARE WS-REGION WS-KEY, WS-REGION
      *> being the first two bytes of WS-KEY. The control-break step
      *> saved each control's current value and stored its prior in ONE
      *> pass, so the second control's "current" was read after the
      *> first control's prior had been stored into the shared bytes.
      *>
      *> "Two or more instances of data-name-1 in the same clause may,
      *> however, refer to the same physical data item or to
      *> overlapping data items."
      *>   cite.py: OK  §2023:13.18.16.3 6)  (Syntax rules)
      *> "a) If any control footing is defined for the report, the
      *> current contents of control data items are saved and the
      *> corresponding prior controls are stored in the control data
      *> items before any control footing is printed."
      *>   cite.py: OK  §2023:13.18.16.4 4) a)  (General rules)
      *> "d) The contents of any control data items are restored to the
      *> values they had at the start of execution of the TERMINATE
      *> statement."
      *>   cite.py: OK  §2023:14.9.46.4 3) d)  (General rules)
      *>
      *> DERIVATION. GENERATE 1 (WS-KEY EAXYZ) prints DT at line 1 and
      *> saves the priors EA / EAXYZ. The program moves WEQRS and
      *> GENERATE 2 senses a break at the most major control, WS-REGION:
      *> the CFs print minor to major with the PRIOR values (CF WS-KEY
      *> EAXYZ line 2, CF WS-REGION EA line 3), the new current values
      *> are restored (WS-KEY is WEQRS again, so DISPLAY AFTER shows
      *> WEQRS) and DT prints WEQRS (line 4). The new priors are WE /
      *> WEQRS. WEQRT then breaks at WS-KEY only: CF WS-KEY WEQRS (line
      *> 5), DT WEQRT (line 6). GENERATE 4 repeats WEQRT: no break, DT
      *> line 7. TERMINATE puts the priors back (WE / WEQRT), prints CF
      *> WS-KEY WEQRT (line 8) and CF WS-REGION WE (line 9), then
      *> restores the contents it began with (WEQRT).
      *> Fails if the break leaves WS-KEY corrupted (AFTER EAQRS, DT
      *> EAQRS, a spurious break at GENERATE 3 or 4) or the TERMINATE
      *> restore does (TERM not WEQRT).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4513T.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           SYMBOLIC CHARACTERS SYM-X0A SYM-X0D SYM-X0C
               ARE 11 14 13.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRT ASSIGN TO "pb4513t.txt".
           SELECT CHK ASSIGN TO "pb4513t.txt".
       DATA DIVISION.
       FILE SECTION.
       FD  PRT REPORT IS R-1.
       FD  CHK.
       01  CHK-REC PIC X.
       WORKING-STORAGE SECTION.
       01  WS-KEY.
           05  WS-REGION PIC X(2).
           05  WS-CITY   PIC X(3).
       01  WS-EOF  PIC X     VALUE "N".
       01  WS-I    PIC 99    VALUE 0.
       01  WS-LN   PIC 99    VALUE 0.
       01  WS-LINE PIC X(20) VALUE SPACES.
       REPORT SECTION.
       RD  R-1 CONTROLS ARE WS-REGION WS-KEY
           PAGE LIMIT IS 60 LINES.
       01  DT-1 TYPE DE LINE PLUS 1.
           02  COLUMN 1 PIC X(2) VALUE "DT".
           02  COLUMN 4 PIC X(5) SOURCE WS-KEY.
       01  TYPE CF WS-KEY LINE PLUS 1.
           02  COLUMN 1 PIC X(2) VALUE "CK".
           02  COLUMN 4 PIC X(5) SOURCE WS-KEY.
       01  TYPE CF WS-REGION LINE PLUS 1.
           02  COLUMN 1 PIC X(2) VALUE "CR".
           02  COLUMN 4 PIC X(5) SOURCE WS-REGION.
       PROCEDURE DIVISION.
       MAIN-P.
           OPEN OUTPUT PRT.
           INITIATE R-1.
           MOVE "EAXYZ" TO WS-KEY.
           GENERATE DT-1.
           MOVE "WEQRS" TO WS-KEY.
           GENERATE DT-1.
           DISPLAY "AFTER [" WS-KEY "]".
           MOVE "WEQRT" TO WS-KEY.
           GENERATE DT-1.
           GENERATE DT-1.
           TERMINATE R-1.
           DISPLAY "TERM  [" WS-KEY "]".
           CLOSE PRT.
           OPEN INPUT CHK.
           PERFORM UNTIL WS-EOF = "Y"
               READ CHK
                   AT END MOVE "Y" TO WS-EOF
                   NOT AT END PERFORM TAKE-BYTE
               END-READ
           END-PERFORM.
           CLOSE CHK.
           IF WS-I > 0
               PERFORM SHOW-LINE
           END-IF.
           STOP RUN.
       TAKE-BYTE.
           IF CHK-REC = SYM-X0A
               PERFORM SHOW-LINE
           ELSE
               IF CHK-REC NOT = SYM-X0D AND CHK-REC NOT = SYM-X0C
                   ADD 1 TO WS-I
                   MOVE CHK-REC TO WS-LINE(WS-I:1)
               END-IF
           END-IF.
       SHOW-LINE.
           ADD 1 TO WS-LN.
           IF WS-LINE NOT = SPACES
               DISPLAY "LINE " WS-LN " [" WS-LINE(1:8) "]"
           END-IF.
           MOVE SPACES TO WS-LINE.
           MOVE 0 TO WS-I.
