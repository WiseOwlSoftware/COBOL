      *>--- conformance-case
      *> id: pb4797_working_numerators_past_the_value_range_85
      *> rules: [RV-15.4.1-1]
      *> source-format: fixed
      *>---
      *> kb/Work PB4797: pb4797_working_numerators_past_the_value_range
      *> uses a floating-point numeric literal, which COBOL-2002 introduced
      *> (ISO §2023:8.3.3.3.3 1; COBOL0900), so under --std 85
      *> the program is refused at the literal and no lane is reached.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4797E85.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 R   PIC S9(9)V9(4).
       PROCEDURE DIVISION.
       MAIN-PARA.
           COMPUTE R = FUNCTION VARIANCE (1, 2.0E+3072).
           STOP RUN.
       END PROGRAM PB4797E85.
