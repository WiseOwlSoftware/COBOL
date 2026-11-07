*>--- conformance-case
*> id: pb4764-length-parenthesized-variable-length-group-length
*> rules: [AR-15.50.3-1]
*> source-format: free
*> portable: unverified
*>---
      *> PB4764 - ISO §2023:15.50.3 r1: argument-1 is a data item of any class or category, a literal of the listed classes, a based entry or
      *> a type-name. FUNCTION LENGTH (VG) written bare is a function-identifier (§2023:8.4.3.2.4 r1) and admitted
      *> (pb4764_length_of_variable_length_group_length); enclosed in parentheses it is "an arithmetic expression enclosed in
      *> parentheses" (§2023:8.8.1.1), as LENGTH((FUNCTION ABS(I2))) is (pb4712-length-parenthesized-numeric-function), and is refused.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. NEGLENGTHPARENVGLEN.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 K     PIC 9 VALUE 3.
       01 VG.
          05 T  PIC X OCCURS 1 TO 9 DEPENDING ON K.
       01 N     PIC 9(4).
       PROCEDURE DIVISION.
           COMPUTE N = FUNCTION LENGTH((FUNCTION LENGTH(VG))).
           STOP RUN.
