      *>--- conformance-case
      *> id: g2_initiate_active_state
      *> rules: [2002-SR-14.8.20.2-1, 2002-GR-14.8.20.3-2,
      *>    2002-GR-14.8.20.3-4]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:14.8.20.2 SR1 - "Report-name-1 shall be defined by a
      *>   report description entry in the report section." R-ACT is an
      *>   RD name and is accepted (the refusal is g2-initiate-not-a-
      *>   report).
      *> §2002:14.8.20.3 GR4 - "A successful INITIATE statement places
      *>   the report in the active state." §2002:13.7.3: active until
      *>   a successful TERMINATE, "At any other time, it is in the
      *>   inactive state."
      *> §2002:14.8.20.3 GR2 - "If it is in the active state, the
      *>   EC-REPORT-ACTIVE exception condition is set to exist and the
      *>   execution of the INITIATE statement has no other effect."
      *>   EC-REPORT-ACTIVE and EC-REPORT-INACTIVE are Fatal
      *>   (§2002:14.5.12.1.5); with checking on each USE procedure
      *>   runs (§2002:14.5.12.1.2 item 2) and RESUME AT NEXT STATEMENT
      *>   continues (§2002:14.8.31.3 GR2).
      *> Observables: S-CTR names the sum counter (§2002:13.16.52.3
      *>   GR5), added at every GENERATE (GR7c1); INITIATE zeroes it and
      *>   LINE-COUNTER and sets PAGE-COUNTER to 1 (§2002:14.8.20.3 GR1).
      *> DERIVATION.
      *>   I1 S=000 LC=000 PC=001 - GR1 at the first INITIATE.
      *>   G1 S=004, G1 LC>0 - GENERATE adds 4 and prints a line; no
      *>     HANDLED line, so the report is active (GR4; §2002:14.8.15.3
      *>     GR7 would raise EC-REPORT-INACTIVE).
      *>   HANDLED EC-REPORT-ACTIVE - the second INITIATE (GR2).
      *>   I2 S=004, I2 LC-KEPT - "no other effect": GR1 did not re-run.
      *>   G2 S=008 - the counter kept 4.
      *>   HANDLED EC-REPORT-INACTIVE, AFTER-TERM - after TERMINATE the
      *>     report is inactive and GENERATE raises.
      *>   I3 S=000 LC=000 - the INITIATE of an inactive report succeeds
      *>     and initializes (no HANDLED line).
      *>   G3 S=004 - it is active again (GR4).
       >>TURN EC-REPORT-ACTIVE EC-REPORT-INACTIVE CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2INI01.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT ASSIGN TO "g2ini01.rpt".
       DATA DIVISION.
       FILE SECTION.
       FD RPT REPORT IS R-ACT.
       WORKING-STORAGE SECTION.
       01 WS-G   PIC 9 VALUE 1.
       01 WS-N   PIC 9 VALUE 4.
       01 WS-S   PIC 999.
       01 WS-LC  PIC 999.
       01 WS-LC1 PIC 999.
       01 WS-PC  PIC 999.
       01 WS-EA  PIC X(16).
       01 WS-EI  PIC X(18).
       REPORT SECTION.
       RD R-ACT CONTROL IS WS-G
           PAGE LIMIT IS 20 LINES HEADING 1 FIRST DETAIL 3
           LAST DETAIL 15.
       01 DET-A TYPE DE LINE PLUS 1.
          02 COLUMN 1 PIC 9 SOURCE IS WS-N.
       01 TYPE CF WS-G LINE PLUS 1.
          02 S-CTR COLUMN 3 PIC 999 SUM WS-N.
       PROCEDURE DIVISION.
       DECLARATIVES.
       HA SECTION.
           USE AFTER EXCEPTION CONDITION EC-REPORT-ACTIVE.
       HA-P.
           MOVE FUNCTION EXCEPTION-STATUS TO WS-EA
           DISPLAY "HANDLED " WS-EA.
           RESUME AT NEXT STATEMENT.
       HI SECTION.
           USE AFTER EXCEPTION CONDITION EC-REPORT-INACTIVE.
       HI-P.
           MOVE FUNCTION EXCEPTION-STATUS TO WS-EI
           DISPLAY "HANDLED " WS-EI.
           RESUME AT NEXT STATEMENT.
       END DECLARATIVES.
       MAIN SECTION.
       MAIN-PARA.
           OPEN OUTPUT RPT.
           INITIATE R-ACT.
           PERFORM SHOW-STATE.
           DISPLAY "I1 S=" WS-S " LC=" WS-LC " PC=" WS-PC.
           GENERATE DET-A.
           PERFORM SHOW-STATE.
           MOVE WS-LC TO WS-LC1.
           DISPLAY "G1 S=" WS-S.
           IF WS-LC1 > 0 DISPLAY "G1 LC>0" ELSE DISPLAY "G1 LC=0".
           INITIATE R-ACT.
           PERFORM SHOW-STATE.
           DISPLAY "I2 S=" WS-S.
           IF WS-LC = WS-LC1
               DISPLAY "I2 LC-KEPT"
           ELSE
               DISPLAY "I2 LC-CHANGED"
           END-IF.
           GENERATE DET-A.
           PERFORM SHOW-STATE.
           DISPLAY "G2 S=" WS-S.
           TERMINATE R-ACT.
           GENERATE DET-A.
           DISPLAY "AFTER-TERM".
           INITIATE R-ACT.
           PERFORM SHOW-STATE.
           DISPLAY "I3 S=" WS-S " LC=" WS-LC.
           GENERATE DET-A.
           PERFORM SHOW-STATE.
           DISPLAY "G3 S=" WS-S.
           TERMINATE R-ACT.
           CLOSE RPT.
           DISPLAY "DONE".
           STOP RUN.
       SHOW-STATE.
           MOVE S-CTR TO WS-S.
           MOVE LINE-COUNTER OF R-ACT TO WS-LC.
           MOVE PAGE-COUNTER OF R-ACT TO WS-PC.
       END PROGRAM G2INI01.
