*>--- conformance-case
*> id: pb3562_boolean_literal_group_move
*> rules: [GR-14.9.25.4-4, GR-8.3.3.4.4-3]
*> accept: [2002, 2014, 2023]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB3562, docs/CONFORMANCE.md D-B3: THE USAGE OF A BOOLEAN LITERAL.
*> A MOVE to an alphanumeric group is ISO 14.9.25.4 GR4's group move, so
*> the sender's representation, hence its usage, decides the bytes:
*>   cite.py --check 14.9.25.4 "there is no conversion of data from one form of internal representation to another"
*>   -> OK  14.9.25.4 4)  (General rules)
*> The standard states a usage for a boolean function (15.2 2) "Boolean
*> functions have an implicit usage bit") but none for a boolean literal:
*>   cite.py --check 8.3.3.4.4 "the value of a boolean literal is the value of the sequence of occurrences of boolean-character-1"
*>   -> OK  8.3.3.4.4 3)  (General rules)
*> so the usage is the implementor's. WiseOwl COBOL gives the literal the
*> usage of a boolean function value, USAGE BIT: n positions are n packed
*> bits (the last byte padded with zero bits), and a USAGE clause at a
*> group level "applies only to each elementary item in the group"
*> (13.18.60.4 1)), so G8 below is an alphanumeric group of eight
*> one-bit items.
*> EXPECTED OUTPUT (the determination, applied):
*>   L1  B"01100110" into G8: the byte 01100110.
*>   L2  BX"A5" into G8 (8.3.3.4.4 GR5: A = 1010, 5 = 0101): 10100101.
*>   L3  the control, MOVE FUNCTION BOOLEAN-OF-INTEGER(102 8) TO G8
*>       (102 = 64+32+4+2): the same byte as L1, 01100110.
*> (Before the fix L1 held the TEXT of the literal: its first character
*> '0' = X"30" = 00110000.)
identification division.
program-id. pb3562bl.
data division.
working-storage section.
01 g8 usage bit.
   05 gb occurs 8 times pic 1.
procedure division.
main-para.
    move b"01100110" to g8
    display "L1 [" gb(1) gb(2) gb(3) gb(4) gb(5) gb(6) gb(7) gb(8) "]"
    move bx"A5" to g8
    display "L2 [" gb(1) gb(2) gb(3) gb(4) gb(5) gb(6) gb(7) gb(8) "]"
    move function boolean-of-integer(102 8) to g8
    display "L3 [" gb(1) gb(2) gb(3) gb(4) gb(5) gb(6) gb(7) gb(8) "]"
    stop run.
end program pb3562bl.
