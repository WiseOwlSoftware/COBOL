      *>--- conformance-case
      *> id: g2002-call-on-overflow-numeric-target
      *> rules: [2002-SR-14.8.4.2-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:14.8.4.2 SR1 - "Identifier-1 shall be defined as an
      *> alphanumeric, national, or program-pointer data item." A numeric
      *> identifier-1 in a Format 1 (on-overflow) CALL is none of the three,
      *> so the statement shall be refused at the CALL line. The accepted
      *> categories are g2002-call-on-overflow-operands.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2CAL02.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A     PIC X(4) VALUE "AAAA".
       01 NUMID PIC 9(8) VALUE 1.
       PROCEDURE DIVISION.
       MAIN.
           CALL NUMID USING A ON OVERFLOW DISPLAY "OVERFLOW" END-CALL
           STOP RUN.
       END PROGRAM G2CAL02.
