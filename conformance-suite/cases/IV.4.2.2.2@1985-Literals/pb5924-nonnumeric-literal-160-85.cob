      *>--- conformance-case
      *> id: pb5924-nonnumeric-literal-160-85
      *> rules: [1985-SR-IV.4.2.2.2.1-p1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5924. §1985:IV.4.2.2.2.1 (cite.py: OK): "nonnumeric literals of 1 through 160 characters in length"
      *> A 160-character nonnumeric literal, continued across lines,
      *> moves whole: its 160th character stands beside the marker.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. NNL160.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  G.
           05  L160          PIC X(160).
           05  MARK          PIC X VALUE "*".
       01  L1                PIC X(2).
       PROCEDURE DIVISION.
       MAIN-PARA.
           MOVE "1234567890123456789012345678901234567890123456789012345
      -    "678901234567890123456789012345678901234567890123456789012345
      -    "678901234567890123456789012345678901234567890" TO L160.
           DISPLAY G.
           MOVE "Z" TO L1.
           DISPLAY L1 "|".
           STOP RUN.
