*>--- conformance-case
*> id: pb4507_iofd_function_identifier_argument_2
*> rules: [AR-15.48.3-3]
*> accept: [2014]
*> source-format: free
*> portable: unverified
*>---
      *> PB4507 - ISO §15.48.3 r3: "Argument-2 shall be a data item of the same type as argument-1." (cite.py --check
      *> 15.48.3 OK) and §8.4.3.2.4 r1: "A function-identifier references a temporary data item whose value is
      *> determined when the function is referenced at runtime." (cite.py --check 8.4.3.2.4 OK). A function-identifier
      *> whose type is alphanumeric IS a data item of argument-1's alphanumeric type, so argument-2 may be one; the rule
      *> excludes a LITERAL (negative/pb58-iofd-literal), a figurative constant (negative/pb4507-iofd-figurative-argument-2)
      *> and an expression. The binder used to admit only a field operand and refused the function with COBOL1627.
      *>
      *> EXPECTED VALUES, from §15.48.4 r1 over §15.5.2's integer date form (days succeeding December 31, 1600):
      *> 2000-03-01 is 145792 (1601-01-01 is 1; the 399 years 1601 to 1999 hold 399 * 365 = 145635 days plus 96 leap
      *> days (99 multiples of 4 from 1604 to 1996, less 1700, 1800 and 1900), so 2000-01-01 is 145635 + 96 + 1 = 145732;
      *> 2000-03-01 adds January's 31 and February's 29 days). 1995-02-15 is 143951 (the
      *> sibling golden l1_iofd_two_argument_format). FUNCTION UPPER-CASE and FUNCTION TRIM leave the digits and hyphens
      *> as written, so every spelling below must return the date of the plain data item. Each leg fails if the
      *> function-identifier is refused (the compile stops) or is read as anything but its returned value.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4507IOFD.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-D   PIC X(10) VALUE "2000-03-01".
       01 WS-P   PIC X(12) VALUE "  1995-02-15".
       01 R7     PIC 9(7).
       PROCEDURE DIVISION.
       MAIN.
           COMPUTE R7 =
               FUNCTION INTEGER-OF-FORMATTED-DATE("YYYY-MM-DD" WS-D)
           DISPLAY "ITEM=" R7
           COMPUTE R7 = FUNCTION INTEGER-OF-FORMATTED-DATE
               ("YYYY-MM-DD", FUNCTION UPPER-CASE(WS-D))
           DISPLAY "UPPER=" R7
           COMPUTE R7 = FUNCTION INTEGER-OF-FORMATTED-DATE
               ("YYYY-MM-DD" FUNCTION TRIM(WS-P))
           DISPLAY "TRIM=" R7
           COMPUTE R7 = FUNCTION INTEGER-OF-FORMATTED-DATE
               ("YYYY-MM-DD" WS-D(1:10))
           DISPLAY "REFMOD=" R7
           STOP RUN.
       END PROGRAM PB4507IOFD.
