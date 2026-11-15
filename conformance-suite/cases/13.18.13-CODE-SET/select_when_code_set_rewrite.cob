*>--- conformance-case
*> id: select_when_code_set_rewrite
*> rules: [GR-13.18.13.4-4, GR-13.18.13.4-6, GR-13.18.51.4-1]
*> accept: [2002, 2014, 2023]
*> constructs: [select-when-clause-2002]
*> source-format: free
*> portable: unverified
*>---
*> ISO/IEC 1989:2023 13.18.13.4 GR4 (ISO/IEC 1989:2002 13.16.13.3 GR4): "If record description entries associated with the file contain a SELECT WHEN
*> clause, a record description entry is selected by evaluation of those SELECT WHEN clauses." 13.18.51.4 GR1 evaluates the clauses "for READ statements,
*> for REWRITE statements with the FILE phrase, and for WRITE statements with the FILE phrase, if the associated file description entry also contains a
*> CODE-SET clause or FORMAT clause"; a file with SELECT WHEN clauses is therefore converted by the alphabet on a REWRITE as on a WRITE. The file is
*> opened I-O (SR9 of 14.9.27.3 forbids that only with a FORMAT clause), its first record is rewritten, and the medium is read through a file
*> description without CODE-SET: the rewritten record holds CCSID 37 codes (C1 C3 C4 F2 F3 = 193 195 196 242 243), and the file reads back through
*> the CODE-SET file as ACD23. Why it can fail: the REWRITE puts the native characters on the medium, a mixed-code file that reads back as other characters.
IDENTIFICATION DIVISION.
PROGRAM-ID. SWRW23.
ENVIRONMENT DIVISION.
CONFIGURATION SECTION.
SPECIAL-NAMES.
    ALPHABET A-EBC IS EBCDIC.
INPUT-OUTPUT SECTION.
FILE-CONTROL.
    SELECT FE ASSIGN TO "SWRW23.dat" ORGANIZATION IS SEQUENTIAL FILE STATUS IS ES.
    SELECT FR ASSIGN TO "SWRW23.dat" ORGANIZATION IS SEQUENTIAL.
DATA DIVISION.
FILE SECTION.
FD FE CODE-SET IS A-EBC.
01 REC-A SELECT WHEN IS-A.
    05 A-KEY PIC X.
        88 IS-A VALUE "A".
    05 A-TXT PIC X(4).
01 REC-B SELECT WHEN OTHER.
    05 B-KEY PIC X.
    05 B-TXT PIC X(4).
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
    MOVE "B" TO B-KEY.
    MOVE "EF45" TO B-TXT.
    WRITE REC-B.
    CLOSE FE.
    OPEN I-O FE.
    READ FE AT END DISPLAY "UNEXPECTED END OF FILE" END-READ.
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
    OPEN INPUT FE.
    READ FE AT END DISPLAY "UNEXPECTED END OF FILE" END-READ.
    DISPLAY "FIRST [" REC-A "]".
    READ FE AT END DISPLAY "UNEXPECTED END OF FILE" END-READ.
    DISPLAY "SECOND [" REC-B "]".
    CLOSE FE.
    STOP RUN.
