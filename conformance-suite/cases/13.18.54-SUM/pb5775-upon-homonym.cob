      *>--- conformance-case
      *> id: pb5775-upon-homonym
      *> rules: [SR-13.18.54.3-7]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5775, PB5932 (listing step 6b-5 review R1-1).
      *> §2023:8.4.2.2.3 SR1: "uniqueness shall be established through a
      *> sequence of qualifiers that precludes any ambiguity of
      *> reference". Working-Storage DA and the detail DA are two
      *> declarations of one user-defined name, and the SUM clause's
      *> UPON DA names it unqualified: refused at every edition
      *> (§1985:IV.4.3.8.1 SRL2.1 below 2002); UPON DA OF R-1 is the
      *> detail.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5775U.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT-FILE ASSIGN TO "pb5775u.rpt".
       DATA DIVISION.
       FILE SECTION.
       FD  RPT-FILE REPORT IS R-1.
       WORKING-STORAGE SECTION.
       01  DA        PIC 9 VALUE 1.
       01  K         PIC 9 VALUE 1.
       01  AMT       PIC 99 VALUE 5.
       REPORT SECTION.
       RD  R-1 CONTROLS ARE K.
       01  DA TYPE DETAIL.
           05 LINE PLUS 1.
              10 COLUMN 1 PIC 99 SOURCE AMT.
       01  TYPE CONTROL FOOTING K.
           05 LINE PLUS 1.
              10 COLUMN 1 PIC 999 SUM AMT UPON DA.
       PROCEDURE DIVISION.
       P1.
           OPEN OUTPUT RPT-FILE
           INITIATE R-1
           GENERATE DA OF R-1
           TERMINATE R-1
           CLOSE RPT-FILE
           DISPLAY "RAN"
           STOP RUN.
