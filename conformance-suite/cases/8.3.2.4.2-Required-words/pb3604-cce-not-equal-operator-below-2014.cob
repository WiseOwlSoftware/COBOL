*>--- conformance-case
*> id: pb3604-cce-not-equal-operator-below-2014
*> rules: [SR-8.3.2.4.2-2, SR-7.3.8.2-1]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB3604. ISO 7.3.8.2 SR1 a): a constant-conditional relation "shall be formed according to the rules in 8.8.4.2",
*> so the symbol <> is the relational operator of 8.7.5.2 SR11 ("<> is an abbreviation for NOT EQUAL") in a >>IF as in a
*> statement, and an introduction of the same edition: COBOL-2014 (the 2002 text's clause 8.7.5.2 ends its abbreviation
*> rules at <=; Annex D.3 item 16 of the 2009 committee draft adds the symbol). The >>IF operand is a fragment the
*> compilation-unit walk never reaches, so the gate has a second asker (RelationalOperatorGate, called by the
*> conditional-compilation stage per evaluated fragment); before this the directive lane had none and the program below
*> compiled clean at 2002. The directive facility itself is a 2002 introduction (VCR row 7.21), so 2002 is the one edition
*> below 2014 where the operator is the only fault.
*>   cite.py --check 7.3.8.2 "The condition shall be formed according to the rules in 8.8.4.2, Simple relation conditions."
*>     -> OK 7.3.8.2 1) a)
*>   cite.py --find "<> is an abbreviation for NOT EQUAL." -> 8.7.5.2 11)
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3604C.
       PROCEDURE DIVISION.
       MAIN.
       >>IF 5 <> 3
           DISPLAY "NE".
       >>END-IF
           STOP RUN.
