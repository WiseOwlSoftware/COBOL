*>--- conformance-case
*> id: pb344_read_previous_indexed_2002
*> rules: [GR-14.9.30.4-21]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB344 — the ISO edition the compilation targets reaches the file connectors and the
      *> generated USE-declarative selector.  Until PB344 no edition reached either, so the 2023 rules
      *> were served to --std 85, 2002 and 2014 as well.
      *>
      *> THE RULES, and every expected value below derived from them (no observation):
      *>  14.9.30.4 GR21, the "When the file is an indexed file" block, rule d) 3 — "If no such record is
      *>    found or PREVIOUS is specified and the previous operation on the file was an OPEN statement, the
      *>    at end condition exists and execution proceeds as indicated in General rule 24."   (P1 at 2023, P3)
      *>  14.9.30.4 GR24 a)/c) — the at end condition sets '10' and transfers control to the AT END
      *>    imperative.                                                                        (P1 at 2023, P3)
      *>  14.9.27.4 GR14 — OPEN INPUT sets the file position indicator to "the characters that have the lowest
      *>    ordinal position in the collating sequence associated with the file", so the first record made
      *>    available under the key of reference is the LOWEST key, K002.                      (P1, P2)
      *>  14.9.41.4 GR17 / GR9 — START positions the indicator at the first record whose key satisfies the
      *>    comparison; a later READ NEXT makes that record available.                         (P4A, P4B)
      *>
      *> EDITIONS.  Rule d) 3 IS THE 2023 RULE; at 2002 the text is §2002:14.8.29.3 GR18c2 and §2002:14.8.26.3 GR12: OPEN sets the file position
      *> indicator to "the characters that have the lowest ordinal position in the collating sequence", and a
      *> PREVIOUS selects "the first existing record ... whose record number or key value is less than or equal to
      *> the file position indicator".  No key here is that low, so P1 is the at end condition, '10' (the
      *> standard's own NOTE: "normally get an at end condition for indexed"), exactly as at 2023 (kb/Work PB5639
      *> overturned kb/Work PB344, which derived the prior rule from Annex E.2 item 22 alone).  P5 is where the
      *> editions part: a record whose key IS the lowest value (LOW-VALUES) satisfies "less than or equal", so
      *> here PREVIOUS makes it available, where 2023 raises the at end condition whatever the file holds.
      *> VERSION_CHANGE_REFERENCE row 29; WiseOwl.Cobol.Runtime.DialectBehavior.IndexedReadPreviousAfterOpenAtEnd.
      *> INDEXED ONLY: the relative block prints rule b) unamended (kb/Work PB343,
      *> tests/conformance/*/pb343_read_previous_relative.cob).  READ ... PREVIOUS is a 2002 introduction, so
      *> there is no --std 85 copy (negative/pb334-read-previous-85 owns that rejection).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB344P02.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT IDX ASSIGN TO "pb344p02.dat"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS DYNAMIC
               RECORD KEY IS I-K
               FILE STATUS IS I-ST.
           SELECT LOWX ASSIGN TO "pb344p02l.dat"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS DYNAMIC
               RECORD KEY IS L-K
               FILE STATUS IS L-ST.
       DATA DIVISION.
       FILE SECTION.
       FD IDX.
       01 I-REC.
          05 I-K PIC X(4).
          05 I-V PIC X(6).
       FD LOWX.
       01 L-REC.
          05 L-K PIC X(4).
          05 L-V PIC X(6).
       WORKING-STORAGE SECTION.
       01 I-ST PIC XX.
       01 L-ST PIC XX.
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT IDX
           MOVE "K002" TO I-K MOVE "BBBBBB" TO I-V WRITE I-REC
           MOVE "K004" TO I-K MOVE "DDDDDD" TO I-V WRITE I-REC
           MOVE "K006" TO I-K MOVE "FFFFFF" TO I-V WRITE I-REC
           CLOSE IDX
      *> P1 — READ ... PREVIOUS as the FIRST operation after OPEN INPUT (2002+: at end unless a key is the lowest value; see P5).
           OPEN INPUT IDX
           READ IDX PREVIOUS RECORD
               AT END DISPLAY "P1=ATEND|" I-ST
               NOT AT END DISPLAY "P1=" I-K "|" I-ST
           END-READ
           CLOSE IDX
      *> P2 — the NEXT control from the same position: the lowest key, at every edition.
           OPEN INPUT IDX
           READ IDX NEXT RECORD
               AT END DISPLAY "P2=ATEND|" I-ST
               NOT AT END DISPLAY "P2=" I-K "|" I-ST
           END-READ
      *> P3 — rule d) 3's OTHER leg, "if no such record is found": a PREVIOUS whose previous operation was a
      *>      successful READ of the lowest record finds nothing.  At end at EVERY edition.
           READ IDX PREVIOUS RECORD
               AT END DISPLAY "P3=ATEND|" I-ST
               NOT AT END DISPLAY "P3=" I-K "|" I-ST
           END-READ
           CLOSE IDX
      *> P4 — the backward walk itself, unchanged by the edition: START to K006, READ NEXT makes it available,
      *>      READ PREVIOUS then makes K004 available.
           OPEN INPUT IDX
           MOVE "K006" TO I-K
           START IDX KEY IS EQUAL TO I-K
               INVALID KEY DISPLAY "P4=STARTFAIL|" I-ST
           END-START
           READ IDX NEXT RECORD
               AT END DISPLAY "P4A=ATEND|" I-ST
               NOT AT END DISPLAY "P4A=" I-K "|" I-ST
           END-READ
           READ IDX PREVIOUS RECORD
               AT END DISPLAY "P4B=ATEND|" I-ST
               NOT AT END DISPLAY "P4B=" I-K "|" I-ST
           END-READ
           CLOSE IDX
      *> P5 — a file whose lowest key IS the lowest-ordinal value OPEN sets the indicator to (LOW-VALUES), beside K002.
           OPEN OUTPUT LOWX
           MOVE LOW-VALUES TO L-K MOVE "AAAAAA" TO L-V WRITE L-REC
           MOVE "K002" TO L-K MOVE "BBBBBB" TO L-V WRITE L-REC
           CLOSE LOWX
           OPEN INPUT LOWX
           READ LOWX PREVIOUS RECORD
               AT END DISPLAY "P5=ATEND|" L-ST
               NOT AT END DISPLAY "P5=" L-V "|" L-ST
           END-READ
           CLOSE LOWX
           STOP RUN.
