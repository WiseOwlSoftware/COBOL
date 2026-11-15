*>--- conformance-case
*> id: select_when_code_set_mixed_usage
*> rules: [SR-13.18.13.3-3, GR-13.18.13.4-4]
*> accept: [2002, 2014, 2023]
*> constructs: [select-when-clause-2002]
*> source-format: free
*> portable: unverified
*>---
*> ISO/IEC 1989:2023 13.18.13.3 SR3 (ISO/IEC 1989:2002 13.16.13.2 SR3): "If there are record description entries associated with the file and no SELECT
*> WHEN clauses are specified, either alphabet-name-1 or alphabet-name-2 may be specified, but not both; and: a) if alphabet-name-1 is specified,
*> all elementary data items of all record description entries associated with the file shall be described as usage display". With SELECT WHEN clauses
*> the rule does not apply, and GR4 converts "each data item described with usage display in the selected record description entry": the binary
*> item of the first entry is no usage display item and crosses the medium as it is. STANDARD-1 is ISO/IEC 646, the identity over the characters of
*> both records. Why it can fail: the binary item refused at compile time as SR3 would refuse it, or converted as a character.
IDENTIFICATION DIVISION.
PROGRAM-ID. SWCS23.
ENVIRONMENT DIVISION.
CONFIGURATION SECTION.
SPECIAL-NAMES.
    ALPHABET ISO646 IS STANDARD-1.
INPUT-OUTPUT SECTION.
FILE-CONTROL.
    SELECT F ASSIGN TO "SWCS23.dat" ORGANIZATION IS SEQUENTIAL.
DATA DIVISION.
FILE SECTION.
FD F CODE-SET IS ISO646.
01 REC-A SELECT WHEN IS-A.
    05 A-KEY  PIC X.
        88 IS-A VALUE "A".
    05 A-NUM  PIC S9(4) COMP.
    05 A-TEXT PIC X(3).
01 REC-B SELECT WHEN OTHER.
    05 B-KEY  PIC X.
    05 B-TEXT PIC X(6).
PROCEDURE DIVISION.
MAIN.
    OPEN OUTPUT F.
    MOVE "A" TO A-KEY.
    MOVE 258 TO A-NUM.
    MOVE "XYZ" TO A-TEXT.
    WRITE REC-A.
    MOVE "B" TO B-KEY.
    MOVE "abcdef" TO B-TEXT.
    WRITE REC-B.
    CLOSE F.
    OPEN INPUT F.
    READ F AT END DISPLAY "UNEXPECTED END OF FILE" END-READ.
    IF IS-A AND A-NUM = 258 AND A-TEXT = "XYZ"
        DISPLAY "A RESTORED"
    ELSE
        DISPLAY "A WRONG"
    END-IF.
    READ F AT END DISPLAY "UNEXPECTED END OF FILE" END-READ.
    IF NOT IS-A AND B-KEY = "B" AND B-TEXT = "abcdef"
        DISPLAY "B RESTORED"
    ELSE
        DISPLAY "B WRONG"
    END-IF.
    CLOSE F.
    STOP RUN.
