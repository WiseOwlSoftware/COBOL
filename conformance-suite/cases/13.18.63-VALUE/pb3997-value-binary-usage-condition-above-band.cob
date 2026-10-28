*>--- conformance-case
*> id: pb3997-value-binary-usage-condition-above-band
*> rules: [SR-13.18.63.3-2]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB3997 - the level-88 arm of the USAGE range screen of pb3997-value-binary-usage-above-band.
*> ISO 1989:2023 13.18.63.3 SR2 - "If the category of the subject of the entry is numeric, all literals in the VALUE
*> clause shall be numeric and shall be permissible values within the range indicated by the PICTURE clause or the
*> USAGE clause" is an all-formats rule, and 13.18.63.4 GR19 gives a condition-name its conditional variable's
*> characteristics, so the BINARY-CHAR conditional variable's band (-128..127) bounds the literals of its
*> condition-names: the THRU end 200, the single value 300 and the WHEN SET TO FALSE value 128 are each refused at
*> every edition that has the usage (COBOL1625).
IDENTIFICATION DIVISION.
PROGRAM-ID. PB3997COND.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 B6 USAGE BINARY-CHAR VALUE 0.
   88 B6-BIG VALUE 100 THRU 200.
   88 B6-FAR VALUE 300.
   88 B6-OFF VALUE 5 WHEN SET TO FALSE 128.
PROCEDURE DIVISION.
MAIN-PARA.
    IF B6-BIG DISPLAY "BIG" END-IF
    STOP RUN.
