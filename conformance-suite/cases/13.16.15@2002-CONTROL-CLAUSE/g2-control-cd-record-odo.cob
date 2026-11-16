      *>--- conformance-case
      *> id: g2-control-cd-record-odo
      *> rules: [2002-SR-13.16.15.2-5]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:13.16.15.2 SR5 - "The entry specified by data-name-1
      *> shall not have a variable-occurrence entry subordinate to it."
      *> The operand CD-G is a group of the record description entry
      *> that follows a CD (§2002:A.1.1: "The communication description
      *> entry is followed by none, one, or more record description
      *> entries"), and the OCCURS DEPENDING table CD-T is subordinate
      *> to it. The CD is an output CD, whose records have no size
      *> requirement (§2002:A.1.2.2 SR9), unlike an input CD's records,
      *> which shall be exactly 87 positions (SR5). The program is
      *> otherwise legal (CD-G is not in the
      *> report section, §2002:13.16.15.2 SR2), so the only refusal is
      *> SR5, reported at the RD that names CD-G. The communication
      *> facility draws the obsolete warning COBOL0903, which is not a
      *> refusal. Why it can fail: the SR5 screen walks only the
      *> working-storage, file and linkage sections and misses an
      *> operand defined in the communication section.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2CTL02.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT ASSIGN TO "g2ctl01.rpt".
       DATA DIVISION.
       FILE SECTION.
       FD  RPT REPORT IS R-1.
       WORKING-STORAGE SECTION.
       01  WN PIC 9 VALUE 2.
       COMMUNICATION SECTION.
       CD  CDIN FOR OUTPUT.
       01  CDIN-REC.
           02  CD-Q    PIC X(12).
           02  CD-G.
               03  CD-T PIC X(15) OCCURS 1 TO 5 DEPENDING ON WN.
       REPORT SECTION.
       RD  R-1 CONTROLS ARE CD-G.
       01  DT-1 TYPE DE LINE PLUS 1.
           05  COLUMN 1 PIC X(2) VALUE "DT".
       PROCEDURE DIVISION.
       P1.
           STOP RUN.
