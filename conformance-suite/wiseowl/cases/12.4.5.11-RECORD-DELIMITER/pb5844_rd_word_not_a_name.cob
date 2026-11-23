      *>--- conformance-case
      *> id: pb5844_rd_word_not_a_name
      *> rules: [FMT-12.4.5.11.2,
      *>    DOC-A.1-150]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> The RECORD DELIMITER clause's operand is STANDARD-1 or a name
      *> the implementor associates with a method (§1985:VII.2.8.4 GR3;
      *> "The implementor shall specify the names available for use as
      *> feature-name-1", §2023:12.4.5.11.3 SR2). WiseOwl COBOL's names
      *> are BINARY-SEQUENTIAL and LINE-SEQUENTIAL (docs/CONFORMANCE.md
      *> section 7, Annex A.1 item 150; kb/Work PB5844). F1, the file's
      *> own name, is neither, so the clause is no form the general
      *> format prints and the program is refused (COBOL7598). Before
      *> PB5844 any word was accepted there with a warning. The file is
      *> variable-length (RECORD CONTAINS 5 TO 10, Annex A.1 item 148),
      *> so the only rule the program breaks is the operand's.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P5844NW.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F1 ASSIGN TO "p5844nw.dat"
               ORGANIZATION IS SEQUENTIAL
               RECORD DELIMITER IS F1.
       DATA DIVISION.
       FILE SECTION.
       FD F1
           RECORD CONTAINS 5 TO 10 CHARACTERS.
       01 R1 PIC X(10).
       01 R2 PIC X(5).
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT F1
           MOVE "ABCDEFGHIJ" TO R1
           WRITE R1
           CLOSE F1
           STOP RUN.
