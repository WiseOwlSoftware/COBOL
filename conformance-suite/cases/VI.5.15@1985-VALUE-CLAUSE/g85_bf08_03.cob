      *>--- conformance-case
      *> id: g85_bf08_03
      *> rules: [1985-GR-VI.5.15.5-1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.5.15.5 GR1 - "In a condition-name entry, the VALUE
      *> clause is required. The VALUE clause and the condition-name
      *> itself are the only two clauses permitted in the entry. The
      *> characteristics of a condition-name are implicitly those of its
      *> conditional variable." (level-2 Nucleus text, legal at 85.)
      *> Each 88 entry here is exactly level, name and VALUE. Its
      *> characteristics are its variable's, observed two ways:
      *> SET ... TO TRUE places the literal "according to the rules of
      *> the VALUE clause" (§1985:VI.6.23.4 GR7), i.e. with the
      *> variable's PICTURE: VALUE 7 into PIC 999 gives N=[007]; VALUE
      *> "A" into PIC X(3) gives X=[A  ]. The condition test compares
      *> "the same as those specified for relation conditions"
      *> (§1985:VI.6.3.1.3), i.e. by the variable's class: numerically
      *> for PIC 999 (007 equals 7: N7-T; 15 lies in 10 THRU 20: NR-T
      *> and N7-F) and for PIC S99 (-3 equals -3: S-T; +3 does not: S-F),
      *> and as alphanumeric with space padding for PIC X(3) (X-T).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85BF0803.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  WS-N PIC 999.
           88  N-SEVEN VALUE 7.
           88  N-RANGE VALUE 10 THRU 20.
       01  WS-S PIC S99.
           88  S-NEG VALUE -3.
       01  WS-X PIC X(3).
           88  X-A VALUE "A".
       PROCEDURE DIVISION.
       MAIN-PARA.
           SET N-SEVEN TO TRUE.
           DISPLAY "N=[" WS-N "]".
           IF N-SEVEN DISPLAY "N7-T" ELSE DISPLAY "N7-F".
           MOVE 15 TO WS-N.
           IF N-RANGE DISPLAY "NR-T" ELSE DISPLAY "NR-F".
           IF N-SEVEN DISPLAY "N7-T" ELSE DISPLAY "N7-F".
           MOVE -3 TO WS-S.
           IF S-NEG DISPLAY "S-T" ELSE DISPLAY "S-F".
           MOVE 3 TO WS-S.
           IF S-NEG DISPLAY "S-T" ELSE DISPLAY "S-F".
           SET X-A TO TRUE.
           DISPLAY "X=[" WS-X "]".
           IF X-A DISPLAY "X-T" ELSE DISPLAY "X-F".
           STOP RUN.
