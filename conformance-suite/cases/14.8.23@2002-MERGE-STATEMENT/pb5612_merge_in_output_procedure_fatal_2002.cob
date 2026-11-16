*>--- conformance-case
*> id: pb5612_merge_in_output_procedure_fatal_2002
*> rules: [2002-GR-14.8.23.3-8]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB5612 (VCR row 7.80). §2002:14.8.23.3 GR8 (cite.py --check): "The range of the output procedure shall
*> not cause the execution of any MERGE, RELEASE, or the file format of the SORT statement"; violated, "the
*> EC-SORT-MERGE-ACTIVE exception condition is set to exist". §2002:14.8.23.2 SR1 forbids a MERGE only in the
*> declaratives, so this program compiles at 2002 (g2mrg01 witnesses that). EC-SORT-MERGE-ACTIVE is Fatal
*> (Table 14), checking is on and no USE procedure applies: §2002:14.5.12.1.2 4) (cite.py --check): "If checking for
*> the exception condition is enabled, execution of the run unit is terminated abnormally". The 2002 text has no
*> item for a SORT or MERGE statement. Expected: "IN OP 0001", then the run unit ends at the inner MERGE (stderr
*> and exit status are not compared): "AFTER INNER MERGE" and "AFTER MERGE" are never displayed.
>>TURN EC-SORT-MERGE-ACTIVE CHECKING ON
IDENTIFICATION DIVISION.
PROGRAM-ID. PB5612MOP2.
ENVIRONMENT DIVISION.
INPUT-OUTPUT SECTION.
FILE-CONTROL.
    SELECT SW ASSIGN TO "pb5612mop2-w.srt".
    SELECT SW2 ASSIGN TO "pb5612mop2-v.srt".
    SELECT MI1 ASSIGN TO "pb5612mop2-1.dat".
    SELECT MI2 ASSIGN TO "pb5612mop2-2.dat".
    SELECT MO1 ASSIGN TO "pb5612mop2-o.dat".
DATA DIVISION.
FILE SECTION.
SD SW.
01 SR PIC X(4).
SD SW2.
01 SR2 PIC X(4).
FD MI1.
01 M1R PIC X(4).
FD MI2.
01 M2R PIC X(4).
FD MO1.
01 MOR PIC X(4).
PROCEDURE DIVISION.
MAIN.
    OPEN OUTPUT MI1 MOVE "0001" TO M1R WRITE M1R CLOSE MI1
    OPEN OUTPUT MI2 MOVE "0002" TO M2R WRITE M2R CLOSE MI2
    MERGE SW ASCENDING KEY SR USING MI1 MI2 OUTPUT PROCEDURE IS OP
    DISPLAY "AFTER MERGE"
    STOP RUN.
OP.
    RETURN SW AT END DISPLAY "END" END-RETURN
    DISPLAY "IN OP " SR
    MERGE SW2 ASCENDING KEY SR2 USING MI1 MI2 GIVING MO1
    DISPLAY "AFTER INNER MERGE [" FUNCTION EXCEPTION-STATUS "]".
