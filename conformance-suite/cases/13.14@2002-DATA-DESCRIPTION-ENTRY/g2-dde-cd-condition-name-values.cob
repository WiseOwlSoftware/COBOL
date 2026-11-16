      *>--- conformance-case
      *> id: g2-dde-cd-condition-name-values
      *> rules: [2002-GR-13.14.3-3]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:13.14.3 GR3 - "Format 3 contains the name of the
      *> condition and the value, values, or range of values associated
      *> with the condition-name." One condition-name of each kind on
      *> A1 in a record after a CD, tested by condition-name conditions
      *> (§2002:8.8.4.1.4.2 GR1 and GR3) and set by SET TO TRUE,
      *> which stores the first literal (§2002:14.8.35.3 GR5). A value
      *> not associated, or a range that excludes an end value, flips
      *> a Y/N below.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2DDE12.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  FLAGS.
           05 FY PIC X.
           05 FR PIC X.
           05 FL PIC X.
       COMMUNICATION SECTION.
       CD  CDOUT FOR OUTPUT
           DESTINATION COUNT IS DC
           TEXT LENGTH IS TL
           STATUS KEY IS SK
           ERROR KEY IS EK
           SYMBOLIC DESTINATION IS SD1.
       01  CDREC.
           05 A1 PIC X(3).
              88 A1-YES VALUE "YES".
              88 A1-RANGE VALUE "AAA" THRU "CZZ".
              88 A1-LIST VALUES "ONE" "TWO".
       PROCEDURE DIVISION.
       MAIN-PARA.
           MOVE "YES" TO A1
           PERFORM SHOW-IT
           MOVE "BBB" TO A1
           PERFORM SHOW-IT
           MOVE "CZZ" TO A1
           PERFORM SHOW-IT
           MOVE "TWO" TO A1
           PERFORM SHOW-IT
           SET A1-LIST TO TRUE
           PERFORM SHOW-IT
           STOP RUN.
       SHOW-IT.
           MOVE "NNN" TO FLAGS
           IF A1-YES MOVE "Y" TO FY END-IF
           IF A1-RANGE MOVE "Y" TO FR END-IF
           IF A1-LIST MOVE "Y" TO FL END-IF
           DISPLAY "A1=" A1 " YES=" FY " RANGE=" FR " LIST=" FL.
