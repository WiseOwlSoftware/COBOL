*>--- conformance-case
*> id: pb3997-value-binary-usage-above-band
*> rules: [SR-13.18.63.3-2]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB3997 - a VALUE literal outside the range the USAGE clause indicates is refused (COBOL1625).
*> ISO 1989:2023 13.18.63.3 SR2 - "If the category of the subject of the entry is numeric, all literals in the VALUE
*> clause shall be numeric and shall be permissible values within the range indicated by the PICTURE clause or the
*> USAGE clause." The band of a fixed-width binary usage is the native one of its byte width (13.18.60.4 GR12 allows
*> the implementor a wider range than the minimum; docs/CONFORMANCE.md DOC-A.1-206), so each entry below is one past
*> an edge of its usage's band, or far beyond it: BINARY-CHAR 255 and -129 and (SIGNED) 200, BINARY-CHAR UNSIGNED 256,
*> BINARY-SHORT 65535, BINARY-LONG 4294967295 and BINARY-DOUBLE 9223372036854775808. It shall be refused at every
*> edition that has the usage (the picture-digit span alone admitted all of them: BINARY-CHAR's three digits hold 255).
*> The in-band edges are pb3997_value_binary_usage_band_edges_2002.
IDENTIFICATION DIVISION.
PROGRAM-ID. PB3997ABOVE.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 BC USAGE BINARY-CHAR VALUE 255.
01 BCS USAGE BINARY-CHAR SIGNED VALUE 200.
01 BSH USAGE BINARY-SHORT VALUE 65535.
01 BL USAGE BINARY-LONG VALUE 4294967295.
01 BCU USAGE BINARY-CHAR UNSIGNED VALUE 256.
01 BCN USAGE BINARY-CHAR VALUE -129.
01 BD USAGE BINARY-DOUBLE VALUE 9223372036854775808.
PROCEDURE DIVISION.
MAIN-PARA.
    DISPLAY BC " " BCS " " BSH " " BL " " BCU " " BCN " " BD
    STOP RUN.
