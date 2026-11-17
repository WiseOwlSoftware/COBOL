      *>--- conformance-case
      *> id: pb5878_use_both_modes_open_mode_runs
      *> rules: [GR-14.9.49.4-6]
      *> accept: [85, 2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5878. §2023:14.9.49.4 GR6c applies the OUTPUT procedure
      *>   "for any file open in the output mode or in the process of being
      *>   opened in the output mode"; GR6b and GR6e say the same of the
      *>   input and extend modes. An OPEN that fails '41' on a file open in
      *>   another mode (§2023:9.1.13.7 1)) meets two of these sentences, and
      *>   the text orders neither. The order is the implementor's, and
      *>   WiseOwl takes GnuCOBOL's (CLAUDE.md rule 1; docs/conformance entry
      *>   D-USE-OPEN-MODE-BOTH): the procedure of the mode the file IS open
      *>   in runs. A compiler that runs the attempted mode's procedure, or
      *>   both, fails.
      *> DERIVATION. Each mode has a procedure. OPEN EXTEND of SF open
      *>   OUTPUT runs USE-OUTPUT; OPEN INPUT of SF open OUTPUT runs
      *>   USE-OUTPUT; OPEN OUTPUT of SF open EXTEND runs USE-EXTEND. Each
      *>   procedure runs before the DISPLAY that follows its statement.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5878SQ.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SF ASSIGN TO "pb5878sf.dat" FILE STATUS IS FS.
       DATA DIVISION.
       FILE SECTION.
       FD  SF.
       01  SF-R PIC X(4).
       WORKING-STORAGE SECTION.
       01  FS PIC XX.
       PROCEDURE DIVISION.
       DECLARATIVES.
       U-INP SECTION.
           USE AFTER STANDARD ERROR PROCEDURE ON INPUT.
       U-INP-P.
           DISPLAY "USE-INPUT " FS.
       U-OUT SECTION.
           USE AFTER STANDARD ERROR PROCEDURE ON OUTPUT.
       U-OUT-P.
           DISPLAY "USE-OUTPUT " FS.
       U-EXT SECTION.
           USE AFTER STANDARD ERROR PROCEDURE ON EXTEND.
       U-EXT-P.
           DISPLAY "USE-EXTEND " FS.
       END DECLARATIVES.
       M SECTION.
       M-P.
           OPEN OUTPUT SF.
           DISPLAY "OPEN OUTPUT " FS.
           OPEN EXTEND SF.
           DISPLAY "OPEN EXTEND WHILE OUTPUT " FS.
           OPEN INPUT SF.
           DISPLAY "OPEN INPUT WHILE OUTPUT " FS.
           CLOSE SF.
           OPEN EXTEND SF.
           DISPLAY "OPEN EXTEND " FS.
           OPEN OUTPUT SF.
           DISPLAY "OPEN OUTPUT WHILE EXTEND " FS.
           CLOSE SF.
           STOP RUN.
