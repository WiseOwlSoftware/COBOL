      *>--- conformance-case
      *> id: pb5722-send-from-numeric-edited-2002
      *> rules: [2002-SR-A.2.6.2-2]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5722 - §2002:A.2.6.2 SR2: "Identifier-1 shall
      *>   reference a data item of class alphanumeric." §2002:8.5.2
      *>   puts "Numeric-edited (if usage is display)" in class
      *>   alphanumeric, so SEND COUT FROM NE, with NE PIC ZZ9,
      *>   translates at COBOL-2002. The program proves translation
      *>   only: the GO TO skips the statement and the output is
      *>   empty. It fails if NE is refused (COBOL6171). The
      *>   communication facility is removed from COBOL-2014 and the
      *>   sentence with it (§2014:E.2).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5722SN.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  NE  PIC ZZ9.
       COMMUNICATION SECTION.
       CD  COUT FOR OUTPUT
           DESTINATION COUNT IS OUT-CNT
           TEXT LENGTH IS OUT-LEN
           STATUS KEY IS OUT-STAT
           DESTINATION TABLE OCCURS 1 TIMES
           ERROR KEY IS OUT-ERR
           SYMBOLIC DESTINATION IS OUT-DEST.
       PROCEDURE DIVISION.
       MAIN-P.
           GO TO DONE-P.
           SEND COUT FROM NE.
       DONE-P.
           STOP RUN.
