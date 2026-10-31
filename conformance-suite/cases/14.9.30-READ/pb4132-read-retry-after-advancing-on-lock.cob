*>--- conformance-case
*> id: pb4132-read-retry-after-advancing-on-lock
*> rules: [FMT-14.9.30.2, SR-8.3.2.1-1]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB4132, the third member of pb331-read-two-from-one-lock-bracket.
      *> ISO 5.2.6.2: brackets "enclosing a portion of a general format indicate that
      *> the syntax element contained within the brackets or one of the alternatives
      *> contained within the brackets may be explicitly specified" (cite.py --check
      *> 5.2.6.2 -> OK). 14.9.30.2 Format 1 puts ADVANCING ON LOCK, IGNORING LOCK and
      *> retry-phrase in ONE such bracket, so at most one of the three may be
      *> written. The READ below writes ADVANCING ON LOCK and then a retry-phrase:
      *> the READ ends before RETRY, and no general format of READ prints it there,
      *> so the READ's own refusal names it (COBOL2269).
      *> RETRY is reserved from 2002 (ISO 8.9), so there it is that keyword, and ISO
      *> 8.3.2.1 1) "Reserved words shall not be used as user-defined words or
      *> system-names" (cite.py --check 8.3.2.1 -> OK) is not what the source
      *> breaks: no user-defined word can stand after the complete READ either. It
      *> used to draw COBOL0901, which the syntax-error listener wrote over every
      *> report on a reservation-gated word. 85, where 8.9 leaves RETRY a
      *> user-defined word, is not listed.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4132RT.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SQF ASSIGN TO "pb4132rt.dat"
               ORGANIZATION IS SEQUENTIAL
               ACCESS MODE IS SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD SQF.
       01 SQ-REC PIC X(4).
       PROCEDURE DIVISION.
       MAIN.
           OPEN INPUT SQF.
           READ SQF NEXT RECORD ADVANCING ON LOCK
               RETRY 3 TIMES
               AT END CONTINUE
           END-READ.
           CLOSE SQF.
           STOP RUN.
