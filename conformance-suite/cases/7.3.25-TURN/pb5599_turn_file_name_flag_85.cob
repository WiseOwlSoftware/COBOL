      *>--- conformance-case
      *> id: pb5599_turn_file_name_flag_85
      *> rules: [SR-7.3.3-6, SR-7.3.25.3-1]
      *> accept: [2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5599 - FLAG-85 is a compiler-directive word of
      *> §2002:8.12 and §2014:8.12 only, so after EC-I-O it is not
      *> file-name-1 at 2002 and 2014 and the directive matches no
      *> format (COBOL0718,
      *> wiseowl/diagnostics/pb5599_turn_file_name_flag_85.json); at 2023
      *> it is an ordinary file-name and the directive is accepted.
      *>   cite.py: OK  §2002:7.2.19.2 SR1  "Any user-defined word that
      *>   duplicates a compiler-directive word is interpreted as a
      *>   compiler-directive word rather than as file-name-1"
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5599TF.
       PROCEDURE DIVISION.
       >>TURN EC-I-O FLAG-85 CHECKING ON
           DISPLAY "A".
           STOP RUN.
