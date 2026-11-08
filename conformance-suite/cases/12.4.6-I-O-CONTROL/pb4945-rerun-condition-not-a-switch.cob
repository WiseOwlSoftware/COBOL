      *>--- conformance-case
      *> id: pb4945-rerun-condition-not-a-switch
      *> rules: [FMT-12.4.6.2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4945 - the X3.23-1985 RERUN clause (§1985:VII.2.12).
      *> FS, FT and FU are sequential (FT by default, the ORGANIZATION
      *> clause omitted), FR is relative and FI indexed; SW1-ON is a
      *> switch status of SPECIAL-NAMES. The clause was parsed and
      *> ignored, so the violation below compiled clean at 85.
      *> Condition-name-1 is a status of a switch "defined in the
      *> SPECIAL-NAMES paragraph" (GR2 f, g); NOTACOND is declared
      *> nowhere.
      *> Expected: COBOL1639 at every edition (from 2002 the clause is
      *> also COBOL0902, removed).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P4945RB.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           SWITCH-1 IS SW1 ON STATUS IS SW1-ON.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT FS ASSIGN TO "P4945RBS" ORGANIZATION IS SEQUENTIAL.
           SELECT FT ASSIGN TO "P4945RBT".
           SELECT FU ASSIGN TO "P4945RBU" ORGANIZATION SEQUENTIAL.
           SELECT FR ASSIGN TO "P4945RBR" ORGANIZATION IS RELATIVE.
           SELECT FI ASSIGN TO "P4945RBI" ORGANIZATION IS INDEXED
               RECORD KEY IS KI.
       I-O-CONTROL.
           RERUN ON SYSRR EVERY NOTACOND.
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
           DISPLAY "P4945RB".
           STOP RUN.
