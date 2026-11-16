      *>--- conformance-case
      *> id: g85_f08_01
      *> rules: [1985-SR-VI.5.8.3-10]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.5.8.3 SR10 - "A data description entry that contains
      *> format 2 of the OCCURS clause may only be followed, within that
      *> record description, by data description entries which are
      *> subordinate to it."  TBL (OCCURS 1 TO 5 DEPENDING ON CNT) is
      *> followed in REC by T-A, T-GRP, T-B and T-C, all subordinate to
      *> it, three levels deep, and REC ends there: the next entry (NXT)
      *> is a new record.  The form is granted, so the program must
      *> translate and run at 85.
      *> Expected (derived): each following entry is part of EACH
      *> occurrence of TBL, so an occurrence is T-A + T-B + T-C = 3
      *> characters; REC as a sending item with CNT outside the group
      *> uses CNT occurrences (§1985:VI.5.8.4 GR3a):
      *>   CNT=3: "H:" + "A1a" + "B2b" + "C3c" = "H:A1aB2bC3c", padded
      *>     to 20;
      *>   T-GRP (2) = "2b";  NXT = "NEXT" (a separate record);
      *>   CNT=2: "H:A1aB2b", padded to 20.
      *> A leg fails if the entries after TBL are refused, are laid out
      *> outside the occurrence (wrong REC image or T-GRP (2)), or if
      *> NXT is taken as part of REC.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85F0801.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  CNT                PIC 99 VALUE 3.
       01  REC.
           05  HDR            PIC X(2) VALUE "H:".
           05  TBL            OCCURS 1 TO 5 TIMES DEPENDING ON CNT.
               10  T-A        PIC X.
               10  T-GRP.
                   15  T-B    PIC 9.
                   15  T-C    PIC X.
       01  NXT                PIC X(4) VALUE "NEXT".
       01  OUT-X              PIC X(20).
       PROCEDURE DIVISION.
       P1.
           MOVE "A" TO T-A (1).
           MOVE 1 TO T-B (1).
           MOVE "a" TO T-C (1).
           MOVE "B" TO T-A (2).
           MOVE 2 TO T-B (2).
           MOVE "b" TO T-C (2).
           MOVE "C" TO T-A (3).
           MOVE 3 TO T-B (3).
           MOVE "c" TO T-C (3).
           MOVE REC TO OUT-X.
           DISPLAY "REC=[" OUT-X "]".
           DISPLAY "T-GRP2=[" T-GRP (2) "]".
           DISPLAY "NXT=[" NXT "]".
           MOVE 2 TO CNT.
           MOVE REC TO OUT-X.
           DISPLAY "REC2=[" OUT-X "]".
           STOP RUN.
