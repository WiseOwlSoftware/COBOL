      *>--- conformance-case
      *> id: pb5077_from_into_outside_the_record_area
      *> rules: [GR-14.9.30.4-4, GR-14.9.51.4-5, GR-14.9.35.4-7, GR-14.9.32.4-4, GR-14.9.34.4-5]
      *> accept: [85, 2002, 2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5077 - the LEGAL side of the record-area operand rule (1985:VII.4.4.3 SR1, VII.4.7.3 SR1, VII.4.5.3 SR1, XI.4.2.3 SR3,
      *>   XI.4.3.3 SR1; 2002:14.8.29.2 SR2 and four siblings; cite.py: OK): every FROM and INTO operand is a Working-Storage item, never the
      *>   file's record area. conformance:negative/pb5077-read-into-own-record-area and its four siblings are the refused side.
      *> DERIVATION. WRITE FROM stores ABCDEFGH. READ INTO moves the record to WS-IN: IN=ABCDEFGH. REWRITE FROM stores 12345678 over it
      *>   (2023:14.9.35.4 GR7 b), and the record read back is NOW=12345678. RELEASE FROM sends BBBB then AAAA; the SORT orders them by SK
      *>   and RETURN INTO delivers AAAA, then BBBB.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5077P1.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SF ASSIGN TO "PB5077P1.DAT"
               ORGANIZATION IS SEQUENTIAL.
           SELECT SRT ASSIGN TO "PB5077P1.SRT".
       DATA DIVISION.
       FILE SECTION.
       FD  SF.
       01  SREC PIC X(8).
       SD  SRT.
       01  SR.
           05  SK PIC X(4).
       WORKING-STORAGE SECTION.
       01  WS-OUT PIC X(8).
       01  WS-IN  PIC X(8).
       01  WS-NEW PIC X(8).
       01  WS-A   PIC X(4).
       01  WS-B   PIC X(4).
       PROCEDURE DIVISION.
       MAIN-SECT SECTION.
       MAIN-PARA.
           OPEN OUTPUT SF.
           MOVE "ABCDEFGH" TO WS-OUT.
           WRITE SREC FROM WS-OUT.
           CLOSE SF.
           OPEN I-O SF.
           READ SF INTO WS-IN AT END DISPLAY "EOF".
           DISPLAY "IN=" WS-IN.
           MOVE "12345678" TO WS-NEW.
           REWRITE SREC FROM WS-NEW.
           CLOSE SF.
           OPEN INPUT SF.
           READ SF AT END DISPLAY "EOF".
           DISPLAY "NOW=" SREC.
           CLOSE SF.
           SORT SRT ASCENDING KEY SK
               INPUT PROCEDURE IS IN-SECT
               OUTPUT PROCEDURE IS OUT-SECT.
           STOP RUN.
       IN-SECT SECTION.
       IN-PARA.
           MOVE "BBBB" TO WS-A.
           RELEASE SR FROM WS-A.
           MOVE "AAAA" TO WS-A.
           RELEASE SR FROM WS-A.
       OUT-SECT SECTION.
       OUT-PARA.
           RETURN SRT INTO WS-B AT END GO TO OUT-EXIT.
           DISPLAY "S=" WS-B.
           GO TO OUT-PARA.
       OUT-EXIT.
           EXIT.
