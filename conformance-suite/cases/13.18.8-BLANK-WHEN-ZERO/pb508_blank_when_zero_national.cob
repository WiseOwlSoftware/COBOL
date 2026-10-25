*>--- conformance-case
*> id: pb508_blank_when_zero_national
*> rules: [GR-13.18.8.4-1, GR-13.18.8.4-2]
*> accept: [2002, 2014, 2023]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB508 - BLANK WHEN ZERO on a category-numeric item of USAGE NATIONAL.
*>
*> ISO/IEC 1989:2023 13.18.8.3 SR2: "The subject of the entry shall be implicitly or explicitly described as usage
*> display or usage national." So PIC 9(5) USAGE NATIONAL BLANK WHEN ZERO is legal, and it is exactly the shape
*> 13.18.8.4 GR2 is written about: "If the subject of the entry is described by its picture character-string as
*> category numeric, the BLANK WHEN ZERO clause defines the item as numeric-edited." GR2's condition is the PICTURE
*> character-string's category; it does not mention the usage, so the display twin and the national item are
*> defined numeric-edited alike. The promotion once tested usage display alone, which would have left the national
*> item a plain numeric one that never blanks.
*>
*> GR1: "When the BLANK WHEN ZERO clause is specified for a data item, the content of the data item is set to all
*> spaces when the item is a receiving operand and the value being stored is zero."
*>
*> Each expected line below is derived from those two rules, not read off a run:
*>   MOVE 0 TO WS-NAT WS-DSP   zero is stored in both: GR1 sets each to all spaces, five national / alphanumeric
*>                             space characters, so the first line shows five spaces inside each bracket pair
*>                             (the national item blanks exactly as its display twin does).
*>   MOVE 12 TO WS-NAT         12 is not zero, so GR1 does not apply; the item is numeric-edited with picture 9(5)
*>                             (GR2) and editing a value with five digit positions gives the zero-filled "00012".
*>   MOVE WS-NAT TO WS-OUT     14.9.25.4 GR6 d) 1: "If the category of the sending operand is numeric-edited, ...
*>                             de-editing establishes the operand's numeric value", 12, so a PIC 9(5) receiver
*>                             holds "00012".
*> The refused companion is pb508_blank_when_zero_national_resultant (an arithmetic resultant of category
*> numeric-edited is a syntax-rule violation, which is only true if GR2 defined the item numeric-edited).
IDENTIFICATION DIVISION.
PROGRAM-ID. PB508BWZN.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 WS-NAT PIC 9(5) USAGE NATIONAL BLANK WHEN ZERO.
01 WS-DSP PIC 9(5) BLANK WHEN ZERO.
01 WS-OUT PIC 9(5).
PROCEDURE DIVISION.
MAIN.
    MOVE 0 TO WS-NAT WS-DSP
    DISPLAY "N=[" WS-NAT "] D=[" WS-DSP "]"
    MOVE 12 TO WS-NAT
    DISPLAY "N=[" WS-NAT "]"
    MOVE WS-NAT TO WS-OUT
    DISPLAY "O=[" WS-OUT "]"
    STOP RUN.
