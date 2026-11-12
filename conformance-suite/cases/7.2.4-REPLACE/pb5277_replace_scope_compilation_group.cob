      *>--- conformance-case
      *> id: pb5277_replace_scope_compilation_group
      *> rules: [GR-7.2.4.4-5]
      *> accept: [2002, 2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:7.1.3.3 GR5 (cite.py --check: OK): "A REPLACE statement that is
      *> placed in the active state remains active until it is placed in the
      *> inactive state, it is cancelled, or the end of the compilation group is
      *> reached". Both programs are in one compilation group, so TAG is replaced
      *> by "A" in each: A, A from 2002 on. COBOL-85 ends the scope at the end of
      *> the separately compiled program: pb5277-replace-scope-ended (kb/Work
      *> PB5277).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. SC44A.
       REPLACE ==TAG== BY =="A"==.
       PROCEDURE DIVISION.
       MAIN-P.
           DISPLAY TAG.
           CALL "SC44B".
           STOP RUN.
       END PROGRAM SC44A.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. SC44B.
       PROCEDURE DIVISION.
       MAIN-P.
           DISPLAY TAG.
           GOBACK.
       END PROGRAM SC44B.
