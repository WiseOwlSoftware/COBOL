*>--- conformance-case
*> id: pb3541_deactivated_clause_releases_its_file
*> rules: [GR-14.9.7.4-3, GR-12.4.6.3.4-5, GR-14.9.36.4-3]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB3541 - a file connector leaves commit control when the last APPLY COMMIT clause naming it is deactivated.
*> ISO 14.9.7.4 GR3: "The COMMIT statement also deactivates any APPLY COMMIT clauses in exited initial programs and
*> canceled runtime elements." ISO 12.4.6.3.4 GR5: "An APPLY COMMIT clause is active when the runtime element in which
*> it is contained has been activated and, in the case of canceled runtime elements and exited initial programs, has
*> not been deactivated by a COMMIT or ROLLBACK statement". ISO 14.9.36.4 GR3: "All files and data-items referenced in
*> active APPLY COMMIT clauses are restored to the state they were in at the execution of the last COMMIT statement".
*> PB3541M's own clause names only WS-X. PB3541S names the EXTERNAL RELATIVE file RELF in its clause; it is called,
*> canceled, and the COMMIT deactivates its clause. PB3541T describes RELF with no clause (no element then has a clause
*> in effect for the connector, ISO 12.4.6.3.4 GR2) and writes record 1 after the COMMIT. Expected, from those rules:
*> the ROLLBACK restores WS-X (an active clause names it) to the 0001 the COMMIT saved and leaves record 1, written
*> through a connector no active clause names, in the file.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3541M.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       I-O-CONTROL.
           APPLY COMMIT ON WS-X.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-X PIC 9(4) VALUE 1.
       01 WS-OP PIC X.
       PROCEDURE DIVISION.
           CALL "PB3541S"
           CANCEL "PB3541S"
           COMMIT
           MOVE "W" TO WS-OP
           CALL "PB3541T" USING WS-OP
           MOVE 5 TO WS-X
           ROLLBACK
           DISPLAY "AFTER ROLLBACK WS-X=" WS-X
           MOVE "R" TO WS-OP
           CALL "PB3541T" USING WS-OP
           STOP RUN.
       END PROGRAM PB3541M.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3541S.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RELF ASSIGN TO "pb3541.dat"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS RANDOM
               RELATIVE KEY IS RK.
       I-O-CONTROL.
           APPLY COMMIT ON RELF.
       DATA DIVISION.
       FILE SECTION.
       FD RELF IS EXTERNAL.
       01 RREC PIC X(10).
       WORKING-STORAGE SECTION.
       01 RK PIC 9(4) IS EXTERNAL.
       PROCEDURE DIVISION.
           DISPLAY "PB3541S ACTIVATED"
           GOBACK.
       END PROGRAM PB3541S.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3541T.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RELF ASSIGN TO "pb3541.dat"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS RANDOM
               RELATIVE KEY IS RK.
       DATA DIVISION.
       FILE SECTION.
       FD RELF IS EXTERNAL.
       01 RREC PIC X(10).
       WORKING-STORAGE SECTION.
       01 RK PIC 9(4) IS EXTERNAL.
       LINKAGE SECTION.
       01 OP PIC X.
       PROCEDURE DIVISION USING OP.
           MOVE 1 TO RK
           IF OP = "W"
               OPEN OUTPUT RELF
               MOVE "AAAA" TO RREC
               WRITE RREC
                   INVALID KEY DISPLAY "WRITE INVALID"
               END-WRITE
               CLOSE RELF
           ELSE
               OPEN INPUT RELF
               READ RELF
                   INVALID KEY DISPLAY "RECORD 1 ABSENT"
                   NOT INVALID KEY DISPLAY "RECORD 1=" RREC
               END-READ
               CLOSE RELF
           END-IF
           GOBACK.
       END PROGRAM PB3541T.
