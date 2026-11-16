      *>--- conformance-case
      *> id: g85_bf13_01
      *> rules: [1985-GR-VI.5.14.4-5, 1985-GR-VI.5.14.4-6]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.5.14.4 GR5 - USAGE IS DISPLAY, explicit at the
      *> elementary level (REC-E) and at the group level (REC-G), puts
      *> the item in standard data format (decimal digits as characters)
      *> aligned on a character boundary: the 4-character view shows the
      *> 1-character item then the 3 digits with no slack (E=, G= lines).
      *> §1985:VI.5.14.4 GR6 - no USAGE on the item or its group (REC-I)
      *> is implicitly DISPLAY: its view is the same characters as the
      *> explicit one (I= lines and the SAME line).
      *> A leg fails if a numeric DISPLAY item is held in a binary or
      *> other non-character form, if it is padded to a word boundary,
      *> or if the implicit usage differs from the explicit one.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85BF1301.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  REC-I.
           05  GRP-I.
               10  I-ALF       PIC X     VALUE "A".
               10  I-NUM       PIC 9(3)  VALUE 123.
           05  VIEW-I REDEFINES GRP-I PIC X(4).
       01  REC-E.
           05  GRP-E.
               10  E-ALF       PIC X     USAGE IS DISPLAY VALUE "A".
               10  E-NUM       PIC 9(3)  USAGE DISPLAY VALUE 123.
           05  VIEW-E REDEFINES GRP-E PIC X(4).
       01  REC-G.
           05  GRP-G           USAGE IS DISPLAY.
               10  G-ALF       PIC X     VALUE "A".
               10  G-NUM       PIC 9(3)  VALUE 123.
           05  VIEW-G REDEFINES GRP-G PIC X(4).
       PROCEDURE DIVISION.
       P-MAIN.
           DISPLAY "I=" VIEW-I.
           DISPLAY "E=" VIEW-E.
           DISPLAY "G=" VIEW-G.
           MOVE 456 TO I-NUM.
           MOVE 456 TO E-NUM.
           DISPLAY "I=" VIEW-I.
           DISPLAY "E=" VIEW-E.
           IF VIEW-I = VIEW-E
               DISPLAY "SAME"
           ELSE
               DISPLAY "DIFFERENT".
           STOP RUN.
