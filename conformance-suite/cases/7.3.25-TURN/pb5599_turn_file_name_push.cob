      *>--- conformance-case
      *> id: pb5599_turn_file_name_push
      *> rules: [SR-7.3.3-6, SR-7.3.25.3-1]
      *> accept: [2002, 2014]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5599 - a user-defined word that duplicates a
      *> compiler-directive word is interpreted as that word rather than
      *> as file-name-1, and which words those are is the edition's own
      *> §8.12. PUSH is on §2023:8.12 only, so at 2002 and 2014 it is a
      *> file-name (the directive is accepted) and at 2023 the directive
      *> matches no format (COBOL0718,
      *> wiseowl/diagnostics/pb5599_turn_file_name_push.json).
      *>   cite.py: OK  §2002:7.2.19.2 SR1  "Any user-defined word that
      *>   duplicates a compiler-directive word is interpreted as a
      *>   compiler-directive word rather than as file-name-1"
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5599TP.
       PROCEDURE DIVISION.
       >>TURN EC-I-O PUSH CHECKING ON
           DISPLAY "A".
           STOP RUN.
