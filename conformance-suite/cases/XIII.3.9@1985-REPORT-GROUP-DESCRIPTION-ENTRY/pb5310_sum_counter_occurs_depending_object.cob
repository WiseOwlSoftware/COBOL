      *>--- conformance-case
      *> id: pb5310_sum_counter_occurs_depending_object
      *> rules: [1985-SR-XIII.3.9.3-10]
      *> accept: [85]
      *> source-format: fixed
      *>---
      *> kb/Work PB5310 - 1985:XIII.3.9.3 SR10d: "Data-name-1 may be
      *> referenced only if the entry defines a sum counter" (cite.py:
      *> OK). The rule limits WHICH report entry a reference may name, not
      *> where it is written; 1985 has no rule barring a data division
      *> clause from naming a sum counter (that is ISO/IEC 1989:2002
      *> 13.7.5.1.3, refused from 2002 by
      *> 13.8.6.2.3-Report-Items/pb1046-report-item-as-occurs-depending-
      *> object). The OCCURS DEPENDING ON object "must describe an
      *> integer" (VI 5.8.3 SR6), and the counter KTOT of PIC 9(3) does.
      *> So TAB's current extent is KTOT's value.
      *> DERIVATION (W-V = 2): after INITIATE, MOVE 3 TO KTOT (a sum
      *> counter may be altered in the procedure division) gives TAB 3
      *> occurrences: LENGTH(TAB) = 3 and MOVE "ABCDEFGHI" TO TAB moves
      *> ABC. GENERATE DET adds W-V to the CF FINAL counter (XIII 3.19.4
      *> GR: summed at each GENERATE of a detail group), so KTOT is 5:
      *> LENGTH(TAB) = 5 and the MOVE moves ABCDE.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5310A.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RFILE ASSIGN TO "pb5310a.txt".
       DATA DIVISION.
       FILE SECTION.
       FD  RFILE REPORT IS RPT.
       WORKING-STORAGE SECTION.
       01  W-V PIC 9 VALUE 2.
       01  W-LX.
           05  W-L PIC 9.
       01  TAB.
           05  TE PIC X OCCURS 1 TO 9 TIMES DEPENDING ON KTOT.
       REPORT SECTION.
       RD  RPT CONTROLS ARE FINAL PAGE LIMIT 20 LINES.
       01  DET TYPE DETAIL LINE PLUS 1.
           05  COLUMN 1 PIC 9 SOURCE W-V.
       01  CFG TYPE CONTROL FOOTING FINAL LINE PLUS 1.
           05  KTOT COLUMN 1 PIC 9(3) SUM W-V.
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT RFILE.
           INITIATE RPT.
           MOVE 3 TO KTOT.
           COMPUTE W-L = FUNCTION LENGTH (TAB).
           MOVE "ABCDEFGHI" TO TAB.
           DISPLAY "[" TAB "] " W-LX.
           GENERATE DET.
           COMPUTE W-L = FUNCTION LENGTH (TAB).
           MOVE "ABCDEFGHI" TO TAB.
           DISPLAY "[" TAB "] " W-LX.
           TERMINATE RPT.
           CLOSE RFILE.
           STOP RUN.
