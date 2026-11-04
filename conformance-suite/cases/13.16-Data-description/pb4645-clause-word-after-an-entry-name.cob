      *>--- conformance-case
      *> id: pb4645-clause-word-after-an-entry-name
      *> rules: [FMT-13.16.2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4645.  The 13.16.2 Format 1 general format (cite.py --check 13.16.2 "level-number" -> OK) is a closed
      *> list: after the entry-name W-A, MOVE is no clause of it, so the entry is refused by name (COBOL1941).  The entry
      *> carries no clause before MOVE, so the parse died looking ahead from the entry's start, before W-A was attached to
      *> it, and the refusal named no entry; the same word after a PIC clause (pb2533-data-entry-reserved-word-clause) named
      *> 'W-A'.  The sentence now names the entry the user wrote.  Refused at every edition: MOVE is reserved in all.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4645.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W-A MOVE.
       PROCEDURE DIVISION.
       MAIN-PARA.
           DISPLAY "SHOULD NOT COMPILE"
           STOP RUN.
