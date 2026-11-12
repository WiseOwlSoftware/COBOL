      *>--- conformance-case
      *> id: pb5277-replace-scope-ended
      *> rules: [1985-GR-XII.3.4-3]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XII.3.4 GR3 (cite.py --check: OK): the REPLACE statement of SC34C
      *> is in effect "until ... the end of the separately compiled program", so
      *> TAG in SC34D, a separately compiled program, is no longer replaced and
      *> names no data item: refused at 85. From 2002 the scope is the compilation
      *> group and the program is legal (pb5277_replace_scope_compilation_group).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. SC34C.
       REPLACE ==TAG== BY =="A"==.
       PROCEDURE DIVISION.
       MAIN-P.
           DISPLAY TAG.
           CALL "SC34D".
           STOP RUN.
       END PROGRAM SC34C.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. SC34D.
       PROCEDURE DIVISION.
       MAIN-P.
           DISPLAY TAG.
           EXIT PROGRAM.
       END PROGRAM SC34D.
