*>--- conformance-case
*> id: pb4108-symbolic-leading-constant-before-names
*> rules: [SR-12.3.7.3-16]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB4108 review R1-1 - ISO 12.3.7.2 general format: every repetition of {symbolic-character-1}...
*>   [IS|ARE] {integer-1}... names its symbolic-character-1 BEFORE its integer-1. `K1 SC1 SC2 66` opens with an
*>   integer constant-name (13.10.3 SR2), so no symbolic-character-1 precedes it: the totals balance (two names,
*>   two ordinals) but the clause is not the format, and SC1 shall not be paired with K1.
*>   cite.py: OK  12.3.7.2  (General format)
*>   cite.py: OK  12.3.7.3 16) c)  (Syntax rules)
IDENTIFICATION DIVISION.
PROGRAM-ID. PB4108L.
ENVIRONMENT DIVISION.
CONFIGURATION SECTION.
SPECIAL-NAMES.
    SYMBOLIC CHARACTERS K1 SC1 SC2 66.
DATA DIVISION.
WORKING-STORAGE SECTION.
01  K1 CONSTANT AS 65.
PROCEDURE DIVISION.
MAIN-PARA.
    DISPLAY SC1 SC2
    STOP RUN.
