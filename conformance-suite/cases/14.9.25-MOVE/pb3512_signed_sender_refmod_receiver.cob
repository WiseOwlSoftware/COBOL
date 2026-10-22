      *>--- conformance-case
      *> id: pb3512_signed_sender_refmod_receiver
      *> rules: [GR-14.9.25.4-6]
      *> accept: [85, 2002, 2014, 2023]
      *> source-format: fixed
      *>---
      *> kb/Work PB3512. A MOVE of a SIGNED NUMERIC sender into a
      *> REFERENCE-MODIFIED alphanumeric receiver does not move the
      *> operational sign, exactly as the same MOVE into the whole item.
      *> ISO §8.4.3.3.4 GR6 makes the slice an elementary item:
      *>   cite.py --check 8.4.3.3.4 "The unique data item is considered
      *>   to be an elementary data item without the JUSTIFIED clause."
      *>   -> OK  §8.4.3.3.4 6)  (General rules)
      *> so the move is §14.9.25.4 GR6 a)'s elementary move:
      *>   cite.py --check 14.9.25.4 "If the sending operand is described
      *>   as being signed numeric, the operational sign is not moved; if
      *>   the operational sign occupies a separate character position,
      *>   that character is not moved and the size of the sending operand
      *>   is considered to be one less than its actual size."
      *>   -> OK  §14.9.25.4 6)  (General rules)
      *> and the receiving alignment is §14.6.8's: left-justified, space
      *> filled, truncated on the right. Before the fix every slice took
      *> one arm before the sender's category was asked and received the
      *> sender's STORED form: B [246H ], C [-1234], D [-1357], E [-42  ].
      *> EXPECTED OUTPUT, derived line by line (the sender digits only):
      *>   A  S9(4) 2468 into the WHOLE X(5): the control, [2468 ].
      *>   B  the same sender into X5(1:5): the operational sign is not
      *>      moved, so [2468 ].
      *>   C  PACKED-DECIMAL -1234: the usage differs, so it is converted
      *>      to the receiver's representation, sign not moved: [1234 ].
      *>   D  SIGN LEADING SEPARATE -1357: the sign character is not moved
      *>      and the size is four, so [1357 ].
      *>   E  the signed literal -42: [42   ].
      *>   F  SIGN TRAILING SEPARATE -55 into X8(2:5): size four, [0055 ]
      *>      into positions 2-6 of an all-'*' item: [*0055 **].
      *>   J  -2468 into a JUSTIFIED RIGHT item's slice (2:4): the slice
      *>      has no JUSTIFIED clause and its size is four: [*2468*].
      *>   K  -2468 into a 2-position slice: truncated on the right, the
      *>      sign not moved: [24******].
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3512RMSIGN.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 S4 PIC S9(4) VALUE 2468.
       01 SN PIC S9(4) VALUE -2468.
       01 P4 PIC S9(4) USAGE PACKED-DECIMAL VALUE -1234.
       01 L4 PIC S9(4) SIGN LEADING SEPARATE VALUE -1357.
       01 T4 PIC S9(4) SIGN TRAILING SEPARATE VALUE -55.
       01 X5 PIC X(5).
       01 X8 PIC X(8).
       01 XJ PIC X(6) JUSTIFIED RIGHT.
       PROCEDURE DIVISION.
           MOVE S4 TO X5
           DISPLAY "A [" X5 "]"
           MOVE S4 TO X5(1:5)
           DISPLAY "B [" X5 "]"
           MOVE P4 TO X5(1:5)
           DISPLAY "C [" X5 "]"
           MOVE L4 TO X5(1:5)
           DISPLAY "D [" X5 "]"
           MOVE -42 TO X5(1:5)
           DISPLAY "E [" X5 "]"
           MOVE ALL "*" TO X8
           MOVE T4 TO X8(2:5)
           DISPLAY "F [" X8 "]"
           MOVE ALL "*" TO XJ
           MOVE SN TO XJ(2:4)
           DISPLAY "J [" XJ "]"
           MOVE ALL "*" TO X8
           MOVE SN TO X8(1:2)
           DISPLAY "K [" X8 "]"
           STOP RUN.
