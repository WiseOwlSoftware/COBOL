*>--- conformance-case
*> id: pb5579_write_file_same_record_area
*> rules: [GR-14.9.51.4-8]
*> accept: [2002, 2014, 2023]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB5579 - WRITE FILE ... FROM on a file in a SAME RECORD AREA clause. §2002:14.8.47.3 GR5 makes the statement
*> MOVE identifier-1 TO implicit-record-1 then WRITE implicit-record-1, "where implicit-record-1 refers to the record
*> area for file-name-1", and GR2 adds "The logical record is also available as a record of other files referenced in the
*> same SAME RECORD AREA clause as the associated output file" (cite.py --check, each OK). Files A and B share one record
*> area, so after WRITE FILE A FROM W6 the record of B is the six characters of W6: EXPECTED "RB=SAMEAR" (B is open for
*> input and nothing was read, so only the shared area can supply it). Each leg can fail: a release that leaves the
*> operand outside the area shows six spaces.
IDENTIFICATION DIVISION.
PROGRAM-ID. PB5579SA.
ENVIRONMENT DIVISION.
INPUT-OUTPUT SECTION.
FILE-CONTROL.
    SELECT A ASSIGN TO "pb5579sa.dat" ORGANIZATION IS SEQUENTIAL FILE STATUS IS FS.
    SELECT B ASSIGN TO "pb5579sb.dat" ORGANIZATION IS SEQUENTIAL FILE STATUS IS FS.
I-O-CONTROL.
    SAME RECORD AREA FOR A B.
DATA DIVISION.
FILE SECTION.
FD A.
01 RA PIC X(6).
FD B.
01 RB PIC X(6).
WORKING-STORAGE SECTION.
01 FS PIC XX.
01 W6 PIC X(6) VALUE "SAMEAR".
PROCEDURE DIVISION.
MAIN.
    OPEN OUTPUT A.
    OPEN OUTPUT B.
    WRITE FILE A FROM W6.
    DISPLAY "A " FS.
    DISPLAY "RB=" RB.
    CLOSE A B.
    STOP RUN.
