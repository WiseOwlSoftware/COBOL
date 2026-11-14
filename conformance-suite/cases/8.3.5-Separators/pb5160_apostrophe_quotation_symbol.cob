      *>--- conformance-case
      *> id: pb5160_apostrophe_quotation_symbol
      *> rules: [SR-8.3.5-5]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5160 - the apostrophe as the quotation symbol, at its
      *> introducing edition, COBOL-2002 (constructs
      *> apostrophe-quotation-symbol-2002, VCR row 7.72; below 2002 it
      *> is COBOL0900, negative IV.4.2.2.2.1@1985-Nonnumeric-Literals/
      *> pb5160-apostrophe-literal-85).
      *>   cite.py --check "§2023:8.3.5 SR5" "Either an apostrophe or a
      *>     quotation mark may be used as the quotation symbol
      *>     character in opening and closing delimiters" -> OK
      *>   cite.py --check "§2023:8.3.5 SR5" "an apostrophe when the
      *>     opening delimiter uses an apostrophe" -> OK
      *>   cite.py --check "§2002:F.2" "Apostrophe as quotation symbol.
      *>     The apostrophe character as well as the quotation mark"
      *>     -> OK (the 2002 introduction)
      *>   cite.py --check "§2023:7.2.4.4 GR8" "The two representations
      *>     of the quotation symbol match" -> OK
      *> EXPECTED OUTPUT, DERIVED (why each leg can fail):
      *>   V=[AB]     VALUE 'AB': the apostrophe delimits, the content is
      *>              AB. A lexer without the form fails to compile.
      *>   IT'S       'IT''S': the doubled opener is one apostrophe of
      *>              content (8.3.3.2.3 SR3).
      *>   IT'S       "IT'S" in the same unit: both forms may be used in
      *>              one source unit (8.3.3.1); the apostrophe inside a
      *>              quotation-mark literal is content.
      *>   LEN-SQ=3   FUNCTION LENGTH('A''B'): A, one apostrophe, B.
      *>   H=[AB]     X'4142': the hexadecimal format takes either
      *>              symbol (8.3.3.2.2 format 2); 41 42 are A B.
      *>   EQ         V = 'AB' compares the content, not the delimiters.
      *>   [RR]       'QQ' matches the REPLACE operand =="QQ"== (the
      *>              two representations of the quotation symbol
      *>              match, 7.2.4.4 GR8) -> a raw compare leaves [QQ].
      *>   [SAY "HI...THERE...]  an apostrophe literal continued on the
      *>              next line ends only at an apostrophe (8.3.5 SR5),
      *>              so the quotation mark is content; the spaces to
      *>              margin R are part of the literal (6.3.5).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5160APOS.
       REPLACE =="QQ"== BY =="RR"==.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 V PIC XX VALUE 'AB'.
       01 H PIC XX VALUE X'4142'.
       01 L PIC 9.
       01 Y PIC X(80) VALUE 'SAY "HI                                    
      -    'THERE'.
       PROCEDURE DIVISION.
       P1.
           DISPLAY "V=[" V "]"
           DISPLAY 'IT''S'
           DISPLAY "IT'S"
           MOVE FUNCTION LENGTH('A''B') TO L
           DISPLAY "LEN-SQ=" L
           DISPLAY "H=[" H "]"
           IF V = 'AB' DISPLAY "EQ" END-IF
           DISPLAY "[" 'QQ' "]"
           DISPLAY "[" Y "]"
           STOP RUN.
