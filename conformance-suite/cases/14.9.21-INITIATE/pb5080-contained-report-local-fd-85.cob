      *>--- conformance-case
      *> id: pb5080-contained-report-local-fd-85
      *> rules: [SR-14.9.21.3-1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §1985:X.4.6.4 GR2: "A statement in a program contained directly or indirectly within a program
      *> which describes a global name may reference that name without describing it again." RPT1 is a
      *> report-name described with GLOBAL (§1985:X.4.4.4 GR2 sends the report description entry's GLOBAL
      *> clause to X.4.6.4), so the contained program PB5080B may INITIATE, GENERATE and TERMINATE it. The only
      *> syntax rule of INITIATE and TERMINATE is "Report-name-1 must be defined by a report description entry
      *> in the Report Section of the Data Division" (§1985:XIII.4.4.3 SR1, §1985:XIII.4.7.3 SR1): 1985 asks
      *> nothing of the FD that names the report, which here is NOT GLOBAL. The rule that it be GLOBAL too begins
      *> at §2002:14.8.20.2 SR2 (INITIATE), §2002:14.8.42.2 SR2 (TERMINATE) and §2002:14.8.15.2 SR3/SR4 (GENERATE),
      *> so the same program is refused from 2002 on (wiseowl/diagnostics/pb5080-contained-report-local-fd-2002.json).
      *> Each cite.py --check of the clauses above printed OK (kb/Work PB5080).
      *> EXPECTED OUTPUT (derived from the program text): the contained program runs its three statements and
      *> returns, then the containing program closes the file.
      *>   INNER DONE
      *>   REPORT DONE
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5080A.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT-FILE ASSIGN TO "pb5080a.rpt".
       DATA DIVISION.
       FILE SECTION.
       FD  RPT-FILE
           REPORT IS RPT1.
       REPORT SECTION.
       RD  RPT1 IS GLOBAL
           PAGE LIMIT IS 10 LINES FIRST DETAIL 3.
       01  DET-LINE TYPE IS DETAIL LINE NUMBER IS PLUS 1.
           05  COLUMN NUMBER IS 1 PIC X(4) VALUE "BODY".
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT RPT-FILE.
           CALL "PB5080B".
           CLOSE RPT-FILE.
           DISPLAY "REPORT DONE".
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5080B.
       PROCEDURE DIVISION.
       INNER-PARA.
           INITIATE RPT1.
           GENERATE DET-LINE.
           TERMINATE RPT1.
           DISPLAY "INNER DONE".
           EXIT PROGRAM.
       END PROGRAM PB5080B.
       END PROGRAM PB5080A.
