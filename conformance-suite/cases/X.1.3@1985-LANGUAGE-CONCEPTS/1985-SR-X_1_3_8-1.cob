      *>--- conformance-case
      *> id: 1985-SR-X_1_3_8-1
      *> rules: [1985-SR-X.1.3.8-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:X.1.3.8 SR1 (LANGUAGE CONCEPTS). A refusal: the rule
      *> forbids line 44 of the program below, and WiseOwl refuses it at
      *> 85 with COBOL6170 there; a compiler that accepts it fails.
      *> Placed from written and run by the PB5453 reader of
      *> lineage-reads-1985/s-15.json by scripts/spec/place_witnesses.py
      *> (kb/Work PB4995).
      * s-15 refusal family r: inner names the container cd-name CDIN
       IDENTIFICATION DIVISION.
       PROGRAM-ID. S15R.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           UPSI-7 ON STATUS IS U7-ON.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  LD          PIC X(5) VALUE "LOCAL".
       01  GD GLOBAL   PIC X(5) VALUE "GLOBL".
       COMMUNICATION SECTION.
       CD  CDIN FOR INPUT
           SYMBOLIC QUEUE IS IN-QUEUE
           SYMBOLIC SOURCE IS IN-SOURCE
           TEXT LENGTH IS IN-LEN
           END KEY IS IN-END
           STATUS KEY IS IN-STATUS
           MESSAGE COUNT IS IN-COUNT.
       01  CDIN-REC.
           05  CDIN-K  PIC X.
               88  CDIN-C VALUE "Y".
           05  FILLER  PIC X(86).
       PROCEDURE DIVISION.
       MAIN-SEC SECTION.
       MAIN-P.
           MOVE "N" TO CDIN-K.
           IF CDIN-C DISPLAY "OUT-CDIN-C" END-IF.
           MOVE "Q" TO IN-QUEUE.
           DISPLAY "OUT-CTL" GD.
           CALL "S15RIN".
           STOP RUN.
       OUT-SEC SECTION.
       OUT-ONLY-P.
           DISPLAY "OUT-ONLY-P".
       IDENTIFICATION DIVISION.
       PROGRAM-ID. S15RIN.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  W           PIC X(87) VALUE SPACES.
       PROCEDURE DIVISION.
       IN-SEC SECTION.
       IN-P.
           ACCEPT CDIN MESSAGE COUNT.
           DISPLAY "IN-W=" W (1:5).
           EXIT PROGRAM.
       IN2-SEC SECTION.
       IN-ONLY-P.
           DISPLAY "IN-ONLY-P".
       END PROGRAM S15RIN.
       END PROGRAM S15R.
