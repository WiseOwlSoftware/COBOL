      *>--- conformance-case
      *> id: pb4412-past-margin-r-in-debugging-line
      *> rules: [DOC-A.1-158]
      *> accept: [85, 2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> COBOL-85 debugging line (the fixed-form indicator D, obsolete in 2002 and removed in 2014;
      *> kb/Work PB1705, docs/CONFORMANCE.md D-DEBUG): the program names no WITH DEBUGGING MODE, so the D
      *> line is a comment line. Its text lies ONLY past margin R (position 72 here, Annex A.1 item 158),
      *> where nothing is program text anyway; the
      *> program is accepted and shows OK. (kb/Work PB4412: the note COBOL5788 never asks about the line of a
      *> unit without the mode, pinned by unit:MarginRCutNoteTests.)
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4412A2.
       PROCEDURE DIVISION.
      D                                                                 DISPLAY "TRACE"
       PB4305-LEAD.
           DISPLAY "OK".
           STOP RUN.
