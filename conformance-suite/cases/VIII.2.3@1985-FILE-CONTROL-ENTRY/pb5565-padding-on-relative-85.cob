      *>--- conformance-case
      *> id: pb5565-padding-on-relative-85
      *> rules: [1985-FMT-VIII.2.3.2]
      *> source-format: fixed
      *>---
      *> kb/Work PB5565. §1985:VIII.2.3.2 prints the relative file control
      *> entry with no PADDING CHARACTER clause; the clause "specifies
      *> the character which is to be used for block padding on
      *> sequential files" (§1985:VII.2.7.4 GR1), so it belongs to the
      *> sequential entry alone. WiseOwl refuses the clause below at
      *> 85 with COBOL2912; a compiler that accepts it fails.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P5565VIIIP.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "P5565VIII"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS RANDOM
               RELATIVE KEY IS FK
               PADDING CHARACTER IS "*".
       DATA DIVISION.
       FILE SECTION.
       FD  F.
       01  FR.
           05 FA PIC XX.
       WORKING-STORAGE SECTION.
       01  FK PIC 9(2).
       PROCEDURE DIVISION.
       P1.
           STOP RUN.
