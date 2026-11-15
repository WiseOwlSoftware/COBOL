      *>--- conformance-case
      *> id: pb5599_define_name_push
      *> rules: [SR-7.3.11.3-1]
      *> accept: [2002, 2014]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5599 - the compiler-directive words are an edition's
      *> own list. §2002:8.12 and §2014:8.12 do not print PUSH or EQUATE
      *> (§2023:8.12 adds them with the PUSH and EQUATE directives), so at
      *> 2002 and 2014 each is an ordinary compilation-variable-name;
      *> at 2023 each is a compiler-directive word and the DEFINE is
      *> refused (COBOL2650,
      *> wiseowl/diagnostics/pb5599_define_name_push.json).
      *>   cite.py: OK  §2002:7.2.9.2 SR1  "Compilation-variable-name-1
      *>   shall not be the same as a compiler-directive reserved word"
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5599PU.
       PROCEDURE DIVISION.
       >>DEFINE PUSH AS 1
       >>DEFINE EQUATE AS 2
       >>IF PUSH = 1
           DISPLAY "PUSH".
       >>END-IF
       >>IF EQUATE = 2
           DISPLAY "EQUATE".
       >>END-IF
           STOP RUN.
