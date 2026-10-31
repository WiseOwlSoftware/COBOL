      *>--- conformance-case
      *> id: pb4199-contained-file-clauses-name-global-data-items
      *> rules: [GR-13.18.27.4-1, GR-13.18.27.4-2, SR-12.4.5.8.3-2]
      *> accept: [2002, 2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4199, the siblings of pb3766-contained-clauses-name-global-data-item. ISO/IEC 1989:2023 13.18.27.4 GR1: "A
      *> constant-name, data-name, file-name, report-name, or screen-name described using a GLOBAL clause is a global name",
      *> and GR2: "A statement in a program contained directly or indirectly within a program that describes a global name
      *> may reference that name without describing it again." The contained program PB4199I names, in the clauses of its
      *> OWN data division, five items of its container PB4199: the FILE STATUS item GS (a two-character alphanumeric item,
      *> 12.4.5.8.3 SR2), the RELATIVE KEY item GK, the ASSIGN USING item GN, the RECORD ... DEPENDING ON item GR, and the
      *> LINAGE operand GL. All five are described GLOBAL in the container, none in the contained program; the compiler
      *> resolved these clauses before it inherited the container's GLOBAL items and refused each as undefined.
      *>
      *> DERIVATION OF THE EXPECTED OUTPUT. GN holds the file name "pb4199-absent.dat" of the file DYN, which is not
      *> OPTIONAL and whose physical file is not present, so OPEN INPUT DYN gives I-O status 35 (9.1.13.6 item 5) and GS,
      *> the FILE STATUS item of DYN, receives it (12.4.5.8.4 GR1); the container displays GS=35 after the contained
      *> program returns. Nothing else of the program reaches the file system.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4199.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 GS PIC XX GLOBAL.
       01 GK PIC 9(4) GLOBAL.
       01 GN PIC X(17) GLOBAL VALUE "pb4199-absent.dat".
       01 GR PIC 9(2) GLOBAL VALUE 5.
       01 GL PIC 9(2) GLOBAL VALUE 4.
       PROCEDURE DIVISION.
           CALL "PB4199I"
           DISPLAY "GS=" GS
           STOP RUN.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4199I.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT DYN ASSIGN USING GN
               FILE STATUS IS GS.
           SELECT REL ASSIGN TO "pb4199-absent-rel.dat"
               ORGANIZATION IS RELATIVE ACCESS MODE IS RANDOM
               RELATIVE KEY IS GK.
           SELECT VAR ASSIGN TO "pb4199-absent-var.dat".
           SELECT PGE ASSIGN TO "pb4199-absent-pge.dat".
       DATA DIVISION.
       FILE SECTION.
       FD DYN.
       01 DYN-REC PIC X.
       FD REL.
       01 REL-REC PIC X.
       FD VAR RECORD IS VARYING IN SIZE FROM 1 TO 5 CHARACTERS
           DEPENDING ON GR.
       01 VAR-REC PIC X(5).
       FD PGE LINAGE IS GL LINES.
       01 PGE-REC PIC X.
       PROCEDURE DIVISION.
           OPEN INPUT DYN
           EXIT PROGRAM.
       END PROGRAM PB4199I.
       END PROGRAM PB4199.
