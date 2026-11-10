      *>--- conformance-case
      *> id: pb5077-return-into-own-record-area
      *> rules: [GR-14.9.34.4-5]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5077 - 1985:XI.4.3.3 SR1: "The storage area associated with identifier-1 and the record area associated with file-name-1
      *>   must not be the same storage area." (cite.py: OK); 2002:14.8.32.2 SR1 says the same with "shall".
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5077N5.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SF ASSIGN TO "PB5077N5.SRT".
       DATA DIVISION.
       FILE SECTION.
       SD  SF.
       01  SR.
           05  SK PIC X(4).
       WORKING-STORAGE SECTION.
       01  WS-A PIC X(4).
       PROCEDURE DIVISION.
       MAIN-SECT SECTION.
       MAIN-PARA.
           SORT SF ASCENDING KEY SK
               INPUT PROCEDURE IS IN-SECT
               OUTPUT PROCEDURE IS OUT-SECT.
           STOP RUN.
       IN-SECT SECTION.
       IN-PARA.
           MOVE "BBBB" TO SK.
           RELEASE SR.
       OUT-SECT SECTION.
       OUT-PARA.
           RETURN SF INTO SR AT END DISPLAY "E".
