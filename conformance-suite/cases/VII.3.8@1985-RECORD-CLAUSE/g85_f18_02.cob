      *>--- conformance-case
      *> id: g85_f18_02
      *> rules: [1985-GR-VII.3.8.4-5, 1985-GR-VII.3.8.4-6,
      *>    1985-GR-VII.3.8.4-7, 1985-GR-VII.3.8.4-10]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VII.3.8.4 GR5 - a record description's size is the
      *> sum of its elementary items "excluding redefinitions and
      *> renamings"; a table counts its minimum elements for the
      *> minimum size (a) and its maximum elements for the maximum
      *> size (b). §1985:VII.3.8.4 GR6 and GR7 - with no integer-2
      *> (integer-3) the file's minimum (maximum) is the least
      *> (greatest) size described for a record in the file.
      *> §1985:VII.3.8.4 GR10 - the record written is LA (LB) long,
      *> and outside [minimum, maximum] the WRITE is unsuccessful;
      *> §1985:VII.1.3.5 - that boundary violation is I-O status 44.
      *> DERIVATION.
      *>   RA = RA-A X(3) + RA-N 9 + RA-T X(2) OCCURS 1 TO 4; RA-B
      *>   (REDEFINES) and RA-R (RENAMES) are excluded. Minimum
      *>   3+1+2*1 = 6, maximum 3+1+2*4 = 12 (GR5 a, b): the file's
      *>   bounds (GR6, GR7: its only record).
      *>   LA=5 -> 44, LA=6 -> 00, LA=12 -> 00, LA=13 -> 44.
      *>   Counting RA-B makes the minimum 9 and LA=6 fail; counting
      *>   RA-R, 10; 4 elements for the minimum, 12; 1 element for
      *>   the maximum makes LA=12 fail.
      *>   FB: RB1 X(15) then RB2 X(7), TO 20 with no FROM: the
      *>   minimum is the LEAST record, 7 (GR6), not the first one.
      *>   LB=6 -> 44, LB=7 -> 00.
      *>   Read back, the records written are 6 and 12 long:
      *>   A1 06 ABC411, A2 12 ABC411223344; B1 07 GGGGGGG.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85F1802.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT FA ASSIGN TO "g85f1802a.dat"
               ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS FS.
           SELECT FB ASSIGN TO "g85f1802b.dat"
               ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS FS.
       DATA DIVISION.
       FILE SECTION.
       FD  FA
           RECORD IS VARYING IN SIZE DEPENDING ON LA.
       01  RA.
           05 RA-A PIC X(3).
           05 RA-B REDEFINES RA-A PIC 9(3).
           05 RA-N PIC 9.
           05 RA-T PIC X(2) OCCURS 1 TO 4 TIMES DEPENDING ON RA-N.
       66  RA-R RENAMES RA-A THRU RA-N.
       FD  FB
           RECORD IS VARYING IN SIZE TO 20 CHARACTERS
               DEPENDING ON LB.
       01  RB1 PIC X(15).
       01  RB2 PIC X(7).
       WORKING-STORAGE SECTION.
       01  FS PIC XX.
       01  LA PIC 99.
       01  LB PIC 99.
       01  K  PIC 9.
       PROCEDURE DIVISION.
       P0.
           OPEN OUTPUT FA.
           MOVE "ABC" TO RA-A.
           MOVE 4 TO RA-N.
           MOVE "11" TO RA-T (1).
           MOVE "22" TO RA-T (2).
           MOVE "33" TO RA-T (3).
           MOVE "44" TO RA-T (4).
           MOVE 5 TO LA.
           WRITE RA.
           DISPLAY "FA 05 " FS.
           MOVE 6 TO LA.
           WRITE RA.
           DISPLAY "FA 06 " FS.
           MOVE 12 TO LA.
           WRITE RA.
           DISPLAY "FA 12 " FS.
           MOVE 13 TO LA.
           WRITE RA.
           DISPLAY "FA 13 " FS.
           CLOSE FA.
           OPEN INPUT FA.
           MOVE 0 TO K.
       P1.
           READ FA AT END GO TO P2.
           ADD 1 TO K.
           DISPLAY "A" K " " LA " " RA (1:LA).
           GO TO P1.
       P2.
           DISPLAY "FA END " K.
           CLOSE FA.
           OPEN OUTPUT FB.
           MOVE ALL "G" TO RB1.
           MOVE 6 TO LB.
           WRITE RB1.
           DISPLAY "FB 06 " FS.
           MOVE 7 TO LB.
           WRITE RB1.
           DISPLAY "FB 07 " FS.
           CLOSE FB.
           OPEN INPUT FB.
           MOVE 0 TO K.
       P3.
           READ FB AT END GO TO P4.
           ADD 1 TO K.
           DISPLAY "B" K " " LB " " RB1 (1:LB).
           GO TO P3.
       P4.
           DISPLAY "FB END " K.
           CLOSE FB.
           STOP RUN.
