*>--- conformance-case
*> id: pb4621_deedit_char1_outside_floating_string
*> rules: [GR-13.18.40.5-6, GR-14.9.25.4-6]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB4621 - de-editing reads the landed sign only at a PICTURE EDITING character-1 that is PART of
*> the floating '+' or '-' string, never at one outside it.
*> ISO/IEC 1989:2023 13.18.40.5 rule 3: "Simple insertion editing results in the insertion character
*> occupying the same character position in the edited item as the associated symbol occupies in
*> character-string-1", so a character-1 renders its literal. Rule 6: "Any of the simple insertion editing
*> symbols embedded in this string or to the immediate right of this string are part of the string", so a
*> character-1 LEFT of the first floating symbol, or past the string's end, is no landing position of the
*> sign: its position always holds the literal. 14.9.25.4 general rule 6 d) 1: de-editing "establishes the
*> operand's numeric value, which may be signed", the sign being the one editing put there (Table 9).
*>   A  55 -> PIC T--9, T is "-": T shows "-", the leftmost '-' is the symbol itself and a space for a
*>      positive value, then the digits: "- 55" -> +00055 (the literal is not the sign)
*>   B  -55 -> "--55" (the literal, then the sign at the leftmost string position) -> -00055
*>   C  55 -> PIC T++9, T is "-": "-+55" (a positive value lands '+') -> +00055
*>   D  -55 -> "--55" -> -00055
*>   E  55 -> PIC --9T9, T is "-" and a 9 stands between the string and T: "  5-5" -> +00055
*>   F  -55 -> " -5-5" (the sign lands at the rightmost suppressed position, the literal follows the
*>      digit 5) -> -00055
identification division.
program-id. PB4621OUT.
data division.
working-storage section.
01 lm-t   pic T--9 editing T is "-".
01 lp-t   pic T++9 editing T is "-".
01 rm-t   pic --9T9 editing T is "-".
01 n      pic s9(5) sign leading separate.
procedure division.
    move 55 to lm-t
    move lm-t to n
    display "A[" lm-t "] " n
    move -55 to lm-t
    move lm-t to n
    display "B[" lm-t "] " n
    move 55 to lp-t
    move lp-t to n
    display "C[" lp-t "] " n
    move -55 to lp-t
    move lp-t to n
    display "D[" lp-t "] " n
    move 55 to rm-t
    move rm-t to n
    display "E[" rm-t "] " n
    move -55 to rm-t
    move rm-t to n
    display "F[" rm-t "] " n
    stop run.
