*>--- conformance-case
*> id: pb5600-flag-native-arithmetic-onoff
*> rules: [2002-FMT-7.2.12.1, 2002-SR-7.2.12.2-1, 2002-GR-7.2.12.3-1, 2002-GR-7.2.12.3-2, 2002-GR-7.2.12.3-3,
*>    2002-GR-7.2.12.3-4, 2002-GR-7.2.12.3-5, 2002-GR-7.2.3-4]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB5600. The FLAG-NATIVE-ARITHMETIC directive of §2002:7.2.12 prints `>> FLAG-NATIVE-ARITHMETIC { ON | OFF }`
*> (ON not underlined, hence optional and the default of its braces, §2002:5.1.5.2), may be written only between
*> statements in the procedure division (§2002:7.2.12.2 SR1) and asks for a warning mechanism (GR1) that flags an
*> arithmetic operation using native arithmetic while standard arithmetic is in effect (GR2): ARITHMETIC IS STANDARD
*> is specified here, and B, described with usage binary-long, is an operand for which "the techniques used shall be
*> those specified for native arithmetic" (§2002:8.8.1.3), so ADD B TO R is flagged while ON is in force and not once OFF
*> is (GR3). A warning never changes the result: ADD B TO R makes R = 0 + 7 = 07, and ADD 1 TO R makes R = 08.
*>
*> EXPECTED OUTPUT, DERIVED FROM THE SPEC AND NOT FROM A RUN: "07" then "08".
IDENTIFICATION DIVISION.
PROGRAM-ID. PB5600FN.
OPTIONS.
    ARITHMETIC IS STANDARD.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 B USAGE BINARY-LONG VALUE 7.
01 R PIC 99 VALUE 0.
PROCEDURE DIVISION.
MAIN-PARA.
>>FLAG-NATIVE-ARITHMETIC ON
    ADD B TO R
    DISPLAY R
>>FLAG-NATIVE-ARITHMETIC OFF
    ADD 1 TO R
    DISPLAY R
>>FLAG-NATIVE-ARITHMETIC
    STOP RUN.
