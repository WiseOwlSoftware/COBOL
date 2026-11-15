      *>--- conformance-case
      *> id: pb5565-padding-on-indexed-85
      *> rules: [1985-FMT-IX.2.3.2]
      *> source-format: fixed
      *>---
      *> kb/Work PB5565. §1985:IX.2.3.2 prints the indexed file control
      *> entry with no PADDING CHARACTER clause; the clause "specifies
      *> the character which is to be used for block padding on
      *> sequential files" (§1985:VII.2.7.4 GR1), so it belongs to the
      *> sequential entry alone. WiseOwl refuses the clause below at
      *> 85 with COBOL2912; a compiler that accepts it fails.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P5565IXP.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "P5565IX"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS DYNAMIC
               RECORD KEY IS FK
               PADDING CHARACTER IS "*".
       DATA DIVISION.
       FILE SECTION.
       FD  F.
       01  FR.
           05 FK PIC XX.
           05 FA PIC XX.
       PROCEDURE DIVISION.
       P1.
           STOP RUN.
