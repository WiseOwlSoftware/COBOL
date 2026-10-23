*>--- conformance-case
*> id: pb3562_group_move_function_values
*> rules: [GR-14.9.25.4-4, RV-15.13.4-1, GR-15.2-2]
*> accept: [2002, 2014, 2023]
*> source-format: free
*>---
*> kb/Work PB3562. A MOVE to an ALPHANUMERIC GROUP is ISO 14.9.25.4
*> GR4's group move:
*>   cite.py --check 14.9.25.4 "there is no conversion of data from one form of internal representation to another"
*>   -> OK  14.9.25.4 4)  (General rules)
*> so a sender's REPRESENTATION lands in the group's storage, whatever its
*> usage. A boolean function's returned value has one:
*>   cite.py --check 15.13.4 "The returned value is a boolean item of usage bit"
*>   -> OK  15.13.4 1)  (Returned value rule)
*>   cite.py --check 15.2 "Boolean functions have an implicit usage bit"
*>   -> OK  15.2 2)  (Types of functions)
*> and a USAGE clause at a group level "applies only to each elementary
*> item in the group":
*>   cite.py --check 13.18.60.4 "If the USAGE clause is specified or implied at a group level, it applies only to each elementary item in the group"
*>   -> OK  13.18.60.4 1)  (General rules)
*> so G8 and G16 below are ALPHANUMERIC groups of eight and sixteen
*> one-bit items (one and two bytes of packed bits, 8.5.1.6.3), and the
*> function value lands as its packed bits, exactly as an elementary
*> USAGE BIT item holding the same value does (control I1).
*> A national function's value is usage national (15.2 3), so it lands as
*> two UTF-16BE bytes a position, as a national item does (control N2).
*> Usage bit, usage national and the boolean functions are new in 2002.
*> EXPECTED OUTPUT, derived line by line:
*>   F1  BOOLEAN-OF-INTEGER(165 8): 165 = 128+32+4+1, 15.13.4 1) "the
*>       rightmost boolean position is the low-order binary digit" ->
*>       10100101. (Before the fix the group held the TEXT of the value,
*>       its first character '1' = X"31" = 00110001.)
*>   F2  BOOLEAN-OF-INTEGER(43690 16): 43690 = X"AAAA" -> 1010101010101010
*>       (before: the text's first two characters, 0011000100110000).
*>   F3  BOOLEAN-OF-INTEGER(165 8) into H8, a GROUP-USAGE BIT group (an
*>       elementary bit item for MOVE, 14.9.25.4 GR4): 10100101.
*>   N1  NATIONAL-OF("GH") into NG, an alphanumeric group holding a
*>       PIC N(2) item: the two national characters GH.
*>   N2  the national item NI (N"AB") into NG: AB (control).
*>   N3  the national literal N"EF" into NG: its value is "the string of
*>       national characters" (8.3.3.5.4 GR3 b), usage national, so EF.
*>       (Before the fix the literal's two characters landed as two
*>       alphanumeric bytes, X"4546", read back as the one character
*>       U+4546.)
*>   I1  the bit item E8 (10100101) into G8: 10100101 (control).
identification division.
program-id. pb3562fv.
data division.
working-storage section.
01 g8 usage bit.
   05 gb occurs 8 times pic 1.
01 g16 usage bit.
   05 gc occurs 16 times pic 1.
01 h8 group-usage bit.
   05 hb occurs 8 times pic 1.
01 e8 pic 1(8) usage bit value b"10100101".
01 ng.
   05 nn pic n(2).
01 ni pic n(2) value n"AB".
procedure division.
main-para.
    move function boolean-of-integer(165 8) to g8
    if gb(1) = b"1" and gb(2) = b"0" and gb(3) = b"1" and gb(4) =
       b"0" and gb(5) = b"0" and gb(6) = b"1" and gb(7) = b"0" and
       gb(8) = b"1"
       display "F1 T" else display "F1 F" end-if
    move function boolean-of-integer(43690 16) to g16
    if gc(1) = b"1" and gc(2) = b"0" and gc(3) = b"1" and gc(4) =
       b"0" and gc(5) = b"1" and gc(6) = b"0" and gc(7) = b"1" and
       gc(8) = b"0" and gc(9) = b"1" and gc(10) = b"0" and gc(11) =
       b"1" and gc(12) = b"0" and gc(13) = b"1" and gc(14) = b"0"
       and gc(15) = b"1" and gc(16) = b"0"
       display "F2 T" else display "F2 F" end-if
    move function boolean-of-integer(165 8) to h8
    if hb(1) = b"1" and hb(2) = b"0" and hb(3) = b"1" and hb(4) =
       b"0" and hb(5) = b"0" and hb(6) = b"1" and hb(7) = b"0" and
       hb(8) = b"1"
       display "F3 T" else display "F3 F" end-if
    move function national-of("GH") to ng
    if nn = n"GH" display "N1 T" else display "N1 F" end-if
    move ni to ng
    if nn = n"AB" display "N2 T" else display "N2 F" end-if
    move n"EF" to ng
    if nn = n"EF" display "N3 T" else display "N3 F" end-if
    move e8 to g8
    if gb(1) = b"1" and gb(2) = b"0" and gb(3) = b"1" and gb(4) =
       b"0" and gb(5) = b"0" and gb(6) = b"1" and gb(7) = b"0" and
       gb(8) = b"1"
       display "I1 T" else display "I1 F" end-if
    stop run.
end program pb3562fv.
