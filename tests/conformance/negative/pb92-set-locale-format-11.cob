      *> reject-at: 2002 2014 2023
      *> ISO 14.9.39 Format 11 (set-locale): SET LOCALE {LC_ALL | ... | USER-DEFAULT} TO {identifier | locale-name |
      *> USER-DEFAULT | SYSTEM-DEFAULT} - Annex A.4.9 item 9 of the optional locale module COBOL.NET does not provide;
      *> documented non-support (4.2.7 / A.4.1) is conformant because it is DIAGNOSED: COBOLNET1518, one diagnostic.
      *> Before kb/Work PB92 it bound as a SET of a data item named LOCALE ("'LOCALE' is not defined" + false 0901s).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB92F11.
       PROCEDURE DIVISION.
           SET LOCALE LC_ALL TO USER-DEFAULT.
           STOP RUN.
