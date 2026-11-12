      *>--- conformance-case
      *> id: pb5311-external-reserve-differs-from-default
      *> rules: [1985-GR-VIII.2.3.4-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5311. RESERVE 3 AREAS in one entry and no RESERVE
      *> clause (the implementor's one area, §1985:VII.2.9.3 GR1) in the
      *> other are not "The same value for integer-1 in the RESERVE
      *> clause" (§1985:VIII.2.3.4 GR1): COBOL6847 at 85.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5311NMAIN.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT OPTIONAL REL-FILE ASSIGN TO "PB5311NF"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS SEQUENTIAL
               RELATIVE KEY IS REL-KEY.
       DATA DIVISION.
       FILE SECTION.
       FD  REL-FILE IS EXTERNAL.
       01  REL-REC PIC X(4).
       WORKING-STORAGE SECTION.
       01  REL-KEY PIC 9(4) EXTERNAL.
       PROCEDURE DIVISION.
       P-MAIN.
           CALL "PB5311NSUB"
           STOP RUN.
       END PROGRAM PB5311NMAIN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5311NSUB.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT OPTIONAL REL-FILE ASSIGN TO "PB5311NF"
               RESERVE 3 AREAS
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS SEQUENTIAL
               RELATIVE KEY IS REL-KEY.
       DATA DIVISION.
       FILE SECTION.
       FD  REL-FILE IS EXTERNAL.
       01  REL-REC PIC X(4).
       WORKING-STORAGE SECTION.
       01  REL-KEY PIC 9(4) EXTERNAL.
       PROCEDURE DIVISION.
       P-SUB.
           EXIT PROGRAM.
       END PROGRAM PB5311NSUB.
