*>--- conformance-case
*> id: pb464-set-size-fraction-below-2023
*> rules: [GR-14.9.39.4-37]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB464 - SET [SIZE OF] data-name-3 TO arithmetic-expression-5 (ISO 14.9.39 Format 16) is a
      *> COBOL-2023 introduction, so the non-integer truncation of GR37 (2023/pb464_set_size_fraction_truncates)
      *> has no statement to govern below it: the statement is refused with the edition-band COBOL0900. The
      *> DYNAMIC LENGTH declaration is legal at 2014 and takes the same code at 85 and 2002, as in
      *> pb463-set-size-below-2023, so all three editions are named.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB464N1.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-D PIC X DYNAMIC LENGTH.
       01 WS-A PIC 9V9 VALUE 2.7.
       PROCEDURE DIVISION.
       MAIN-PARA.
           MOVE "ABCDE" TO WS-D
           SET SIZE OF WS-D TO WS-A
           DISPLAY WS-D
           STOP RUN.
