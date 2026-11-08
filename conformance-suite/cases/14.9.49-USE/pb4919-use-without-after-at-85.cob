      *>--- conformance-case
      *> id: pb4919-use-without-after-at-85
      *> rules: [FMT-14.9.49.2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4919 - the edition half of pb332_use_optional_words
      *>   (whose USE ERROR F1 omits AFTER and is accepted from 2002).
      *>   COBOL-85 underlines AFTER in the file-exception USE statement
      *>   (§1985:VII.4.6.2, page VII-50: USE AFTER STANDARD
      *>   {EXCEPTION | ERROR} PROCEDURE ON ...), so it is a required
      *>   word; the ISO/IEC 1989:2002 post-ballot text (14.8.45.1
      *>   Format 1) prints it without underlining, as §2023:14.9.49.2
      *>   does. The statement below keeps PROCEDURE, so only the AFTER
      *>   relaxation is measured. Compiled clean at 85 before the gate.
      *>   Expected: COBOL0900 at 85.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P4919NA.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "P4919NOFILE".
       DATA DIVISION.
       FILE SECTION.
       FD F.
       01 R PIC X(10).
       PROCEDURE DIVISION.
       DECLARATIVES.
       D1 SECTION.
           USE STANDARD ERROR PROCEDURE ON F.
       D1-P.
           DISPLAY "ERR".
       END DECLARATIVES.
       MAIN SECTION.
       M1.
           STOP RUN.
