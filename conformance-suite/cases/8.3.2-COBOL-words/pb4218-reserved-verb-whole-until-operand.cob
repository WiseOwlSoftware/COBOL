*>--- conformance-case
*> id: pb4218-reserved-verb-whole-until-operand
*> rules: [SR-8.3.2.1-1]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB4218 - ISO 8.3.2.1 1): "Reserved words shall not be used as user-defined words or
      *> system-names" (cite.py --check 8.3.2.1 -> OK).  RECEIVE is reserved at 85 and again at 2023 (8.9), and
      *> it is the WHOLE left operand of the inline PERFORM's UNTIL condition (14.9.28.2 until-phrase), so the
      *> source breaks the rule once: ONE diagnostic, COBOL0901.  Deleting the word could not repair the
      *> condition (nothing remains of it), the recovery resumed at the "1" (an integer begins a TIMES phrase)
      *> and the PERFORM lost its UNTIL reading: a false COBOL5506 "missing token before DISPLAY ... expected
      *> TIMES" followed.  The word is now read as the user-defined word it stood for and the PERFORM is
      *> parsed as written.  RECEIVE is a user word at 2002 and 2014, so those editions are not listed.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4218UNTIL.
       PROCEDURE DIVISION.
       MAIN-P.
           PERFORM UNTIL RECEIVE = 1 DISPLAY "X" END-PERFORM.
           STOP RUN.
