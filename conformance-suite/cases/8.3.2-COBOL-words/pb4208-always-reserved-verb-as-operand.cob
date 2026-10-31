*>--- conformance-case
*> id: pb4208-always-reserved-verb-as-operand
*> rules: [SR-8.3.2.1-1]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB4208 - ISO 8.3.2.1 1): "Reserved words shall not be used as user-defined words or
      *> system-names" (cite.py --check 8.3.2.1 -> OK).  CONTINUE is reserved at EVERY edition (8.9) and
      *> begins a statement, so written as the sending operand of the MOVE it is that misuse, exactly as
      *> RECEIVE or SEND is where 8.9 reserves them: ONE diagnostic, COBOL0901 naming the word.  It drew
      *> COBOL0001 and then COBOL2269 at TO ("The CONTINUE statement ends before this token"): the parser's
      *> recovery resumed AT the verb, read it as a CONTINUE statement of its own and refused the next token.
      *> Refused at every edition.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4208CONT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W-X PIC X(4).
       PROCEDURE DIVISION.
       MAIN-P.
           MOVE CONTINUE TO W-X.
           STOP RUN.
