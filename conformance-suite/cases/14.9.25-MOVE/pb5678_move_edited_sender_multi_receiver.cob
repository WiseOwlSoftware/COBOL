      *>--- conformance-case
      *> id: pb5678_move_edited_sender_multi_receiver
      *> rules: [GR-14.9.25.4-1]
      *> accept: [85, 2002, 2014, 2023]
      *> source-format: fixed
      *>---
      *> kb/Work PB5678. §2023:14.9.25.4 GR1 - "Literal-1 or the
      *>   content of the data item referenced by identifier-1 is
      *>   moved to the data item referenced by each identifier-2 in
      *>   the order specified." (cite.py: OK). The CONTENT of an
      *>   alphanumeric-edited sender is the characters it holds, the
      *>   inserted B blank and / slash included; the same sentence
      *>   is §1985:VI.6.19.4 GR2 and §2002:14.8.24.3 GR1. A second
      *>   receiving operand makes the implementor's intermediate
      *>   result item hold the sender, and that intermediate must
      *>   hold the characters as they are: a copy that applies the
      *>   sender's insertion editing again stores "A" (SXE) and
      *>   "A//" (SXS). Each leg fails on that.
      *>   SXE PIC XBX holds "A B"; SXS PIC X/X holds "A/B".
      *>   1 one receiver: [A B  ] (the control, no intermediate).
      *>   2 two receivers: [A B  ][A B  ], each the content padded
      *>     with spaces to the receiver's five positions (GR4 a).
      *>   3 SXS to two receivers: [A/B  ][A/B  ].
      *>   4 three receivers, the middle one a group of five
      *>     characters: every receiver gets the content.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5678A.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 SXE  PIC XBX  VALUE "A B".
       01 SXS  PIC X/X  VALUE "A/B".
       01 RX   PIC X(5).
       01 RX2  PIC X(5).
       01 RG.
          05 RGC PIC X(5).
       01 RX3  PIC X(5).
       PROCEDURE DIVISION.
       MAIN.
           MOVE SXE TO RX
           DISPLAY "1 [" RX "]"
           MOVE SXE TO RX RX2
           DISPLAY "2 [" RX "][" RX2 "]"
           MOVE SXS TO RX RX2
           DISPLAY "3 [" RX "][" RX2 "]"
           MOVE SXE TO RX RG RX3
           DISPLAY "4 [" RX "][" RGC "][" RX3 "]"
           STOP RUN.
