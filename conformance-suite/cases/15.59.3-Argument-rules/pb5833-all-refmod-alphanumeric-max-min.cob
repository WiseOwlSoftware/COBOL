      *>--- conformance-case
      *> id: pb5833-all-refmod-alphanumeric-max-min
      *> rules: [AR-15.59.3-2, AR-15.63.3-2]
      *> accept: [2002, 2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5833. A reference-modified element under an ALL subscript is
      *> class and category alphanumeric: the reference-modifier "is applied to
      *> each of the implicitly specified elements of the table" (§1985:IV.4.3.8.4.4
      *> GR3a) and a reference-modified numeric item is "considered class and
      *> category alphanumeric" (§1985:IV.4.3.8.4.4 GR5). MAX and MIN take a list
      *> whose arguments are all alphanumeric, or a mix of alphabetic and
      *> alphanumeric ("mixing of arguments of alphabetic and alphanumeric classes
      *> is allowed", §2023:15.59.3 AR2; §2023:15.63.3 AR2).
      *> N-EL holds 11 72 33, so (2:1) reads 1 2 3 and (1:1) reads 1 7 3.
      *>   MAX (N-EL (ALL) (2:1) "1")   1 2 3 1   -> "3"
      *>   MIN (N-EL (ALL) (1:1) A-ITEM) 1 7 3 Q   -> "1" (digits collate below
      *>                                letters)
      *> Each leg fails if the ALL element is read as its numeric item: the list
      *> is then numeric against an alphanumeric literal (COBOL1627).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5833MAXMIN.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  TV.
           02  FILLER  PIC X(6) VALUE "117233".
       01  T REDEFINES TV.
           02  N-EL    PIC 99 OCCURS 3.
       01  A-ITEM  PIC A    VALUE "Q".
       01  X1      PIC X.
       PROCEDURE DIVISION.
       P1.
           MOVE FUNCTION MAX (N-EL (ALL) (2:1) "1") TO X1.
           DISPLAY "MAX [" X1 "]".
           MOVE FUNCTION MIN (N-EL (ALL) (1:1) A-ITEM) TO X1.
           DISPLAY "MIN [" X1 "]".
           STOP RUN.
