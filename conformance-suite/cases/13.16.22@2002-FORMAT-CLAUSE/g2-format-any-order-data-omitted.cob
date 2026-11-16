      *>--- conformance-case
      *> id: g2-format-any-order-data-omitted
      *> rules: [2002-FMT-13.16.22.1]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:13.16.22.1 - the FORMAT general format encloses BIT,
      *> CHARACTER and NUMERIC in braces with choice indicators and
      *> follows them with DATA, not underlined (PDF page 285 rendered).
      *> §2002:5.1.5.3: within braces "one or more of the alternatives
      *> contained within the choice indicators shall be specified" and
      *> "The alternatives may be specified in any order."
      *> §2002:5.1.2: optional words are "not underlined in general
      *> formats. They may be written to add clarity", so DATA may be
      *> omitted. F1 writes NUMERIC before CHARACTER and omits DATA; F2
      *> writes CHARACTER alone and omits DATA. Each file is written
      *> and read back; by §2002:13.16.22.3 GR9 and GR11 ("restored to
      *> the same internal representation when read with the same
      *> FORMAT clause") every item returns as written:
      *> F1 HELLO -012 -0345 (S9(3) -12 and S9(4) COMP -345 through
      *> PIC -999 and PIC -9999), F2 0042 ABC.
      *> Why it can fail: the clause is refused in reverse order or
      *> without DATA, or an alternative written second is dropped and
      *> its items are not restored.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2FMT03.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F1 ASSIGN TO "G2FMT01A.dat"
               ORGANIZATION IS SEQUENTIAL.
           SELECT F2 ASSIGN TO "G2FMT01B.dat"
               ORGANIZATION IS SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD  F1 RECORD IS VARYING IN SIZE FROM 1 TO 60 CHARACTERS
           FORMAT NUMERIC CHARACTER.
       01  F1-REC.
           05  F1-TEXT  PIC X(5).
           05  F1-ZONED PIC S9(3).
           05  F1-BIN   PIC S9(4) COMP.
       FD  F2 RECORD IS VARYING IN SIZE FROM 1 TO 60 CHARACTERS
           FORMAT CHARACTER.
       01  F2-REC.
           05  F2-NUM   PIC 9(4).
           05  F2-TXT   PIC X(3).
       WORKING-STORAGE SECTION.
       01  WS-E3 PIC -999.
       01  WS-E4 PIC -9999.
       PROCEDURE DIVISION.
       P1.
           OPEN OUTPUT F1.
           MOVE "HELLO" TO F1-TEXT.
           MOVE -12 TO F1-ZONED.
           MOVE -345 TO F1-BIN.
           WRITE F1-REC.
           CLOSE F1.
           INITIALIZE F1-REC.
           OPEN INPUT F1.
           READ F1.
           MOVE F1-ZONED TO WS-E3.
           MOVE F1-BIN TO WS-E4.
           DISPLAY "F1 " F1-TEXT " " WS-E3 " " WS-E4.
           CLOSE F1.
           OPEN OUTPUT F2.
           MOVE 42 TO F2-NUM.
           MOVE "ABC" TO F2-TXT.
           WRITE F2-REC.
           CLOSE F2.
           INITIALIZE F2-REC.
           OPEN INPUT F2.
           READ F2.
           DISPLAY "F2 " F2-NUM " " F2-TXT.
           CLOSE F2.
           STOP RUN.
