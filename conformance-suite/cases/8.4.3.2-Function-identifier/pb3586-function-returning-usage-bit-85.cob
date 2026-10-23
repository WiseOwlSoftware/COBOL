*>--- conformance-case
*> id: pb3586-function-returning-usage-bit-85
*> rules: [GR-8.4.3.2.4-1, GR-15.2-2]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB3586 - the negative of pb3586_function_returning_usage_bit: a user-defined function whose RETURNING
      *> item is PIC 1(8) USAGE BIT. Both the FUNCTION-ID paragraph (ISO 11.5) and the boolean data (ISO 8.5.2.5 Boolean
      *> category) are COBOL-2002 introductions; COBOL-85 has neither, so the gate names the edition.
       IDENTIFICATION DIVISION.
       FUNCTION-ID. PB3586N.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L-X PIC 9.
       01 L-R PIC 1(8) USAGE BIT.
       PROCEDURE DIVISION USING L-X RETURNING L-R.
       P.
           MOVE B"00001010" TO L-R.
           GOBACK.
       END FUNCTION PB3586N.
