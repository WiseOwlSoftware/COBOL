      *>--- conformance-case
      *> id: g85_bf06_02
      *> rules: [1985-GR-X.2.4.1-2, 1985-GR-X.2.4.2-2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      * §1985:X.2.4.1 GR2 - initial state: "Files with internal file
      * connectors associated with the program are not in the open
      * mode."
      * §1985:X.2.4.2 GR2 - a program is in the initial state "The first
      * time the program is called after the execution of a CANCEL
      * statement referencing the program or a CANCEL statement
      * referencing a program that directly or indirectly contains the
      * program." Both branches are exercised: BF06C2 is CANCELled by
      * name; BF06F2 is directly contained in BF06C2.
      * Controls: §1985:X.5.2.4 GR5 (other entries: state "remains
      * unchanged from its state when last exited") and GR7 (files keep
      * their open mode on those entries). §1985:X.2.4.1 GR1 sets VALUE
      * items to their defined value in the initial state.
      * I-O status values, §1985:VII.1.3.5: 00 "successfully executed";
      * 41 "An OPEN statement is attempted for a file in the open mode";
      * 48 WRITE "on a file not open in the output" mode.
      * MAIN calls BF06C2 four times, CANCELling it after the second
      * call. Each BF06C2 entry adds 1 to CCNT (VALUE 0), shows it and
      * calls BF06F2, which adds 1 to FCNT (VALUE 0); at FCNT 1 it
      * OPENs OUTPUT its file, otherwise it WRITEs a record.
      * Expected, derived:
      * C1 / F1 OPEN 00 - first call in the run unit (X.2.4.2 GR1): the
      *   file is not open, so OPEN succeeds with 00.
      * C2 / F2 WRITE 00 - last-used state: the file is still open
      *   (X.5.2.4 GR7), so WRITE gives 00 (not 48).
      * C1 / F1 OPEN 00 - after CANCEL "BF06C2", BF06C2 is initial
      *   (first branch: CCNT back to 0+1); BF06F2 is initial (second
      *   branch: its container was cancelled), so FCNT is 1 and its
      *   file is not in the open mode: OPEN gives 00, where a
      *   connector left open would give 41.
      * C2 / F2 WRITE 00 - the next call is again a last-used entry.
      * A leg fails if either counter survives the CANCEL or the file
      * connector is still open after it.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85BF0602.
       PROCEDURE DIVISION.
       MAIN-PARA.
           CALL "BF06C2".
           CALL "BF06C2".
           CANCEL "BF06C2".
           CALL "BF06C2".
           CALL "BF06C2".
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. BF06C2.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  CCNT PIC 9 VALUE 0.
       PROCEDURE DIVISION.
       C-PARA.
           ADD 1 TO CCNT.
           DISPLAY "C" CCNT.
           CALL "BF06F2".
           EXIT PROGRAM.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. BF06F2.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT FF ASSIGN TO "BF0602.DAT"
               ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS FST.
       DATA DIVISION.
       FILE SECTION.
       FD  FF.
       01  FREC PIC X(4).
       WORKING-STORAGE SECTION.
       01  FCNT PIC 9 VALUE 0.
       01  FST PIC XX VALUE SPACES.
       PROCEDURE DIVISION.
       F-PARA.
           ADD 1 TO FCNT.
           IF FCNT = 1
               OPEN OUTPUT FF
               DISPLAY "F" FCNT " OPEN " FST
           ELSE
               MOVE "REC1" TO FREC
               WRITE FREC
               DISPLAY "F" FCNT " WRITE " FST.
           EXIT PROGRAM.
       END PROGRAM BF06F2.
       END PROGRAM BF06C2.
       END PROGRAM G85BF0602.
