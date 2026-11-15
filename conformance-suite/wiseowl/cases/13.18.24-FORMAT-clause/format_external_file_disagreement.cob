*>--- conformance-case
*> id: format_external_file_disagreement
*> rules: [GR-13.18.24.4-12, 2002-GR-13.16.22.3-12]
*> constructs: [format-clause-2002]
*> source-format: free
*> portable: unverified
*>---
*> 13.18.24.4 GR12 (2002 13.16.22.3 GR12): "If the associated file connector is an external file connector, all file description entries in the run unit that
*> are associated with that file connector shall specify the same FORMAT clause." The two programs of this compilation group describe one external file; one
*> specifies FORMAT CHARACTER and the other none: refused.
IDENTIFICATION DIVISION.
PROGRAM-ID. FEXT.
ENVIRONMENT DIVISION.
INPUT-OUTPUT SECTION.
FILE-CONTROL.
    SELECT F ASSIGN TO "fext.dat" ORGANIZATION IS SEQUENTIAL.
DATA DIVISION.
FILE SECTION.
FD F IS EXTERNAL RECORD IS VARYING IN SIZE FROM 1 TO 20 CHARACTERS FORMAT CHARACTER DATA.
01 F-REC PIC X(20).
PROCEDURE DIVISION.
MAIN.
    CALL "FEXTINNER".
    STOP RUN.
IDENTIFICATION DIVISION.
PROGRAM-ID. FEXTINNER.
ENVIRONMENT DIVISION.
INPUT-OUTPUT SECTION.
FILE-CONTROL.
    SELECT F ASSIGN TO "fext.dat" ORGANIZATION IS SEQUENTIAL.
DATA DIVISION.
FILE SECTION.
FD F IS EXTERNAL RECORD IS VARYING IN SIZE FROM 1 TO 20 CHARACTERS.
01 F-REC PIC X(20).
PROCEDURE DIVISION.
INNER.
    EXIT PROGRAM.
END PROGRAM FEXTINNER.
END PROGRAM FEXT.
