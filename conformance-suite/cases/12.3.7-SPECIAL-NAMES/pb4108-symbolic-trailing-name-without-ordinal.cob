*>--- conformance-case
*> id: pb4108-symbolic-trailing-name-without-ordinal
*> rules: [SR-12.3.7.3-16]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB4108 review R1-1 - ISO 12.3.7.2 general format: the SYMBOLIC CHARACTERS clause repeats the group
*>   {symbolic-character-1}... [IS|ARE] {integer-1}..., so every repetition carries at least one integer-1 AFTER its
*>   names. `SC1 66 67 SC2` writes SC2 after every ordinal, so its group has no integer-1: the totals balance (two
*>   names, two ordinals) but the clause is not the format, and SC2 shall not be paired with 67.
*>   cite.py: OK  12.3.7.2  (General format)
*>   cite.py: OK  12.3.7.3 16) c)  (Syntax rules)
IDENTIFICATION DIVISION.
PROGRAM-ID. PB4108T.
ENVIRONMENT DIVISION.
CONFIGURATION SECTION.
SPECIAL-NAMES.
    SYMBOLIC CHARACTERS SC1 66 67 SC2.
PROCEDURE DIVISION.
MAIN-PARA.
    DISPLAY SC1 SC2
    STOP RUN.
