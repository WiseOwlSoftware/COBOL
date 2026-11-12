*>--- conformance-case
*> id: pb5211-read-into-alphabetic-refmod
*> rules: [SR-14.9.30.3-1]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB5211. READ's INTO phrase with two record descriptions is admitted only when identifier-1 and
*> every record-name "describe an alphanumeric group item or an elementary item of category alphanumeric or
*> category national" (ISO 1989:2023 14.9.30.3 SR1 b; §1985:IX.4.5.4 GR7 b, "a group item or an elementary
*> alphanumeric item", the same words in §1985:VII.4.4.4 GR7 for a sequential file).
*> WS-ALPHA (1:5) is a reference-modified item, and the unique data item reference modification creates "has the
*> same class and category as that defined for the data item referenced by data-name-1" except that the
*> categories numeric, numeric edited and alphanumeric edited are considered alphanumeric (§1985:IV.4.3.8.4.4
*> GR5; ISO 1989:2023 8.4.3.3.4 GR6). WS-ALPHA is PIC A, category alphabetic, which is none of the exceptions, so
*> the slice is category alphabetic as the item is, arm b does not hold, and arm a does not either (the FD has two
*> record descriptions): refused, as READ ... INTO WS-ALPHA is (COBOL1994). A compiler that reads every
*> reference-modified receiver as alphanumeric accepts it.
IDENTIFICATION DIVISION.
PROGRAM-ID. PB5211N.
ENVIRONMENT DIVISION.
INPUT-OUTPUT SECTION.
FILE-CONTROL.
    SELECT SQF ASSIGN TO "pb5211n.dat"
        ORGANIZATION IS SEQUENTIAL.
DATA DIVISION.
FILE SECTION.
FD SQF.
01 REC-A.
   05 A-1 PIC X(4).
01 REC-B.
   05 B-1 PIC X(4).
WORKING-STORAGE SECTION.
01 WS-ALPHA PIC A(10).
PROCEDURE DIVISION.
MAIN.
    OPEN INPUT SQF
    READ SQF INTO WS-ALPHA (1:5)
        AT END CONTINUE
    END-READ
    CLOSE SQF
    STOP RUN.
