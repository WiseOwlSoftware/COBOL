      *>--- conformance-case
      *> id: pb2500-assign-constant-numeric
      *> rules: [SR-12.4.5.2-4, SR-13.10.3-2, GR-13.10.4-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB2500 - ISO 12.4.5.2 SR4: "Literal-1 shall be an
      *>   alphanumeric literal and shall be neither a figurative
      *>   constant nor a zero-length literal." A constant-name written
      *> as the ASSIGN operand is literal-1 as if written (13.10.4 GR1),
      *> so a constant that stands for a NUMERIC literal violates SR4:
      *> COBOL2645, naming the constant. (The word is no longer read
      *> as device-name-1 once the program declares it a constant.)
      *>   cite.py --check 12.4.5.2 "Literal-1 shall be an alphanumeric
      *>     literal and shall be neither a figurative constant nor a
      *>     zero-length literal" -> OK 12.4.5.2 4)
      *>   cite.py --check 13.10.4 "as if literal-1" -> OK 13.10.4 1)
      *> The 2002, 2014 and 2023 editions refuse it; at 85 the constant
      *> entry itself is refused first (pb2500-assign-constant-below-2002).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2500NUM.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT WR ASSIGN TO PB2500KN
               ORGANIZATION IS SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD  WR.
       01  WR-REC PIC X(5).
       WORKING-STORAGE SECTION.
       01  PB2500KN CONSTANT AS 5.
       PROCEDURE DIVISION.
           STOP RUN.
