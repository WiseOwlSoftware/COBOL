*>--- conformance-case
*> id: format_sr2_redefines_in_record
*> rules: [SR-13.18.24.3-2, 2002-SR-13.16.22.2-2, 2002-SR-13.16.42.2-3]
*> constructs: [format-clause-2002]
*> source-format: free
*> portable: unverified
*>---
*> 13.18.24.3 SR2 (2002 13.16.22.2 SR2): "The REDEFINES clause and the RENAMES clause shall not be specified in a record description entry associated
*> with a file description entry in which a FORMAT clause is specified"; REDEFINES SR3 (2002 13.16.42.2 SR3) says the same of the entries subordinate
*> to such a file description entry. The REDEFINES clause beneath the FORMAT file description entry is refused.
IDENTIFICATION DIVISION.
PROGRAM-ID. FSR2.
ENVIRONMENT DIVISION.
INPUT-OUTPUT SECTION.
FILE-CONTROL.
    SELECT F ASSIGN TO "FSR2.dat" ORGANIZATION IS SEQUENTIAL.
DATA DIVISION.
FILE SECTION.
FD F RECORD IS VARYING IN SIZE FROM 1 TO 20 CHARACTERS FORMAT CHARACTER DATA.
01 F-REC.
    05 F-NUM PIC 9(4).
    05 F-TEXT REDEFINES F-NUM PIC X(4).
PROCEDURE DIVISION.
MAIN.
    STOP RUN.
