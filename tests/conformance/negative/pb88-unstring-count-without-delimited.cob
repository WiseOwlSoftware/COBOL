      *> reject-at: 85 2002 2014 2023
      *> ISO 14.9.48.3 SR7: "The DELIMITER IN phrase and the COUNT IN phrase may be specified only if the
      *> DELIMITED BY phrase is specified." kb/Work PB88: COBOLNET1651 at bind (a run-time stage before).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB88NUNSCNT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 S PIC X(6) VALUE "ABCDEF".
       01 R PIC X(3).
       01 C PIC 99.
       PROCEDURE DIVISION.
           UNSTRING S INTO R COUNT IN C.
           STOP RUN.
