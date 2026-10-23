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
*> functions have an implicit usage bit") and for a national literal
*> (8.3.3.5.4 GR3 a) "usage NATIONAL") but none for a boolean literal:
*>   cite.py --check 8.3.3.4.4 "the value of a boolean literal is the value of the sequence of occurrences of boolean-character-1"
*>   -> OK  8.3.3.4.4 3)  (General rules)
*>   cite.py --check 8.3.3.4.3 "Boolean-character-1 shall be a boolean character, '0' or '1', from the computer's coded character set"
*>   -> OK  8.3.3.4.3 2)  (Syntax rules)
*> so the usage is the implementor's. WiseOwl COBOL reads the literal as
*> its characters, '0' and '1' of the coded character set: usage DISPLAY,
*> one byte a position (the one static-usage reader the compiler has,
*> IntrinsicArgumentRules.StaticUsageOf, says the same for every
*> argument screen). A boolean FUNCTION value is usage bit, so the two
*> deposit DIFFERENT bytes for the same eight boolean positions.
*> GX below is an alphanumeric group holding one PIC X(8) item.
*> EXPECTED OUTPUT (the determination, applied):
*>   L1  B"01100110" into GX: the eight characters 01100110.
*>   L2  BX"A5" into GX (8.3.3.4.4 GR5: A = 1010, 5 = 0101): 10100101.
*>   L3  MOVE FUNCTION BOOLEAN-OF-INTEGER(102 8) TO GX: usage bit, one
*>       packed byte, 102 = X"66" = "f", the group's tail space-filled:
*>       "f" and seven spaces (14.6.8 alphanumeric alignment).
*>   L4  the control: the item E8 (PIC 1(8) USAGE BIT holding the same
*>       eight positions) into GX: the same packed byte, "f" + 7 spaces.
identification division.
program-id. pb3562bl.
data division.
working-storage section.
01 gx.
   05 gxc pic x(8).
01 e8 pic 1(8) usage bit value b"01100110".
procedure division.
main-para.
    move b"01100110" to gx
    display "L1 [" gxc "]"
    move bx"A5" to gx
    display "L2 [" gxc "]"
    move function boolean-of-integer(102 8) to gx
    display "L3 [" gxc "]"
    move e8 to gx
    display "L4 [" gxc "]"
    stop run.
end program pb3562bl.
