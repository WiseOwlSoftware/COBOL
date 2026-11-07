*>--- conformance-case
*> id: pb4764_length_of_variable_length_group_length
*> rules: [AR-15.50.3-1, AR-15.14.3-1]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> PB4764 - the length of a function-identifier whose value is a variable-length group's LENGTH. ISO §2023:15.50.3 r1 and §2023:15.14.3 r1
      *> admit "a data item of any class or category", and a function-identifier "references a temporary data item" (§2023:8.4.3.2.4 r1),
      *> so FUNCTION LENGTH (FUNCTION LENGTH (VG)) is legal, and the same bare function inside BYTE-LENGTH. The temporary's
      *> characteristics are the implementor's (§2023:15.4.1, DOC-A.1-92), GnuCOBOL 3.2.0's by CLAUDE.md rule 1: the run-time LENGTH of a
      *> variable-length group is a 4-byte binary item, so each leg is 4 whatever the group's current length (3 here). The inner
      *> LENGTH (VG) is the current length (§2023:15.50.4 r4b), 3.
      *> Each leg was refused COBOL1627 as "an arithmetic expression" because the argument's BoundBinary sum had no arm.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4764ACCEPT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 K     PIC 9 VALUE 3.
       01 VG.
          05 T  PIC X OCCURS 1 TO 9 DEPENDING ON K.
       01 N     PIC 9(4).
       PROCEDURE DIVISION.
           COMPUTE N = FUNCTION LENGTH(VG)
           DISPLAY "L-VG " N
           COMPUTE N = FUNCTION LENGTH(FUNCTION LENGTH(VG))
           DISPLAY "L-L-VG " N
           COMPUTE N = FUNCTION LENGTH(FUNCTION BYTE-LENGTH(VG))
           DISPLAY "L-B-VG " N
           COMPUTE N = FUNCTION BYTE-LENGTH(FUNCTION LENGTH(VG))
           DISPLAY "B-L-VG " N
           COMPUTE N = FUNCTION BYTE-LENGTH(FUNCTION BYTE-LENGTH(VG))
           DISPLAY "B-B-VG " N
           STOP RUN.
