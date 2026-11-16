      *>--- conformance-case
      *> id: pb5786_own_file_name_beside_container_global_data_item
      *> rules: [GR-8.4.6.2.1-1, GR-8.4.6.2.1-3]
      *> accept: [85, 2002, 2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5786, the legal side of the refusals beside it - ISO 8.4.6.2.1 3) a): PB5786E declares the data item FX IS
      *> GLOBAL; PB5786F, contained in it, declares its own file FX and uses it as a file-name only (OPEN, CLOSE: a file-name is
      *> what FX names there). The container's item is untouched.
      *> EXPECTED OUTPUT, DERIVED: PB5786F displays OPENED; PB5786E then displays its own FX, still GLOB.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5786E.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 FX GLOBAL PIC X(4) VALUE "GLOB".
       PROCEDURE DIVISION.
       PA.
           CALL "PB5786F".
           DISPLAY "A FX=" FX.
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5786F.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT FX ASSIGN TO "PB5786F.DAT".
       DATA DIVISION.
       FILE SECTION.
       FD FX.
       01 FXR PIC X(4).
       PROCEDURE DIVISION.
       PB.
           OPEN OUTPUT FX.
           CLOSE FX.
           DISPLAY "OPENED".
           EXIT PROGRAM.
       END PROGRAM PB5786F.
       END PROGRAM PB5786E.
