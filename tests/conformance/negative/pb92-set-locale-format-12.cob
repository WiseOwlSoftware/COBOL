      *> reject-at: 2002 2014 2023
      *> ISO 14.9.39 Format 12 (save-locale): SET identifier-11 TO LOCALE {LC_ALL | USER-DEFAULT} - Annex A.4.9 item 9,
      *> documented non-support (COBOLNET1518). Before kb/Work PB92 it was `unexpected '.'`, a bare parse error.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB92F12.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-L PIC X(20).
       PROCEDURE DIVISION.
           SET WS-L TO LOCALE LC_ALL.
           STOP RUN.
