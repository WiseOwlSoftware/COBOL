      *>--- conformance-case
      *> id: pb4206_free_verbs_as_procedure_names
      *> rules: [SR-8.3.2.1-1]
      *> accept: [2014]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4206 - the edition half of pb4206-perform-reserved-verb-
      *> as-procedure-name. RECEIVE and SEND are on the 8.9 lists of
      *> COBOL-85, COBOL-2002 and COBOL-2023 but not COBOL-2014
      *> (reserved-words.json r2014 false), so at 2014 each is an ordinary
      *> user-defined word: 8.3.2.2 "A user-defined word is a COBOL word
      *> that is supplied by the user to satisfy the format of a clause or
      *> statement", and 14.4.1 "A procedure-name is a word used to refer to
      *> a paragraph or section". Every procedure-name slot below holds one:
      *> PERFORM's procedure-name-1, its THRU procedure-name-2, the TIMES
      *> form and GO TO DEPENDING's list. Each also BEGINS a statement, so a
      *> parse that read the word as the RECEIVE / SEND verb instead of the
      *> name (the inline PERFORM, a target-less GO TO followed by a
      *> statement) fails here. Expected, by 14.9.28.4 and 14.9.17.4 GR2
      *> with W-N = 2: RECEIVE; SEND RECEIVE; RECEIVE twice; then GO TO
      *> RECEIVE, which falls through into P-END.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4206FREE.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W-N PIC 9 VALUE 2.
       PROCEDURE DIVISION.
       MAIN-P.
           PERFORM RECEIVE.
           PERFORM SEND THRU RECEIVE.
           PERFORM RECEIVE 2 TIMES.
           GO TO P-END RECEIVE DEPENDING ON W-N.
       SEND.
           DISPLAY "SEND".
       RECEIVE.
           DISPLAY "RECEIVE".
       P-END.
           DISPLAY "END".
           STOP RUN.
