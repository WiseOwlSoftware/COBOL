      *>--- conformance-case
      *> id: pb4945-multiple-file-undefined-file
      *> rules: [FMT-12.4.6.2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4945 sibling sweep - the X3.23-1985 MULTIPLE FILE
      *> TAPE
      *> clause (§1985:VII.2.11). The clause was parsed and ignored, so
      *> the undefined file-name below compiled clean at 85.
      *> The sibling clause: the MULTIPLE FILE TAPE clause's file-names
      *> are references to the files "used in the object program"
      *> (§1985:VII.2.11.3 GR1), and NOSUCH names none (§1985:IV.4.3.8).
      *> Expected: COBOL1639 at every edition (from 2002 the clause is
      *> also COBOL0902, removed).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P4945RC.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           SWITCH-1 IS SW1 ON STATUS IS SW1-ON.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT FS ASSIGN TO "P4945RCS" ORGANIZATION IS SEQUENTIAL.
           SELECT FT ASSIGN TO "P4945RCT".
           SELECT FU ASSIGN TO "P4945RCU" ORGANIZATION SEQUENTIAL.
           SELECT FR ASSIGN TO "P4945RCR" ORGANIZATION IS RELATIVE.
           SELECT FI ASSIGN TO "P4945RCI" ORGANIZATION IS INDEXED
               RECORD KEY IS KI.
       I-O-CONTROL.
           MULTIPLE FILE TAPE CONTAINS NOSUCH FS.
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
           DISPLAY "P4945RC".
           STOP RUN.
