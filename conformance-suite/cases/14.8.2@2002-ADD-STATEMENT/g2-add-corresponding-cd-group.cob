      *>--- conformance-case
      *> id: g2-add-corresponding-cd-group
      *> rules: [2002-GR-14.8.2.3-5]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:14.8.2.3 GR5 - "Data items within identifier-4 are
      *> selected to be added to selected data items within
      *> identifier-5 according to the rules specified in 14.6.5" and
      *> "The results are the same as if the user had referred to each
      *> pair of corresponding identifiers in separate ADD statements".
      *> At 2002 identifier-4 and identifier-5 may be groups in a record
      *> under a CD entry (§2002:A.1.1.2: the CD record area "may be
      *> implicitly redefined by user-specified record description
      *> entries"). CG is such a group; WG is in working-storage.
      *> §2002:14.6.5 rule 3: "In an ADD or SUBTRACT statement, both of
      *> the data items are numeric data items" - FC is numeric in CG
      *> and alphanumeric in WG, so FC is no pair; FG and FE have no
      *> namesake, so they are no pair either.
      *>  A1: CG (10 20 30 40) TO WG (1 2 XYZ 5) -> 011 022 XYZ 005
      *>  A2: WG TO CG (identifier-5 in the CD record) -> 021 042 030 040
      *>  A3: FA of CG = 990; ADD CORR WG TO CG ON SIZE ERROR: the FA pair
      *>      990 + 11 = 1001 overflows PIC 9(3) and FA stays 990, the FB
      *>      pair 42 + 22 = 64 is stored, as two separate ADD statements
      *>      would; the SIZE ERROR imperative runs after all the implied
      *>      statements (§2002:14.6.5) -> SE then A3=990 064
      *> Why each leg can fail: a CD-record group that is not resolved
      *> as a group, a pair chosen by name alone (FC), a non-pair added
      *> (FG, FE), or a size error that stops at the first pair.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2ADD01.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WG.
          05 FA PIC 9(3) VALUE 1.
          05 FB PIC 9(3) VALUE 2.
          05 FC PIC X(3) VALUE "XYZ".
          05 FE PIC 9(3) VALUE 5.
       COMMUNICATION SECTION.
       CD CDIN FOR INPUT
           SYMBOLIC QUEUE IS SQ.
       01 INREC.
          05 CG.
             10 FA PIC 9(3).
             10 FB PIC 9(3).
             10 FC PIC 9(3).
             10 FG PIC 9(3).
          05 FILLER PIC X(75).
       PROCEDURE DIVISION.
       MAIN.
           MOVE 10 TO FA OF CG.
           MOVE 20 TO FB OF CG.
           MOVE 30 TO FC OF CG.
           MOVE 40 TO FG.
           ADD CORRESPONDING CG TO WG.
           DISPLAY "A1=" FA OF WG " " FB OF WG " " FC OF WG " " FE.
           ADD CORRESPONDING WG TO CG.
           DISPLAY "A2=" FA OF CG " " FB OF CG " " FC OF CG " " FG.
           MOVE 990 TO FA OF CG.
           ADD CORR WG TO CG
               ON SIZE ERROR DISPLAY "SE"
           END-ADD.
           DISPLAY "A3=" FA OF CG " " FB OF CG.
           STOP RUN.
       END PROGRAM G2ADD01.
