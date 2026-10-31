*>--- conformance-case
*> id: pb4207-next-statement-verb-after-missing-receiver
*> rules: [SR-8.3.2.1-1]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB4207 - the MOVE below has no receiving operand (14.9.25.2 prints "TO identifier-2 ..."; cite.py --check 14.9.25.2 "TO" -> OK), and
      *> the verb on the next line begins a RECEIVE statement that is complete.  The fault is the missing
      *> receiver; the verb is written correctly.  ISO 8.3.2.1 1) ("Reserved words shall not be used as
      *> user-defined words or system-names", cite.py --check 8.3.2.1 -> OK) is therefore NOT what the source
      *> breaks, and the report must not say RECEIVE "cannot be used as a user-defined word" (COBOL0901): it
      *> is the ordinary refusal at that token, "unexpected 'RECEIVE'", and the one diagnostic of the source.
      *> RECEIVE is reserved at 85 and 2023; at 2002 and 2014 it is a user word, so the MOVE's receiver
      *> would be the data item RECEIVE (a different refusal), and those editions are not listed.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4207NEXT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W-X PIC X(4).
       01 W-Y PIC X(4).
       01 W-N PIC 9(4).
       PROCEDURE DIVISION.
       MAIN-P.
           MOVE W-X TO
           RECEIVE FROM W-X GIVING W-Y W-N
           DISPLAY "A".
           STOP RUN.
