      *>--- conformance-case
      *> id: pb3373-data-entry-signed-and-concatenated-run
      *> rules: [FMT-13.16.2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB3373 - a word run among the clauses of the CLOSED ISO 13.16.2 Format 1 general format
      *> is refused BY NAME, quoting its first word (pb487-unrecognized-data-clause), whatever literals the
      *> run holds.  A sign-led numeric literal (ISO 8.3.3.3.2 2)) and a concatenation expression (ISO
      *> 8.8.3.3 3): "equivalent to a literal of the same class and value") are literals of more than one
      *> token; the run's tail once held only one-token literals, so FOO -1 ended at the sign with a bare
      *> "COBOL0307: unexpected '-'. A period may be missing ..." where no period is missing, and FOO
      *> "A" & "B" ended at the ampersand the same way.  The run holds every literal but the reserved
      *> words now, so the entry draws the one refusal, COBOLNET1941 naming FOO, at every edition:
      *> nothing below the error production is diagnosed, so the 2002 concatenation adds nothing at 85.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. N3373DR.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W-X PIC X(3) FOO -1 "A" & "B".
       PROCEDURE DIVISION.
       MAIN-PARA.
           DISPLAY "N3373DR".
           STOP RUN.
