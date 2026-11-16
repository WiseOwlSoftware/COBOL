*>--- conformance-case
*> id: pb5578_record_size_variable_occurrence_maximum
*> rules: [2002-GR-13.16.41.3-3]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB5578 - the size of a record holding a variable-occurrence table, at COBOL-2002.
*> THE RULE (cite.py --check, each OK):
*>   §2002:13.16.41.3 GR3 "The size of a record is determined by the sum of the number of bytes in all fixed length
*>     elementary items plus the sum of the maximum number of bytes in any variable-length data item subordinate to the
*>     record": FO-REC is the 1-byte FO-N plus FO-T OCCURS 1 TO 4, at most 4 bytes, so its size is 5.
*>   §2002:13.16.41.2 SR2 "No record description entry for the file may specify a number of bytes greater than integer-1":
*>     RECORD CONTAINS 5 CHARACTERS is accepted here; its control, pb5578-record-size-variable-occurrence-over, states 4
*>     and is refused (COBOL2010), so a size taken at the minimum (2) or at the current count would accept both.
*> The program only proves translation (an empty .out).
IDENTIFICATION DIVISION.
PROGRAM-ID. PB5578RS.
ENVIRONMENT DIVISION.
INPUT-OUTPUT SECTION.
FILE-CONTROL.
    SELECT FO ASSIGN TO "pb5578rs.dat" ORGANIZATION SEQUENTIAL.
DATA DIVISION.
FILE SECTION.
FD FO RECORD CONTAINS 5 CHARACTERS.
01 FO-REC.
   05 FO-N PIC 9.
   05 FO-T PIC X OCCURS 1 TO 4 DEPENDING ON FO-N.
PROCEDURE DIVISION.
MAIN.
    STOP RUN.
