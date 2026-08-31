      *> reject-at: 2002 2014 2023
      *> ISO 14.9.42.3 syntax rule 2: "Identifier-1 shall reference an integer
      *> data item or a data item with usage display or usage national." A GROUP
      *> is none of the three - it has no elementary description of its own
      *> (8.5.2.1 gives it class alphanumeric).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB169N3.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-GRP.
          05 WS-A PIC X(2) VALUE "07".
          05 WS-B PIC X(2) VALUE "12".
       PROCEDURE DIVISION.
       MAIN.
           STOP RUN WITH ERROR STATUS WS-GRP.
