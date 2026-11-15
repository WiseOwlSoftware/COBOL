*>--- conformance-case
*> id: select_when_code_set_rewrite_record_name_by_edition
*> rules: [GR-13.18.13.4-4, GR-13.18.51.4-2]
*> accept: [2002, 2014, 2023]
*> constructs: [select-when-clause-2002]
*> source-format: free
*> portable: unverified
*>---
*> A REWRITE REC-A (record-name-1, no FILE phrase) of a CODE-SET file whose entries carry SELECT WHEN clauses, after the tag of the record has been changed so
*> that the conditions select the OTHER entry. REC-A is five display characters; REC-B is one display character and a four-byte binary item, which the alphabet
*> does not convert (§2023:13.18.13.4 GR4: "each data item described with usage display in the selected record description entry").
*> 2002 (.2002.out): §2002:13.16.49.3 GR1 evaluates the clauses "for READ, REWRITE, and WRITE statements associated with that file when a CODE-SET clause or FORMAT
*> clause is specified", so the REWRITE selects REC-B (tag B) and converts its first byte only: the medium holds C2 (194) and the four bytes as they are, 67 68 50 51 (shown 067 068 050 051).
*> 2014 and 2023 (.out): §2023:13.18.51.4 GR2 gives the clauses no effect for a REWRITE without the FILE phrase (§2014:13.18.51.3 GR2 the same), so the record is the
*> entry the statement names, REC-A, and all five characters are converted: 194 195 196 242 243. Why it can fail: the conditions evaluated at 2014 and after, or not at 2002.
IDENTIFICATION DIVISION.
PROGRAM-ID. SWRW24.
ENVIRONMENT DIVISION.
CONFIGURATION SECTION.
SPECIAL-NAMES.
    ALPHABET A-EBC IS EBCDIC.
INPUT-OUTPUT SECTION.
FILE-CONTROL.
    SELECT FE ASSIGN TO "SWRW24.dat" ORGANIZATION IS SEQUENTIAL FILE STATUS IS ES.
    SELECT FR ASSIGN TO "SWRW24.dat" ORGANIZATION IS SEQUENTIAL.
DATA DIVISION.
FILE SECTION.
FD FE CODE-SET IS A-EBC.
01 REC-A SELECT WHEN IS-A.
    05 A-KEY PIC X.
        88 IS-A VALUE "A".
    05 A-TXT PIC X(4).
01 REC-B SELECT WHEN OTHER.
    05 B-KEY PIC X.
    05 B-BIN PIC 9(8) COMP.
FD FR.
01 RR PIC X(5).
WORKING-STORAGE SECTION.
01 ES PIC XX.
01 I PIC 9.
01 N1 PIC 999.
PROCEDURE DIVISION.
MAIN.
    OPEN OUTPUT FE.
    MOVE "A" TO A-KEY.
    MOVE "AB01" TO A-TXT.
    WRITE REC-A.
    CLOSE FE.
    OPEN I-O FE.
    READ FE AT END DISPLAY "UNEXPECTED END OF FILE" END-READ.
    MOVE "B" TO A-KEY.
    MOVE "CD23" TO A-TXT.
    REWRITE REC-A.
    DISPLAY "REWRITE " ES.
    CLOSE FE.
    OPEN INPUT FR.
    READ FR AT END DISPLAY "UNEXPECTED END OF FILE" END-READ.
    PERFORM VARYING I FROM 1 BY 1 UNTIL I > 5
        COMPUTE N1 = FUNCTION ORD(RR(I:1)) - 1
        DISPLAY "MEDIUM " I " " N1
    END-PERFORM.
    CLOSE FR.
    STOP RUN.
