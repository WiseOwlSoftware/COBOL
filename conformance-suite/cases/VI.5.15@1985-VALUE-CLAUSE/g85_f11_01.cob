      *>--- conformance-case
      *> id: g85_f11_01
      *> rules: [1985-GR-VI.5.15.6-4]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.5.15.6 GR4 - group VALUE beside USAGE IS DISPLAY.
      *> GR4: "The VALUE clause must not be specified for a group item
      *> containing items subordinate to it with descriptions including
      *> JUSTIFIED, SYNCHRONIZED or USAGE (other than USAGE IS
      *> DISPLAY)." The parenthesis grants the form: a group VALUE over
      *> subordinates that state USAGE IS DISPLAY (G1, and G2/G3 whose
      *> subordinate group states it). The rule binds only the group
      *> that carries the VALUE: G4, with no group VALUE, holds COMP,
      *> SYNCHRONIZED and JUSTIFIED items whose own entries have VALUE.
      *> §1985:VI.5.15.6 GR3 gives the content: "the group area is
      *> initialized without consideration for the individual
      *> elementary or group items contained within this group."
      *> Each leg fails if the translator refuses the explicit DISPLAY
      *> usage under a group VALUE, or if the group literal is not laid
      *> over the subordinate positions left to right.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85F1101.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  G1 VALUE "ABC123".
           05  G1-A  PIC X(3) USAGE IS DISPLAY.
           05  G1-B  PIC 9(3) USAGE DISPLAY.
       01  G2 VALUE ALL "7".
           05  G2-S  USAGE IS DISPLAY.
               10  G2-N  PIC 99.
           05  G2-X  PIC X.
       01  G3 VALUE SPACES.
           05  G3-S  USAGE IS DISPLAY.
               10  G3-T  PIC XX.
       01  G4.
           05  G4-C  PIC 9(4) USAGE IS COMPUTATIONAL SYNCHRONIZED
                     VALUE 12.
           05  G4-J  PIC X(4) JUSTIFIED RIGHT.
       01  W-N       PIC 9(4).
       PROCEDURE DIVISION.
       MAIN-PARA.
           DISPLAY "G1-A " G1-A.
           DISPLAY "G1-B " G1-B.
           ADD 1 TO G1-B.
           DISPLAY "G1-B+1 " G1-B.
           ADD 1 TO G2-N.
           DISPLAY "G2-N+1 " G2-N.
           DISPLAY "G2-X " G2-X.
           DISPLAY "G3 [" G3-T "]".
           MOVE G4-C TO W-N.
           DISPLAY "G4-C " W-N.
           MOVE "AB" TO G4-J.
           DISPLAY "G4-J [" G4-J "]".
           STOP RUN.
