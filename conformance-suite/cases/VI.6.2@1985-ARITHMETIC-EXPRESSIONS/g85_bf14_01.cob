      *>--- conformance-case
      *> id: g85_bf14_01
      *> rules: [1985-GR-VI.6.2.3-1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.6.2.3 GR1 - parentheses first, innermost set first;
      *> otherwise unary +/-, then **, then * and /, then + and -.
      *> L1 (14 not 20) fails if + binds before *; L2 (20) if parentheses
      *> are ignored; L3 (18 not 36) and L5 (38 not 100) if ** does not
      *> bind before * and +; L4 (+4 not -4) and L9 (+25 not -25) if unary
      *> minus does not bind before **; L6, L7 if nested parentheses are not
      *> evaluated least inclusive first; L8 if same-level parenthesized
      *> sets are not each evaluated before the operators between them.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85BF1401.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A PIC 9 VALUE 2.
       01 B PIC 9 VALUE 3.
       01 C PIC 9 VALUE 4.
       01 R PIC S999 VALUE 0.
       01 E PIC -999.
       PROCEDURE DIVISION.
       P1.
           COMPUTE R = A + B * C.
           MOVE R TO E.
           DISPLAY "L1 " E.
           COMPUTE R = (A + B) * C.
           MOVE R TO E.
           DISPLAY "L2 " E.
           COMPUTE R = A * B ** 2.
           MOVE R TO E.
           DISPLAY "L3 " E.
           COMPUTE R = - A ** 2.
           MOVE R TO E.
           DISPLAY "L4 " E.
           COMPUTE R = A + B ** 2 * C.
           MOVE R TO E.
           DISPLAY "L5 " E.
           COMPUTE R = ((A + B) * (C - A)) - B.
           MOVE R TO E.
           DISPLAY "L6 " E.
           COMPUTE R = (A + (B * (C + 1))) * 2.
           MOVE R TO E.
           DISPLAY "L7 " E.
           COMPUTE R = A * (B + C) - B * C.
           MOVE R TO E.
           DISPLAY "L8 " E.
           COMPUTE R = - (A + B) ** 2.
           MOVE R TO E.
           DISPLAY "L9 " E.
           STOP RUN.
