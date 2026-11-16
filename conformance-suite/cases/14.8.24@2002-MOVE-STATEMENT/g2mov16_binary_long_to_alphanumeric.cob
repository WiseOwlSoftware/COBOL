      *>--- conformance-case
      *> id: g2mov16_binary_long_to_alphanumeric
      *> rules: [2002-SR-14.8.24.2-8]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:14.8.24.2 SR8 - "If identifier-1 is a data item
      *>   described with usage binary-char, binary-short, binary-long,
      *>   or binary-double, identifier-2 shall be a numeric or
      *>   numeric-edited item". BL is of usage binary-long and RX is
      *>   alphanumeric, so the MOVE is refused at its line, although
      *>   Table 17 alone (Numeric integer to Alphanumeric: Yes) would
      *>   admit it. Fails if the binary sender is judged by the table.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2MOV16.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 BL  USAGE BINARY-LONG VALUE 12.
       01 RX  PIC X(5).
       PROCEDURE DIVISION.
       MAIN.
           MOVE BL TO RX.
           STOP RUN.
