      *>--- conformance-case
      *> id: pb5821_use_global_input_excepts_named_file
      *> rules: [GR-14.9.49.4-5, GR-14.9.49.4-6]
      *> accept: [85, 2002, 2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5821. The containing program holds USE GLOBAL ... ON INPUT and,
      *>   in the same Procedure Division, USE ... ON FG (not GLOBAL).
      *>   §2023:14.9.49.4 GR6b applies the INPUT procedure to a file open or being
      *>   opened in the input mode "except those files referenced by file-name-1
      *>   in another USE statement specifying the same condition"; FG is such a
      *>   file. The contained program's failing OPEN INPUT FG therefore reaches no
      *>   qualifying declarative: the container's INPUT one is excepted, and the
      *>   one naming FG is not GLOBAL (§1985:X.5.5.4 GR1b).
      *> DERIVATION. FG is absent, so every OPEN INPUT answers '35' (§2023:9.1.13.6 5)).
      *>   In the container the FG declarative runs (file-name scope), so its
      *>   line precedes "OUTER OPEN". The contained OPEN runs no declarative, so
      *>   "INNER OPEN 35" has no line before it. A compiler that runs the GLOBAL
      *>   INPUT declarative there prints OUTER-GLOBAL-INPUT 35 and fails the test.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5821NT.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT FG ASSIGN TO "pb5821-absent.rel"
               ORGANIZATION IS RELATIVE ACCESS MODE IS SEQUENTIAL
               FILE STATUS IS FS.
       DATA DIVISION.
       FILE SECTION.
       FD  FG IS GLOBAL.
       01  FG-R PIC X(4).
       WORKING-STORAGE SECTION.
       01  FS PIC XX IS GLOBAL.
       PROCEDURE DIVISION.
       DECLARATIVES.
       D-IN SECTION.
           USE GLOBAL AFTER STANDARD ERROR PROCEDURE ON INPUT.
       D1.
           DISPLAY "OUTER-GLOBAL-INPUT " FS.
       D-FG SECTION.
           USE AFTER STANDARD ERROR PROCEDURE ON FG.
       D2.
           DISPLAY "OUTER-NAMED-LOCAL " FS.
       END DECLARATIVES.
       M SECTION.
       P.
           OPEN INPUT FG.
           DISPLAY "OUTER OPEN " FS.
           CALL "PB5821IN".
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5821IN.
       PROCEDURE DIVISION.
       P2.
           OPEN INPUT FG.
           DISPLAY "INNER OPEN " FS.
           EXIT PROGRAM.
       END PROGRAM PB5821IN.
       END PROGRAM PB5821NT.
