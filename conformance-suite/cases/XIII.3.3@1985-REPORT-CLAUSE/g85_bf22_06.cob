      *>--- conformance-case
      *> id: g85_bf22_06
      *> rules: [1985-GR-XIII.3.3.4-3]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XIII.3.3.4 GR3 - an external report file connector, every
      *> FD for it a report file. RULE (cite.py OK): "If the associated
      *> file connector is an external file connector, every file
      *> description entry in the run unit which is associated with that
      *> file connector must describe it as a report file." The rule
      *> binds the programs; the obligation on the implementation is that
      *> a run unit that OBEYS it is accepted and runs. G85BF2206 and the
      *> separately compiled G85BF226S each describe the EXTERNAL file
      *> REP-F with a REPORT clause (each its own report, RM and RS, of
      *> equal 8-character lines). Only the main opens REP-F; the sub
      *> INITIATEs, GENERATEs and TERMINATEs RS on the open connector.
      *> DERIVATION: §1985:XIII.4.4.4 GR1c (cite.py OK) "PAGE-COUNTER is
      *> set to one." by each INITIATE: the main shows M-PC=01, the sub
      *> S-PC=01; checkpoints SUB-DONE after the sub returns its report,
      *> E-DONE after the main's TERMINATE and CLOSE. The report file's
      *> record structure is the implementor's (§1985:XIII.3.2.4 GR2,
      *> cite.py OK), so its content is not compared.
      *> A leg fails if the agreeing report FDs are refused or the run
      *> unit does not complete.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85BF2206.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT REP-F ASSIGN TO "G85BF226.TXT".
       DATA DIVISION.
       FILE SECTION.
       FD  REP-F IS EXTERNAL
           REPORT IS RM.
       WORKING-STORAGE SECTION.
       01  WS-PC PIC 99.
       REPORT SECTION.
       RD  RM.
       01  RM-D TYPE IS DETAIL LINE NUMBER IS PLUS 1.
           02  COLUMN NUMBER IS 1 PIC X(8) VALUE "MAINLINE".
       PROCEDURE DIVISION.
       P1.
           OPEN OUTPUT REP-F.
           INITIATE RM.
           MOVE PAGE-COUNTER TO WS-PC.
           DISPLAY "M-PC=" WS-PC.
           GENERATE RM-D.
           CALL "G85BF226S".
           TERMINATE RM.
           CLOSE REP-F.
           DISPLAY "E-DONE".
           STOP RUN.
       END PROGRAM G85BF2206.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85BF226S.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT REP-F ASSIGN TO "G85BF226.TXT".
       DATA DIVISION.
       FILE SECTION.
       FD  REP-F IS EXTERNAL
           REPORT IS RS.
       WORKING-STORAGE SECTION.
       01  WS-PC PIC 99.
       REPORT SECTION.
       RD  RS.
       01  RS-D TYPE IS DETAIL LINE NUMBER IS PLUS 1.
           02  COLUMN NUMBER IS 1 PIC X(8) VALUE "SUB-LINE".
       PROCEDURE DIVISION.
       S1.
           INITIATE RS.
           MOVE PAGE-COUNTER TO WS-PC.
           DISPLAY "S-PC=" WS-PC.
           GENERATE RS-D.
           TERMINATE RS.
           DISPLAY "SUB-DONE".
           EXIT PROGRAM.
       END PROGRAM G85BF226S.
