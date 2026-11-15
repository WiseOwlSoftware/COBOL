      *>--- conformance-case
      *> id: pb5579-rewrite-file-at-85
      *> rules: [FMT-14.9.35.2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5579 - the edition half of pb5579_rewrite_file. §1985:VII.4.5.2 prints REWRITE record-name-1 [FROM
      *> identifier-1] with no FILE alternative; the 2002 format (14.8.33.1) adds it. Expected: COBOL0900 at 85, at FILE.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P5579N02.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "P5579N02.dat" ORGANIZATION IS SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD F.
       01 R PIC X(10).
       WORKING-STORAGE SECTION.
       01 W10 PIC X(10) VALUE "EXACT-TEN!".
       PROCEDURE DIVISION.
       MAIN.
           OPEN I-O F.
           REWRITE FILE F FROM W10.
           CLOSE F.
           STOP RUN.
