*>--- conformance-case
*> id: pb508_blank_when_zero_national_resultant
*> rules: [GR-13.18.8.4-2]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB508 - the refused companion of pb508_blank_when_zero_national.
*>
*> ISO/IEC 1989:2023 13.18.8.4 GR2: "If the subject of the entry is described by its picture character-string as
*> category numeric, the BLANK WHEN ZERO clause defines the item as numeric-edited." WS-NAT is category numeric by
*> its picture character-string and legal by 13.18.8.3 SR2 (usage national), so GR2 defines it numeric-edited.
*> 14.9.2.3 SR2: "Identifier-1 and identifier-2 shall reference numeric data items." A numeric-edited item is not a
*> numeric data item, so ADD 1 TO WS-NAT violates SR2 - COBOLNET1675. That refusal can only fire if the promotion
*> reached the national usage: were the item left a plain category-numeric one, this program would compile and
*> add. The same program with the usage display refuses identically (the display twin is the control).
*> Refused at every edition that has national data; below COBOL-2002 the USAGE NATIONAL item is itself refused
*> (pb646-national-form-numeric-at-85).
IDENTIFICATION DIVISION.
PROGRAM-ID. PB508BWZR.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 WS-NAT PIC 9(5) USAGE NATIONAL BLANK WHEN ZERO.
PROCEDURE DIVISION.
MAIN.
    ADD 1 TO WS-NAT
    STOP RUN.
