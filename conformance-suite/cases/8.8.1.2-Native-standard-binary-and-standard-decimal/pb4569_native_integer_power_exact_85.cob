      *>--- conformance-case
      *> id: pb4569_native_integer_power_exact_85
      *> rules: [GR-8.8.1.2-6]
      *> source-format: fixed
      *>---
      *> kb/Work PB4569: the exact integer power of
      *> pb4569_native_integer_power_exact is witnessed against items of
      *> 19 to 31 digits (B65 = 2**65 has 20), which COBOL-2002
      *> introduced (construct pic-wide-19-digits-2002, ISO 8.3.3.3.2),
      *> and by intrinsic functions, so under --std 85 the program is
      *> refused at the item and the lane is never reached there.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4569POWEXACT85.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 B65   PIC 9(20) VALUE 36893488147419103232.
       PROCEDURE DIVISION.
       MAIN-PARA.
           IF 2 ** 130 = B65 * B65
               DISPLAY "TRUE"
           ELSE
               DISPLAY "FALSE".
           STOP RUN.
