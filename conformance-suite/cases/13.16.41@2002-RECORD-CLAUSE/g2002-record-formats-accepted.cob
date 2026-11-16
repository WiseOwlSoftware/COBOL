      *>--- conformance-case
      *> id: g2002-record-formats-accepted
      *> rules: [2002-FMT-13.16.41.1]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:13.16.41.1 - the three general formats of the RECORD
      *> clause, each written with every optional word (CONTAINS, IS,
      *> IN, SIZE, CHARACTERS, ON) and again without them, in FD and
      *> SD entries. Only RECORD, VARYING, TO and DEPENDING are
      *> underlined (keys) on the printed page. A refusal of any entry
      *> fails the case; each file is then written and read back.
      *> F1 fmt 1, all words; F2 fmt 1, bare: "AAAA", "BBBB" read back.
      *> F3 fmt 2, FROM/TO/CHARACTERS/DEPENDING ON: WL = 3 when
      *>    "CCCCCC" is written, so 3 bytes go out (§2002:13.16.41.3
      *>    GR13a) and the READ sets WL = 3 (GR15): "03 [CCC]".
      *> F6 fmt 2, no FROM, DEPENDING without ON: the minimum is the
      *>    least record described, R6A's 1 byte (GR9), so WL6 = 5 is
      *>    in range -> "05 [FFFFF]". F5 fmt 2, "RECORD VARYING"
      *>    alone: "EEE".
      *> F4 fmt 3, all words; F8 fmt 3, bare: the 2-byte records "DD"
      *>    and "HH" read back.
      *> S1 SD fmt 1 bare: "ZZZ", "YYY" sorted "YYY", "ZZZ". SV SD
      *>    fmt 2 DEPENDING ON SL: "BBBB" (SL 4)
      *>    and "AA" (SL 2) released, RETURNed in key order with SL set
      *>    (GR15): "AA 02", "BBBB 04".
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2REC05.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F1 ASSIGN TO "g2rec01-1.dat".
           SELECT F2 ASSIGN TO "g2rec01-2.dat".
           SELECT F3 ASSIGN TO "g2rec01-3.dat".
           SELECT F4 ASSIGN TO "g2rec01-4.dat".
           SELECT F5 ASSIGN TO "g2rec01-5.dat".
           SELECT F6 ASSIGN TO "g2rec01-6.dat".
           SELECT F8 ASSIGN TO "g2rec01-8.dat".
           SELECT S1 ASSIGN TO "g2rec01-s1.srt".
           SELECT SV ASSIGN TO "g2rec01-sv.srt".
       DATA DIVISION.
       FILE SECTION.
       FD  F1 RECORD CONTAINS 4 CHARACTERS.
       01  R1 PIC X(4).
       FD  F2 RECORD 4.
       01  R2 PIC X(4).
       FD  F3 RECORD IS VARYING IN SIZE FROM 1 TO 6 CHARACTERS
               DEPENDING ON WL.
       01  R3 PIC X(6).
       FD  F6 RECORD VARYING TO 6 DEPENDING WL6.
       01  R6A PIC X(1).
       01  R6 PIC X(6).
       FD  F5 RECORD VARYING.
       01  R5 PIC X(3).
       FD  F4 RECORD CONTAINS 2 TO 6 CHARACTERS.
       01  R4A PIC X(2).
       01  R4B PIC X(6).
       FD  F8 RECORD 2 TO 6.
       01  R8A PIC X(2).
       01  R8B PIC X(6).
       SD  S1 RECORD 3.
       01  S1-REC PIC X(3).
       SD  SV RECORD IS VARYING IN SIZE FROM 2 TO 5 CHARACTERS
               DEPENDING ON SL.
       01  SV-REC.
           05 SV-K PIC X(2).
           05 FILLER PIC X(3).
       WORKING-STORAGE SECTION.
       01  WL  PIC 99 VALUE 3.
       01  WL6 PIC 99 VALUE 5.
       01  SL  PIC 99.
       01  EOF PIC 9.
       PROCEDURE DIVISION.
       MAIN-P.
           OPEN OUTPUT F1 F2 F3 F4 F5 F6 F8
           MOVE "AAAA" TO R1 WRITE R1
           MOVE "BBBB" TO R2 WRITE R2
           MOVE "CCCCCC" TO R3 WRITE R3
           MOVE "DD" TO R4A WRITE R4A
           MOVE "EEE" TO R5 WRITE R5
           MOVE "FFFFFF" TO R6 WRITE R6
           MOVE "HH" TO R8A WRITE R8A
           CLOSE F1 F2 F3 F4 F5 F6 F8
           OPEN INPUT F1 F2 F3 F4 F5 F6 F8
           MOVE 0 TO WL WL6
           READ F1 READ F2 READ F3 READ F4 READ F5 READ F6 READ F8
           DISPLAY "F1 [" R1 "] F2 [" R2 "]"
           DISPLAY "F3 " WL " [" R3(1:WL) "]"
           DISPLAY "F6 " WL6 " [" R6(1:WL6) "] F5 [" R5 "]"
           DISPLAY "F4 [" R4A "] F8 [" R8A "]"
           CLOSE F1 F2 F3 F4 F5 F6 F8
           SORT S1 ON ASCENDING KEY S1-REC
               INPUT PROCEDURE IS FEED-1 OUTPUT PROCEDURE IS DRAIN-1
           SORT SV ON ASCENDING KEY SV-K
               INPUT PROCEDURE IS FEED-V OUTPUT PROCEDURE IS DRAIN-V
           STOP RUN.
       FEED-1.
           MOVE "ZZZ" TO S1-REC RELEASE S1-REC
           MOVE "YYY" TO S1-REC RELEASE S1-REC.
       DRAIN-1.
           MOVE 0 TO EOF
           PERFORM UNTIL EOF = 1
               RETURN S1 AT END MOVE 1 TO EOF
                   NOT AT END DISPLAY "S1 " S1-REC
               END-RETURN
           END-PERFORM.
       FEED-V.
           MOVE "BBBB" TO SV-REC MOVE 4 TO SL RELEASE SV-REC
           MOVE "AA" TO SV-REC MOVE 2 TO SL RELEASE SV-REC.
       DRAIN-V.
           MOVE 0 TO EOF
           PERFORM UNTIL EOF = 1
               MOVE 0 TO SL
               RETURN SV AT END MOVE 1 TO EOF
                   NOT AT END DISPLAY "SV " SV-REC(1:SL) " " SL
               END-RETURN
           END-PERFORM.
