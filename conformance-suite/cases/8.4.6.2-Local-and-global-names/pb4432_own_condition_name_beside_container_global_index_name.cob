      *>--- conformance-case
      *> id: pb4432_own_condition_name_beside_container_global_index_name
      *> rules: [GR-8.4.6.2.1-3]
      *> accept: [85, 2002, 2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4432, the legal side of the refusals beside it - ISO 8.4.6.2.1 3) a): "If the name is declared in source
      *> element B, the item in source element B is the referenced item." PB4432P declares the GLOBAL table T1 INDEXED BY
      *> RELF and uses its index; PB4432Q, contained in it, declares its own condition-name RELF. In PB4432Q, RELF is the
      *> condition-name (SET RELF TO TRUE makes X equal its VALUE, 1, and IF RELF is then true); PB4432P's index is the
      *> same one before and after the call.
      *> EXPECTED OUTPUT, DERIVED: PB4432P sets its index to 2 and stores 7 in E (2); PB4432Q displays COND (its
      *> condition-name is true after SET ... TO TRUE) and 1 (X); PB4432P then displays E (RELF), which still names
      *> element 2: 7.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4432P.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 T1 IS GLOBAL.
          05 E PIC 9 OCCURS 3 INDEXED BY RELF.
       PROCEDURE DIVISION.
           SET RELF TO 2.
           MOVE 7 TO E (RELF).
           CALL "PB4432Q".
           DISPLAY E (RELF).
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4432Q.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 X PIC 9 VALUE 0.
          88 RELF VALUE 1.
       PROCEDURE DIVISION.
           SET RELF TO TRUE.
           IF RELF DISPLAY "COND" END-IF.
           DISPLAY X.
           EXIT PROGRAM.
       END PROGRAM PB4432Q.
       END PROGRAM PB4432P.
