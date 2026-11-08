      *>--- conformance-case
      *> id: pb4945-rerun-clock-units-on-file
      *> rules: [FMT-12.4.6.2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4945 - the X3.23-1985 RERUN clause (§1985:VII.2.12).
      *> FS, FT and FU are sequential (FT by default, the ORGANIZATION
      *> clause omitted), FR is relative and FI indexed; SW1-ON is a
      *> switch status of SPECIAL-NAMES. The clause was parsed and
      *> ignored, so the violation below compiled clean at 85.
      *> SR3: "When either the integer-1 RECORDS phrase or the integer-2
      *> CLOCK-UNITS phrase is specified, implementor-name-1 must be
      *> given in the RERUN clause." The ON phrase names file-name-1
      *> FS, not an implementor-name.
      *> Expected: COBOL6530 at every edition (from 2002 the clause is
      *> also COBOL0902, removed).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P4945R4.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           SWITCH-1 IS SW1 ON STATUS IS SW1-ON.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT FS ASSIGN TO "P4945R4S" ORGANIZATION IS SEQUENTIAL.
           SELECT FT ASSIGN TO "P4945R4T".
           SELECT FU ASSIGN TO "P4945R4U" ORGANIZATION SEQUENTIAL.
           SELECT FR ASSIGN TO "P4945R4R" ORGANIZATION IS RELATIVE.
           SELECT FI ASSIGN TO "P4945R4I" ORGANIZATION IS INDEXED
               RECORD KEY IS KI.
       I-O-CONTROL.
           RERUN ON FS EVERY 10 CLOCK-UNITS.
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
           DISPLAY "P4945R4".
           STOP RUN.
