      *>--- conformance-case
      *> id: pb4945-rerun-reel-same-file-twice
      *> rules: [FMT-12.4.6.2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4945 - the X3.23-1985 RERUN clause (§1985:VII.2.12).
      *> FS, FT and FU are sequential (FT by default, the ORGANIZATION
      *> clause omitted), FR is relative and FI indexed; SW1-ON is a
      *> switch status of SPECIAL-NAMES. The clause was parsed and
      *> ignored, so the violation below compiled clean at 85.
      *> SR4 b: "When multiple END OF REEL or END OF UNIT phrases are
      *> specified, no two of them may specify the same file-name-2."
      *> Expected: COBOL6531 at every edition (from 2002 the clause is
      *> also COBOL0902, removed).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P4945R7.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           SWITCH-1 IS SW1 ON STATUS IS SW1-ON.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT FS ASSIGN TO "P4945R7S" ORGANIZATION IS SEQUENTIAL.
           SELECT FT ASSIGN TO "P4945R7T".
           SELECT FU ASSIGN TO "P4945R7U" ORGANIZATION SEQUENTIAL.
           SELECT FR ASSIGN TO "P4945R7R" ORGANIZATION IS RELATIVE.
           SELECT FI ASSIGN TO "P4945R7I" ORGANIZATION IS INDEXED
               RECORD KEY IS KI.
       I-O-CONTROL.
           RERUN EVERY END OF REEL OF FS
           RERUN ON SYSRR EVERY END OF UNIT OF FS.
       DATA DIVISION.
       FILE SECTION.
       FD FS.
       01 RS PIC X(10).
       FD FT.
       01 RT PIC X(10).
       FD FU.
       01 RU PIC X(10).
       FD FR.
       01 RR PIC X(10).
       FD FI.
       01 RI.
           02 KI PIC X(10).
       PROCEDURE DIVISION.
       MAIN-PARA.
           DISPLAY "P4945R7".
           STOP RUN.
