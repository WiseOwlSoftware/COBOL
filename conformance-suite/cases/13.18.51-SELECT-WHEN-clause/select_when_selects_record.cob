*>--- conformance-case
*> id: select_when_selects_record
*> rules: [GR-13.18.51.4-3, GR-13.18.51.4-1, SR-13.18.51.3-1, SR-13.18.51.3-2, SR-13.18.51.3-6]
*> accept: [2002, 2014, 2023]
*> constructs: [format-clause-2002, select-when-clause-2002]
*> source-format: free
*> portable: unverified
*>---
*> ISO/IEC 1989:2023 13.18.51 (ISO/IEC 1989:2002 13.16.49) SELECT WHEN clause. "The condition-name condition specified in a SELECT WHEN clause is
*> evaluated for each record description entry, in the order the record description entries are written, until the evaluation is true. When the
*> evaluation is true, the associated record description entry is selected for use with the CODE-SET and FORMAT clauses. The evaluation is
*> always true when the OTHER phrase is specified." (GR3; 2002 GR3). Three record description entries share the record area; the first holds a
*> packed number, the second text, the third a binary number, and each is read back by the entry its tag selects. Why it can fail: the wrong
*> entry formats the record (a packed item read as text or the reverse), or OTHER is not the entry the unmatched tag falls to.
IDENTIFICATION DIVISION.
PROGRAM-ID. SWSEL23.
ENVIRONMENT DIVISION.
INPUT-OUTPUT SECTION.
FILE-CONTROL.
    SELECT F ASSIGN TO "SWSEL23.dat" ORGANIZATION IS SEQUENTIAL.
DATA DIVISION.
FILE SECTION.
FD F RECORD IS VARYING IN SIZE FROM 1 TO 100 CHARACTERS
    FORMAT CHARACTER NUMERIC DATA.
01 REC-A SELECT WHEN IS-A.
    05 A-KEY  PIC X.
        88 IS-A VALUE "A".
    05 A-NUM  PIC S9(5) COMP-3.
01 REC-B SELECT WHEN IS-B.
    05 B-KEY  PIC X.
        88 IS-B VALUE "B".
    05 B-TEXT PIC X(6).
01 REC-C SELECT WHEN OTHER.
    05 C-KEY  PIC X.
    05 C-NUM  PIC S9(4) COMP.
PROCEDURE DIVISION.
MAIN.
    OPEN OUTPUT F.
    MOVE "A" TO A-KEY.
    MOVE -12345 TO A-NUM.
    WRITE REC-A.
    MOVE "B" TO B-KEY.
    MOVE "SECOND" TO B-TEXT.
    WRITE REC-B.
    MOVE "Z" TO C-KEY.
    MOVE 77 TO C-NUM.
    WRITE REC-C.
    CLOSE F.
    OPEN INPUT F.
    READ F AT END DISPLAY "UNEXPECTED END OF FILE" END-READ.
    IF IS-A AND A-NUM = -12345
        DISPLAY "RECORD A"
    ELSE
        DISPLAY "RECORD A WRONG"
    END-IF.
    READ F AT END DISPLAY "UNEXPECTED END OF FILE" END-READ.
    IF IS-B AND B-TEXT = "SECOND"
        DISPLAY "RECORD B"
    ELSE
        DISPLAY "RECORD B WRONG"
    END-IF.
    READ F AT END DISPLAY "UNEXPECTED END OF FILE" END-READ.
    IF NOT IS-A AND NOT IS-B AND C-KEY = "Z" AND C-NUM = 77
        DISPLAY "RECORD OTHER"
    ELSE
        DISPLAY "RECORD OTHER WRONG"
    END-IF.
    CLOSE F.
    STOP RUN.
