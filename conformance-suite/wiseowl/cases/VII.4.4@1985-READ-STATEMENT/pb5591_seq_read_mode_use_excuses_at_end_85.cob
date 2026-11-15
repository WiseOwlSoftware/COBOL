      *>--- conformance-case
      *> id: pb5591_seq_read_mode_use_excuses_at_end_85
      *> rules: [1985-SR-VII.4.4.3-2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5591 - a LATITUDE CHOICE (docs/CONFORMANCE.md 3.5).
      *> "The AT END phrase must be specified if no applicable USE
      *> AFTER STANDARD EXCEPTION procedure is specified for
      *> file-name-1" (§1985:VII.4.4.3 SR2). The 1985 text does not
      *> define "applicable"; WiseOwl COBOL reads it as a procedure
      *> that applies to the file in some mode it can be open in
      *> (reading A), so the only declarative here, USE ... ON OUTPUT,
      *> excuses the READ that has no AT END. The CCVS-85 program
      *> SQ148A has this shape. A compiler on reading B (only a mode
      *> in which the at end condition can arise) refuses the program.
      *> At run time the READ of a file open in the output mode fails:
      *> "The execution of a READ statement is attempted on a file not
      *> open in the input or I-O mode" (§1985:VII.1.3.5, status 47),
      *> and the procedure runs "for any file open in the output mode
      *> or in the process of being opened in the output mode"
      *> (§1985:VII.4.6.4 GR5c). After a critical status "the
      *> implementor determines what action is taken"
      *> (§1985:VII.4.6.4 GR6); WiseOwl COBOL continues with the next
      *> statement (docs/CONFORMANCE.md 3.5). The test fails if the
      *> program is refused, if the procedure does not run, or if the
      *> status is not 47.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P5591A.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SF ASSIGN TO "P5591A.DAT" FILE STATUS IS FS.
       DATA DIVISION.
       FILE SECTION.
       FD  SF.
       01  SREC PIC X(5).
       WORKING-STORAGE SECTION.
       01  FS PIC XX.
       PROCEDURE DIVISION.
       DECLARATIVES.
       D-OUT SECTION.
           USE AFTER STANDARD EXCEPTION PROCEDURE ON OUTPUT.
       D-OUT-P.
           DISPLAY "USE OUTPUT " FS.
       END DECLARATIVES.
       MAIN SECTION.
       M-1.
           OPEN OUTPUT SF.
           DISPLAY "OPEN " FS.
           READ SF.
           DISPLAY "AFTER READ " FS.
           MOVE "ALPHA" TO SREC.
           WRITE SREC.
           DISPLAY "WRITE " FS.
           CLOSE SF.
           STOP RUN.
