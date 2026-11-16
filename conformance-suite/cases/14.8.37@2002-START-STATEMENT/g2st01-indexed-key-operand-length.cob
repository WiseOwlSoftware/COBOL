      *>--- conformance-case
      *> id: g2st01-indexed-key-operand-length
      *> rules: [2002-SR-14.8.37.2-6, 2002-GR-14.8.37.3-13]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> An indexed file with prime key PK PIC X(6) and alternate key NK
      *>   PIC N(6); four records R1..R4 (PK and NK AAX001, ABX002, ABY003,
      *>   BBB004). After each START a READ NEXT shows the record
      *>   positioned at (§2002:14.8.37.3 GR16, GR17e).
      *> §2002:14.8.37.2 SR6 - data-name-1 "shall reference either: a) A
      *>   data item specified as a prime or alternate record key" or b) an
      *>   item at the leftmost position of a key defined without SOURCE,
      *>   "It has the same class, category, and usage as that record key",
      *>   "Its length is not greater than the length of that record key."
      *>   Each form is ACCEPTED and positions:
      *>   SR6-PK   a) the prime key, KEY = "ABY003"          -> R3
      *>   SR6-NK   a) the alternate key NK, KEY >= N"ABX000" -> R2
      *>   SR6-PK3  b) PK3 PIC X(3) at PK's leftmost, KEY = "ABY" -> R3
      *>   SR6-NK2  b) NK2 PIC N(2) at NK's leftmost, KEY > N"AB" -> R4
      *> §2002:14.8.37.3 GR13 - with LENGTH, "If data-name-1 or record-key-
      *>   name-1 is of class alphanumeric, arithmetic-expression-1 is the
      *>   number of alphanumeric character positions; if ... of class
      *>   national, arithmetic-expression-1 is the number of national
      *>   character positions."
      *>   LEN-ALN  KEY = PK WITH LENGTH 2, PK "ABZZZZ": the first two
      *>            characters "AB" -> R2 (one position would give R1);
      *>   LEN-NAT  KEY = NK WITH LENGTH 2, NK N"ABZZZZ": two national
      *>            characters "AB" -> R2; counting 2 as bytes (one
      *>            national character "A") would give R1;
      *>   LEN-NAT6 KEY = NK WITH LENGTH 6, NK N"ABY003": the whole key
      *>            -> R3;
      *>   LEN-NAT7 KEY = NK WITH LENGTH 7: NK is 6 national character
      *>            positions, so 7 exceeds the key's length and GR14 makes
      *>            the START unsuccessful with I-O status 23 (counted in
      *>            bytes, 12, it would be accepted).
      *> Refused beside it (not the closing witness): a PIC 9(2) USAGE
      *>   NATIONAL item at NK's leftmost, class and category numeric
      *>   (§2002:8.5.2.9 item 1; kb/Work PB5625 reading B) against a key
      *>   of class and category national (SR6b item 2),
      *>   g2st02-start-numeric-national-at-national-key.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2ST01.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F1 ASSIGN TO "g2st01-indexed-key-operand-length.dat"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS DYNAMIC
               RECORD KEY IS PK
               ALTERNATE RECORD KEY IS NK
               FILE STATUS IS FS.
       DATA DIVISION.
       FILE SECTION.
       FD  F1.
       01  REC.
           05  PK              PIC X(6).
           05  PK-R REDEFINES PK.
               10  PK3         PIC X(3).
               10  FILLER      PIC X(3).
           05  NK              PIC N(6).
           05  NK-R REDEFINES NK.
               10  NK2         PIC N(2).
               10  FILLER      PIC N(4).
           05  TG              PIC X(2).
       WORKING-STORAGE SECTION.
       01  FS                  PIC XX.
       PROCEDURE DIVISION.
       MAIN-P.
           OPEN OUTPUT F1
           MOVE "AAX001" TO PK  MOVE N"AAX001" TO NK
           MOVE "R1" TO TG  WRITE REC
           MOVE "ABX002" TO PK  MOVE N"ABX002" TO NK
           MOVE "R2" TO TG  WRITE REC
           MOVE "ABY003" TO PK  MOVE N"ABY003" TO NK
           MOVE "R3" TO TG  WRITE REC
           MOVE "BBB004" TO PK  MOVE N"BBB004" TO NK
           MOVE "R4" TO TG  WRITE REC
           CLOSE F1
           OPEN INPUT F1
           MOVE "ABY003" TO PK
           START F1 KEY = PK
           PERFORM SHOW-NEXT
           DISPLAY "SR6-PK=" TG
           MOVE N"ABX000" TO NK
           START F1 KEY >= NK
           PERFORM SHOW-NEXT
           DISPLAY "SR6-NK=" TG
           MOVE "ABY" TO PK3
           START F1 KEY = PK3
           PERFORM SHOW-NEXT
           DISPLAY "SR6-PK3=" TG
           MOVE N"AB" TO NK2
           START F1 KEY > NK2
           PERFORM SHOW-NEXT
           DISPLAY "SR6-NK2=" TG
           MOVE "ABZZZZ" TO PK
           START F1 KEY = PK WITH LENGTH 2
           PERFORM SHOW-NEXT
           DISPLAY "LEN-ALN=" TG
           MOVE N"ABZZZZ" TO NK
           START F1 KEY = NK WITH LENGTH 2
           PERFORM SHOW-NEXT
           DISPLAY "LEN-NAT=" TG
           MOVE N"ABY003" TO NK
           START F1 KEY = NK WITH LENGTH 6
           PERFORM SHOW-NEXT
           DISPLAY "LEN-NAT6=" TG
           MOVE N"ABY003" TO NK
           START F1 KEY = NK WITH LENGTH 7
               INVALID KEY DISPLAY "LEN-NAT7=INVALID FS=" FS
               NOT INVALID KEY DISPLAY "LEN-NAT7=VALID FS=" FS
           END-START
           CLOSE F1
           STOP RUN.
       SHOW-NEXT.
           IF FS NOT = "00"
               DISPLAY "START-FS=" FS
               MOVE "--" TO TG
           ELSE
               READ F1 NEXT RECORD
                   AT END MOVE "EOF" TO TG
               END-READ
           END-IF.
