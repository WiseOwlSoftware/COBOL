*>--- conformance-case
*> id: pb5612_merge_using_open_fatal_2002
*> rules: [2002-GR-14.8.23.3-7]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB5612 (VCR row 7.80). §2002:14.8.23.3 GR7 (cite.py --check): "shall not be in the open mode, otherwise the EC-SORT-MERGE-FILE-OPEN exception condition is set to exist and the execution of the MERGE statement terminates".
*> Here the MERGE names MI1, a USING file, while the program holds it open. EC-SORT-MERGE-FILE-OPEN is Fatal (Table 14) and
*> checking is on, so the fatal exception procedure decides (§2002:14.5.12.1.2 2), cite.py --check): "If execution
*> of the declarative completes normally the execution of the run unit is terminated abnormally"; the 2002 text
*> has no item for a SORT or MERGE statement. Expected: "DECL=EC-SORT-MERGE-FILE-OPEN", then the run unit ends
*> (stderr and exit status are not compared): "AFTER MERGE" is never displayed (the 2023 reading of
*> §2023:14.6.13.1.3 2) prints it).
>>TURN EC-SORT-MERGE-FILE-OPEN CHECKING ON
IDENTIFICATION DIVISION.
PROGRAM-ID. PB5612MU2.
ENVIRONMENT DIVISION.
INPUT-OUTPUT SECTION.
FILE-CONTROL.
    SELECT SW ASSIGN TO "pb5612_merge_using_open_fatal_2002-w.srt".
    SELECT MI1 ASSIGN TO "pb5612_merge_using_open_fatal_2002-1.dat" FILE STATUS IS FS1.
    SELECT MI2 ASSIGN TO "pb5612_merge_using_open_fatal_2002-2.dat".
    SELECT MO1 ASSIGN TO "pb5612_merge_using_open_fatal_2002-o.dat" FILE STATUS IS FSO.
DATA DIVISION.
FILE SECTION.
SD SW.
01 SR PIC X(4).
FD MI1.
01 M1R PIC X(4).
FD MI2.
01 M2R PIC X(4).
FD MO1.
01 MOR PIC X(4).
WORKING-STORAGE SECTION.
01 FS1 PIC XX.
01 FSO PIC XX.
PROCEDURE DIVISION.
DECLARATIVES.
D-FO SECTION.
    USE AFTER EXCEPTION CONDITION EC-SORT-MERGE-FILE-OPEN.
D-FO1.
    DISPLAY "DECL=" FUNCTION EXCEPTION-STATUS.
END DECLARATIVES.
MAIN SECTION.
MAIN1.
    OPEN OUTPUT MI1 MOVE "0001" TO M1R WRITE M1R CLOSE MI1
    OPEN OUTPUT MI2 MOVE "0002" TO M2R WRITE M2R CLOSE MI2
    OPEN INPUT MI1
    MERGE SW ASCENDING KEY SR USING MI1 MI2 GIVING MO1
    DISPLAY "AFTER MERGE"
    STOP RUN.
