      *>--- conformance-case
      *> id: pb4919-set-condition-repeated-at-85
      *> rules: [FMT-14.9.39.2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4919 - the edition half of pb450_set_outer_repetition.
      *>   COBOL-85's Format 4 is SET {condition-name-1} ... TO TRUE
      *>   with no ellipsis on the whole unit (§1985:VI.6.23.2, page
      *>   VI-127), so one SET statement carries one TO TRUE phrase; the
      *>   ISO/IEC 1989:2002 post-ballot text (14.8.35.1 Format 4) adds
      *>   the outer ellipsis, as §2023:14.9.39.2 prints it. The
      *>   statement below writes TRUE only, so the FALSE arm's own gate
      *>   (set-condition-false-2002) is not what refuses it. Compiled
      *>   clean at 85 before the gate.
      *>   Expected: COBOL0900 at 85.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P4919ST.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A PIC 9 VALUE 0.
          88 A1 VALUE 1.
       01 B PIC 9 VALUE 0.
          88 B1 VALUE 2.
       PROCEDURE DIVISION.
       MAIN-PARA.
           SET A1 TO TRUE B1 TO TRUE.
           DISPLAY A B.
           STOP RUN.
