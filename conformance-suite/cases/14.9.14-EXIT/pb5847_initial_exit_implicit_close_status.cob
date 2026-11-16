*>--- conformance-case
*> id: pb5847_initial_exit_implicit_close_status
*> rules: [GR-14.9.14.4-3, GR-14.9.18.4-2, GR-14.9.5.4-9, GR-14.9.6.4-4]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB5847. An EXIT PROGRAM in a called program proceeds as a
*> GOBACK (14.9.14.4 GR3), and for an initial program "an implicit CANCEL
*> statement referencing that program is executed upon return" (14.9.18.4
*> GR2), whose implicit CLOSE (14.9.5.4 GR9) updates the I-O status
*> (14.9.6.4 GR4). GST, a GLOBAL item of this container, holds 10 inside
*> the initial program after its read to the end (S 10) and 00 once it
*> returned (A 00 by EXIT PROGRAM, K 00 by GOBACK; 10 before the fix). The
*> next activation's OPEN OUTPUT gives 00, not 41 (O 00): the file was
*> closed. A file the initial program closed itself gets no implicit
*> CLOSE, so the 42 of its second CLOSE stands (B 42).
IDENTIFICATION DIVISION.
PROGRAM-ID. W5847J.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 GST PIC XX GLOBAL.
01 OP  PIC X GLOBAL.
PROCEDURE DIVISION.
    MOVE "E" TO OP
    CALL "W5847K"
    DISPLAY "A " GST
    MOVE "G" TO OP
    CALL "W5847K"
    DISPLAY "K " GST
    MOVE "N" TO OP
    CALL "W5847K"
    DISPLAY "B " GST
    STOP RUN.

IDENTIFICATION DIVISION.
PROGRAM-ID. W5847K IS INITIAL.
ENVIRONMENT DIVISION.
INPUT-OUTPUT SECTION.
FILE-CONTROL.
    SELECT KF ASSIGN TO "W5847K.DAT" FILE STATUS IS GST.
DATA DIVISION.
FILE SECTION.
FD KF.
01 KR PIC X(4).
PROCEDURE DIVISION.
    OPEN OUTPUT KF
    IF OP = "N"
        CLOSE KF
        CLOSE KF
        GOBACK
    END-IF
    DISPLAY "O " GST
    MOVE "KKKK" TO KR
    WRITE KR
    CLOSE KF
    OPEN INPUT KF
    READ KF AT END DISPLAY "UNEXPECTED EOF" END-READ
    READ KF AT END DISPLAY "S " GST END-READ
    IF OP = "E"
        EXIT PROGRAM
    END-IF
    GOBACK.
END PROGRAM W5847K.
END PROGRAM W5847J.
