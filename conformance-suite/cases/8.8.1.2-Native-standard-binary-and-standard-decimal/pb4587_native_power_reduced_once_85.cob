      *>--- conformance-case
      *> id: pb4587_native_power_reduced_once_85
      *> rules: [GR-8.8.1.2-6]
      *> source-format: fixed
      *>---
      *> kb/Work PB4587: the exact power of pb4587_native_power_reduced_once
      *> is witnessed against receivers of 19 to 31 digits (R30 holds 31),
      *> which COBOL-2002 introduced (ISO 8.3.3.3.2), so under --std 85
      *> the program is refused at the item and the lane is never reached.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4587POW85.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 X101  PIC 9V99 VALUE 1.01.
       01 R30   PIC 9V9(30).
       PROCEDURE DIVISION.
           COMPUTE R30 = X101 ** 119.
           STOP RUN.
       END PROGRAM PB4587POW85.
