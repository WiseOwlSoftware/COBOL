*>--- conformance-case
*> id: pb4755_deedit_for_floating_blank_landing_before_z
*> rules: [GR-13.18.40.5-6, GR-14.9.25.4-6]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB4755 - de-editing a FOR-form floating string whose landing is BLANK and wider than one position,
*> when suppressed Z positions follow the string.
*> ISO/IEC 1989:2023 13.18.40.3 SR12 b: a FOR character-1 picture may also hold Z. 13.18.40.5 rule 6 a): the
*> replacement character(s) land in the position(s) immediately preceding the first nonzero digit, or the first
*> position for which no floating insertion editing is specified (the Z), "Any character positions preceding
*> this (these) insertion character(s) will contain the space character"; Z (rule 4) suppresses leading zeros
*> to spaces. 13.18.40.4 GR14 'es': one occurrence of the literal counts in the size. 14.9.25.4 GR6 d) 1: the
*> de-editing MOVE establishes the numeric value, which may be signed.
*>   PIC LLLZZ9 EDITING L FOR NEGATIVE IS "((" (positive text defaults to two spaces, SR12c): 6 positions, the
*>   landing two characters wide, so the image is 7 characters. Digit positions: L2 L3 Z Z 9.
*>   5  -> every position suppressed, landing at L3 (two spaces), Z Z spaces: "      5" -> +00005.
*>   -5 -> "  ((  5" -> -00005.  35 -> "     35" -> +00035.  4321 -> "   4321" -> +04321.
*>   PIC LLLZZ.99 holds 0.5 as "      .50" (the landing precedes the decimal point) and -0.5 as "  ((  .50".
identification division.
program-id. PB4755DE.
data division.
working-storage section.
01 fw   pic LLLZZ9 editing L for negative is "((".
01 fz   pic LLLZZ.99 editing L for negative is "((".
01 src  pic s9v99.
01 n    pic s9(5) sign leading separate.
01 m    pic s9v99 sign leading separate.
procedure division.
    move 5 to fw
    move fw to n
    display "A[" fw "] " n
    move -5 to fw
    move fw to n
    display "B[" fw "] " n
    move 35 to fw
    move fw to n
    display "C[" fw "] " n
    move 4321 to fw
    move fw to n
    display "D[" fw "] " n
    move 0.5 to src
    move src to fz
    move fz to m
    display "E[" fz "] " m
    move -0.5 to src
    move src to fz
    move fz to m
    display "F[" fz "] " m
    stop run.
