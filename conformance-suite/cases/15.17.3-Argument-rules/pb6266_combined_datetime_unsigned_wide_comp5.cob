      *>--- conformance-case
      *> id: pb6266_combined_datetime_unsigned_wide_comp5
      *> rules: [AR-15.17.3-1, AR-15.17.3-2, RV-15.17.4-1]
      *> accept: [2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB6266 - FUNCTION COMBINED-DATETIME over an unsigned
      *> 16-byte COMP-5 argument.
      *> §2023:15.17.3 AR2: "Argument-2 shall be in standard numeric
      *> time form." That rule constrains the VALUE of argument-2, so
      *> the USAGE of the item that holds it cannot decide whether the
      *> call is accepted: a PIC 9(20) COMP-5 item holding 18867 is a
      *> legal argument exactly as a PIC 9(5) DISPLAY item holding
      *> 18867 is.
      *> §2023:15.17.4 RV1: "The equivalent arithmetic expression is
      *> as follows: argument-1 + (argument-2 / 100000)".
      *> Expected values, computed from RV1 by hand:
      *>   D + S / 100000  = 143951 + 18867 / 100000   = 143951.18867
      *>   D + SS / 100000 = 143951 + 188.67 / 100000  = 143951.0018867
      *>   UD + 5 / 100000 = 143951 + 5 / 100000       = 143951.00005
      *> The last call takes the first call's arguments from DISPLAY
      *> items; it is the control and prints the first call's value.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB6266CDT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 D    PIC 9(7)  VALUE 143951.
       01 UD   PIC 9(20) COMP-5 VALUE 143951.
       01 S    PIC 9(20) COMP-5 VALUE 18867.
       01 SS   PIC 9(18)V99 COMP-5 VALUE 188.67.
       01 DS   PIC 9(5)  VALUE 18867.
       01 R    PIC 9(7).9(7).
       PROCEDURE DIVISION.
           COMPUTE R = FUNCTION COMBINED-DATETIME (D S)
           DISPLAY "COMP-5 integer " R
           COMPUTE R = FUNCTION COMBINED-DATETIME (D SS)
           DISPLAY "COMP-5 scaled  " R
           COMPUTE R = FUNCTION COMBINED-DATETIME (UD 5)
           DISPLAY "COMP-5 date    " R
           COMPUTE R = FUNCTION COMBINED-DATETIME (D DS)
           DISPLAY "DISPLAY control " R
           STOP RUN.
