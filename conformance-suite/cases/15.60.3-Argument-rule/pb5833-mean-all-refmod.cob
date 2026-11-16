      *>--- conformance-case
      *> id: pb5833-mean-all-refmod
      *> rules: [AR-15.60.3-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5833. §2023:15.60.3 AR1: "Argument-1 shall be of class numeric." (§1989:A.2.24.3 AR1 at 85).
      *> N-EL (ALL) (1:1) is a reference-modified numeric element, class and
      *> category alphanumeric (§1985:IV.4.3.8.4.4 GR3a and GR5), so the list is
      *> refused (COBOL1627). It was accepted and computed from digit slices.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5833MEANALLREFMOD.
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
           COMPUTE R = FUNCTION MEAN (N-EL (ALL) (1:1)).
           STOP RUN.
