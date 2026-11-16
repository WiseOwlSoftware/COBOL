      *>--- conformance-case
      *> id: g85_f17_01
      *> rules: [1985-FMT-VII.2.5.2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VII.2.5.2 - FILE STATUS IS data-name-1, in which only
      *> STATUS is a key word: FILE and IS are optional. Four files use
      *> the four spellings (FILE STATUS IS, STATUS, FILE STATUS, STATUS
      *> IS). Each status item is set to XX before each statement, so
      *> each 00 or 10 shows the clause took effect (§1985:VII.2.5.4 GR1;
      *> 00 and 10 from §1985:VII.1.3.5). A spelling the compiler refuses
      *> fails the translation; one it ignores leaves XX in its line.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85F1701.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT FA ASSIGN TO "G85F17A.DAT"
               FILE STATUS IS ST-A.
           SELECT FB ASSIGN TO "G85F17B.DAT"
               STATUS ST-B.
           SELECT FC ASSIGN TO "G85F17C.DAT"
               FILE STATUS ST-C.
           SELECT FD-FILE ASSIGN TO "G85F17D.DAT"
               STATUS IS ST-D.
       DATA DIVISION.
       FILE SECTION.
       FD  FA.
       01  FA-REC PIC X(4).
       FD  FB.
       01  FB-REC PIC X(4).
       FD  FC.
       01  FC-REC PIC X(4).
       FD  FD-FILE.
       01  FD-REC PIC X(4).
       WORKING-STORAGE SECTION.
       01  ST-A PIC XX.
       01  ST-B PIC XX.
       01  ST-C PIC XX.
       01  ST-D PIC XX.
       01  R1 PIC XX.
       01  R2 PIC XX.
       01  R3 PIC XX.
       01  R4 PIC XX.
       PROCEDURE DIVISION.
       P1.
           MOVE "XX" TO ST-A.
           OPEN OUTPUT FA.
           MOVE ST-A TO R1.
           MOVE "AAAA" TO FA-REC.
           WRITE FA-REC.
           CLOSE FA.
           MOVE "XX" TO ST-A.
           OPEN INPUT FA.
           MOVE ST-A TO R2.
           MOVE "XX" TO ST-A.
           READ FA AT END MOVE "EOF!" TO FA-REC.
           MOVE ST-A TO R3.
           DISPLAY "A RECORD " FA-REC.
           MOVE "XX" TO ST-A.
           READ FA AT END MOVE "EOF!" TO FA-REC.
           MOVE ST-A TO R4.
           CLOSE FA.
           DISPLAY "A " R1 " " R2 " " R3 " " R4.
           MOVE "XX" TO ST-B.
           OPEN OUTPUT FB.
           MOVE ST-B TO R1.
           MOVE "BBBB" TO FB-REC.
           WRITE FB-REC.
           CLOSE FB.
           MOVE "XX" TO ST-B.
           OPEN INPUT FB.
           MOVE ST-B TO R2.
           MOVE "XX" TO ST-B.
           READ FB AT END MOVE "EOF!" TO FB-REC.
           MOVE ST-B TO R3.
           MOVE "XX" TO ST-B.
           READ FB AT END MOVE "EOF!" TO FB-REC.
           MOVE ST-B TO R4.
           CLOSE FB.
           DISPLAY "B " R1 " " R2 " " R3 " " R4.
           MOVE "XX" TO ST-C.
           OPEN OUTPUT FC.
           MOVE ST-C TO R1.
           MOVE "CCCC" TO FC-REC.
           WRITE FC-REC.
           CLOSE FC.
           MOVE "XX" TO ST-C.
           OPEN INPUT FC.
           MOVE ST-C TO R2.
           MOVE "XX" TO ST-C.
           READ FC AT END MOVE "EOF!" TO FC-REC.
           MOVE ST-C TO R3.
           MOVE "XX" TO ST-C.
           READ FC AT END MOVE "EOF!" TO FC-REC.
           MOVE ST-C TO R4.
           CLOSE FC.
           DISPLAY "C " R1 " " R2 " " R3 " " R4.
           MOVE "XX" TO ST-D.
           OPEN OUTPUT FD-FILE.
           MOVE ST-D TO R1.
           MOVE "DDDD" TO FD-REC.
           WRITE FD-REC.
           CLOSE FD-FILE.
           MOVE "XX" TO ST-D.
           OPEN INPUT FD-FILE.
           MOVE ST-D TO R2.
           MOVE "XX" TO ST-D.
           READ FD-FILE AT END MOVE "EOF!" TO FD-REC.
           MOVE ST-D TO R3.
           MOVE "XX" TO ST-D.
           READ FD-FILE AT END MOVE "EOF!" TO FD-REC.
           MOVE ST-D TO R4.
           CLOSE FD-FILE.
           DISPLAY "D " R1 " " R2 " " R3 " " R4.
           STOP RUN.
