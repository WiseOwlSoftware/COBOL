*> reject-at: 2002 2014 2023
*> The P10 RECURSIVE-WS slice's honest-subset stage (0899 recursive-contained-working-storage):
*> a RECURSIVE program's WS is STATIC data (ISO 13.5.4 GR1 - one last-used copy, 14.6.2.3.3), but a
*> contained program's GLOBAL/__outer ref-bridges alias the CONTAINER INSTANCE's fields (13.18.27 GR2)
*> and cannot reach class statics - the composition stages LOUD, never a half-wired model.
IDENTIFICATION DIVISION.
PROGRAM-ID. RWCONT-P10RW RECURSIVE.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 WS-X PIC 9(2) GLOBAL VALUE 0.
PROCEDURE DIVISION.
MAIN.
    CALL "RWIN-P10RW".
    GOBACK.
IDENTIFICATION DIVISION.
PROGRAM-ID. RWIN-P10RW.
PROCEDURE DIVISION.
P2.
    ADD 1 TO WS-X.
    GOBACK.
END PROGRAM RWIN-P10RW.
END PROGRAM RWCONT-P10RW.
