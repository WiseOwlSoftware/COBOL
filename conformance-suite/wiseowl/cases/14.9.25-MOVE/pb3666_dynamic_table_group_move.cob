*>--- conformance-case
*> id: pb3666_dynamic_table_group_move
*> rules: [GR-14.6.9.2-2, GR-14.9.25.4-6]
*> accept: [2014, 2023]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB3666 (the sibling path PB3598's review found), docs/CONFORMANCE.md D-B5: a variable-length group MOVE
*> between corresponding dynamic-capacity tables moves "Correspondingly numbered elements ... according to the rules of
*> the MOVE statement" (14.6.9.2), so the element MOVE of a PIC X sender to a PIC 1(3) USAGE BIT receiver is the
*> elementary MOVE of Table 16 (alphanumeric to boolean) and converts each character as the determination says:
*>   cite.py --check 14.6.9.2 "Correspondingly numbered elements are moved according to the rules of the MOVE statement"
*>   -> OK  14.6.9.2
*> The two elements are one byte each (8.5.1.12.3 matches tables whose elements have equal byte length), the
*> sender's elements Z, 1 and A; each converts ('1' to one, any other character to zero) and the receiver fills
*> the rest of its three positions with boolean zeros (14.6.8.6):
*>   T1 (1) = 000   T1 (2) = 100   T1 (3) = 000
*> and the operators agree: B-NOT of element 1 is 111. Before the fix element 1 displayed Z00 and element 3 A00.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3666DT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 G2.
          05 T2 PIC X OCCURS DYNAMIC CAPACITY IN C2 FROM 3.
       01 G1.
          05 T1 PIC 1(3) USAGE BIT OCCURS DYNAMIC CAPACITY IN C1 FROM 1.
       01 RN PIC 1(3).
       PROCEDURE DIVISION.
       MAIN-PARA.
           MOVE "Z" TO T2 (1)
           MOVE "1" TO T2 (2)
           MOVE "A" TO T2 (3)
           MOVE G2 TO G1
           DISPLAY "T1 " T1 (1) " " T1 (2) " " T1 (3)
           COMPUTE RN = B-NOT T1 (1)
           DISPLAY "NOT " RN
           STOP RUN.
