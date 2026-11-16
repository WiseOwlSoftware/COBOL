*>--- conformance-case
*> id: pb5600-flag-native-arithmetic-option-word
*> rules: [2002-FMT-7.2.12.1, 2002-SR-7.2.2-4]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB5600. §2002:7.2.12.1 prints the FLAG-NATIVE-ARITHMETIC format as `{ ON | OFF }` and nothing else (it selects
*> no option, unlike FLAG-85), and §2002:7.2.2 SR4 lets only space characters and an optional inline comment follow a
*> directive in free form. `>>FLAG-NATIVE-ARITHMETIC ALL ON` writes ALL where the format has no place for it, so the
*> directive is malformed (COBOL1622); it was accepted in silence before kb/Work PB5600.
IDENTIFICATION DIVISION.
PROGRAM-ID. PB5600N2.
PROCEDURE DIVISION.
>>FLAG-NATIVE-ARITHMETIC ALL ON
    DISPLAY "B"
    STOP RUN.
