*>--- conformance-case
*> id: pb4703_deedit_for_floating_embedded_landing
*> rules: [GR-13.18.40.5-6, GR-13.18.40.4-14, GR-14.9.25.4-6]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB4703 - a floating string lands its sign (or currency string) on a simple insertion symbol
*> EMBEDDED in it, and de-editing reads the landed text back.
*> ISO/IEC 1989:2023 13.18.40.5 rule 6: "The currency symbol, the extended editing sign control symbols,
*> if specified, and the fixed editing sign control symbols '+' and '-' are used as the floating insertion
*> symbols" and "Any of the simple insertion editing symbols embedded in this string or to the immediate
*> right of this string are part of the string"; rule 6 a) lands the replacement character(s) in the
*> position(s) immediately preceding the first nonzero numeric character, "Any character positions
*> preceding this (these) insertion character(s) will contain the space character". Table 9 gives the
*> extended editing sign control symbol literal-2 for a negative value and literal-3 otherwise (13.18.40.3
*> SR12c: the unspecified side is spaces). 14.9.25.4 general rule 6 d) 1: de-editing "establishes the
*> operand's numeric value, which may be signed".
*>   PIC LLTLL9 EDITING L FOR NEGATIVE "-" POSITIVE "+" EDITING T IS ":" - the floating string is L L T L L,
*>   the 9 is the last digit position; its digit positions (the string's second and later L, and the 9)
*>   hold 0555 for a magnitude of 555.
*>   A  -555  -> positions L1 (zero, suppressed) and T are suppressed; the sign lands at the rightmost,
*>               T: "  -555", de-edited -555 -> "-00555".
*>   B   555  -> "  +555" -> "+00555".
*>   C -1555  -> the 1 at L1 is nonzero, T keeps ':', the sign lands at the first position: "-1:555".
*>   D  literal of T is itself "-": -555 -> "  -555" (sign at T, nothing before it) -> "-00555";
*>      1555 -> "+1-555" (the digit 1 precedes T, so its "-" is the literal) -> "+01555".
*>   E  literal-2 and literal-3 two characters wide ("((" and "))"): the landed text is written whole
*>      (GR14 'es': one occurrence of the literal counts in the size): -555 -> "  ((555", 555 -> "  ))555".
*>   W  literal-2 "((" with literal-3 left to its default (SR12c: two spaces): the positive landing is two
*>      spaces wide (GR14 'es'). PIC LLTLL9 EDITING L FOR NEGATIVE IS "((" EDITING T IS ":" holds 555 as
*>      "    555": the sign lands at T, the rightmost suppressed position (one space for each of the two
*>      positions before it, two for the landing), and de-edits to "+00555"; -555 is "  ((555" -> "-00555".
*>   PIC $$B$$9 and PIC $$,$$9 with CURRENCY SIGN IS "USD" - the currency STRING lands on the embedded
*>   B or ',' (GR14 'cs': the first occurrence adds the string's length): 555 -> "  USD555",
*>   1555 -> "USD1 555" and "USD1,555".
*>   PIC LLL9 EDITING L FOR POSITIVE "+" - the negative text is the space (SR12c): 5 -> "  +5", -5 -> "   5".
*>   De-editing reads the sign from the landed "+" and not from the spaces of the suppressed positions.
*>   PIC --.T9 EDITING T IS "-": the floating string ends at the decimal point, so the "-" literal of T
*>   after it is not the sign: 0.5 -> "  .-5" (positive), -0.5 -> " -.-5" (the sign lands before the point).
identification division.
program-id. PB4703EMB.
environment division.
configuration section.
special-names.
    currency sign is "USD" with picture symbol "$".
data division.
working-storage section.
01 fa   pic LLTLL9 editing L for negative is "-" positive is "+" editing T is ":".
01 fb   pic LLTLL9 editing L for negative is "-" positive is "+" editing T is "-".
01 fe   pic LLTLL9 editing L for negative is "((" positive is "))" editing T is ":".
01 fw   pic LLTLL9 editing L for negative is "((" editing T is ":".
01 ca   pic $$B$$9.
01 cb   pic $$,$$9.
01 hp   pic LLL9 editing L for positive is "+".
01 ip   pic --.T9 editing T is "-".
01 src  pic s9v9.
01 n    pic s9(5) sign leading separate.
01 m    pic s9v9 sign leading separate.
procedure division.
    move -555 to fa
    move fa to n
    display "A[" fa "] " n
    move 555 to fa
    move fa to n
    display "B[" fa "] " n
    move -1555 to fa
    move fa to n
    display "C[" fa "] " n
    move -555 to fb
    move fb to n
    display "D1[" fb "] " n
    move 1555 to fb
    move fb to n
    display "D2[" fb "] " n
    move -555 to fe
    move fe to n
    display "E1[" fe "] " n
    move 555 to fe
    move fe to n
    display "E2[" fe "] " n
    move 555 to fw
    move fw to n
    display "W1[" fw "] " n
    move -555 to fw
    move fw to n
    display "W2[" fw "] " n
    move 555 to ca
    move ca to n
    display "F1[" ca "] " n
    move 1555 to ca
    move ca to n
    display "F2[" ca "] " n
    move 1555 to cb
    move cb to n
    display "F3[" cb "] " n
    move 5 to hp
    move hp to n
    display "G1[" hp "] " n
    move -5 to hp
    move hp to n
    display "G2[" hp "] " n
    move 0.5 to src
    move src to ip
    move ip to m
    display "H1[" ip "] " m
    move -0.5 to src
    move src to ip
    move ip to m
    display "H2[" ip "] " m
    stop run.
