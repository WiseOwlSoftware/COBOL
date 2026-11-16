      *>--- conformance-case
      *> id: g2-record-contains-odo-maximum
      *> rules: [2002-SR-13.16.41.2-2, 2002-GR-13.16.41.3-3,
      *>     2002-SR-13.16.41.2-5]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:13.16.41.2 SR2 - "No record description entry for the file
      *>   may specify a number of bytes greater than integer-1."
      *> §2002:13.16.41.3 GR3 - the record size is "the sum of the number
      *>   of bytes in all fixed length elementary items plus the sum of
      *>   the maximum number of bytes in any variable-length data item
      *>   subordinate to the record"; §2002:13.16.36.3 GR8: a group with
      *>   a subordinate variable-table OCCURS is a variable-length data
      *>   item.
      *> §2002:13.16.41.2 SR5 - "Data-name-1 shall describe an elementary
      *>   unsigned integer in the working-storage, local-storage, or
      *>   linkage section."
      *> All citations: python scripts/spec/cite.py --check -> OK.
      *> F: RECORD CONTAINS 12. FR = C PIC 99 (2 bytes) + T OCCURS 1 TO
      *>   10 (max 10 bytes) = 12 bytes by GR3, not greater than 12; FR2
      *>   is 5 bytes. So SR2 holds and the program compiles.  The
      *>   refused twin g2-record-contains-odo-maximum-over states 11,
      *>   which is below FR's 12-byte maximum: together they pin the
      *>   size GR3 computes at 12 (not the 3-byte minimum).
      *>   C=10 and T filled "ABCDEFGHIJ"; WRITE, then READ back:
      *>   "F=00 [10ABCDEFGHIJ]".
      *> G: RECORD IS VARYING ... DEPENDING ON WL, WL an elementary
      *>   unsigned integer in working-storage (SR5 holds). WL=5 before
      *>   the WRITE (GR11); after the READ WL holds the bytes read
      *>   (GR15): "G=00 WL=0005 [HELLO]".
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2REC01.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "g2rec01f.dat"
               ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS FS1.
           SELECT G ASSIGN TO "g2rec01g.dat"
               ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS FS2.
       DATA DIVISION.
       FILE SECTION.
       FD  F RECORD CONTAINS 12 CHARACTERS.
       01  FR.
           05 C  PIC 99.
           05 T  PIC X OCCURS 1 TO 10 DEPENDING ON C.
       01  FR2 PIC X(5).
       FD  G RECORD IS VARYING IN SIZE FROM 1 TO 20 CHARACTERS
             DEPENDING ON WL.
       01  GR PIC X(20).
       WORKING-STORAGE SECTION.
       01  FS1 PIC XX.
       01  FS2 PIC XX.
       01  WL  PIC 9(4).
       01  K   PIC 99.
       01  SRC PIC X(10) VALUE "ABCDEFGHIJ".
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT F
           MOVE 10 TO C
           PERFORM VARYING K FROM 1 BY 1 UNTIL K > 10
               MOVE SRC(K:1) TO T(K)
           END-PERFORM
           WRITE FR
           CLOSE F
           OPEN INPUT F
           MOVE 1 TO C
           READ F
           DISPLAY "F=" FS1 " [" FR "]"
           CLOSE F
           OPEN OUTPUT G
           MOVE "HELLO" TO GR
           MOVE 5 TO WL
           WRITE GR
           CLOSE G
           OPEN INPUT G
           MOVE 0 TO WL
           READ G
           DISPLAY "G=" FS2 " WL=" WL " [" GR(1:WL) "]"
           CLOSE G
           STOP RUN.
