      *>--- conformance-case
      *> id: pb5871-assign-using-alphabetic-2002
      *> rules: [2002-SR-12.3.4.2-7]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5871 - §2002:12.3.4.2 SR7: "Data-name-1 shall
      *>   reference an alphanumeric data item and shall not be
      *>   subordinate to the file description entry for file-name-1."
      *>   §2002:8.5.2.1: "An alphabetic data item may be specified
      *>   wherever an elementary alphanumeric data item is permitted as
      *>   an operand. Where an alphabetic data item is not expressly
      *>   permitted, the alphabetic data item is treated as though it
      *>   were an alphanumeric data item." WS-ALPHA is PIC A, so at
      *>   COBOL-2002 it names the file in ASSIGN USING as a PIC X item
      *>   does. The record written is read back, so the file was
      *>   named by the PIC A item's value: FOUR. COBOL-2014
      *>   withdrew the sentence (§2014:E.2), so the same source is
      *>   refused at 2014 and 2023 (pb5871-assign-using-alphabetic-
      *>   refused).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5871A.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F4 ASSIGN USING WS-ALPHA.
       DATA DIVISION.
       FILE SECTION.
       FD  F4.
       01  R4          PIC X(8).
       WORKING-STORAGE SECTION.
       01  WS-ALPHA    PIC A(11) VALUE "PB5871ASGNA".
       PROCEDURE DIVISION.
       MAIN-P.
           OPEN OUTPUT F4.
           MOVE "FOUR" TO R4.
           WRITE R4.
           CLOSE F4.
           MOVE SPACES TO R4.
           OPEN INPUT F4.
           READ F4 AT END DISPLAY "F4 EMPTY".
           DISPLAY "F4 ALPHABETIC " R4.
           CLOSE F4.
           STOP RUN.
