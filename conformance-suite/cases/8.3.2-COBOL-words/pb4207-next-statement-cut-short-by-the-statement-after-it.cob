*>--- conformance-case
*> id: pb4207-next-statement-cut-short-by-the-statement-after-it
*> rules: [SR-8.3.2.1-1]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB4207, review R2-1.  The MOVE below has no receiving operand (14.9.25.2 prints "TO identifier-2 ..."; cite.py
      *> --check 14.9.25.2 "TO" -> OK), and the verb on the next line begins an ADD statement that has a missing receiver of
      *> its own (14.9.2.2 prints "TO identifier-2 ..."; cite.py --check 14.9.2.2 "TO" -> OK), no period between it and the
      *> DISPLAY statement after it.  Both faults are missing operands; ADD is written correctly.  ISO 8.3.2.1 1) ("Reserved
      *> words shall not be used as user-defined words or system-names", cite.py --check 8.3.2.1 -> OK) is NOT what the source
      *> breaks, so the first diagnostic is the ordinary refusal at that token, "unexpected 'ADD'", and the ADD's own fault
      *> (the DISPLAY where its receiver stands) is reported after it, not lost.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4207CUT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W-X PIC X(4).
       01 W-Y PIC X(4).
       PROCEDURE DIVISION.
       MAIN-P.
           MOVE W-X TO
           ADD 1 TO
           DISPLAY W-Y.
           STOP RUN.
