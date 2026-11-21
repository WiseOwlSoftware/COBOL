      *>--- conformance-case
      *> id: 1985-SR-IV_6_4_2_3_1-p2
      *> rules: [1985-SR-IV.6.4.2.3.1-p2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:IV.6.4.2.3.1 p2 (Imperative Statements and Imperative
      *> Sentences). An output test: the program must translate and run
      *> at 85 and DISPLAY exactly the lines its .out holds, the outcome
      *> the rule requires. Placed from written and run by the PB5453
      *> reader of lineage-reads-1985/s-07.json by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P03FN.
      * 1985 IV.6.4.2.3.1 footnotes 1-7: each verb written WITHOUT
      * its optional conditional phrases is an imperative verb, so
      * it may stand where a format writes imperative-statement.
      * Every statement below is in the ON SIZE ERROR phrase of an
      * ADD (N=9, so the phrase runs). USE (a compiler directing
      * statement) supplies the exception procedure for RLF.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SF ASSIGN TO "P03FNS.TMP"
               ORGANIZATION IS SEQUENTIAL.
           SELECT RLF ASSIGN TO "P03FNR.TMP"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS DYNAMIC
               RELATIVE KEY IS RK.
       DATA DIVISION.
       FILE SECTION.
       FD SF.
       01 SF-REC PIC X(4).
       FD RLF.
       01 RLF-REC PIC X(4).
       WORKING-STORAGE SECTION.
       01 RK   PIC 9(4) VALUE 1.
       01 N    PIC 9 VALUE 9.
       01 A    PIC 99 VALUE 10.
       01 S1   PIC X(4) VALUE "AB,C".
       01 S2   PIC X(4) VALUE SPACES.
       01 S3   PIC X(4) VALUE SPACES.
       01 S4   PIC X(4) VALUE SPACES.
       PROCEDURE DIVISION.
       DECLARATIVES.
       RLF-ERR SECTION.
           USE AFTER STANDARD EXCEPTION PROCEDURE ON SF RLF.
       RLF-ERR-P.
           DISPLAY "USE RAN".
       END DECLARATIVES.
       MAIN-S SECTION.
       MAIN-P.
           OPEN OUTPUT SF RLF.
           ADD 1 TO N ON SIZE ERROR
               ADD 1 TO A
               SUBTRACT 2 FROM A
               MULTIPLY 2 BY A
               DIVIDE 2 INTO A
               COMPUTE A = A + 1
               STRING S1 DELIMITED BY SIZE INTO S2
               UNSTRING S1 DELIMITED BY "," INTO S3 S4
               MOVE "REC1" TO SF-REC
               WRITE SF-REC
               MOVE 1 TO RK
               MOVE "RRR1" TO RLF-REC
               WRITE RLF-REC
               MOVE 2 TO RK
               MOVE "RRR2" TO RLF-REC
               WRITE RLF-REC
           END-ADD.
           CLOSE SF RLF.
           DISPLAY "1 A=" A " S2=" S2 " S3=" S3 " S4=" S4.
           OPEN INPUT SF I-O RLF.
           MOVE 9 TO N.
           ADD 1 TO N ON SIZE ERROR
               READ SF
               MOVE 1 TO RK
               READ RLF
               MOVE "RRRX" TO RLF-REC
               REWRITE RLF-REC
               MOVE 2 TO RK
               DELETE RLF RECORD
               MOVE 1 TO RK
               START RLF KEY IS EQUAL TO RK
               READ RLF NEXT
           END-ADD.
           DISPLAY "2 SF=" SF-REC " RLF=" RLF-REC.
           CLOSE SF RLF.
      * CALL without its phrases is imperative; it sits in a phrase
      * that does not run here (a compile-time witness).
           MOVE 0 TO N.
           ADD 1 TO N ON SIZE ERROR
               CALL "P03NONE"
           END-ADD.
           DISPLAY "3 DONE".
           STOP RUN.
