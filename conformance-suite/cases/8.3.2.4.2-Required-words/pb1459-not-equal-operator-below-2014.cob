*>--- conformance-case
*> id: pb1459-not-equal-operator-below-2014
*> rules: [SR-8.3.2.4.2-2]
*> source-format: free
*> portable: unverified
*>---
*> ISO 8.7.5.2 SR11 (cite.py --check 8.7.5.2 "<> is an abbreviation for NOT EQUAL"; cite.py --find places the sentence at
*> SR11) and 8.3.2.4.2 2) ("<>" - "Relational operator - not equal"). COBOL-85's relational operators are the six
*> GREATER / LESS / EQUAL forms with their symbols and NOT, and so are the 2002 text's (its clause 8.7.5.2 ends the
*> abbreviation rules at <=); <> is a COBOL-2014 introduction (Annex D.3 item 16 of the 2009 committee draft; kb/Work
*> PB1459, PB3604; VCR row 7.26), so it is refused at 85 and at 2002 and accepted from 2014. The word form NOT EQUAL TO
*> is COBOL-85 and stays legal (the positive twin is 8.3.2.4.2-Required-words/l1c26_relational_operator_not_equal_symbol,
*> accepted at 2014).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1459N.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A PIC 9 VALUE 7.
       PROCEDURE DIVISION.
       MAIN.
           IF A <> 3 DISPLAY "NE".
           STOP RUN.
