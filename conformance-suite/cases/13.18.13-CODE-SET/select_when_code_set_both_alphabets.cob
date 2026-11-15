*>--- conformance-case
*> id: select_when_code_set_both_alphabets
*> rules: [SR-13.18.13.3-3, GR-13.18.13.4-4]
*> accept: [2002, 2014, 2023]
*> constructs: [select-when-clause-2002]
*> source-format: free
*> portable: unverified
*>---
*> ISO/IEC 1989:2023 13.18.13.3 SR3 (ISO/IEC 1989:2002 13.16.13.2 SR3): "If there are record description entries associated with the file and no SELECT
*> WHEN clauses are specified, either alphabet-name-1 or alphabet-name-2 may be specified, but not both"; with SELECT WHEN clauses the restriction does
*> not apply, and both alphabets are legal: GR4 converts the alphanumeric items of the selected entry by alphabet-name-1 and its national items by
*> alphabet-name-2. The first entry holds alphanumeric items, the second a national one; each record reads back as written.
*> Why it can fail: both names refused as SR3 refuses them for a file without SELECT WHEN clauses.
IDENTIFICATION DIVISION.
PROGRAM-ID. SWBA23.
ENVIRONMENT DIVISION.
CONFIGURATION SECTION.
SPECIAL-NAMES.
    ALPHABET A-EBC IS EBCDIC
    ALPHABET A-NAT FOR NATIONAL IS UTF-16.
INPUT-OUTPUT SECTION.
FILE-CONTROL.
    SELECT FE ASSIGN TO "SWBA23.dat" ORGANIZATION IS SEQUENTIAL.
DATA DIVISION.
FILE SECTION.
FD FE CODE-SET FOR ALPHANUMERIC IS A-EBC FOR NATIONAL IS A-NAT.
01 REC-A SELECT WHEN IS-A.
    05 A-KEY PIC X.
        88 IS-A VALUE "A".
    05 A-TXT PIC X(4).
01 REC-B SELECT WHEN OTHER.
    05 B-KEY PIC X.
    05 B-NAT PIC N(2).
PROCEDURE DIVISION.
MAIN.
    OPEN OUTPUT FE.
    MOVE "A" TO A-KEY.
    MOVE "AB01" TO A-TXT.
    WRITE REC-A.
    MOVE "B" TO B-KEY.
    MOVE N"xy" TO B-NAT.
    WRITE REC-B.
    CLOSE FE.
    OPEN INPUT FE.
    READ FE AT END DISPLAY "UNEXPECTED END OF FILE" END-READ.
    DISPLAY "FIRST [" REC-A "]".
    READ FE AT END DISPLAY "UNEXPECTED END OF FILE" END-READ.
    DISPLAY "SECOND [" B-KEY "][" B-NAT "]".
    CLOSE FE.
    STOP RUN.
