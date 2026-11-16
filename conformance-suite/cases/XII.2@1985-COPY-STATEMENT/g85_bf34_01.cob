      *>--- conformance-case
      *> id: g85_bf34_01
      *> rules: [1985-GR-XII.2.4-1, 1985-GR-XII.2.4-2]
      *> accept: [85]
      *> files: [G85BF34A.cpy, G85BF34B.cpy, G85BF34C.cpy]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XII.2.4 GR1 - all COPY statements are processed before
      *> the resultant source program is: a MOVE statement and a data
      *> description entry are each completed only by library text, so
      *> they compile only if the program is compiled after copying.
      *> §1985:XII.2.4 GR2 - the library text named by text-name-1
      *> replaces the entire COPY statement from COPY through its
      *> period, inclusive: the COPY's period does not survive.
      *> Library texts: G85BF34A = "ABC"; G85BF34B = VALUE "XYZ";
      *> G85BF34C = DISPLAY "IN-IF-1" (no period in any of them).
      *> EXPECTED OUTPUT, DERIVED (why each leg can fail):
      *>   B=ABC     MOVE COPY G85BF34A. TO WS-B becomes
      *>             MOVE "ABC" TO WS-B (GR2: period replaced; GR1:
      *>             the statement is compiled after copying). A
      *>             surviving period or per-unit compile refuses it.
      *>   C=XYZ     the VALUE clause of WS-C comes from G85BF34B and
      *>             the entry's own period follows the COPY (GR1/GR2).
      *>             A wrong library (text-name-1 not honoured) or a
      *>             dropped clause prints other than XYZ.
      *>   AFTER-IF  WS-F is 0: the IF's sentence runs through
      *>             DISPLAY "IN-IF-2". because the COPY's period was
      *>             replaced (GR2). A surviving period would end the
      *>             sentence after the copied DISPLAY "IN-IF-1" and
      *>             print IN-IF-2 unconditionally.
      * NO OTHER LINE MAY BE PRINTED.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85BF3401.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  WS-B  PIC X(3) VALUE SPACES.
       01  WS-C  PIC X(3)
           COPY G85BF34B.
           .
       01  WS-F  PIC 9 VALUE 0.
       PROCEDURE DIVISION.
       P1.
           MOVE
           COPY G85BF34A.
               TO WS-B.
           DISPLAY "B=" WS-B.
           DISPLAY "C=" WS-C.
           IF WS-F = 1
               COPY G85BF34C.
               DISPLAY "IN-IF-2".
           DISPLAY "AFTER-IF".
           STOP RUN.
