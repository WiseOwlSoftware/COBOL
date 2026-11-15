      *>--- conformance-case
      *> id: pb5591_relative_input_use_excuses_write_85
      *> rules: [1985-SR-VIII.4.9.3-3]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5591 - a LATITUDE CHOICE (docs/CONFORMANCE.md 3.5).
      *> "The INVALID KEY phrase must be specified if an applicable
      *> USE AFTER STANDARD EXCEPTION procedure is not specified for
      *> the associated file-name" (§1985:VIII.4.9.3 SR3). The 1985
      *> text does not define "applicable"; WiseOwl COBOL reads it as
      *> a procedure that applies to the file in some mode it can be
      *> open in (reading A), so the only declarative, USE ... ON
      *> INPUT, excuses the WRITE with no INVALID KEY. A compiler on
      *> reading B refuses it. At run time the file is open in the
      *> input mode, so the WRITE fails: "The execution of a WRITE
      *> statement is attempted on a file not open in the I-O,
      *> output, or extend mode" (§1985:VIII.1.3.4, status 48), and
      *> the procedure runs "for any file open in the input mode or in
      *> the process of being opened in the input mode"
      *> (§1985:VIII.4.8.4 GR5b). After a critical status WiseOwl
      *> COBOL continues with the next statement (docs/CONFORMANCE.md
      *> 3.5). The test fails if the program is refused, if the
      *> procedure does not run, or if the status is not 48.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P5591E.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RLF ASSIGN TO "P5591E.REL"
               ORGANIZATION IS RELATIVE ACCESS MODE IS RANDOM
               RELATIVE KEY IS RK FILE STATUS IS FS.
       DATA DIVISION.
       FILE SECTION.
       FD  RLF.
       01  RREC PIC X(5).
       WORKING-STORAGE SECTION.
       01  RK PIC 9(4).
       01  FS PIC XX.
       PROCEDURE DIVISION.
       DECLARATIVES.
       D-IN SECTION.
           USE AFTER STANDARD EXCEPTION PROCEDURE ON INPUT.
       D-IN-P.
           DISPLAY "USE INPUT " FS.
       END DECLARATIVES.
       MAIN SECTION.
       M-1.
           OPEN OUTPUT RLF.
           CLOSE RLF.
           OPEN INPUT RLF.
           DISPLAY "OPEN INPUT " FS.
           MOVE 1 TO RK.
           MOVE "ALPHA" TO RREC.
           WRITE RREC.
           DISPLAY "AFTER WRITE " FS.
           CLOSE RLF.
           STOP RUN.
