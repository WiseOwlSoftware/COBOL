      *>--- conformance-case
      *> id: pb5604-output-cd-pointer-records
      *> rules: [2002-SR-13.16.58.2-7, 2002-SR-13.16.58.2-8, 2002-GR-13.16.58.3-15, 2002-GR-13.16.58.3-16, 2002-SR-14.8.3.2-3, 2002-SR-14.8.35.2-17, 2002-SR-14.8.35.2-20, 2002-SR-14.8.35.2-26, 2002-SR-14.8.35.2-27, 2002-SR-14.8.28.2-3]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5604 - records after an OUTPUT CD are ordinary record
      *> description entries that implicitly redefine the CD's record
      *> area (2002 A.1.2.2 SR9), and 13.16.58.2 SR13 lets USAGE POINTER,
      *> PROGRAM-POINTER and OBJECT REFERENCE stand at level 1. Each such
      *> record is an item of its own usage (13.16.58.3 GR15 and GR16) and
      *> takes part in ALLOCATE, SET, CALL, INVOKE and RAISE like any
      *> other data item of that category. Expected output below is what
      *> those statements define: BX shows the four bytes BUF holds.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5604A.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS PB5604K.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 BUF PIC X(4) VALUE "WXYZ".
       01 BX BASED.
          05 BXC PIC X(4).
       01 AX BASED.
          05 AXC PIC X(8).
       01 TARGET-OBJ USAGE OBJECT REFERENCE PB5604K.
       COMMUNICATION SECTION.
       CD COUT FOR OUTPUT.
       01 CP USAGE POINTER.
       01 PP USAGE PROGRAM-POINTER.
       01 OR1 USAGE OBJECT REFERENCE PB5604K.
       PROCEDURE DIVISION.
       MAIN.
           SET CP TO ADDRESS OF BUF.
           SET ADDRESS OF BX TO CP.
           DISPLAY "BX=" BXC.
           SET CP TO NULL.
           ALLOCATE AX RETURNING CP.
           IF CP NOT = NULL DISPLAY "ALLOC OK".
           SET ADDRESS OF AX TO CP.
           MOVE "ABCDEFGH" TO AXC.
           DISPLAY "AX=" AXC.
           SET PP TO ENTRY "PB5604S".
           CALL PP.
           INVOKE PB5604K "NEW" RETURNING OR1.
           SET TARGET-OBJ TO OR1.
           INVOKE OR1 "SAY".
           INVOKE TARGET-OBJ "SAY".
           RAISE OR1.
           SET CP TO LOCALE LC_ALL.
           SET LOCALE LC_ALL TO CP.
           DISPLAY "DONE".
           STOP RUN.
       END PROGRAM PB5604A.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5604S.
       PROCEDURE DIVISION.
           DISPLAY "SUB CALLED".
           EXIT PROGRAM.
       END PROGRAM PB5604S.

       IDENTIFICATION DIVISION.
       CLASS-ID. PB5604K INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       METHOD-ID. SAY.
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY "SAY".
       END METHOD SAY.
       END OBJECT.
       END CLASS PB5604K.
