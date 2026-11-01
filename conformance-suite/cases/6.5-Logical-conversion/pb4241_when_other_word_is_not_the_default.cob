      *>--- conformance-case
      *> id: pb4241_when_other_word_is_not_the_default
      *> rules: [GR-6.5-5]
      *> accept: [2002, 2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO 1989:2023 7.3.13.3 SR5/SR6: >>WHEN OTHER is the phrase whose
      *> selection object is the word OTHER, specified entirely on its
      *> line. A selection object that merely BEGINS with those letters
      *> (the compilation variable OTHERV) is an ordinary >>WHEN, so the
      *> >>EVALUATE has no default branch and the branch that spells
      *> IDENTIFICATION DIVISION is one the logical conversion cannot
      *> know to be omitted (kb/Work PB4241, review round 1, R1-2). The
      *> text after >>END-EVALUATE is not taken for an IDENTIFICATION
      *> DIVISION, and the paragraph named REMARKS there is an ordinary
      *> paragraph.
      *> Leg that can fail: OTHERV read as the default branch makes every
      *> branch agree on IDENTIFICATION, REMARKS a comment-entry
      *> paragraph, and the statement after its header a syntax error.
000100 IDENTIFICATION DIVISION.
000200 PROGRAM-ID. PB4241WO.
000300 >>DEFINE OTHERV AS 2
000400 PROCEDURE DIVISION.
000500 MAIN-PARA.
000600     PERFORM REMARKS.
000700     STOP RUN.
000800 >>EVALUATE 1
000900 >>WHEN OTHERV
001000 IDENTIFICATION DIVISION.
001100 >>END-EVALUATE
001200 REMARKS.
001300     DISPLAY "IN-REMARKS".
