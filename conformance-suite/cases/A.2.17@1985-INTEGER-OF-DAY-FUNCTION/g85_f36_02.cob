      *>--- conformance-case
      *> id: g85_f36_02
      *> rules: [1989-FMT-A.2.17.2, 1989-AR-A.2.17.3-1, 1989-RV-A.2.17.4-1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1989:A.2.17.2 - the format FUNCTION INTEGER-OF-DAY
      *> (argument-1) with a data item, a literal and an arithmetic
      *> expression (§1989:A.2.2 AR4) as argument-1, and the function
      *> used inside a larger arithmetic expression.
      *> §1989:A.2.17.4 RV1 - days after 1600-12-31: 1604366 is
      *> 1604-12-31 (1461), 2000060 is 2000-02-29 (145791), 1988213
      *> is 1988-07-31 (141561), 1601001 is 1; 2000060 less 2000001
      *> is 59. A leg fails if any form is refused or a value differs.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85F3602.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  D  PIC 9(7) VALUE 1604366.
       01  W  PIC 9(7).
       PROCEDURE DIVISION.
       P1.
           COMPUTE W = FUNCTION INTEGER-OF-DAY (D).
           DISPLAY W.
           COMPUTE W = FUNCTION INTEGER-OF-DAY (2000060).
           DISPLAY W.
           COMPUTE W = FUNCTION INTEGER-OF-DAY (1988000 + 213).
           DISPLAY W.
           COMPUTE W = FUNCTION INTEGER-OF-DAY (1601001).
           DISPLAY W.
           COMPUTE W = FUNCTION INTEGER-OF-DAY (2000060)
                     - FUNCTION INTEGER-OF-DAY (2000001).
           DISPLAY W.
           STOP RUN.
