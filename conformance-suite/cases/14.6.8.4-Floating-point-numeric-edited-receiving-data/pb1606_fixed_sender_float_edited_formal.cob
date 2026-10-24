*>--- conformance-case
*> id: pb1606_fixed_sender_float_edited_formal
*> rules: [GR-14.2.3-9, GR-14.6.8.4-1]
*> accept: [2002, 2014, 2023]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB1606 (review round 1, finding R1-1) - 14.2.3 GR9: for a program with a program-specifier in the
      *> REPOSITORY paragraph of the activating runtime element, a BY CONTENT argument is moved into a record of the
      *> FORMAL's description ("otherwise, a MOVE statement"); here the formal is a FLOATING-POINT numeric-edited item.
      *> 14.6.8.4 rule 1 adjusts "the algebraic value of the sending operand" and rule 2 truncates by the PICTURE's editing
      *> rules, so a FIXED-POINT sender is edited by its own decimal digits - never by a binary64 image of them.
      *>
      *> HAND-DERIVED: PIC +9.9(16)E+99 holds a 17-digit significand.
      *>   V  PIC 9V9 0.3 is exactly 0.3 -> +3.0000000000000000E-01 (the same text a MOVE V TO FE edits).
      *>   W  PIC 9V99 0.07 is exactly 0.07 -> +7.0000000000000000E-02.
      *>   X  PIC 9V99 0.09, FUNCTION SQRT (X): the root is 0.3; a returned value is an implementor-defined approximation
      *>      whose WiseOwl COBOL image is its shortest round-trip decimal (docs/CONFORMANCE.md, section 3), 0.3, and a MOVE
      *>      edits it -> +3.0000000000000000E-01 (the binary64's expansion, 0.29999999999999998..., is the image of no
      *>      value the program wrote).
      *>      The same function value passed BY CONTENT to the float-edited formal (review round 2, finding R2-1) is the
      *>      sending operand of the same GR9 MOVE, so the formal holds the same +3.0000000000000000E-01.
      *>   FH FLOAT-LONG 0.5 is exactly 0.5 -> +5.0000000000000000E-01.
      *>   F3 FLOAT-LONG 0.3 IS the binary64 0.299999999999999988897769753748..., a floating-point DATA ITEM read by its
      *>      exact value (the PB1606 rule): truncated to 17 digits -> +2.9999999999999998E-01.
      *> Each leg can fail on its own.
IDENTIFICATION DIVISION.
PROGRAM-ID. PB1606M.
ENVIRONMENT DIVISION.
CONFIGURATION SECTION.
REPOSITORY.
    PROGRAM PB1606P.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 V    PIC 9V9 VALUE 0.3.
01 W    PIC 9V99 VALUE 0.07.
01 X    PIC 9V99 VALUE 0.09.
01 FH   USAGE FLOAT-LONG VALUE 0.5.
01 F3   USAGE FLOAT-LONG VALUE 0.3.
01 FE   PIC +9.9(16)E+99.
PROCEDURE DIVISION.
MAIN.
    MOVE V TO FE
    DISPLAY "MOVE V=" FE
    CALL PB1606P USING BY CONTENT V
    CALL PB1606P USING BY CONTENT W
    MOVE FUNCTION SQRT (X) TO FE
    DISPLAY "MOVE SQRT=" FE
    CALL PB1606P USING BY CONTENT FUNCTION SQRT (X)
    CALL PB1606P USING BY CONTENT FH
    CALL PB1606P USING BY CONTENT F3
    STOP RUN.
END PROGRAM PB1606M.

IDENTIFICATION DIVISION.
PROGRAM-ID. PB1606P.
DATA DIVISION.
LINKAGE SECTION.
01 L    PIC +9.9(16)E+99.
PROCEDURE DIVISION USING L.
MAIN.
    DISPLAY "CALL=" L
    GOBACK.
END PROGRAM PB1606P.
