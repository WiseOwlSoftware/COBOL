*>--- conformance-case
*> id: pb5600-flag85-options-and-defaults
*> rules: [2002-FMT-7.2.11.1, 2002-SR-7.2.11.2-1, 2002-GR-7.2.11.3-1, 2002-GR-7.2.11.3-2, 2002-GR-7.2.11.3-3,
*>    2002-GR-7.2.11.3-4, 2002-GR-7.2.11.3-5, 2002-GR-7.2.3-4]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB5600. The FLAG-85 directive of §2002:7.2.11 is a real directive: its general format prints
*> `>> FLAG-85 { ALL | |option ...| } { ON | OFF }` with ALL and ON NOT underlined, so each is an optional word and the
*> default of its braces (§2002:5.1.5.2), and the options (CORRESPONDING, DE-EDITING, DIVIDE, FUNCTION-ARGUMENT, MOVE,
*> NUMVAL, SET, STANDARD-1, STANDARD-2, ZERO-LENGTH) are choice-indicator words of one brace, in any order, each once. It
*> may be written only between clauses outside the procedure division and between statements within it (§2002:7.2.11.2
*> SR1), and §2002:7.2.11.3 GR1 asks for a warning mechanism, which never changes what the program does: GR2 enables
*> it for the text that follows, GR3 disables it, GR5 makes OFF the default.
*>
*> Every spelling below is legal: an option list with ON omitted, ALL alone, ALL OFF, a lone OFF, a bare directive,
*> and a directive between two statements. Each is processed, none changes the program: NUMVAL is flagged (GR4 g)
*> and still returns its value, FUNCTION NUMVAL("123") = 123, which PIC 9(5) displays as 00123.
*>
*> EXPECTED OUTPUT, DERIVED FROM THE SPEC AND NOT FROM A RUN: "00123".
IDENTIFICATION DIVISION.
PROGRAM-ID. PB5600F85.
ENVIRONMENT DIVISION.
CONFIGURATION SECTION.
SPECIAL-NAMES.
>>FLAG-85 STANDARD-1 STANDARD-2 ON
    ALPHABET A1 IS STANDARD-1.
DATA DIVISION.
WORKING-STORAGE SECTION.
>>FLAG-85 ALL
01 Y PIC X(10) VALUE "123".
>>FLAG-85 ALL OFF
01 X PIC 9(5) VALUE 0.
PROCEDURE DIVISION.
MAIN-PARA.
>>FLAG-85 NUMVAL
    COMPUTE X = FUNCTION NUMVAL(Y)
>>FLAG-85 CORRESPONDING DE-EDITING DIVIDE FUNCTION-ARGUMENT MOVE SET ZERO-LENGTH OFF
>>FLAG-85 OFF
>>FLAG-85
    DISPLAY X
    STOP RUN.
