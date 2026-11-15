      *>--- conformance-case
      *> id: g2-type-cd-group-hierarchy
      *> rules: [2002-GR-13.16.55.3-2]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:13.16.55.3 GR2 - "If type-name-1 describes a group item:
      *> a) the subject of the entry is a group whose subordinate
      *> elements have the same names, descriptions, and hierarchy as the
      *> subordinate elements of type-name-1, b) the level-numbers of
      *> items subordinate to that group are adjusted, if necessary, to
      *> preserve the hierarchy of type-name-1". In 2002 the subject may
      *> be a record under a CD (§2002:A.1.1 "The communication
      *> description entry is followed by none, one, or more record
      *> description entries"), a reach no 2023 case shows. CREC is a
      *> level-1 CD record of TYPE T; CR-SUB, at level 05 of the CD
      *> record CREC2, is of TYPE T too, so T's 05/10 hierarchy is
      *> re-levelled under it (GR2 b). An output CD has no fixed record
      *> length, and both records implicitly redefine its area
      *> (§2002:A.1.2.2 SR9); each is displayed right after its MOVE.
      *> DERIVATION (T = T-A X(2), T-B group of T-B1 X(3), T-B2 9(2)):
      *>  MOVE "ABCDE12" TO CREC (group move, left to right)
      *>    T-A=AB  T-B1=CDE  T-B2=12  T-B=CDE12
      *>    -> "A=AB B1=CDE B2=12 B=CDE12"
      *>  MOVE "XYZPQR34" TO CREC2: CR-X=X, CR-SUB = "YZPQR34"
      *>    -> "SUB-A=YZ SUB-B1=PQR SUB-B2=34"
      *>  each subject holds the 2+3+2 = 7 alphanumeric positions of
      *>  T (§2002:15.39.3 RV3); the integer 7 is MOVEd to unsigned
      *>  PIC 99 items (§2002:14.8.24.3 GR4b: decimal-point alignment,
      *>  zero fill; GR4b 2.: no sign), so each holds 07, and a DISPLAY
      *>  item is transferred as it is (§2002:14.8.10.3 GR4)
      *>    -> "LEN=07 LEN2=07"
      *> A leg fails if the CD record's TYPE is not expanded (the names
      *> would not resolve), if the subordinate subject loses the T-B
      *> hierarchy (T-B would not hold CDE12) or if the lengths differ.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2TYP02.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  T TYPEDEF.
           05  T-A PIC X(2).
           05  T-B.
               10  T-B1 PIC X(3).
               10  T-B2 PIC 9(2).
       01  WS-LEN PIC 99.
       01  WS-LEN2 PIC 99.
       COMMUNICATION SECTION.
       CD  CO FOR OUTPUT
           TEXT LENGTH IS CO-LEN
           STATUS KEY IS CO-STAT.
       01  CREC TYPE T.
       01  CREC2.
           05  CR-X PIC X.
           05  CR-SUB TYPE T.
       PROCEDURE DIVISION.
           MOVE "ABCDE12" TO CREC.
           DISPLAY "A=" T-A OF CREC " B1=" T-B1 OF CREC
                   " B2=" T-B2 OF CREC " B=" T-B OF CREC.
           MOVE "XYZPQR34" TO CREC2.
           DISPLAY "SUB-A=" T-A OF CR-SUB OF CREC2
                   " SUB-B1=" T-B1 OF CR-SUB
                   " SUB-B2=" T-B2 OF CR-SUB.
           MOVE FUNCTION LENGTH(CREC) TO WS-LEN.
           MOVE FUNCTION LENGTH(CR-SUB) TO WS-LEN2.
           DISPLAY "LEN=" WS-LEN " LEN2=" WS-LEN2.
           STOP RUN.
