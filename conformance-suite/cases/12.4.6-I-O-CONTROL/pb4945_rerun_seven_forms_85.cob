      *>--- conformance-case
      *> id: pb4945_rerun_seven_forms_85
      *> rules: [FMT-12.4.6.2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4945 - the seven forms of the X3.23-1985 RERUN clause
      *> (§1985:VII.2.12.4 GR2 a-g), each written once, and every
      *> syntax rule of §1985:VII.2.12.3 respected:
      *>   SR1 - file-name-1 (FT) is sequential (by default).
      *>   SR2 - every END OF REEL/UNIT file-name-2 is sequential.
      *>   SR3 - RECORDS and CLOCK-UNITS name implementor-name-1 SYSRR.
      *>   SR4 - no file-name-2 twice among the RECORDS phrases, nor
      *>         among the END OF REEL/UNIT phrases; FS is in one of
      *>         each, which SR4 permits.
      *>   SR5 - one CLOCK-UNITS phrase.
      *> The RECORDS phrase's file-name-2 may have "any organization or
      *> access" (GR2 d), so FI, an indexed file, is legal there. A null
      *> rerun facility is conforming (GR2: "at least one of the
      *> specified forms"), so the clause has no program-visible effect.
      *> FS, FT and FU are sequential, FR relative, FI indexed.
      *> Expected: the program compiles at 85 and displays its name.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P4945RA.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           SWITCH-1 IS SW1 ON STATUS IS SW1-ON.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT FS ASSIGN TO "P4945RAS" ORGANIZATION IS SEQUENTIAL.
           SELECT FT ASSIGN TO "P4945RAT".
           SELECT FU ASSIGN TO "P4945RAU" ORGANIZATION SEQUENTIAL.
           SELECT FR ASSIGN TO "P4945RAR" ORGANIZATION IS RELATIVE.
           SELECT FI ASSIGN TO "P4945RAI" ORGANIZATION IS INDEXED
               RECORD KEY IS KI.
       I-O-CONTROL.
           RERUN EVERY END OF REEL OF FS
           RERUN ON FT EVERY END OF UNIT OF FT
           RERUN ON SYSRR EVERY END OF UNIT OF FU
           RERUN ON SYSRR EVERY 100 RECORDS OF FS
           RERUN ON SYSRR EVERY 100 RECORDS OF FI
           RERUN ON SYSRR EVERY 5 CLOCK-UNITS
           RERUN ON SYSRR EVERY SW1-ON
           RERUN ON FT EVERY SW1-ON.
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
           DISPLAY "P4945RA".
           STOP RUN.
