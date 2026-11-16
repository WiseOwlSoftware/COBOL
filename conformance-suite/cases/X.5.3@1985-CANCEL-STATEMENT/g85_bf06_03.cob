      *>--- conformance-case
      *> id: g85_bf06_03
      *> rules: [1985-GR-X.5.3.4-2, 1985-GR-X.5.3.4-6]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      * §1985:X.5.3.4 GR2 - "If the program referenced by a successfully
      * executed explicit or implicit CANCEL statement in a run unit is
      * subsequently called in that run unit, that program is in its
      * initial state."
      * §1985:X.5.3.4 GR6 - "A logical relationship to a cancelled
      * program is established only by execution of a subsequent CALL
      * statement naming that program."
      * Control: §1985:X.5.2.4 GR5 - on other entries the state "remains
      * unchanged from its state when last exited"; §1985:X.2.4.1 GR1 -
      * in the initial state a VALUE item has its defined value.
      * BF06S3 adds 1 to SCNT (VALUE 0) and DISPLAYs "S" SCNT; BF06T3 is
      * a sibling that DISPLAYs "T".
      * Expected, derived:
      * S1, S2 - first call initial, second call last-used (GR5).
      * T      - CANCEL "BF06S3" (literal form), then a CALL of another
      *          program: by GR6 that CALL does not re-establish BF06S3.
      * S1     - GR2: BF06S3 is in its initial state (0+1), not 3.
      * S2     - GR6: the CALL naming BF06S3 re-established it, so this
      *          entry is last-used again (GR5).
      * S1     - CANCEL W-PGM (identifier form, content "BF06S3") then
      *          CALL: initial state again by GR2.
      * A leg fails if a cancelled program keeps its last-used data, or
      * if a re-called program does not keep state across later calls.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85BF0603.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  W-PGM PIC X(6) VALUE "BF06S3".
       PROCEDURE DIVISION.
       MAIN-PARA.
           CALL "BF06S3".
           CALL "BF06S3".
           CANCEL "BF06S3".
           CALL "BF06T3".
           CALL "BF06S3".
           CALL "BF06S3".
           CANCEL W-PGM.
           CALL "BF06S3".
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. BF06S3.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  SCNT PIC 9 VALUE 0.
       PROCEDURE DIVISION.
       S-PARA.
           ADD 1 TO SCNT.
           DISPLAY "S" SCNT.
           EXIT PROGRAM.
       END PROGRAM BF06S3.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. BF06T3.
       PROCEDURE DIVISION.
       T-PARA.
           DISPLAY "T".
           EXIT PROGRAM.
       END PROGRAM BF06T3.
       END PROGRAM G85BF0603.
