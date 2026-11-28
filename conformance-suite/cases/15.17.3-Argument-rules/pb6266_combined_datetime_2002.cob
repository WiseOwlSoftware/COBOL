      *>--- conformance-case
      *> id: pb6266_combined_datetime_2002
      *> rules: [AR-15.17.3-2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB6266 negative: FUNCTION COMBINED-DATETIME is a
      *> 2014 function (COBOL1502 below --std 2014), so the golden
      *> pb6266_combined_datetime_unsigned_wide_comp5 is refused at
      *> --std 2002.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB6266NEG.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 D    PIC 9(7)  VALUE 143951.
       01 S    PIC 9(5)  VALUE 18867.
       01 R    PIC 9(7).9(7).
       PROCEDURE DIVISION.
       MAIN-PARA.
           COMPUTE R = FUNCTION COMBINED-DATETIME (D S)
           DISPLAY R
           STOP RUN.
