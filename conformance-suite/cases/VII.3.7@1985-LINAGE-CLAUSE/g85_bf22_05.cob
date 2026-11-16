      *>--- conformance-case
      *> id: g85_bf22_05
      *> rules: [1985-GR-VII.3.7.4-11]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VII.3.7.4 GR11 - external file connector: every FD has a
      *> LINAGE clause with the same integers and the same external data
      *> items. RULE (cite.py OK GR11, GR11b, GR11c): "all file
      *> description entries in the run unit which are associated with
      *> this file connector must have: a. A LINAGE clause, if any file
      *> description entry has a LINAGE clause. b. The same corresponding
      *> values for integer-1, integer-2, integer-3, and integer-4, if
      *> specified. c. The same corresponding external data items
      *> referenced by data-name-1, ...". The rule binds the programs;
      *> the obligation on the implementation is that a run unit that
      *> OBEYS it works. G85BF2205 and the separately compiled G85BF225S
      *> both describe the EXTERNAL file LF with
      *>   LINAGE IS BF-PG LINES WITH FOOTING AT 8
      *>       LINES AT TOP 1 LINES AT BOTTOM 1
      *> where BF-PG is the SAME EXTERNAL data item (c.) and 8, 1, 1 the
      *> same integers (b.). DERIVATION: §1985:X.4.2.4 GR1 (cite.py OK):
      *> with LINAGE and EXTERNAL "the LINAGE-COUNTER data item is an
      *> external data item", so both programs see one counter.
      *> §1985:VII.3.7.4 GR9d (cite.py OK): OPEN OUTPUT sets it to one;
      *> GR9c 3) (cite.py OK): each WRITE without ADVANCING adds one.
      *>   main OPEN -> 1, main WRITE -> 2, sub WRITE -> 3: SUB LC=0003
      *>   main WRITE -> 4:                               MAIN LC=0004
      *> (page body BF-PG = 10, footing at 8: no page change).
      *> A leg fails if the agreeing FDs are refused, or the connector
      *> or its LINAGE-COUNTER is not shared between the programs.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85BF2205.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT LF ASSIGN TO "G85BF225.TXT"
               ORGANIZATION IS SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD  LF IS EXTERNAL
           LINAGE IS BF-PG LINES WITH FOOTING AT 8
               LINES AT TOP 1 LINES AT BOTTOM 1.
       01  LF-REC PIC X(6).
       WORKING-STORAGE SECTION.
       01  BF-PG PIC 99 EXTERNAL.
       01  WS-LC PIC 9(4).
       PROCEDURE DIVISION.
       P1.
           MOVE 10 TO BF-PG.
           OPEN OUTPUT LF.
           MOVE "MAIN-1" TO LF-REC.
           WRITE LF-REC.
           CALL "G85BF225S".
           MOVE "MAIN-2" TO LF-REC.
           WRITE LF-REC.
           MOVE LINAGE-COUNTER TO WS-LC.
           DISPLAY "MAIN LC=" WS-LC.
           CLOSE LF.
           STOP RUN.
       END PROGRAM G85BF2205.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85BF225S.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT LF ASSIGN TO "G85BF225.TXT"
               ORGANIZATION IS SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD  LF IS EXTERNAL
           LINAGE IS BF-PG LINES WITH FOOTING AT 8
               LINES AT TOP 1 LINES AT BOTTOM 1.
       01  LF-REC PIC X(6).
       WORKING-STORAGE SECTION.
       01  BF-PG PIC 99 EXTERNAL.
       01  WS-LC PIC 9(4).
       PROCEDURE DIVISION.
       S1.
           MOVE "SUB--1" TO LF-REC.
           WRITE LF-REC.
           MOVE LINAGE-COUNTER TO WS-LC.
           DISPLAY "SUB LC=" WS-LC.
           EXIT PROGRAM.
       END PROGRAM G85BF225S.
