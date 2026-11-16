      *>--- conformance-case
      *> id: pb5833-max-all-refmod-numeric-mix
      *> rules: [AR-15.59.3-2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5833. §2023:15.59.3 AR2: "All arguments shall be of the same class with the exception that mixing of arguments of alphabetic and alphanumeric classes is allowed." The numeric N-ITEM and the alphanumeric element are two classes (written where an alphanumeric function may stand: it compiled and failed at run time).
      *> N-EL (ALL) (1:1) is a reference-modified numeric element, class and
      *> category alphanumeric (§1985:IV.4.3.8.4.4 GR3a and GR5), so the list is
      *> refused (COBOL1627). It was accepted and computed from digit slices.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5833MAXALLREFMODNUMERICMIX.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  TV.
           02  FILLER  PIC X(6) VALUE "117233".
       01  T REDEFINES TV.
           02  N-EL    PIC 99 OCCURS 3.
       01  N-ITEM  PIC 99   VALUE 5.
       01  R       PIC 9(4)V9(4).
       01  X2      PIC X(2).
       PROCEDURE DIVISION.
       P1.
           MOVE FUNCTION MAX (N-EL (ALL) (1:1) N-ITEM) TO X2.
           STOP RUN.
