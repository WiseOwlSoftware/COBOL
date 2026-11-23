      *>--- conformance-case
      *> id: pb5703_turn_exception_name_2014_set
      *> rules: [SR-7.3.25.3-2]
      *> accept: [2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5703 - exception-name-1 shall be one of the names the
      *> edition's own exception-conditions clause lists. §2002:14.5.12.1
      *> lacks EC-FUNCTION and its children, EC-BOUND-SET,
      *> EC-BOUND-TABLE-LIMIT, EC-DATA-NOT-FINITE, EC-DATA-OVERFLOW,
      *> EC-FLOW-SEARCH and EC-OO-ARG-OMITTED, which §2014:14.6.12.1 and
      *> §2023:14.6.13.1 list; so the TURN is accepted from 2014 and
      *> refused at 2002 (COBOL0878,
      *> wiseowl/diagnostics/pb5703_turn_exception_name_2014_set.json).
      *>   cite.py: OK  §2002:7.2.19.2 SR2  "Exception-name-1 shall be
      *>   one of the exception names listed in 14.5.12.1, Exception
      *>   conditions"
       >>TURN EC-FUNCTION CHECKING ON
       >>TURN EC-BOUND-SET CHECKING ON
       >>TURN EC-FLOW-SEARCH CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5703TN.
       PROCEDURE DIVISION.
           DISPLAY "A".
           STOP RUN.
