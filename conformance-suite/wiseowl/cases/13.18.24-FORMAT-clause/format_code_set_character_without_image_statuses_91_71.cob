*>--- conformance-case
*> id: format_code_set_character_without_image_statuses_91_71
*> rules: [GR-13.18.13.4-6, GR-13.18.24.4-8]
*> accept: [2023]
*> constructs: [format-clause-2002]
*> source-format: free
*> portable: unverified
*>---
*> 13.18.13.4 GR6 (2002 13.16.13.3 GR6): the alphabet converts the data of a record to its coded character set when it is written. STANDARD-1 is ISO/IEC 646,
*> which has no code for the character E9 hex. A WRITE of a FORMAT CHARACTER record holding it is unsuccessful: status 91 for a record sequential file
*> (9.1.13.11), status 71 for a line sequential one (14.9.51.4 GR23), and the record is not written, so the next WRITE of ABCD answers 00.
*> Why it can fail: the character written as a substitute with status 00, or one status for both organizations.
IDENTIFICATION DIVISION.
PROGRAM-ID. FMW91.
ENVIRONMENT DIVISION.
CONFIGURATION SECTION.
SPECIAL-NAMES.
    ALPHABET ISO646 IS STANDARD-1.
INPUT-OUTPUT SECTION.
FILE-CONTROL.
    SELECT F ASSIGN TO "FMW91.dat" ORGANIZATION IS SEQUENTIAL FILE STATUS IS FS.
    SELECT L ASSIGN TO "FMW71.dat" ORGANIZATION IS LINE SEQUENTIAL FILE STATUS IS LS.
DATA DIVISION.
FILE SECTION.
FD F RECORD IS VARYING IN SIZE FROM 1 TO 100 CHARACTERS
    FORMAT CHARACTER DATA CODE-SET IS ISO646.
01 F-REC.
    05 F-TEXT PIC X(4).
FD L RECORD IS VARYING IN SIZE FROM 1 TO 100 CHARACTERS
    FORMAT CHARACTER DATA CODE-SET IS ISO646.
01 L-REC.
    05 L-TEXT PIC X(4).
WORKING-STORAGE SECTION.
01 FS PIC XX.
01 LS PIC XX.
PROCEDURE DIVISION.
MAIN.
    OPEN OUTPUT F.
    MOVE X"41E94243" TO F-TEXT.
    WRITE F-REC.
    DISPLAY "WRITE F " FS.
    MOVE "ABCD" TO F-TEXT.
    WRITE F-REC.
    DISPLAY "WRITE F AGAIN " FS.
    CLOSE F.
    OPEN OUTPUT L.
    MOVE X"41E94243" TO L-TEXT.
    WRITE L-REC.
    DISPLAY "WRITE L " LS.
    CLOSE L.
    STOP RUN.
