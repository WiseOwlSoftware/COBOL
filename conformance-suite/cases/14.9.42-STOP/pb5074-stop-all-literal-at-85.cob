      *>--- conformance-case
      *> id: pb5074-stop-all-literal-at-85
      *> rules: [FMT-14.9.42.2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5074 - 1985 VI.6.24.3 SR1: "Literal-1 must not be a
      *>   figurative constant that begins with the word ALL."
      *>   Expected: COBOL6817 at 85.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5074NSA.
       PROCEDURE DIVISION.
       MAIN-P.
           STOP ALL "A"
           STOP RUN.
