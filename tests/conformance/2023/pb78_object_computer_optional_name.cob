      *> PB78 - ISO 12.3.6.2: OBJECT-COMPUTER. [computer-name-1] [ | CHARACTER CLASSIFICATION ... | PROGRAM COLLATING
      *> SEQUENCE ... | ] . - computer-name-1 is OPTIONAL and the clauses may follow the paragraph's period directly
      *> (the figure notes: zero or more of the two clauses, each at most once, in any order); 12.3.5.2 makes
      *> SOURCE-COMPUTER's name optional too, and 12.3.5.3 SR1 / 12.3.6.3 SR4 let the second period go when nothing
      *> follows. `OBJECT-COMPUTER. PROGRAM COLLATING SEQUENCE IS REV.` was `unexpected 'PROGRAM'` - the grammar
      *> hung the clause off a required name (X3.23-1985's shape; the name-less clause form is the 2002 relaxation,
      *> gated below 2002 as computer-name-optional-2002). 12.3.6.4 GR3: with no computer-name the object computer is
      *> the implementor's - the same one; GR9/GR11: the PROGRAM COLLATING SEQUENCE governs alphanumeric comparisons,
      *> so REV ("B" before "A") makes "A" > "B" - the observable.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB78OCN.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SOURCE-COMPUTER.
       OBJECT-COMPUTER.
           PROGRAM COLLATING SEQUENCE IS REV.
       SPECIAL-NAMES.
           ALPHABET REV IS "B" "A".
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 X PIC X VALUE "A".
       PROCEDURE DIVISION.
           IF X > "B"
               DISPLAY "PCS=REV"
           ELSE
               DISPLAY "PCS=NATIVE"
           END-IF.
           STOP RUN.
