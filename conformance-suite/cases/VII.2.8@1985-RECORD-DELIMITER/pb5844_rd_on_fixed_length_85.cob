      *>--- conformance-case
      *> id: pb5844_rd_on_fixed_length_85
      *> rules: [1985-SR-VII.2.8.3-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VII.2.8.3 SR1: "The RECORD DELIMITER clause may be
      *> specified only for variable length records." The file's RECORD
      *> clause is format 1, and "Format 1 is used to specify fixed
      *> length records" (§1985:VII.3.8.4 GR3), so the clause is written
      *> for fixed length records and the program is refused
      *> (COBOL7599). The operand is STANDARD-1, the phrase this
      *> implementation declines as a reel-device element
      *> (§1985:I.1.5.2.5.1, §1985:C.3): that licence reaches the
      *> phrase, not the clause's placement, so the decline does not
      *> excuse the rule (kb/Work PB5844). A compiler that accepts it
      *> fails the test.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P5844F8.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F1 ASSIGN TO "p5844f8.dat"
               ORGANIZATION IS SEQUENTIAL
               RECORD DELIMITER IS STANDARD-1.
       DATA DIVISION.
       FILE SECTION.
       FD F1
           RECORD CONTAINS 10 CHARACTERS.
       01 R1 PIC X(10).
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT F1
           MOVE "ABCDEFGHIJ" TO R1
           WRITE R1
           CLOSE F1
           STOP RUN.
