      *>--- conformance-case
      *> id: pb5703_defined_condition_flag_85
      *> rules: [SR-7.3.8.4.3-1]
      *> accept: [2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5599 - a compilation-variable-name shall not be a
      *> compiler-directive word of the edition. §2002:8.12 and §2014:8.12
      *> print FLAG-85, §2023:8.12 does not, so the defined condition
      *> naming it is refused at 2002 and 2014 (COBOL2650,
      *> wiseowl/diagnostics/pb5703_defined_condition_flag_85.json) and
      *> at 2023 it is a name that is not defined.
      *>   cite.py: OK  §2002:7.2.7.3.2 SR1  "Compilation-variable-name-1
      *>   shall not be the same as a compiler-directive reserved word"
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5703DC.
       PROCEDURE DIVISION.
       >>IF FLAG-85 DEFINED
           DISPLAY "A".
       >>ELSE
           DISPLAY "B".
       >>END-IF
           STOP RUN.
