*>--- conformance-case
*> id: pb4113-value-comp5-above-container-band
*> rules: [SR-13.18.63.3-2, DOC-A.1-208]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB4113, the refused side of pb4113_value_comp5_container_range. 13.18.63.3 SR2 holds a numeric VALUE literal to
*> "the range indicated by the PICTURE clause or the USAGE clause", and for COMP-5 the range is the container's
*> (docs/CONFORMANCE.md DOC-A.1-208), so each entry below is one past an edge of its band: PIC 9(4) COMP-5 (2 bytes) 65536,
*> PIC S9(4) COMP-5 32768 and -32769, PIC 9(2) COMP-5 (1 byte) 256, PIC S99V99 COMP-5 327.68 (the band at the PICTURE's
*> two places is -327.68 through 327.67), and a level-88 entry VALUE 65536 THRU 70000 over a 2-byte item. Each is refused
*> with the USAGE range named.
IDENTIFICATION DIVISION.
PROGRAM-ID. PB4113ABOVE.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 A1 PIC 9(4) COMP-5 VALUE 65536.
01 A2 PIC S9(4) COMP-5 VALUE 32768.
01 A3 PIC S9(4) COMP-5 VALUE -32769.
01 A4 PIC 9(2) COMP-5 VALUE 256.
01 A5 PIC S99V99 COMP-5 VALUE 327.68.
01 B1 PIC 9(4) COMP-5.
   88 TOO-BIG VALUE 65536 THRU 70000.
PROCEDURE DIVISION.
MAIN-PARA.
    STOP RUN.
