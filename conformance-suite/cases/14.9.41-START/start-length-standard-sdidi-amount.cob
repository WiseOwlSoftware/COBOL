*>--- conformance-case
*> id: start-length-standard-sdidi-amount
*> rules: [GR-14.9.41.4-14]
*> accept: [2002]
*> source-format: free
*>---
      *> START WITH LENGTH arithmetic-expression-1 under ARITHMETIC IS
      *> STANDARD (kb/Work PB5619): the count is a standard
      *> intermediate value, tested as it is.
      *> §2002:14.8.37.3 GR14: "If arithmetic-expression-1 does not
      *> evaluate to a positive non-zero integer that is less than or
      *> equal to the length of the associated key, the I-O status
      *> value in the file connector referenced by file-name-1 is set
      *> to '23', the invalid key condition exists, and the execution
      *> of the START statement is unsuccessful."
      *> The file holds AB01 and AB02; the prime key is 4 characters.
      *> T1 count = 1 + 1/10**20 (21 digits, inside the 32-digit
      *>    standard intermediate): not an integer -> '23'.
      *> T2 count = 10 ** 40: an integer greater than the key's 4 ->
      *>    '23' (not a size error that ends the run unit).
      *> T3 count = 1 + 1 = 2: an integer, 1 <= 2 <= 4 -> '00'.
      *> T4 count = 0 - 1: not positive -> '23'.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. STARTSD.
       OPTIONS.
           ARITHMETIC IS STANDARD.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT IXF ASSIGN TO "startsd.dat"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS DYNAMIC
               RECORD KEY IS IX-KEY
               FILE STATUS IS ST1.
       DATA DIVISION.
       FILE SECTION.
       FD IXF.
       01 IX-REC.
          05 IX-KEY PIC X(4).
       WORKING-STORAGE SECTION.
       01 ST1 PIC XX.
       01 ONE PIC 9 VALUE 1.
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT IXF
           MOVE "AB01" TO IX-KEY WRITE IX-REC
           MOVE "AB02" TO IX-KEY WRITE IX-REC
           CLOSE IXF
           OPEN INPUT IXF
           MOVE "AB01" TO IX-KEY
           START IXF KEY IS EQUAL TO IX-KEY
               WITH LENGTH ONE + ONE / 100000000000000000000
               INVALID KEY DISPLAY "T1=INVALID " ST1
               NOT INVALID KEY DISPLAY "T1=OK " ST1
           END-START
           START IXF KEY IS EQUAL TO IX-KEY WITH LENGTH 10 ** 40
               INVALID KEY DISPLAY "T2=INVALID " ST1
               NOT INVALID KEY DISPLAY "T2=OK " ST1
           END-START
           START IXF KEY IS EQUAL TO IX-KEY WITH LENGTH ONE + ONE
               INVALID KEY DISPLAY "T3=INVALID " ST1
               NOT INVALID KEY DISPLAY "T3=OK " ST1
           END-START
           START IXF KEY IS EQUAL TO IX-KEY WITH LENGTH 0 - ONE
               INVALID KEY DISPLAY "T4=INVALID " ST1
               NOT INVALID KEY DISPLAY "T4=OK " ST1
           END-START
           CLOSE IXF
           STOP RUN.
