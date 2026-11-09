*>--- conformance-case
*> id: pb5003-external-fd-record-external-type
*> rules: [SR-13.18.22.3-5, GR-13.18.22.4-4]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB5003 - ISO 13.18.22.4 GR4 b): the record of an EXTERNAL file is external data, one area per run unit keyed by the FILE's
      *> externalized name; 13.18.22.3 SR5 requires a strongly typed record's type declaration to be external too, so R is external twice
      *> over (the FD's clause and its type's) and must still occupy ONE cell: it declared _scell_R / _redef_R twice (CS0102).  W, the
      *> non-FD record of the same external type, keeps its own cell keyed by its own name.  Why each leg can fail: a double claim does not
      *> compile; a W that took R's cell would print WXYZ07 for both; a lost R/file alias would not read back what was written.
      *> Expected: R reads back what it wrote (WXYZ then 07), W keeps ABCD42.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5003A.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "pb5003a.dat" ORGANIZATION SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD F IS EXTERNAL.
       01 R TYPE TG.
       WORKING-STORAGE SECTION.
       01 TG IS TYPEDEF STRONG EXTERNAL.
          05 RX PIC X(4).
          05 RY PIC 9(2).
       01 W TYPE TG.
       PROCEDURE DIVISION.
       MAIN-P.
           MOVE "ABCD" TO RX OF W
           MOVE 42 TO RY OF W
           OPEN OUTPUT F
           MOVE "WXYZ" TO RX OF R
           MOVE 7 TO RY OF R
           WRITE R
           CLOSE F
           OPEN INPUT F
           READ F
           CLOSE F
           DISPLAY RX OF R RY OF R " " RX OF W RY OF W
           STOP RUN.
