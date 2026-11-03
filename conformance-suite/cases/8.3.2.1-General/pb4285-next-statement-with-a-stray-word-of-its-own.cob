*>--- conformance-case
*> id: pb4285-next-statement-with-a-stray-word-of-its-own
*> rules: [SR-8.3.2.1-1]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB4285 item 3.  The MOVE below has no receiving operand (14.9.25.2 prints "TO identifier-2 ..."), and the verb on
      *> the next line begins an ADD statement written with a stray FROM (14.9.2.2 prints no FROM phrase).  ADD is a statement
      *> verb written correctly, so ISO 8.3.2.1 1) ("Reserved words shall not be used as user-defined words or system-names")
      *> is not what the source breaks: the first diagnostic is the ordinary refusal at that token, "unexpected 'ADD'", and the
      *> ADD's own fault, the stray FROM, is reported after it, not lost to a COBOL0901 that consumed the ADD as the receiver.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4285STR.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W-X PIC X(4).
       01 W-Y PIC X(4).
       PROCEDURE DIVISION.
       MAIN-P.
           MOVE W-X TO
           ADD 1 FROM W-Y.
           STOP RUN.
