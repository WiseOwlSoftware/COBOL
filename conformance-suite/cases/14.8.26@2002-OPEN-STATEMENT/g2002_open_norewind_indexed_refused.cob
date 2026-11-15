      *>--- conformance-case
      *> id: g2002_open_norewind_indexed_refused
      *> rules: [2002-SR-14.8.26.2-4]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:14.8.26.2 SR4 - "The NO REWIND phrase may be specified
      *> only for sequential files." X is INDEXED, so OPEN OUTPUT X
      *> WITH NO REWIND violates the restriction (OUTPUT satisfies SR5,
      *> so only SR4 is broken). The refusal sits beside the accepted
      *> g2002_open_norewind_input_output, which writes the granted form.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2OPN1X.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT X ASSIGN TO "g2opn1x.dat"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS SEQUENTIAL
               RECORD KEY IS X-KEY.
       DATA DIVISION.
       FILE SECTION.
       FD X.
       01 X-REC.
          05 X-KEY PIC X(4).
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT X WITH NO REWIND
           STOP RUN.
