*>--- conformance-case
*> id: pb6259_options_initialize_alias_index_float
*> rules: [GR-14.6.2.3.2-1, GR-11.9.10.4-5]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB6259 review R1-1/R1-2 - a Tier-A (alias) REDEFINES class keeps its NATIVE field for the canonical item, so
*> OPTIONS INITIALIZE seeds a USAGE INDEX or a USAGE FLOAT-LONG there with the native value, not the image string
*> (ISO 1989:2023 14.6.2.3.2 GR1, 11.9.10.4 GR5). The program must compile and run; the fill X"00" makes the float zero.
IDENTIFICATION DIVISION.
PROGRAM-ID. PB6259OIAIF.
OPTIONS.
    INITIALIZE WORKING-STORAGE SECTION TO X"00".
DATA DIVISION.
WORKING-STORAGE SECTION.
01 I1 USAGE INDEX.
01 I2 REDEFINES I1 USAGE INDEX.
01 F1 USAGE FLOAT-LONG.
01 F2 REDEFINES F1 USAGE FLOAT-LONG.
PROCEDURE DIVISION.
MAIN-PARA.
    SET I1 TO I2
    DISPLAY "I-OK"
    IF F2 = 0 DISPLAY "F-ZERO" ELSE DISPLAY "F-NONZERO" END-IF
    STOP RUN.
