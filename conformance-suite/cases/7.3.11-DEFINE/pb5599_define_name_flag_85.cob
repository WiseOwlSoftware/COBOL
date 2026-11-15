      *>--- conformance-case
      *> id: pb5599_define_name_flag_85
      *> rules: [SR-7.3.11.3-1]
      *> accept: [2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5599 - FLAG-85 is a compiler-directive word of
      *> §2002:8.12 and §2014:8.12 and of no later list: §2023:8.12 drops
      *> it (with FLAG-NATIVE-ARITHMETIC). So the DEFINE below is refused
      *> at 2002 and 2014 (COBOL2650,
      *> wiseowl/diagnostics/pb5599_define_name_flag_85.json) and
      *> defines an ordinary compilation variable at 2023.
      *>   cite.py: OK  §2002:7.2.9.2 SR1  "Compilation-variable-name-1
      *>   shall not be the same as a compiler-directive reserved word"
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5599F8.
       PROCEDURE DIVISION.
       >>DEFINE FLAG-85 AS 1
       >>IF FLAG-85 = 1
           DISPLAY "FLAG-85".
       >>END-IF
           STOP RUN.
