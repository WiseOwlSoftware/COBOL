      *>--- conformance-case
      *> id: 1985-SR-XII_3_3-6
      *> rules: [1985-SR-XII.3.3-6]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XII.3.3 SR6 (THE REPLACE STATEMENT). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from the confirmation pass's redraft (WEAK original) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. RPLTST.
       PROCEDURE DIVISION.
           REPLACE ==X== BY =="A"==
               ==ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789ABCDEFGHIJKLMNOPQRS
      -    TUVWXYZ0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789ABCDEFGH
      -    IJKLMNOPQRSTUVWXYZ0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456
      -    789ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789ABCDEFGHIJKLMNOPQRSTUV
      -    WXYZ0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789ABCDEFGHIJK
      -    LMNOPQRSTUVWXYZ01234567==
               BY =="B"==
             =="""""""""""""""""""""""""""""""""""""""""""""""""""""""""
      -    """""""""""""""""""""""""""""""""""""""""""""""""""""""""""""
      -    """""""""""""""""""""""""""""""""""""""""""""""""""""""""""""
      -    """""""""""""""""""""""""""""""""""""""""""""""""""""""""""""
      -    """""""""""""""""""""""""""""""""""""""""""""""""""""""""""""
      -    """"""""""""""""""""""""""==
               BY =="C"==.
       P1.
           DISPLAY X.
           DISPLAY ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789ABCDEFGHIJKLMNOPQ
      -    RSTUVWXYZ0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789ABCDEF
      -    GHIJKLMNOPQRSTUVWXYZ0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ01234
      -    56789ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789ABCDEFGHIJKLMNOPQRST
      -    UVWXYZ0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789ABCDEFGHI
      -    JKLMNOPQRSTUVWXYZ01234567.
           DISPLAY """""""""""""""""""""""""""""""""""""""""""""""""""""
      -    """""""""""""""""""""""""""""""""""""""""""""""""""""""""""""
      -    """""""""""""""""""""""""""""""""""""""""""""""""""""""""""""
      -    """""""""""""""""""""""""""""""""""""""""""""""""""""""""""""
      -    """""""""""""""""""""""""""""""""""""""""""""""""""""""""""""
      -    """""""""""""""""""""""""""""".
           STOP RUN.
