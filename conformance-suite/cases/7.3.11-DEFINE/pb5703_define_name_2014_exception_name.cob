      *>--- conformance-case
      *> id: pb5703_define_name_2014_exception_name
      *> rules: [SR-7.3.11.3-1]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5703 - §2002:8.12 reserves in compiler directives the
      *> exception-names of §2002:14.5.12.1 only. EC-FLOW-SEARCH is on
      *> the 2014 and 2023 lists, so at 2002 it is an ordinary
      *> compilation-variable-name and from 2014 the DEFINE is refused
      *> (COBOL2650,
      *> wiseowl/diagnostics/pb5703_define_name_2014_exception_name.json).
      *>   cite.py: OK  §2002:7.2.9.2 SR1  "Compilation-variable-name-1
      *>   shall not be the same as a compiler-directive reserved word"
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5703DF.
       PROCEDURE DIVISION.
       >>DEFINE EC-FLOW-SEARCH AS 1
       >>IF EC-FLOW-SEARCH = 1
           DISPLAY "A".
       >>END-IF
           STOP RUN.
