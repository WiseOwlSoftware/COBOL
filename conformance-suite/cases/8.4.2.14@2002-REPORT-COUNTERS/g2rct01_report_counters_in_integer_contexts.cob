      *>--- conformance-case
      *> id: g2rct01_report_counters_in_integer_contexts
      *> rules: [2002-SR-8.4.2.14.2-1]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:8.4.2.14.2 SR1 - "In the report section, PAGE-COUNTER and
      *> LINE-COUNTER may be referenced only in a SOURCE clause. In the
      *> procedure division, PAGE-COUNTER and LINE-COUNTER may be
      *> referenced in any context where an integer data item may
      *> appear." The granted forms: PAGE-COUNTER in a SOURCE clause, and
      *> both counters in procedure-division integer contexts - a MOVE
      *> sender, a relation, a subscript, an ADD sender (qualified by
      *> report-name, §2002:8.4.2.14.1), a MOVE receiver (only
      *> LINE-COUNTER is barred as a receiver, §2002:8.4.2.14.2 SR3), and
      *> at 2002 SEND ... ADVANCING identifier-3 LINES (§2002:A.2.6.2 SR4
      *> "Identifier-3 shall reference an integer data item"; SR6 permits
      *> the value zero).
      *> Values: INITIATE sets PAGE-COUNTER to 1 (§2002:8.4.2.14.3 GR2)
      *> and LINE-COUNTER to zero (§2002:8.4.2.14.3 GR3); no GENERATE runs,
      *> so no page advance changes them.
      *>   "PC 01"       MOVE PAGE-COUNTER TO N
      *>   "LC ZERO"     IF LINE-COUNTER = 0
      *>   "SUB A"       TE (PAGE-COUNTER) is TE (1) of "ABC"
      *>   "ADD 02"      ADD PAGE-COUNTER IN RPT1 TO N (1 + 1)
      *>   "SET 03"      MOVE 3 TO PAGE-COUNTER, then MOVE it to N
      *>   "SENT"        both SENDs accepted (MCS disposition not shown)
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2RCT01.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT ASSIGN TO "G2RCT01.RPT".
       DATA DIVISION.
       FILE SECTION.
       FD  RPT REPORT IS RPT1.
       WORKING-STORAGE SECTION.
       01  N   PIC 99.
       01  MSG PIC X(10) VALUE "HELLO".
       01  TBV PIC XXX VALUE "ABC".
       01  TB REDEFINES TBV.
           02  TE PIC X OCCURS 3 TIMES.
       COMMUNICATION SECTION.
       CD  OUT-CD FOR OUTPUT
           DESTINATION COUNT IS OUT-COUNT
           STATUS KEY IS OUT-ST
           SYMBOLIC DESTINATION IS OUT-DEST.
       REPORT SECTION.
       RD  RPT1 PAGE LIMIT 60 LINES.
       01  DETAIL-LINE TYPE IS DETAIL LINE PLUS 1.
           02  COLUMN 1 PIC 99 SOURCE IS PAGE-COUNTER.
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT RPT.
           INITIATE RPT1.
           MOVE PAGE-COUNTER TO N.
           DISPLAY "PC " N.
           IF LINE-COUNTER = 0
               DISPLAY "LC ZERO"
           ELSE
               DISPLAY "LC NOT-ZERO"
           END-IF.
           DISPLAY "SUB " TE (PAGE-COUNTER).
           ADD PAGE-COUNTER IN RPT1 TO N.
           DISPLAY "ADD " N.
           MOVE 3 TO PAGE-COUNTER.
           MOVE PAGE-COUNTER TO N.
           DISPLAY "SET " N.
           SEND OUT-CD FROM MSG WITH EMI
               AFTER ADVANCING PAGE-COUNTER LINES.
           SEND OUT-CD FROM MSG WITH EMI
               BEFORE ADVANCING LINE-COUNTER LINES.
           DISPLAY "SENT".
           TERMINATE RPT1.
           CLOSE RPT.
           STOP RUN.
