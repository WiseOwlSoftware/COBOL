      *>--- conformance-case
      *> id: pb3347-switch-condition-name-ambiguous-set
      *> rules: [SR-8.4.2.2.1-1, SR-8.4.2.2.3-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB3347, the SET Format 4 position of pb3347-switch-condition-name-ambiguous-if: `SET S-ON TO TRUE`
      *> names condition-name-1, and S-ON is both the level-88 below and this element's switch condition-name, so
      *> the unqualified reference is non-unique (8.4.2.2.1 1; 8.4.2.2.3 SR1) and is refused (COBOL1639) at every
      *> edition. Before the fix it set the level-88 and said nothing.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3347NEGSET.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           SWITCH-31 ON STATUS IS S-ON.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 V PIC 9 VALUE 2.
          88 S-ON VALUE 1.
       PROCEDURE DIVISION.
       MAIN.
           SET S-ON TO TRUE.
           STOP RUN.
