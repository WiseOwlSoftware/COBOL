*>--- conformance-case
*> id: pb4108-symbolic-clause-count-mismatch
*> rules: [SR-12.3.7.3-16]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB4108 - ISO 12.3.7.3 SR16 c): "There shall be a one-to-one correspondence between occurrences of
*>   symbolic-character-1 and occurrences of integer-1." Three names against two ordinals (the literal 66 and the
*>   constant-name K2, each one integer-1 occurrence, 5.5 1) and 13.10.3 SR2) is refused whatever the order of the
*>   ordinals: a literal then a constant must count as two, exactly as a constant then a literal does.
*>   cite.py: OK  12.3.7.3 16) c)  (Syntax rules)
*>   cite.py: OK  13.10.3 2)  (Syntax rules)
IDENTIFICATION DIVISION.
PROGRAM-ID. PB4108N.
ENVIRONMENT DIVISION.
CONFIGURATION SECTION.
SPECIAL-NAMES.
    SYMBOLIC CHARACTERS SC1 SC2 SC3 ARE 66 K2.
DATA DIVISION.
WORKING-STORAGE SECTION.
01  K2 CONSTANT AS 67.
PROCEDURE DIVISION.
MAIN-PARA.
    DISPLAY SC1
    STOP RUN.
