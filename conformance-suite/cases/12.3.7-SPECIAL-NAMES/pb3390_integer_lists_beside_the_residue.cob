      *>--- conformance-case
      *> id: pb3390_integer_lists_beside_the_residue
      *> rules: [FMT-12.3.6.2, FMT-12.3.7.2, FMT-12.4.5.1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB3390 - outside the data division the closed formats' error
      *> production now opens and continues at an unsigned integer (no
      *> level-number can follow those lists), so an integer that a clause
      *> of the list owns could also be read as the clause's end and a
      *> foreign run. The reading that keeps the integers in their clause
      *> wins, so this LEGAL program means what it says: the 85 MEMORY SIZE
      *> and SEGMENT-LIMIT clauses (IS omitted), the SYMBOLIC CHARACTERS
      *> integer list SC-B SC-D ARE 2 4 and SELECT ... RESERVE 2 AREAS.
      *> ISO 12.3.7.4 GR11 b): a symbolic character is "the coded character
      *> at ordinal position integer-1 ... in the character set referenced
      *> by alphabet-name-3"; GR7 k) 2.: the literals' order "specifies, in
      *> ascending sequence, the ordinal number of the character". AL-LIT
      *> is "A" "B" "C" "D", so ordinals 2, 3 and 4 are B, C and D: BCD.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P3390IL.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SOURCE-COMPUTER. GENERIC-BOX.
       OBJECT-COMPUTER. GENERIC-BOX
           MEMORY SIZE 8000 CHARACTERS
           SEGMENT-LIMIT 50.
       SPECIAL-NAMES.
           ALPHABET AL-LIT IS "A" "B" "C" "D"
           SYMBOLIC CHARACTERS SC-B SC-D ARE 2 4 IN AL-LIT
           SYMBOLIC CHARACTERS SC-C IS 3 IN AL-LIT.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F1 ASSIGN TO "p3390il.dat"
               RESERVE 2 AREAS.
       DATA DIVISION.
       FILE SECTION.
       FD F1.
       01 R1 PIC X(4).
       PROCEDURE DIVISION.
       MAIN-PARA.
           DISPLAY SC-B SC-C SC-D.
           STOP RUN.
