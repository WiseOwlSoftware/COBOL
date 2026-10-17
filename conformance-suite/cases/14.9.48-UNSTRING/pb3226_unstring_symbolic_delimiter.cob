      *>--- conformance-case
      *> id: pb3226_unstring_symbolic_delimiter
      *> rules: [GR-14.9.48.4-7, GR-14.9.48.4-8]
      *> accept: [85, 2002, 2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB3226 - a bare symbolic-character delimiter is a figurative constant WITHOUT the word ALL
      *> (12.3.7.4 GR11 a): ONE occurrence of its character. UNSTRING DELIMITED BY symbolic-character ran as
      *> DELIMITED BY ALL, so contiguous delimiters collapsed into one and every later receiver shifted.
      *>
      *> ISO 14.9.48.4 GR7: "When the ALL phrase is specified, one occurrence or two or more contiguous
      *> occurrences of literal-1 (figurative constant or not) ... are treated as if they were only one
      *> occurrence" - the collapse belongs to the ALL phrase alone.
      *> ISO 14.9.48.4 GR8: "When any examination encounters two contiguous delimiters, the current receiving
      *> area shall be space-filled if it is described as alphabetic, alphanumeric, or national; or
      *> zero-filled if it is described as numeric."
      *>
      *> SYMBOLIC CHARACTERS COMMA-CH IS 45 names the character at ordinal position 45 of the native
      *> collating sequence, the comma. The sender "A,,12" holds A, two commas, 1, 2.
      *>   L1 DELIMITED BY COMMA-CH (no ALL): receivers X1 / N2 / X3 take "A", the empty field between the
      *>      two commas (N2 is numeric, so zero-filled: 00) and "12" -> [A  ][00][12 ].
      *>   L2 DELIMITED BY ",": the same statement over the literal - the same result.
      *>   L3 DELIMITED BY ALL COMMA-CH: the two commas are ONE delimiter, so X1 = "A", N2 = 12 and X3
      *>      is not accessed (it keeps its previous content) -> [A  ][12][zzz].
      *>   L4 DELIMITED BY ALL ",": the same as L3.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3226P1.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           SYMBOLIC CHARACTERS COMMA-CH IS 45.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 SRC PIC X(5) VALUE "A,,12".
       01 X1 PIC X(3).
       01 N2 PIC 99.
       01 X3 PIC X(3).
       PROCEDURE DIVISION.
       MAIN.
           MOVE "zzz" TO X1 X3
           MOVE 99 TO N2
           UNSTRING SRC DELIMITED BY COMMA-CH INTO X1 N2 X3
           DISPLAY "L1 [" X1 "][" N2 "][" X3 "]"
           MOVE "zzz" TO X1 X3
           MOVE 99 TO N2
           UNSTRING SRC DELIMITED BY "," INTO X1 N2 X3
           DISPLAY "L2 [" X1 "][" N2 "][" X3 "]"
           MOVE "zzz" TO X1 X3
           MOVE 99 TO N2
           UNSTRING SRC DELIMITED BY ALL COMMA-CH INTO X1 N2 X3
           DISPLAY "L3 [" X1 "][" N2 "][" X3 "]"
           MOVE "zzz" TO X1 X3
           MOVE 99 TO N2
           UNSTRING SRC DELIMITED BY ALL "," INTO X1 N2 X3
           DISPLAY "L4 [" X1 "][" N2 "][" X3 "]"
           STOP RUN.
