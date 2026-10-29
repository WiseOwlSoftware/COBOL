*>--- conformance-case
*> id: pb3997_value_binary_usage_band_edges_2002
*> rules: [SR-13.18.63.3-2]
*> accept: [2002, 2014, 2023]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB3997 - the over-rejection guard for the USAGE half of the VALUE range screen
*> (pb3997-value-binary-usage-above-band and pb3997-value-binary-usage-condition-above-band).
*> ISO 1989:2023 13.18.63.3 SR2 - "If the category of the subject of the entry is numeric, all literals in the VALUE
*> clause shall be numeric and shall be permissible values within the range indicated by the PICTURE clause or the
*> USAGE clause." 13.18.60.4 GR12 lets the implementor allow a wider range than the minimum one for the fixed-width
*> binary usages; WiseOwl COBOL provides the native two's-complement band of each byte width (docs/CONFORMANCE.md
*> DOC-A.1-206): BINARY-CHAR SIGNED -128..127, UNSIGNED 0..255; BINARY-SHORT -32768..32767 / 0..65535; BINARY-LONG
*> -2147483648..2147483647 / 0..4294967295; BINARY-DOUBLE -9223372036854775808..9223372036854775807 /
*> 0..18446744073709551615. Every item below carries a value AT an edge of its band, so every one shall compile and
*> hold its literal; the condition-name at the band's edge is true. A literal one past an edge is the refused
*> sibling. The values are read back through comparisons, which are edition-independent. Every item prints OK.
IDENTIFICATION DIVISION.
PROGRAM-ID. PB3997EDGES.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 C-LO  USAGE BINARY-CHAR VALUE -128.
01 C-HI  USAGE BINARY-CHAR VALUE 127.
01 C-UHI USAGE BINARY-CHAR UNSIGNED VALUE 255.
01 S-LO  USAGE BINARY-SHORT VALUE -32768.
01 S-UHI USAGE BINARY-SHORT UNSIGNED VALUE 65535.
01 L-LO  USAGE BINARY-LONG VALUE -2147483648.
01 L-UHI USAGE BINARY-LONG UNSIGNED VALUE 4294967295.
01 D-LO  USAGE BINARY-DOUBLE VALUE -9223372036854775808.
01 D-HI  USAGE BINARY-DOUBLE VALUE 9223372036854775807.
01 D-UHI USAGE BINARY-DOUBLE UNSIGNED VALUE 18446744073709551615.
01 C-LEVEL USAGE BINARY-CHAR VALUE 127.
   88 C-AT-EDGE VALUE 100 THRU 127.
PROCEDURE DIVISION.
MAIN-PARA.
    IF C-LO = -128 DISPLAY "C-LO OK" END-IF
    IF C-HI = 127 DISPLAY "C-HI OK" END-IF
    IF C-UHI = 255 DISPLAY "C-UHI OK" END-IF
    IF S-LO = -32768 DISPLAY "S-LO OK" END-IF
    IF S-UHI = 65535 DISPLAY "S-UHI OK" END-IF
    IF L-LO = -2147483648 DISPLAY "L-LO OK" END-IF
    IF L-UHI = 4294967295 DISPLAY "L-UHI OK" END-IF
    IF D-LO = -9223372036854775808 DISPLAY "D-LO OK" END-IF
    IF D-HI = 9223372036854775807 DISPLAY "D-HI OK" END-IF
    IF D-UHI = 18446744073709551615 DISPLAY "D-UHI OK" END-IF
    IF C-AT-EDGE DISPLAY "C-AT-EDGE OK" END-IF
    STOP RUN.
