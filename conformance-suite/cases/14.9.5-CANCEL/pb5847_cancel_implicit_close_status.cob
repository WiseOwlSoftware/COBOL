*>--- conformance-case
*> id: pb5847_cancel_implicit_close_status
*> rules: [GR-14.9.5.4-9, GR-14.9.5.4-4, GR-14.9.6.4-4]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB5847. During a CANCEL "an implicit CLOSE statement without
*> any optional phrases is executed for each file-name associated with an
*> internal file connector that is open" (14.9.5.4 GR9), and a CLOSE
*> "causes the value of the I-O status associated with file-name-1 to be
*> updated" (14.9.6.4 GR4), so GST, a GLOBAL item of this container that
*> outlives the cancelled program, reads 00 after the CANCEL where the last
*> READ left 10 (B 00; 10 before the fix). The reCALL's OPEN gives 00, not
*> 41 (the second C 00). A closed file gets no implicit CLOSE: the 42 of a
*> second explicit CLOSE stands (E 42). GR4 cancels the contained programs
*> first: a RECURSIVE containee of a RECURSIVE container has no live
*> instance once both returned, and its close still stores into the outer
*> GLOBAL item (G 00; it ended the run unit with an exception before).
IDENTIFICATION DIVISION.
PROGRAM-ID. W5847P.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 GST PIC XX GLOBAL.
PROCEDURE DIVISION.
    CALL "W5847Q"
    DISPLAY "A " GST
    CANCEL "W5847Q"
    DISPLAY "B " GST
    CALL "W5847Q"
    CANCEL "W5847Q"
    CALL "W5847U"
    DISPLAY "D " GST
    CANCEL "W5847U"
    DISPLAY "E " GST
    CALL "W5847R"
    DISPLAY "F " GST
    CANCEL "W5847R"
    DISPLAY "G " GST
    STOP RUN.

IDENTIFICATION DIVISION.
PROGRAM-ID. W5847Q.
ENVIRONMENT DIVISION.
INPUT-OUTPUT SECTION.
FILE-CONTROL.
    SELECT QF ASSIGN TO "W5847Q.DAT" FILE STATUS IS GST.
DATA DIVISION.
FILE SECTION.
FD QF.
01 QR PIC X(4).
PROCEDURE DIVISION.
    OPEN OUTPUT QF
    DISPLAY "C " GST
    MOVE "QQQQ" TO QR
    WRITE QR
    CLOSE QF
    OPEN INPUT QF
    READ QF AT END DISPLAY "UNEXPECTED EOF" END-READ
    READ QF AT END CONTINUE END-READ
    GOBACK.
END PROGRAM W5847Q.

IDENTIFICATION DIVISION.
PROGRAM-ID. W5847U.
ENVIRONMENT DIVISION.
INPUT-OUTPUT SECTION.
FILE-CONTROL.
    SELECT UF ASSIGN TO "W5847U.DAT" FILE STATUS IS GST.
DATA DIVISION.
FILE SECTION.
FD UF.
01 UR PIC X(4).
PROCEDURE DIVISION.
    OPEN OUTPUT UF
    CLOSE UF
    CLOSE UF
    GOBACK.
END PROGRAM W5847U.

IDENTIFICATION DIVISION.
PROGRAM-ID. W5847R RECURSIVE.
PROCEDURE DIVISION.
    CALL "W5847V"
    GOBACK.

IDENTIFICATION DIVISION.
PROGRAM-ID. W5847V RECURSIVE.
ENVIRONMENT DIVISION.
INPUT-OUTPUT SECTION.
FILE-CONTROL.
    SELECT VF ASSIGN TO "W5847V.DAT" FILE STATUS IS GST.
DATA DIVISION.
FILE SECTION.
FD VF.
01 VR PIC X(4).
PROCEDURE DIVISION.
    OPEN OUTPUT VF
    MOVE "VVVV" TO VR
    WRITE VR
    CLOSE VF
    OPEN INPUT VF
    READ VF AT END DISPLAY "UNEXPECTED EOF" END-READ
    READ VF AT END CONTINUE END-READ
    GOBACK.
END PROGRAM W5847V.
END PROGRAM W5847R.
END PROGRAM W5847P.
