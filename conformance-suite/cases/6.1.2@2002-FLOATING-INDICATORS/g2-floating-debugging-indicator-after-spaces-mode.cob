      *>--- conformance-case
      *> id: g2-floating-debugging-indicator-after-spaces-mode
      *> rules: [2002-SR-6.1.2.1-7]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:6.1.2.1 SR7 - "A floating debugging indicator may be
      *> preceded on a line only by zero or more space characters": the
      *> granted form, accepted. §2002:6.1.2 - >>D is "a debugging line
      *> when followed by at least one space character". §2002:12.2.4.3
      *> GR4 - with DEBUGGING MODE "all debugging lines are compiled as
      *> if the debugging indicator was replaced by three spaces".
      *> [D-ZERO]     >>D first in the program-text area (column 8: no
      *>              space of the program-text area before it). A >>D
      *>              not read as a debugging indicator fails the
      *>              compile; a debugging line dropped as a comment
      *>              loses D-ZERO.
      *> [D-SPACES]   >>D after thirteen spaces: the same, at any depth.
      *> BEFORE/AFTER pin the ordinary lines around them.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2FDI01.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SOURCE-COMPUTER. COMPUTER-1 WITH DEBUGGING MODE.
       PROCEDURE DIVISION.
       P1.
           DISPLAY "BEFORE".
       >>D DISPLAY "D-ZERO".
                    >>D DISPLAY "D-SPACES".
           DISPLAY "AFTER".
           STOP RUN.
