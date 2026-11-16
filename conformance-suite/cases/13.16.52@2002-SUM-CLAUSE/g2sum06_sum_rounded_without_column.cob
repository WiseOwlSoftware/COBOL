      *>--- conformance-case
      *> id: g2sum06_sum_rounded_without_column
      *> rules: [2002-SR-13.16.52.2-7]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5644. §2002:13.16.52.1 prints [ ROUNDED ] in the SUM
      *> clause's general format, and §2002:13.16.52.2 (syntax rules 1 to
      *> 7) holds no rule that limits the phrase to an entry with a COLUMN
      *> clause: that limit is COBOL-2014's (§2014:13.18.54.2 SR3, kept by
      *> §2023:13.18.54.3 SR3). The unprintable sum counter below (no
      *> COLUMN clause: §2002:13.16.52.3 GR4 prints only "if the entry also
      *> contains a COLUMN clause") writes the phrase and is legal at 2002.
      *> conformance:pb883-sum-rounded-without-column is the 2014 side.
      *> The standard states no effect of the phrase on such a counter, so
      *> the program proves translation and a run that completes.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2SUM06.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT ASSIGN TO "g2sum06.rpt".
       DATA DIVISION.
       FILE SECTION.
       FD RPT REPORT IS R-1.
       WORKING-STORAGE SECTION.
       01 WS-A PIC 9V99 VALUE 1.55.
       REPORT SECTION.
       RD R-1 CONTROL IS FINAL.
       01 DET-A TYPE DE LINE PLUS 1.
          02 COLUMN 1 PIC X(2) VALUE "D=".
       01 CFT TYPE CF FINAL LINE PLUS 1.
          02 CF-S PIC 9999 SUM WS-A ROUNDED.
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT RPT.
           INITIATE R-1.
           GENERATE DET-A.
           TERMINATE R-1.
           CLOSE RPT.
           DISPLAY "DONE".
           STOP RUN.
