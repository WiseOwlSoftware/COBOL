      *>--- conformance-case
      *> id: 1985-SR-VI_5_7_3-1
      *> rules: [1985-SR-VI.5.7.3-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.5.7.3 SR1 (LEVEL-NUMBER): "A level-number is
      *> required as the first element in each data description
      *> entry." A refusal: line 000600 below is a data description
      *> entry that begins with its data-name and has no
      *> level-number, so the rule forbids it, and a compiler that
      *> accepts the program fails. Line 000500 is the same kind of
      *> entry with its level-number and is legal, so the missing
      *> level-number is the only fault. Written by hand in 1985
      *> reference format: the free-form case of the 2023 rule this
      *> row is kept by cannot be converted (kb/Work PB5754).
000100 IDENTIFICATION DIVISION.
000200 PROGRAM-ID. LVLMIS.
000300 DATA DIVISION.
000400 WORKING-STORAGE SECTION.
000500 77  V PIC X(4) VALUE "ABCD".
000600 W PIC X(3).
000700 PROCEDURE DIVISION.
000800 P1.
000900     DISPLAY V.
001000     STOP RUN.
