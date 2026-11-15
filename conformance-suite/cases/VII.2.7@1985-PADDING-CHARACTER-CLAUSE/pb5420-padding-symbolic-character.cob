      *>--- conformance-case
      *> id: pb5420-padding-symbolic-character
      *> rules: [1985-SR-VII.2.7.3-1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5420. §1985:VII.2.7.3 SR1: "Literal-1 must be a
      *> one-character nonnumeric literal." A figurative constant may
      *> stand where a format specifies a literal (§1985:IV.4.2.2.2.3),
      *> and "[ALL] symbolic-character" is one that "Represents one or
      *> more of the character specified as the value of this
      *> symbolic-character", so PADDING CHARACTER IS SYM1 and
      *> PADDING CHARACTER IS ALL SYM1 are both legal, as QUOTE is. The
      *> clause changes no I-O here, so each file reads back what was
      *> written: the program must translate and run at 85 and DISPLAY
      *> exactly the lines its .out holds. A compiler that read the
      *> word as data-name-1 (no data item SYM1) or refused ALL SYM1
      *> fails to translate it.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5420PAD.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           SYMBOLIC CHARACTERS SYM1 IS 43.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT FILE-A ASSIGN TO "PB5420A"
               ORGANIZATION IS SEQUENTIAL
               PADDING CHARACTER IS SYM1.
           SELECT FILE-B ASSIGN TO "PB5420B"
               ORGANIZATION IS SEQUENTIAL
               PADDING CHARACTER IS ALL SYM1.
       DATA DIVISION.
       FILE SECTION.
       FD  FILE-A
           LABEL RECORDS ARE STANDARD.
       01  REC-A PIC X(4).
       FD  FILE-B
           LABEL RECORDS ARE STANDARD.
       01  REC-B PIC X(4).
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT FILE-A.
           MOVE "ABCD" TO REC-A.
           WRITE REC-A.
           CLOSE FILE-A.
           OPEN INPUT FILE-A.
           READ FILE-A AT END DISPLAY "EOF A".
           DISPLAY "A " REC-A.
           CLOSE FILE-A.
           OPEN OUTPUT FILE-B.
           MOVE "WXYZ" TO REC-B.
           WRITE REC-B.
           CLOSE FILE-B.
           OPEN INPUT FILE-B.
           READ FILE-B AT END DISPLAY "EOF B".
           DISPLAY "B " REC-B.
           CLOSE FILE-B.
           STOP RUN.
