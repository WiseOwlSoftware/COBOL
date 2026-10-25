      *>--- conformance-case
      *> id: pb2508_display_negative_zero_literal
      *> rules: []
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB2508. ISO 14.9.11.4 GR1: "Any conversion of data
      *> required between literal-1 or the data item referenced by
      *> identifier-1 and the device is defined by the implementor", so
      *> the character image of a numeric literal is the implementor's
      *> (CONFORMANCE.md DOC-A.1-56): the literal as written, with one
      *> exception settled GnuCOBOL's way (the precedence tiebreaker,
      *> measured on 3.2.0, -std=default and -std=cobol2014): a literal
      *> whose value is ZERO shows '+' for a written '-', because a zero
      *> is neither negative nor positive. Every other literal is as
      *> written. A constant-name that substitutes the literal (13.10.4
      *> GR1) shows the same image.
      *> EXPECTED, one line each, bracketed:
      *>   A[+0] B[+0.0] C[+0] D[0] E[+00] F[+0.00] G[+5] H[-5]
      *>   I[1.50] J[+.0] K[+0] L[007] M[-0.5]
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2508A.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 C-NZ CONSTANT AS -0.
       PROCEDURE DIVISION.
           DISPLAY "A[" -0 "]"
           DISPLAY "B[" -0.0 "]"
           DISPLAY "C[" +0 "]"
           DISPLAY "D[" 0 "]"
           DISPLAY "E[" -00 "]"
           DISPLAY "F[" -0.00 "]"
           DISPLAY "G[" +5 "]"
           DISPLAY "H[" -5 "]"
           DISPLAY "I[" 1.50 "]"
           DISPLAY "J[" -.0 "]"
           DISPLAY "K[" C-NZ "]"
           DISPLAY "L[" 007 "]"
           DISPLAY "M[" -0.5 "]"
           STOP RUN.
