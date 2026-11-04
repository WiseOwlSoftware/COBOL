*>--- conformance-case
*> id: pb4514_deedit_embedded_char1_sign
*> rules: [GR-13.18.40.5-6, GR-14.9.25.4-6]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB4514 (review round 1, R1-1) - de-editing recovers the sign a floating '+' or '-' string
*> landed on a PICTURE EDITING character-1 embedded in the string.
*> ISO/IEC 1989:2023 13.18.40.5 rule 6: "Any of the simple insertion editing symbols embedded in this
*> string or to the immediate right of this string are part of the string", and rule 3 makes an IS-form
*> character-1 a simple insertion symbol, so PIC --T--9 EDITING T IS ":" is one floating string whose
*> third position is a landing position; rule 6 a) lands the sign at the rightmost suppressed position
*> of the string. 14.9.25.4 general rule 6 d) 1: de-editing "establishes the operand's numeric value,
*> which may be signed", the sign being the one editing put there (Table 9).
*>   A  -555 -> PIC --T--9: digits 0555 fill the last three positions of the string, the position of T
*>      is the rightmost suppressed one, the sign lands there: "  -555"; de-edited -555 -> "-00555".
*>   B  555 -> "   555" (the floating '-' is a space for a positive value); -> "+00555".
*>   C  -555 -> PIC ++T++9: "  -555" (Table 9, '-' for a negative value); -> "-00555".
*>   D  555 -> PIC ++T++9: "  +555"; -> "+00555".
*>   E  -1555 -> PIC --T--9: digits 1555, the T position holds the ':' (a nonzero digit precedes it), the
*>      sign lands at the leftmost position: "-1:555"; -> "-01555".
*>   When the character-1 literal IS "-" (PIC --T--9 EDITING T IS "-") the landed sign and the literal are
*>   the same character, told apart by position: rule 6 a) places the sign immediately preceding
*>   "the first nonzero numeric character in the item" and spaces before it, so the string positions
*>   before the first nonzero digit hold only spaces or the sign; a '-' at T with no digit of the string
*>   before it is the landed sign, and one after a digit is the literal.
*>   F  -555 -> "  -555" -> "-00555" (the sign landed at T, no digit before it)
*>   G  1555 -> " 1-555" -> "+01555" (the digit 1 precedes T, so the '-' is the literal)
*>   H  -555 -> PIC ++T++9: "  -555" -> "-00555"
*>   I  -1555 -> "-1-555" -> "-01555" (the literal after the digit 1, the sign at the leftmost position)
identification division.
program-id. PB4514EMB.
data division.
working-storage section.
01 em-t   pic --T--9 editing T is ":".
01 ep-t   pic ++T++9 editing T is ":".
01 em-d   pic --T--9 editing T is "-".
01 ep-d   pic ++T++9 editing T is "-".
01 n      pic s9(5) sign leading separate.
procedure division.
    move -555 to em-t
    move em-t to n
    display "A[" em-t "] " n
    move 555 to em-t
    move em-t to n
    display "B[" em-t "] " n
    move -555 to ep-t
    move ep-t to n
    display "C[" ep-t "] " n
    move 555 to ep-t
    move ep-t to n
    display "D[" ep-t "] " n
    move -1555 to em-t
    move em-t to n
    display "E[" em-t "] " n
    move -555 to em-d
    move em-d to n
    display "F[" em-d "] " n
    move 1555 to em-d
    move em-d to n
    display "G[" em-d "] " n
    move -555 to ep-d
    move ep-d to n
    display "H[" ep-d "] " n
    move -1555 to em-d
    move em-d to n
    display "I[" em-d "] " n
    stop run.
