      *> reject-at: 2002 2014 2023
      *> ISO 14.9.28 - the EC-SCREEN family in the WHEN phrase of an exception-checking PERFORM (item 10)
      *> Annex A.4.2 (ACCEPT and DISPLAY screen handling) is Not claimed - docs/CONFORMANCE.md
      *> section 5; A.4.1 admits an optional element's syntax only when support is claimed.
      *> Witness for kb/Work PB260.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. A42ECP.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-X PIC X VALUE "A".
       PROCEDURE DIVISION.
           PERFORM
               CONTINUE
               WHEN EC-SCREEN-LINE-NUMBER
                   CONTINUE
           END-PERFORM.
           STOP RUN.
