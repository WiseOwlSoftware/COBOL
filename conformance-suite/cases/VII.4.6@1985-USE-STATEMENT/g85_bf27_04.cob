      *>--- conformance-case
      *> id: g85_bf27_04
      *> rules: [1985-SR-VII.4.6.3-4]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VII.4.6.3 SR4 - "The words ERROR and EXCEPTION are
      *> synonymous and may be used interchangeably." Two sequential
      *> files, declared alike and driven alike; one declarative is
      *> spelled ERROR, the other EXCEPTION. If either spelling were not
      *> the one USE statement form, that program would be refused or
      *> its handler line would be missing.
      *> DERIVATION.
      *>  Each file holds one record; the second READ has no AT END
      *>  phrase, so §1985:VII.4.4.4 GR10 a/c put the at end value 10
      *>  in the I-O status (VII.1.3.5 "I-O Status = 10") and execute
      *>  the USE procedure for the file (VII.4.6.4 GR5a), which shows
      *>  HANDLER-VIA-...=10. Status 10 is not critical (VII.1.3.5:
      *>  only 3x, 4x and some 9x are), so VII.4.6.4 GR6 returns
      *>  control to the statement after the READ: AFTER-...=10. The
      *>  ERROR and EXCEPTION legs are identical but for the tag.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85BF2704.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F1 ASSIGN TO "g85bf2704a.dat"
               ORGANIZATION IS SEQUENTIAL
               ACCESS MODE IS SEQUENTIAL
               FILE STATUS IS ST1.
           SELECT F2 ASSIGN TO "g85bf2704b.dat"
               ORGANIZATION IS SEQUENTIAL
               ACCESS MODE IS SEQUENTIAL
               FILE STATUS IS ST2.
       DATA DIVISION.
       FILE SECTION.
       FD  F1.
       01  R1 PIC X(4).
       FD  F2.
       01  R2 PIC X(4).
       WORKING-STORAGE SECTION.
       01  ST1 PIC XX VALUE "??".
       01  ST2 PIC XX VALUE "??".
       PROCEDURE DIVISION.
       DECLARATIVES.
       SPELLED-ERROR-SECT SECTION.
           USE AFTER STANDARD ERROR PROCEDURE ON F1.
       SPELLED-ERROR-PARA.
           DISPLAY "HANDLER-VIA-ERROR=" ST1.
       SPELLED-EXCEPTION-SECT SECTION.
           USE AFTER STANDARD EXCEPTION PROCEDURE ON F2.
       SPELLED-EXCEPTION-PARA.
           DISPLAY "HANDLER-VIA-EXCEPTION=" ST2.
       END DECLARATIVES.
       MAIN-SECT SECTION.
       P-CREATE.
           OPEN OUTPUT F1 F2.
           MOVE "AAAA" TO R1.
           WRITE R1.
           MOVE "BBBB" TO R2.
           WRITE R2.
           CLOSE F1 F2.
       P-READ.
           OPEN INPUT F1 F2.
           READ F1.
           DISPLAY "F1-REC=" R1.
           READ F1.
           DISPLAY "AFTER-F1=" ST1.
           READ F2.
           DISPLAY "F2-REC=" R2.
           READ F2.
           DISPLAY "AFTER-F2=" ST2.
           CLOSE F1 F2.
           DISPLAY "DONE".
           STOP RUN.
