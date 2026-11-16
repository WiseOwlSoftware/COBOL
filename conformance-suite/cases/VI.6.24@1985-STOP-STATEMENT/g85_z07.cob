      *>--- conformance-case
      *> id: g85_z07
      *> rules: [1985-GR-VI.6.24.4-2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *> output-file: {name: G85Z07.DAT, expected: g85_z07.exp}
      *>---
      *> §1985:VI.6.24.4 GR2 - STOP RUN executes an implicit CLOSE
      *> for each file in the open mode, and runs no USE procedure
      *> of those files. First sentence: KEPT-FILE gets three records
      *> and is never closed; G85Z07.DAT must hold all three (24
      *> bytes: fixed-length record sequential, no control
      *> information, docs/conformance/7/DOC-A.1-146.md, one byte a
      *> DISPLAY character, DOC-A.1-31.md). At 85 a lone connector
      *> buffers its records until CLOSE, so no implicit CLOSE leaves
      *> the file empty. Second sentence: one debugging section is a
      *> USE procedure of both files; it runs after each OPEN and
      *> after the explicit CLOSE of SHUT-FILE (§1985:XV.3.2.4 GR2 a;
      *> DEBUG-NAME is the file-name, GR13), and NOT for STOP RUN's
      *> implicit CLOSE of KEPT-FILE: nothing follows BEFORE STOP RUN.
      *> WITH DEBUGGING MODE compiles the section (§1985:XV.2.1.3
      *> GR1); the object time switch is ON by default in WiseOwl
      *> COBOL (RunUnit.DebugMode), so the explicit CLOSE's line shows
      *> the section is live.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85Z07.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SOURCE-COMPUTER. WISEOWL WITH DEBUGGING MODE.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT KEPT-FILE ASSIGN TO "G85Z07.DAT".
           SELECT SHUT-FILE ASSIGN TO "G85Z07S.DAT".
       DATA DIVISION.
       FILE SECTION.
       FD  KEPT-FILE
           LABEL RECORDS ARE STANDARD.
       01  KEPT-REC  PIC X(8).
       FD  SHUT-FILE
           LABEL RECORDS ARE STANDARD.
       01  SHUT-REC  PIC X(8).
       PROCEDURE DIVISION.
       DECLARATIVES.
       DBG SECTION.
           USE FOR DEBUGGING ON KEPT-FILE SHUT-FILE.
       DBG-P.
           DISPLAY "DEBUG " DEBUG-NAME.
       END DECLARATIVES.
       MAIN SECTION.
       P1.
           OPEN OUTPUT KEPT-FILE.
           OPEN OUTPUT SHUT-FILE.
           MOVE "RECORD-1" TO KEPT-REC.
           WRITE KEPT-REC.
           MOVE "RECORD-2" TO KEPT-REC.
           WRITE KEPT-REC.
           MOVE "RECORD-3" TO KEPT-REC.
           WRITE KEPT-REC.
           MOVE "SHUT-REC" TO SHUT-REC.
           WRITE SHUT-REC.
           CLOSE SHUT-FILE.
           DISPLAY "BEFORE STOP RUN".
           STOP RUN.
