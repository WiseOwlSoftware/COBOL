*>--- conformance-case
*> id: select_when_record_name_write_by_edition
*> rules: [GR-13.18.51.4-2, GR-13.18.51.4-4, GR-9.1.13.7-5]
*> accept: [2002, 2014, 2023]
*> constructs: [format-clause-2002, select-when-clause-2002]
*> source-format: free
*> portable: unverified
*>---
*> A WRITE that names record-name-1 (no FILE phrase) of a FORMAT file whose entries carry SELECT WHEN clauses, for a record whose tag no clause selects.
*> 2002 (.2002.out): §2002:13.16.49.3 GR1 (cite.py --check) "Any SELECT WHEN clauses of record description entries specified in the file section are
*> evaluated for READ, REWRITE, and WRITE statements associated with that file when a CODE-SET clause or FORMAT clause is specified", and GR4 "If no record
*> description entry is selected, the input-output operation is unsuccessful and the I-O status is set to a value indicating a record identification
*> failure", which is 45 (§2002:9.1.12.6 item 5): the WRITE of tag Z answers 45 and puts nothing on the medium, so the file holds only the tag B record (WRITE 00),
*> and the READ of it is 00, the next 10.
*> 2014 and 2023 (.out): §2023:13.18.51.4 GR2 (cite.py --check; §2014:13.18.51.3 GR2 prints the same) "SELECT WHEN clauses of record description entries
*> specified in the file section have no effect for statements other than READ statements, REWRITE statements with the FILE phrase, and WRITE statements
*> with the FILE phrase", so WRITE G-A is formatted by the entry it names and is successful (00) whatever the tag holds; it is the READ of that record that
*> evaluates the clauses (GR1) and, none being true, answers 45 (GR4, §2023:9.1.13.7 item 5), as a READ of a record written by any other means would; the next READ is 46
*> (§2023:14.9.30.4 GR21, the previous READ being unsuccessful).
*> Why it can fail: the record-name WRITE evaluating the clauses at 2014 and after (status 45, record dropped), or not evaluating them at 2002.
IDENTIFICATION DIVISION.
PROGRAM-ID. SW4525.
ENVIRONMENT DIVISION.
INPUT-OUTPUT SECTION.
FILE-CONTROL.
    SELECT G ASSIGN TO "SW4525.dat" ORGANIZATION IS SEQUENTIAL FILE STATUS IS GS.
DATA DIVISION.
FILE SECTION.
FD G RECORD IS VARYING IN SIZE FROM 1 TO 100 CHARACTERS
    FORMAT CHARACTER DATA.
01 G-A SELECT WHEN G-IS-A.
    05 G-A-KEY PIC X.
        88 G-IS-A VALUE "A".
    05 G-A-TEXT PIC X(4).
01 G-B SELECT WHEN G-IS-B.
    05 G-B-KEY PIC X.
        88 G-IS-B VALUE "B".
    05 G-B-TEXT PIC X(4).
WORKING-STORAGE SECTION.
01 GS PIC XX.
PROCEDURE DIVISION.
MAIN.
    OPEN OUTPUT G.
    MOVE "Z" TO G-A-KEY.
    MOVE "ONE" TO G-A-TEXT.
    WRITE G-A.
    DISPLAY "WRITE Z " GS.
    MOVE "B" TO G-B-KEY.
    MOVE "TWO" TO G-B-TEXT.
    WRITE G-B.
    DISPLAY "WRITE B " GS.
    CLOSE G.
    OPEN INPUT G.
    READ G END-READ.
    DISPLAY "READ 1 " GS " " G-B-KEY.
    READ G END-READ.
    DISPLAY "READ 2 " GS.
    CLOSE G.
    STOP RUN.
