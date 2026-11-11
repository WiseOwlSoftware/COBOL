      *>--- conformance-case
      *> id: pb5077-release-from-own-record-area
      *> rules: [GR-14.9.32.4-4]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5077 - 1985:XI.4.2.3 SR3: "Record-name-1 and identifier-1 must not reference the same storage area." (cite.py: OK);
      *>   2002:14.8.30.2 SR2 says the same with "shall".
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5077N4.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SF ASSIGN TO "PB5077N4.SRT".
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
           RELEASE SR FROM SR.
       OUT-SECT SECTION.
       OUT-PARA.
           RETURN SF AT END DISPLAY "E".
