*>--- conformance-case
*> id: select_when_without_code_set_or_format_has_no_effect
*> rules: [GR-13.18.51.4-5, DOC-A.1-173]
*> accept: [2002, 2014, 2023]
*> constructs: [select-when-clause-2002]
*> source-format: free
*> portable: unverified
*>---
*> 13.18.51.4 GR5: "The implementor shall specify whether a SELECT WHEN clause takes effect for READ statements, for REWRITE statements with the FILE phrase, or for WRITE
*> statements with the FILE phrase, when the associated file-description entry contains neither a CODE-SET clause nor a FORMAT clause." Annex A.1 item 173, docs/conformance/7/
*> DOC-A.1-173.md: in WiseOwl COBOL it takes effect for none. The record tagged Z is selected by no entry and is written and read with status 00, where a
*> file with a CODE-SET or FORMAT clause is I-O status 45 (GR4).
IDENTIFICATION DIVISION.
PROGRAM-ID. SWNO.
ENVIRONMENT DIVISION.
INPUT-OUTPUT SECTION.
FILE-CONTROL.
    SELECT F ASSIGN TO "swno.dat" ORGANIZATION IS SEQUENTIAL FILE STATUS IS FS.
DATA DIVISION.
FILE SECTION.
FD F RECORD CONTAINS 5 CHARACTERS.
01 F-A SELECT WHEN F-IS-A.
    05 F-KEY PIC X.
        88 F-IS-A VALUE "A".
    05 F-TEXT PIC X(4).
01 F-B SELECT WHEN F-IS-B.
    05 F-B-KEY PIC X.
        88 F-IS-B VALUE "B".
    05 F-B-TEXT PIC X(4).
WORKING-STORAGE SECTION.
01 FS PIC XX.
PROCEDURE DIVISION.
MAIN.
    OPEN OUTPUT F.
    MOVE "Z" TO F-KEY.
    MOVE "TEXT" TO F-TEXT.
    WRITE F-A.
    DISPLAY "WRITE " FS.
    CLOSE F.
    OPEN INPUT F.
    READ F AT END DISPLAY "UNEXPECTED END OF FILE" END-READ.
    DISPLAY "READ " FS.
    CLOSE F.
    STOP RUN.
