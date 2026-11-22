      *>--- conformance-case
      *> id: pb5790-sign-separate-blank-when-zero-85
      *> rules: [1985-SR-VI.5.12.3-1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5790. "The SIGN clause may be specified only for a
      *> numeric data description entry whose PICTURE contains the
      *> character 'S', or a group item containing at least one such
      *> numeric data description entry" (§1985:VI.5.12.3 SR1, cite.py:
      *> OK) asks the PICTURE: S9(3) contains the S though BLANK WHEN
      *> ZERO makes the item numeric edited (§1985:VI.5.4.4 GR2), and
      *> 1985 has no rule against S beside BLANK WHEN ZERO. SEPARATE
      *> gives the sign its own leading or trailing position
      *> (§1985:VI.5.12.4 GR6a), whether the entry writes the clause or
      *> its group does (GR1); zero is nothing but spaces, the sign
      *> position too (§1985:VI.5.4.4 GR1). An embedded sign has no
      *> position in the edited image, so WE edits as the item without
      *> SIGN does (kb/Work PB5191). COBOL-2002 refuses S beside BLANK
      *> WHEN ZERO.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5790W.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WL PIC S9(5) SIGN LEADING SEPARATE BLANK WHEN ZERO.
       01 WT PIC S9(3)V99 SIGN TRAILING SEPARATE BLANK WHEN ZERO.
       01 WE PIC S9(3) SIGN LEADING BLANK WHEN ZERO.
       01 G SIGN TRAILING SEPARATE.
          02 GA PIC S9(3) BLANK WHEN ZERO.
          02 GB PIC S9(3).
       PROCEDURE DIVISION.
       P0.
           MOVE -12 TO WL. DISPLAY "[" WL "]".
           MOVE 12 TO WL. DISPLAY "[" WL "]".
           MOVE 0 TO WL. DISPLAY "[" WL "]".
           MOVE -1.5 TO WT. DISPLAY "[" WT "]".
           MOVE -7 TO WE. DISPLAY "[" WE "]".
           MOVE 12 TO GA. MOVE -5 TO GB. DISPLAY "[" G "]".
           MOVE 0 TO GA. DISPLAY "[" G "]".
           STOP RUN.
