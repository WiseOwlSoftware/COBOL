      *>--- conformance-case
      *> id: pb4432_container_global_condition_name_beside_other_index_name
      *> rules: [GR-8.4.6.2.1-3]
      *> accept: [85, 2002, 2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4432 review R1-1, the legal side of the refusals beside it - ISO 8.4.6.2.1 3) b) 2.: "The item in the
      *> containing source element if the name is not declared in source element A and is declared in the source element
      *> containing source element A." PB4432AC declares the GLOBAL item X with the condition-name RELF; PB4432AD, contained
      *> in it, declares a table T1 INDEXED BY SPARE-IX, a different spelling, so RELF is declared in PB4432AD's container
      *> only and names the container's condition-name.
      *> EXPECTED OUTPUT, DERIVED: X holds 1, the VALUE of RELF, so IF RELF is true in PB4432AD: COND.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4432AC.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 X IS GLOBAL PIC 9 VALUE 1.
          88 RELF VALUE 1.
       PROCEDURE DIVISION.
           CALL "PB4432AD".
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4432AD.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 T1.
          05 E PIC X OCCURS 3 INDEXED BY SPARE-IX.
       PROCEDURE DIVISION.
           IF RELF DISPLAY "COND" ELSE DISPLAY "NOT" END-IF.
           EXIT PROGRAM.
       END PROGRAM PB4432AD.
       END PROGRAM PB4432AC.
