*>--- conformance-case
*> id: pb4283-bare-gated-verb-sentence-is-an-incomplete-statement
*> rules: [SR-8.3.2.1-1]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB4283.  FREE is reserved from COBOL-2002 (ISO 8.9), and the sentence FREE. below is the verb alone.  It is a
      *> paragraph header naming a reserved word only if the program USES the word as a name (8.3.2.1 1), "Reserved words
      *> shall not be used as user-defined words or system-names"; cite.py --check 8.3.2.1 -> OK), and no procedure reference
      *> anywhere writes one.  Nothing written shows a use of the word as a name, while the operand the FREE statement lacks
      *> is a fault the source certainly holds (14.9.15.2 prints "FREE { data-name-1 } ..."; cite.py --check 14.9.15.2
      *> "FREE" -> OK).  So the sentence draws the statement's own refusal, never COBOL0901.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4283BARE.
       PROCEDURE DIVISION.
       MAIN-P.
           FREE.
           STOP RUN.
       OTHER-P.
           EXIT.
