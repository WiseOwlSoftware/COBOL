      *>--- conformance-case
      *> id: g2lct01_linage_counter_in_procedure_statements
      *> rules: [2002-SR-8.4.2.13.2-1, 2002-SR-8.4.2.13.2-2]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:8.4.2.13.2 SR1 - "LINAGE-COUNTER may be referenced only in
      *> procedure division statements." The granted form: LINAGE-COUNTER
      *> referenced in procedure division statements - MOVE, IF, COMPUTE
      *> and, at 2002, the communication SEND ... AFTER ADVANCING
      *> identifier-3 LINES (§2002:A.2.6.2 SR4: "Identifier-3 shall
      *> reference an integer data item"; LINAGE-COUNTER is an "unsigned
      *> integer data item", §2002:8.4.2.13.3 GR1).
      *> §2002:8.4.2.13.2 SR2 - LINAGE-COUNTER "shall not be referenced as
      *> a receiving operand": every reference here is a SENDING operand
      *> (qualified and not), which SR2 leaves legal; the refusals of
      *> the receiving uses are g2lct02 and g2lct03.
      *> Values (§2002:13.16.32.3 GR7): OPEN OUTPUT sets it to one (GR7d);
      *> a WRITE without ADVANCING increments it by one (GR7c3); a WRITE
      *> AFTER ADVANCING 3 LINES by three (GR7c2).
      *>   OPEN    -> 1: "LC1 01"
      *>   WRITE   -> 2: "LC2 OK" (IF LINAGE-COUNTER = 2)
      *>   COMPUTE N = LINAGE-COUNTER OF LPF * 10 -> "LC3 20"
      *>   WRITE AFTER ADVANCING 3 -> 5: "LC4 05"
      *>   SEND ... AFTER ADVANCING LINAGE-COUNTER LINES accepted: "SENT"
      *> (the message's disposition belongs to the MCS and is not shown).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2LCT01.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT LPF ASSIGN TO "G2LCT01.TXT".
       DATA DIVISION.
       FILE SECTION.
       FD  LPF LINAGE IS 20 LINES.
       01  LREC PIC X(10).
       WORKING-STORAGE SECTION.
       01  N   PIC 99.
       01  MSG PIC X(10) VALUE "HELLO".
       COMMUNICATION SECTION.
       CD  OUT-CD FOR OUTPUT
           DESTINATION COUNT IS OUT-COUNT
           STATUS KEY IS OUT-ST
           SYMBOLIC DESTINATION IS OUT-DEST.
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT LPF.
           MOVE LINAGE-COUNTER TO N.
           DISPLAY "LC1 " N.
           MOVE "L1" TO LREC.
           WRITE LREC.
           IF LINAGE-COUNTER = 2
               DISPLAY "LC2 OK"
           ELSE
               DISPLAY "LC2 NOT-2"
           END-IF.
           COMPUTE N = LINAGE-COUNTER OF LPF * 10.
           DISPLAY "LC3 " N.
           MOVE "L2" TO LREC.
           WRITE LREC AFTER ADVANCING 3 LINES.
           MOVE LINAGE-COUNTER OF LPF TO N.
           DISPLAY "LC4 " N.
           SEND OUT-CD FROM MSG WITH EMI
               AFTER ADVANCING LINAGE-COUNTER LINES.
           DISPLAY "SENT".
           CLOSE LPF.
           STOP RUN.
