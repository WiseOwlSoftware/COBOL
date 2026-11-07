*>--- conformance-case
*> id: pb4766_counter_register_concat_baseconvert_argument
*> rules: [AR-15.18.3-1, AR-15.12.3-1]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> PB4766 - ISO §2023:15.18.3 r1: "Argument-1 and argument-2 shall be data items or literals of class alphabetic, alphanumeric,
      *> boolean, numeric or national." and §2023:15.12.3 r1: argument-1 "shall be a usage display or national data item or literal, and,
      *> if the base specified in argument-2 is less than 11, shall also be an unsigned integer data item or literal" (both
      *> cite.py --check OK). LINAGE-COUNTER "references a temporary unsigned integer data item of class and category numeric whose
      *> size is equal to the page size specified in the LINAGE clause" (§2023:8.4.3.14.4 r1), so written bare it is a legal argument of
      *> both functions. Its size is the page size, LINAGE IS 20 LINES, so two digits, and the register is 1 after OPEN OUTPUT
      *> (pb1153_counter_register_character_image): the item's image is "01". CONCAT of "01" and "A" is "01A" (§2023:15.18.4 r1,
      *> the characters of the arguments in order) and BASECONVERT of 01 from base 10 to base 2 is "1". The legs fail if the
      *> register has no image at a string-function argument, which compiled clean and aborted at run time.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4766CTRARG.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "pb4766-counter-arg.dat".
       DATA DIVISION.
       FILE SECTION.
       FD F LINAGE IS 20 LINES.
       01 FR    PIC X(10).
       WORKING-STORAGE SECTION.
       01 R1    PIC X(10).
       01 R2    PIC X(10).
       PROCEDURE DIVISION.
           OPEN OUTPUT F
           MOVE FUNCTION CONCAT(LINAGE-COUNTER "A") TO R1
           DISPLAY "CONCAT [" R1 "]"
           MOVE FUNCTION BASECONVERT(LINAGE-COUNTER 10 2) TO R2
           DISPLAY "BASECONV [" R2 "]"
           CLOSE F
           STOP RUN.
