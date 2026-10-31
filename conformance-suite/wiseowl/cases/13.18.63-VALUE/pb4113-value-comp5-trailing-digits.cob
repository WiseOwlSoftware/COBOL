*>--- conformance-case
*> id: pb4113-value-comp5-trailing-digits
*> rules: [SR-13.18.63.3-2, DOC-A.1-208]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB4113, the trailing side of pb4113_value_comp5_container_range. 13.18.63.3 SR2: the values specified "shall be
*> representable exactly in the subject of the entry, without truncation of leading or trailing nonzero digits". The
*> container widens only the LEADING side of a COMP-5 item's range; the PICTURE's scale still fixes how many places it
*> stores, so 1.234 in PIC S99V99 COMP-5 truncates a trailing nonzero digit and is refused, as it is for any
*> numeric item of that PICTURE.
IDENTIFICATION DIVISION.
PROGRAM-ID. PB4113TRAIL.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 A1 PIC S99V99 COMP-5 VALUE 1.234.
PROCEDURE DIVISION.
MAIN-PARA.
    STOP RUN.
