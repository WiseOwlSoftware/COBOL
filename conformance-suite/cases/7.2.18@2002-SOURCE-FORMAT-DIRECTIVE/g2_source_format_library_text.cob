      *>--- conformance-case
      *> id: g2_source_format_library_text
      *> rules: [2002-GR-7.2.18.2-3, 2002-GR-7.2.18.2-5]
      *> accept: [2002]
      *> files: [g2sfl2a.cpy, g2sfl2b.cpy]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:7.2.18.2 GR3 - "The default reference format of library
      *>   text is the reference format that was in effect for the COPY
      *>   statement that resulted in processing of this library text."
      *> §2002:7.2.18.2 GR5 - a SOURCE FORMAT directive in library text
      *>   is in effect "until another SOURCE FORMAT directive is
      *>   encountered or the end of the library text is reached", and
      *>   afterwards "the reference format shall revert to the
      *>   reference format that was in effect for the COPY statement".
      *> Each leg is a 2002 debugging line, whose form differs by
      *>   format: 'D'/'d' in the fixed indicator area (§2002:6.1.1) or
      *>   the floating >>D, "a debugging line when followed by at least
      *>   one space character" (§2002:6.1.2), written in column 1 so a
      *>   fixed-form reading would see the indicator area hold 'S'.
      *>   WITH DEBUGGING MODE compiles them (§2002:6.2.7, 6.3.4).
      *> EXPECTED OUTPUT, DERIVED (why each leg can fail):
      *>   [LIB-D-FIXED]  GR3: g2sfl2a is copied from fixed text, so it
      *>                  starts fixed; read free, 'D' is a word.
      *>   [LIB-D-FREE] [LIB-FREE]  GR5: its own >>SOURCE FORMAT FREE
      *>                  holds to its end; read fixed, column 1-6 text
      *>                  is a sequence number and the lines fail.
      *>   [MAIN-D-FIXED] [MAIN-FIXED]  GR5: after g2sfl2a the main
      *>                  text reverts to fixed: a 'D' line and 000400
      *>                  in the sequence area.
      *>   [LIB2-D-FREE]  GR3: g2sfl2b is copied from free text, so its
      *>                  first line, >>D in column 1, is free form.
      *>   [LIB2-D-FIXED] GR5: its >>SOURCE FORMAT FIXED, then a 'd'
      *>                  line in column 7.
      *>   [MAIN-D-FREE] [MAIN-FREE]  GR5: after g2sfl2b the main text
      *>                  reverts to free form.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2SFL2.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SOURCE-COMPUTER. ANY-KIND WITH DEBUGGING MODE.
       PROCEDURE DIVISION.
       MAIN-PARA.
           COPY g2sfl2a.
      D    DISPLAY "[MAIN-D-FIXED]"
000400     DISPLAY "[MAIN-FIXED]"
       >>SOURCE FORMAT FREE
COPY g2sfl2b.
>>D DISPLAY "[MAIN-D-FREE]"
DISPLAY "[MAIN-FREE]"
STOP RUN.
