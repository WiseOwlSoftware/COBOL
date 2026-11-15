*>--- conformance-case
*> id: format_file_opened_i_o
*> rules: [SR-14.9.27.3-9, 2002-SR-14.8.26.2-7]
*> constructs: [format-clause-2002]
*> source-format: free
*> portable: unverified
*>---
*> 14.9.27.3 SR9 (2002 14.8.26.2 SR7): "The I-O phrase shall not be specified if the FORMAT clause is specified in the file description entry for
*> file-name-1." OPEN I-O of a FORMAT file is refused.
IDENTIFICATION DIVISION.
PROGRAM-ID. FSR5.
ENVIRONMENT DIVISION.
INPUT-OUTPUT SECTION.
FILE-CONTROL.
    SELECT F ASSIGN TO "FSR5.dat" ORGANIZATION IS SEQUENTIAL.
DATA DIVISION.
FILE SECTION.
FD F RECORD IS VARYING IN SIZE FROM 1 TO 20 CHARACTERS FORMAT CHARACTER DATA.
01 F-REC PIC X(20).
PROCEDURE DIVISION.
MAIN.
    OPEN I-O F.
    STOP RUN.
