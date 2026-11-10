      *>--- conformance-case
      *> id: pb5086_multiple_file_reel_legal_85
      *> rules: []
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5086 - the X3.23-1985 multiple file reel rules, from
      *> the legal side. The MULTIPLE FILE TAPE clause is the source
      *> text that places F1 and F2 on one multiple file reel
      *> (§1985:VII.2.11.3 GR1, "required when more than one file
      *> shares the same physical reel of tape"), and
      *> §1985:VII.4.3.3 SR2 forbids "The EXTEND phrase ... for a
      *> multiple file reel". F3 is named in no clause, so OPEN EXTEND
      *> F3 is legal. OPEN OUTPUT and OPEN INPUT of the reel file F1
      *> are legal (the rules bar EXTEND and a non-sequential set).
      *> Expected: AAAA and BBBB from F3 (EXTEND appends), then ONE1.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P5086OK.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F1 ASSIGN TO "pb5086a.dat".
           SELECT F2 ASSIGN TO "pb5086b.dat".
           SELECT F3 ASSIGN TO "pb5086c.dat".
       I-O-CONTROL.
           MULTIPLE FILE TAPE CONTAINS F1 F2.
       DATA DIVISION.
       FILE SECTION.
       FD  F1.
       01  R1 PIC X(4).
       FD  F2.
       01  R2 PIC X(4).
       FD  F3.
       01  R3 PIC X(4).
       WORKING-STORAGE SECTION.
       01  EOF-SW PIC X VALUE "N".
       PROCEDURE DIVISION.
       P1.
           OPEN OUTPUT F1.
           MOVE "ONE1" TO R1.
           WRITE R1.
           CLOSE F1.
           OPEN OUTPUT F3.
           MOVE "AAAA" TO R3.
           WRITE R3.
           CLOSE F3.
           OPEN EXTEND F3.
           MOVE "BBBB" TO R3.
           WRITE R3.
           CLOSE F3.
           OPEN INPUT F3.
           READ F3 AT END MOVE "Y" TO EOF-SW.
           PERFORM UNTIL EOF-SW = "Y"
               DISPLAY R3
               READ F3 AT END MOVE "Y" TO EOF-SW END-READ
           END-PERFORM.
           CLOSE F3.
           OPEN INPUT F1.
           READ F1 AT END DISPLAY "EMPTY".
           DISPLAY R1.
           CLOSE F1.
           STOP RUN.
