      *>--- conformance-case
      *> id: pb5103_inspect_identifier_sizes_match
      *> rules: [SR-14.9.22.3-6, SR-14.9.22.3-7, SR-14.9.22.3-9]
      *> accept: [85, 2002, 2014, 2023]
      *> source-format: fixed
      *>---
      *> kb/Work PB5103, PB5089. The identifier forms of INSPECT's
      *> size rules that every edition ACCEPTS.
      *> §1985:VI.6.18.3 SR7: "The size of literal-3 or the data item
      *> referenced by identifier-5 must be equal to the size of
      *> literal-1 or the data item referenced by identifier-3."
      *> SR8: "When the CHARACTERS phrase is used, literal-3 or the
      *> size of the data item referenced by identifier-5 must be one
      *> character in length." SR9 is the CONVERTING twin of SR7.
      *> SR10: "The same character must not appear more than once
      *> either in literal-4 or in the data item referenced by
      *> identifier-6." Each statement keeps the rule, so every
      *> edition compiles it; --std 85 refuses the mis-sized forms
      *> (the pb5103-inspect-*-85 and pb5089-inspect-*-85 tests).
      *> EXPECTED OUTPUT, DERIVED FROM THE RULES, NOT FROM A RUN:
      *>  L1: WS-F (two characters) and WS-T2 (two) are equal in
      *>      size, so ALL "AB" -> "00" in "ABCDAB": 00CD00.
      *>  L2: WS-ONE is one character; CHARACTERS BY "*" BEFORE
      *>      INITIAL "D" replaces the three characters before the
      *>      first D of "ABCDAB": ***DAB.
      *>  L3: WS-CF and WS-CT are two characters each, so CONVERTING
      *>      "AB" TO "XY" over "ABCD": XYCD.
      *>  L4: literal-4 "ABC" repeats no character; "ABCABC" converts
      *>      to XYZXYZ.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5103A.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  WS-X   PIC X(6) VALUE "ABCDAB".
       01  WS-Y   PIC X(6) VALUE "ABCDAB".
       01  WS-Z   PIC X(4) VALUE "ABCD".
       01  WS-W   PIC X(6) VALUE "ABCABC".
       01  WS-F   PIC XX VALUE "AB".
       01  WS-T2  PIC XX VALUE "00".
       01  WS-ONE PIC X VALUE "*".
       01  WS-CF  PIC XX VALUE "AB".
       01  WS-CT  PIC XX VALUE "XY".
       PROCEDURE DIVISION.
       MAIN-PARA.
           INSPECT WS-X REPLACING ALL WS-F BY WS-T2.
           DISPLAY "L1=" WS-X.
           INSPECT WS-Y REPLACING CHARACTERS BY WS-ONE
               BEFORE INITIAL "D".
           DISPLAY "L2=" WS-Y.
           INSPECT WS-Z CONVERTING WS-CF TO WS-CT.
           DISPLAY "L3=" WS-Z.
           INSPECT WS-W CONVERTING "ABC" TO "XYZ".
           DISPLAY "L4=" WS-W.
           STOP RUN.
