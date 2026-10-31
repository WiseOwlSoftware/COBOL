      *>--- conformance-case
      *> id: pb4206-perform-reserved-verb-as-procedure-name
      *> rules: [SR-8.3.2.1-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4206 - 8.3.2.1 1): "Reserved words shall not be used as
      *> user-defined words or system-names." A procedure-name is a
      *> user-defined word (8.3.2.2), and 8.9 reserves RECEIVE at COBOL-85,
      *> COBOL-2002 and COBOL-2023, so PERFORM RECEIVE names a procedure
      *> with a reserved word. Expected: COBOL0901 naming RECEIVE. It fails
      *> when the procedure-name slot cannot hold the word and the parse
      *> reads it as the RECEIVE verb of an inline PERFORM instead: the
      *> error then falls at the period ("unexpected '.'") and the word is
      *> never named. No paragraph is named RECEIVE, so 14.9.28.3 SR12 is
      *> broken too and COBOL1639 is reported beside it. The 2014 twin,
      *> where 8.9 leaves the word free, is
      *> pb4206_free_verbs_as_procedure_names.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4206PFM.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-A PIC X(8).
       PROCEDURE DIVISION.
       MAIN-P.
           PERFORM RECEIVE.
           STOP RUN.
