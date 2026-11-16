*>--- conformance-case
*> id: pb5578-record-size-variable-occurrence-over
*> rules: [2002-GR-13.16.41.3-3]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB5578 - §2002:13.16.41.3 GR3 counts a variable-occurrence table at its MAXIMUM ("the sum of the maximum number
*> of bytes in any variable-length data item subordinate to the record"), so FO-REC is 5 bytes and RECORD CONTAINS 4
*> CHARACTERS breaks §2002:13.16.41.2 SR2 ("No record description entry for the file may specify a number of bytes greater
*> than integer-1"). Expected: COBOL2010. The accepted control is pb5578_record_size_variable_occurrence_maximum.
IDENTIFICATION DIVISION.
PROGRAM-ID. PB5578RO.
ENVIRONMENT DIVISION.
INPUT-OUTPUT SECTION.
FILE-CONTROL.
    SELECT FO ASSIGN TO "pb5578ro.dat" ORGANIZATION SEQUENTIAL.
DATA DIVISION.
FILE SECTION.
FD FO RECORD CONTAINS 4 CHARACTERS.
01 FO-REC.
   05 FO-N PIC 9.
   05 FO-T PIC X OCCURS 1 TO 4 DEPENDING ON FO-N.
PROCEDURE DIVISION.
MAIN.
    STOP RUN.
