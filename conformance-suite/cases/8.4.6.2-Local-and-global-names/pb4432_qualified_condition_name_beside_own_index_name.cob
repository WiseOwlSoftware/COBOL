      *>--- conformance-case
      *> id: pb4432_qualified_condition_name_beside_own_index_name
      *> rules: [GR-8.4.6.2.1-2]
      *> accept: [85, 2002, 2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4432 review R2-1 - ISO 8.4.6.2.1 2): "If only one item is identified, it is the referenced item." Rule 1
      *> applies qualification over the whole set before rule 3 ranks it by source element. PB4432AG declares the GLOBAL
      *> item X with the condition-name RELF; PB4432AH, contained in it, declares a table INDEXED BY RELF. The written
      *> qualifier OF X reaches only the condition-name, so RELF OF X identifies one item - the container's level-88 -
      *> although PB4432AH's own index-name RELF is declared nearer: the own index-name is no candidate the qualifier reaches.
      *> EXPECTED OUTPUT, DERIVED: X holds 0, so IF RELF OF X is false: NOT; SET RELF OF X TO TRUE makes X equal the VALUE
      *> of RELF, 1, so IF RELF OF X is then true: COND.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4432AG.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 X IS GLOBAL PIC 9 VALUE 0.
          88 RELF VALUE 1.
       PROCEDURE DIVISION.
       PB4305-LEAD.
           CALL "PB4432AH".
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4432AH.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 T1.
          05 E PIC X OCCURS 3 INDEXED BY RELF.
       PROCEDURE DIVISION.
       PB4305-LEAD.
           IF RELF OF X DISPLAY "COND" ELSE DISPLAY "NOT" END-IF.
           SET RELF OF X TO TRUE.
           IF RELF OF X DISPLAY "COND" ELSE DISPLAY "NOT" END-IF.
           EXIT PROGRAM.
       END PROGRAM PB4432AH.
       END PROGRAM PB4432AG.
