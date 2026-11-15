      *>--- conformance-case
      *> id: g2002_open_norewind_io_refused
      *> rules: [2002-SR-14.8.26.2-5]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:14.8.26.2 SR5 - "The NO REWIND phrase may be specified
      *> only when the INPUT or OUTPUT phrase is specified." S is a
      *> sequential file (SR4 holds), opened I-O WITH NO REWIND, so only
      *> SR5 is broken. The refusal sits beside the accepted
      *> g2002_open_norewind_input_output, which writes the granted form.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2OPN1Y.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT S ASSIGN TO "g2opn1y.dat"
               ORGANIZATION IS SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD S.
       01 S-REC PIC X(4).
       PROCEDURE DIVISION.
       MAIN.
           OPEN I-O S WITH NO REWIND
           STOP RUN.
