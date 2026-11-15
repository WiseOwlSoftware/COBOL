      *>--- conformance-case
      *> id: g2-terminate-inactive-and-no-generate
      *> rules: [2002-GR-14.8.42.3-1, 2002-GR-14.8.42.3-2]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:14.8.42.3 GR1 - TERMINATE of an inactive report
      *> "If the report is not in the active state, the
      *> EC-REPORT-INACTIVE exception condition is set to exist and the
      *> execution of the statement has no other effect."
      *>   cite.py: OK  §2002:14.8.42.3 GR1
      *> §2002:14.8.42.3 GR2 - TERMINATE with no GENERATE since INITIATE
      *> "the TERMINATE statement causes no processing of any kind to
      *> take place for any report groups and has the sole effect of
      *> changing the state of the report to inactive."
      *>   cite.py: OK  §2002:14.8.42.3 GR2
      *> Supporting 2002 text (each cite.py OK):
      *>  §2002:14.8.20.3 GR1b "LINE-COUNTER is set to zero", GR1c
      *>   "PAGE-COUNTER is set to 1", GR4 "A successful INITIATE
      *>   statement places the report in the active state", GR2 an
      *>   INITIATE of an active report: "the EC-REPORT-ACTIVE exception
      *>   condition is set to exist";
      *>  §2002:14.8.15.3 GR4a "The report heading is printed if
      *>   defined" (the first GENERATE after an INITIATE);
      *>  §2002:14.8.42.3 GR3c "The report footing is printed, if
      *>   defined" (a TERMINATE after a GENERATE);
      *>  §2002:14.8.42.3 GR6 "The TERMINATE statement does not close the
      *>   file associated with report-name-1";
      *>  §2002:14.8.45.3 GR8 "The declarative is invoked just before the
      *>   named report group is produced", so each RH/RF declarative
      *>   DISPLAY shows that group being processed;
      *>  §2002:14.5.12.1.2 (EC-REPORT-INACTIVE is fatal; checking is on)
      *>   "there is an applicable USE statement in the source unit that
      *>   specifies the exception-name associated with the exception
      *>   condition": the declarative runs, and §2002:14.8.31.3 GR2
      *>   RESUME NEXT STATEMENT continues after the TERMINATE.
      *> DERIVATION of every .out line:
      *>  INITIATE R-A (LC 0, PC 1, active); TERMINATE R-A with no
      *>   GENERATE: no report group processed (no "RH-A", no "RF-A"),
      *>   counters untouched, R-A inactive (GR2)  -> "T1 LC=0 PC=1"
      *>  TERMINATE R-A again: inactive, EC raised, nothing else (GR1;
      *>   no "RF-A")              -> "INACTIVE EC-REPORT-INACTIVE" "T2"
      *>  TERMINATE R-B, never initiated: the same, no "RF-B" (GR1)
      *>                           -> "INACTIVE EC-REPORT-INACTIVE" "T3"
      *>  INITIATE R-A: R-A is inactive (GR2's sole effect), so no
      *>   EC-REPORT-ACTIVE (no "ACTIVE" line); GENERATE DA: "RH-A";
      *>   TERMINATE R-A after a GENERATE: "RF-A"; then "END".
      *> The last three lines are the control: the same declaratives DO
      *> run when the groups are processed, so their absence above is
      *> the rule's effect. Fails if a no-GENERATE TERMINATE produces the
      *> RH or RF or moves a counter, if a TERMINATE of an inactive
      *> report is not diagnosed or produces the RF, or if GR2 leaves
      *> R-A active (the second TERMINATE would print "RF-A" and the
      *> re-INITIATE would print "ACTIVE").
       >>TURN EC-REPORT-INACTIVE EC-REPORT-ACTIVE CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2TRM01.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RFA ASSIGN TO "G2TRM01A.RPT".
           SELECT RFB ASSIGN TO "G2TRM01B.RPT".
       DATA DIVISION.
       FILE SECTION.
       FD  RFA REPORT IS R-A.
       FD  RFB REPORT IS R-B.
       WORKING-STORAGE SECTION.
       01  WS-LC PIC 9.
       01  WS-PC PIC 9.
       REPORT SECTION.
       RD  R-A.
       01  HA TYPE RH LINE PLUS 1.
           02  COLUMN 1 PIC X(2) VALUE "HA".
       01  DA TYPE DE LINE PLUS 1.
           02  COLUMN 1 PIC X(2) VALUE "DA".
       01  FA TYPE RF LINE PLUS 1.
           02  COLUMN 1 PIC X(2) VALUE "FA".
       RD  R-B.
       01  DB TYPE DE LINE PLUS 1.
           02  COLUMN 1 PIC X(2) VALUE "DB".
       01  FB TYPE RF LINE PLUS 1.
           02  COLUMN 1 PIC X(2) VALUE "FB".
       PROCEDURE DIVISION.
       DECLARATIVES.
       UI SECTION.
           USE AFTER EXCEPTION CONDITION EC-REPORT-INACTIVE.
       UI-P.
           DISPLAY "INACTIVE " FUNCTION EXCEPTION-STATUS.
           RESUME AT NEXT STATEMENT.
       UACT SECTION.
           USE AFTER EXCEPTION CONDITION EC-REPORT-ACTIVE.
       UACT-P.
           DISPLAY "ACTIVE " FUNCTION EXCEPTION-STATUS.
           RESUME AT NEXT STATEMENT.
       UHA SECTION.
           USE BEFORE REPORTING HA.
       UHA-P.
           DISPLAY "RH-A".
       UFA SECTION.
           USE BEFORE REPORTING FA.
       UFA-P.
           DISPLAY "RF-A".
       UFB SECTION.
           USE BEFORE REPORTING FB.
       UFB-P.
           DISPLAY "RF-B".
       END DECLARATIVES.
       MAIN SECTION.
       M1.
           OPEN OUTPUT RFA RFB.
           INITIATE R-A.
           TERMINATE R-A.
           MOVE LINE-COUNTER OF R-A TO WS-LC.
           MOVE PAGE-COUNTER OF R-A TO WS-PC.
           DISPLAY "T1 LC=" WS-LC " PC=" WS-PC.
           TERMINATE R-A.
           DISPLAY "T2".
           TERMINATE R-B.
           DISPLAY "T3".
           INITIATE R-A.
           GENERATE DA.
           TERMINATE R-A.
           DISPLAY "END".
           CLOSE RFA RFB.
           STOP RUN.
