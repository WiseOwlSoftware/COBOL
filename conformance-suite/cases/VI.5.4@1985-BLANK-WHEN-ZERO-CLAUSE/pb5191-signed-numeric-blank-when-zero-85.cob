      *>--- conformance-case
      *> id: pb5191-signed-numeric-blank-when-zero-85
      *> rules: [1985-SR-VI.5.4.3-1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5191 - §1985:VI.5.4.3 SR1: "The BLANK WHEN ZERO clause
      *> can be specified only for an elementary item whose PICTURE is
      *> specified as numeric or numeric edited" (cite.py: OK). S9(4) is
      *> a numeric PICTURE, and the only 1985 bar beside the clause is
      *> the asterisk (§1985:VI.5.9.3 SR7). §1985:VI.5.4.4 GR2 makes the
      *> item numeric edited; the S is not counted in its size, so it is
      *> four positions, and GR1 blanks it when its value is zero. A
      *> numeric edited item holds no sign: 0 -> four spaces, 12 and -12
      *> -> 0012. COBOL-2002 refuses the entry (§2002:13.16.38.2 SR18).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5191BZ.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  N2 PIC S9(4) BLANK WHEN ZERO.
       PROCEDURE DIVISION.
       MAIN-PARA.
           MOVE 0 TO N2.
           DISPLAY "[" N2 "]".
           MOVE 12 TO N2.
           DISPLAY "[" N2 "]".
           MOVE -12 TO N2.
           DISPLAY "[" N2 "]".
           STOP RUN.
