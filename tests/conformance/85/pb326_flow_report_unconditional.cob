      *> ISO §14.9.49.4 GR10: "If a GENERATE, INITIATE, or TERMINATE statement is executed within the range of
      *> a declarative procedure whose USE statement contains the BEFORE REPORTING phrase, the EC-FLOW-REPORT
      *> exception condition is set to exist, the result of the execution of the GENERATE, INITIATE, or
      *> TERMINATE statement is unsuccessful, and the state of the report is unchanged."
      *>
      *> THE POINT OF THIS PROGRAM IS THE UNCONDITIONAL HALF, AT THE OLDEST EDITION. §14.6.13.1.1 gates only the
      *> RAISING of the condition on checking being enabled; "the result … is unsuccessful, and the state of the
      *> report is unchanged" is stated outright and holds at every edition and every checking setting. COBOL-85
      *> has no >>TURN directive at all, so no checking can be enabled here — and the nested GENERATE must still
      *> produce nothing.  (Its checked twin is 2002/pb326_ec_flow_report.)
      *>
      *> The program is legal: §14.9.49.3 SR10 forbids GENERATE/INITIATE/TERMINATE only "in a paragraph within a
      *> USE BEFORE REPORTING procedure", and GEN-P is a paragraph of a DIFFERENT declarative section, which SR4
      *> ("Procedure-names within a declarative section may be referenced in a different declarative section …
      *> only with a PERFORM statement") expressly permits BR-P to PERFORM.
      *>
      *> DERIVATION OF THE EXPECTED OUTPUT.  The single GENERATE presents DET-1; §14.9.49.4 GR8/GR9d run the
      *> declarative just before the group's LINE clauses are processed, so BR-P sets WS-N to 1 and PERFORMs
      *> GEN-P, whose GENERATE is inside the range and therefore does nothing at all.  Control returns and the
      *> outer presentation composes DET-1 with WS-N = 1 (§13.18.53.4 GR1/GR3 — the implicit MOVE executes when
      *> the line is printed).  The report has no RH/PH/CH/CF/PF/RF, so TERMINATE prints nothing further
      *> (§14.9.46.4 GR3).  Expected: N=1 and EXACTLY ONE detail line, D=1.  Before kb/Work PB326 the nested
      *> GENERATE succeeded and a SECOND D=1 line was produced.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB326RW85.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT ASSIGN TO "pb326-flow85.rpt".
           SELECT RBACK ASSIGN TO "pb326-flow85.rpt"
               ORGANIZATION IS LINE SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD RPT REPORT IS R-F85.
       FD RBACK.
       01 RB-REC PIC X(30).
       WORKING-STORAGE SECTION.
       01 WS-N   PIC 9 VALUE 0.
       01 WS-EOF PIC 9 VALUE 0.
       REPORT SECTION.
       RD R-F85 PAGE LIMIT IS 20 LINES HEADING 1 FIRST DETAIL 3
           LAST DETAIL 15.
       01 DET-1 TYPE DE LINE PLUS 1.
          02 COLUMN 1 PIC X(2) VALUE "D=".
          02 COLUMN 3 PIC 9 SOURCE IS WS-N.
       PROCEDURE DIVISION.
       DECLARATIVES.
       BR-SEC SECTION.
           USE BEFORE REPORTING DET-1.
       BR-P.
           ADD 1 TO WS-N.
           IF WS-N < 3
               PERFORM GEN-P
           END-IF.
       ERR-SEC SECTION.
           USE AFTER STANDARD ERROR PROCEDURE ON RPT.
       GEN-P.
           GENERATE DET-1.
       END DECLARATIVES.
       MAIN SECTION.
       MAIN-PARA.
           OPEN OUTPUT RPT.
           INITIATE R-F85.
           GENERATE DET-1.
           TERMINATE R-F85.
           CLOSE RPT.
           DISPLAY "N=" WS-N.
      *> The report content, blank lines skipped — the vertical placement of a report group is a separate
      *> question (kb/Work PB484) and this program deliberately does not depend on it.
       READ-BACK.
           OPEN INPUT RBACK.
           PERFORM UNTIL WS-EOF = 1
               READ RBACK
                   AT END MOVE 1 TO WS-EOF
                   NOT AT END
                       IF RB-REC NOT = SPACES
                           DISPLAY RB-REC
                       END-IF
               END-READ
           END-PERFORM.
           CLOSE RBACK.
           STOP RUN.
