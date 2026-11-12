      *>--- conformance-case
      *> id: pb5299-value-of-table-element-sequential
      *> rules: [1985-GR-VII.3.9.4-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5299. §1985:VII.3.9.3 SR1: data-name-1 "cannot be
      *> subscripted"; a table element reference "must be subscripted
      *> except" in the contexts §1985:IV.4.3.8.2.3 SR4 lists, and a VALUE
      *> OF clause is none of them, so E (an element of T) is refused.
      *> Refused at 85.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5299SEQ.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SF ASSIGN TO "pb5299s.dat".
       DATA DIVISION.
       FILE SECTION.
       FD  SF
           VALUE OF FILE-ID E.
       01  SF-REC PIC X(10).
       WORKING-STORAGE SECTION.
       01  T. 05 E PIC X(10) OCCURS 2 TIMES.
       PROCEDURE DIVISION.
       P.  STOP RUN.
