      *>--- conformance-case
      *> id: pb5844_rd_on_fixed_length
      *> rules: [SR-12.4.5.11.3-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2023:12.4.5.11.3 SR1: "The RECORD DELIMITER clause may be
      *> specified only for variable-length records." The file's RECORD
      *> clause is format 1, and "Format 1 is used to specify
      *> fixed-length records" (§2023:13.18.43.4 GR6), so the clause is
      *> written for fixed-length records and the program is refused
      *> (COBOL7599), whichever operand it names: here BINARY-SEQUENTIAL,
      *> an implementor-name this implementation defines, so the
      *> refusal is the placement rule's and not the operand's (kb/Work
      *> PB5844). A compiler that accepts it fails the test.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P5844FX.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F1 ASSIGN TO "p5844fx.dat"
               ORGANIZATION IS SEQUENTIAL
               RECORD DELIMITER IS BINARY-SEQUENTIAL.
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
