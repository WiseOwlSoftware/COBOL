      *>--- conformance-case
      *> id: pb5074-stop-decimal-literal-at-85
      *> rules: [FMT-14.9.42.2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5074 - 1985 VI.6.24.3 SR3: "If literal-1 is numeric,
      *>   then it must be an unsigned integer." 1.5 is no integer.
      *>   Expected: COBOL6817 at 85 (from 2002 STOP literal is removed).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5074NSP.
       PROCEDURE DIVISION.
       MAIN-P.
           STOP 1.5
           STOP RUN.
