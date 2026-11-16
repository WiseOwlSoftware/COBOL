      *>--- conformance-case
      *> id: pb5825_use_output_for_file_open_output
      *> rules: [GR-14.9.49.4-6]
      *> accept: [85, 2002, 2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5825. §2023:14.9.49.4 GR6c applies the OUTPUT procedure "for any
      *>   file open in the output mode or in the process of being opened in the
      *>   output mode"; GR6e does the same for the extend mode. A
      *>   file open OUTPUT is such a file when an OPEN INPUT of it fails '41'
      *>   (§2023:9.1.13.7 1)), and with only OUTPUT and EXTEND procedures
      *>   declared nothing competes.
      *> DERIVATION. Each failing OPEN INPUT runs the procedure of the mode the
      *>   file is open in, before the DISPLAY that follows the statement: OUTPUT for
      *>   SF, EXTEND for GF. The OPEN OUTPUT of the open SF fails '41' too and
      *>   runs the OUTPUT procedure (the attempted mode).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5825MD.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SF ASSIGN TO "pb5825sf.dat" FILE STATUS IS FS.
           SELECT GF ASSIGN TO "pb5825gf.dat" FILE STATUS IS FG.
       DATA DIVISION.
       FILE SECTION.
       FD  SF.
       01  SF-R PIC X(4).
       FD  GF.
       01  GF-R PIC X(4).
       WORKING-STORAGE SECTION.
       01  FS PIC XX.
       01  FG PIC XX.
       PROCEDURE DIVISION.
       DECLARATIVES.
       U-OUT SECTION.
           USE AFTER STANDARD ERROR PROCEDURE ON OUTPUT.
       U-OUT-P.
           DISPLAY "USE-OUTPUT SF=" FS.
       U-EXT SECTION.
           USE AFTER STANDARD ERROR PROCEDURE ON EXTEND.
       U-EXT-P.
           DISPLAY "USE-EXTEND GF=" FG.
       END DECLARATIVES.
       M SECTION.
       M-P.
           OPEN OUTPUT SF.
           DISPLAY "OPEN OUTPUT SF " FS.
           OPEN INPUT SF.
           DISPLAY "OPEN INPUT SF WHILE OPEN OUTPUT " FS.
           OPEN OUTPUT SF.
           DISPLAY "OPEN OUTPUT SF WHILE OPEN OUTPUT " FS.
           OPEN OUTPUT GF. CLOSE GF.
           OPEN EXTEND GF.
           DISPLAY "OPEN EXTEND GF " FG.
           OPEN INPUT GF.
           DISPLAY "OPEN INPUT GF WHILE OPEN EXTEND " FG.
           CLOSE SF GF.
           STOP RUN.
