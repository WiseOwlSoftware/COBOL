      *>--- conformance-case
      *> id: g2rsm02-resume-outside-declarative-2002
      *> rules: [2002-SR-14.8.31.2-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:14.8.31.2 SR1 - "The RESUME statement may be specified
      *>   only in a declarative." RESUME in the nondeclarative
      *>   portion violates it and is refused at its line. COBOL-2002
      *>   has no exception-checking PERFORM WHEN phrase (the 2023
      *>   SR1 second place), so no other placement is legal at 2002.
      *>   Accepted leg: g2rsm01_resume_in_declarative_2002.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2RSM02.
       PROCEDURE DIVISION.
       MAIN-P.
           DISPLAY "A".
           RESUME AT NEXT STATEMENT.
           STOP RUN.
