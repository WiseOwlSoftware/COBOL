      *>--- conformance-case
      *> id: pb5298_collating_then_segment_limit
      *> rules: [1985-FMT-XVI.2.2.2, 1985-FMT-XVI.2.3.2,
      *>     1985-GR-XVI.2.2.4-3]
      *> accept: [85]
      *> source-format: fixed
      *>---
      * X3.23-1985 XVI 2.2.2 prints [ PROGRAM COLLATING SEQUENCE IS
      * alphabet-name-1 ] [ SEGMENT-LIMIT IS segment-number ] in that
      * order, and XVI 2.3.2 makes IS optional. ISO 2002's optional
      * alphabet-name-2 slot took the word SEGMENT-LIMIT, and the
      * integer was left over (kb/Work PB5298). The clause here
      * follows the collating clause with no IS, on lines of its own.
      * The leg fails if the program is refused, or if REV-SEQ is not
      * the program collating sequence: under it "Z" < "A" is true
      * (XVI 2.2.4 GR3 sends the comparison through the alphabet).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5298A.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       OBJECT-COMPUTER. GENERIC
           PROGRAM COLLATING SEQUENCE IS REV-SEQ
           SEGMENT-LIMIT
           49.
       SPECIAL-NAMES.
           ALPHABET REV-SEQ IS "Z" THRU "A".
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A PIC X VALUE "A".
       01 Z PIC X VALUE "Z".
       PROCEDURE DIVISION.
       P1.
           IF Z < A DISPLAY "REVERSED" ELSE DISPLAY "NATIVE".
           STOP RUN.
