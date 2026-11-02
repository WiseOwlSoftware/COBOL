      *>--- conformance-case
      *> id: pb4412-past-margin-r-in-omitted-branch
      *> rules: [DOC-A.1-158]
      *> accept: [2002, 2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO/IEC 1989:2023 7.3.16.4 GR 3: the constant-conditional-expression is FALSE (TRACING is not
      *> defined), so all lines of text-1 are omitted from the resultant text. The line inside the branch
      *> holds text ONLY past margin R (position 72 here, Annex A.1 item 158, 6.3.1), so it reads as blank
      *> in any case; the program is accepted and shows OK. (kb/Work PB4412: the note COBOL5788 that names a
      *> vanished statement beside a syntax error never asks about this line, pinned by
      *> unit:MarginRCutNoteTests.)
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4412A1.
       PROCEDURE DIVISION.
       >>IF TRACING IS DEFINED
                                                                        DISPLAY "TRACE"
       >>END-IF
           DISPLAY "OK".
           STOP RUN.
