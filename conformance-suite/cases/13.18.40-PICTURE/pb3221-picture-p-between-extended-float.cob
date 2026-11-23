*>--- conformance-case
*> id: pb3221-picture-p-between-extended-float
*> rules: [SR-13.18.40.3-16]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB3221 - ISO 1989:2023 13.18.40.3 SR16: "The symbol 'P' may appear only as a continuous
      *> string of 'P's in the leftmost or rightmost digit positions in character-string-1." 13.18.40.5 rule 6
      *> names the extended editing sign control symbols among the floating insertion symbols, so the
      *> floating string LL holds a digit position, and the 'P's of LLPP99 stand BETWEEN it and the '9's:
      *> neither leftmost nor rightmost. The composition validator left the extended symbols out of its
      *> digit positions, so this bound and the analyzer (which counted them) gave it scale 0: MOVE -5
      *> rendered "  00". The entry is COBOL1934 naming SR16. The EDITING phrase is a COBOL-2023
      *> introduction, so 2023 is the only edition at which the rule can bite here; the below-2023 gate is
      *> negative/pb490-picture-editing-at-85. The positive sibling is
      *> 2023/pb3221_picture_p_beside_extended_float_2023.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3221PB.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 PB01 PIC LLPP99 EDITING L FOR NEGATIVE IS "-".
       PROCEDURE DIVISION.
           STOP RUN.
