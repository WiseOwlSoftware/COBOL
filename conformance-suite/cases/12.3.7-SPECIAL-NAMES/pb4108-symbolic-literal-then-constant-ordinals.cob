*>--- conformance-case
*> id: pb4108-symbolic-literal-then-constant-ordinals
*> rules: [SR-12.3.7.3-16, GR-12.3.7.4-11]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB4108 - the pairing of SYMBOLIC CHARACTERS names with ordinals does not depend on the SPELLING of the
*>   ordinals. ISO 12.3.7.2 prints the clause as { {symbolic-character-1}... [IS|ARE] {integer-1}... }... and
*>   12.3.7.3 SR16 b) pairs "by position in the SYMBOLIC CHARACTERS clause": "The first symbolic-character-1 is
*>   paired with the first integer-1; the second symbolic-character-1 is paired with the second integer-1; and so
*>   on", c) requires a one-to-one correspondence between their occurrences. 5.5 1) makes integer-1 a fixed-point
*>   integer literal and 13.10.3 SR2 lets an integer constant-name stand where a format specifies one, so a literal
*>   ordinal may precede a constant-name ordinal (K2) in one group, and a group of ordinals may be a different
*>   length from the group of names before it, since only the clause's totals are paired.
*>   cite.py: OK  12.3.7.3 16) b)  (Syntax rules)
*>   cite.py: OK  12.3.7.3 16) c)  (Syntax rules)
*>   cite.py: OK  5.5 1)  (Integer operands)
*>   cite.py: OK  13.10.3 2)  (Syntax rules)
*> The native character set of this compiler is the UTF-16 code-unit order (12.3.7.4 GR6, implementor-defined), so
*> ordinal n is the character whose code is n - 1: 66 -> 'A', 67 -> 'B', ... (12.3.7.4 GR11 b).
*>   SC1 SC2 ARE 66 K2          -> A B       (literal, then constant; K2 = 67)
*>   SC3 SC4 SC5 68 K3 71       -> C D F     (literal, constant, literal; K3 = 69; no IS/ARE)
*>   SC6 SC7 72 SC8 73 74       -> G H I     (groups of 2 names / 1 ordinal and 1 name / 2 ordinals: 3 against 3)
*>   SC9 SCA IS 75 K4           -> J K       (IS; K4 = 76)
IDENTIFICATION DIVISION.
PROGRAM-ID. PB4108P.
ENVIRONMENT DIVISION.
CONFIGURATION SECTION.
SPECIAL-NAMES.
    SYMBOLIC CHARACTERS SC1 SC2 ARE 66 K2
    SYMBOLIC CHARACTERS SC3 SC4 SC5 68 K3 71
    SYMBOLIC CHARACTERS SC6 SC7 72 SC8 73 74
    SYMBOLIC CHARACTERS SC9 SCA IS 75 K4.
DATA DIVISION.
WORKING-STORAGE SECTION.
01  K2 CONSTANT AS 67.
01  K3 CONSTANT AS 69.
01  K4 CONSTANT AS 76.
PROCEDURE DIVISION.
MAIN-PARA.
    DISPLAY SC1 SC2
    DISPLAY SC3 SC4 SC5
    DISPLAY SC6 SC7 SC8
    DISPLAY SC9 SCA
    STOP RUN.
