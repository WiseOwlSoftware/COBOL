      *>--- conformance-case
      *> id: g2-typedef-cd-record-assumes-clauses
      *> rules: [2002-GR-13.16.56.3-3]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:13.16.56.3 GR3 - "The GLOBAL clause applies to the scope
      *> of the type-name. All other data description clauses and
      *> subordinate data descriptions are assumed by data defined using
      *> the type-name."
      *> In 2002 data defined by a type-name may be a record under a CD
      *> (§2002:A.1.1 "The communication description entry is followed
      *> by none, one, or more record description entries"). CR is the
      *> only record of an output CD and is of TYPE GT, a GLOBAL group
      *> type declaration whose subordinates carry USAGE PACKED-DECIMAL,
      *> JUSTIFIED RIGHT and VALUE:
      *>  - subordinate descriptions assumed: G1, G2, G3 OF CR resolve;
      *>  - VALUE assumed: G3 OF CR is "VV" at initial state;
      *>  - JUSTIFIED assumed: "AB" MOVEd to the X(4) G2 is aligned at
      *>    the rightmost position, space filled (§2002:13.16.30.3 GR2)
      *>    -> "[  AB]";
      *>  - USAGE assumed: CR's length equals that of HW, the same three
      *>    entries written out by hand (a lost PACKED-DECIMAL would make
      *>    CR longer than HW);
      *>  - GLOBAL applies to the scope of the type-name: the contained
      *>    G2TDF06N declares NW TYPE GT, and NW assumes the same clauses
      *>    -> NG1 "0056", NG2 "[   C]", NG3 "VV".
      *> DERIVATION
      *>  "G3=VV"
      *>  MOVE 1234 TO G1, "AB" TO G2      -> "G1=1234 G2=[  AB]"
      *>  LEN(CR) = LEN(HW)                -> "CR LEN SAME AS HAND-CODED"
      *>  MOVE 56 TO an unsigned 9(4), "C" to a JUSTIFIED X(4)
      *>                                   -> "NG1=0056 NG2=[   C] NG3=VV"
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2TDF06.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  GT TYPEDEF GLOBAL.
           05  G1 PIC 9(4) USAGE PACKED-DECIMAL.
           05  G2 PIC X(4) JUSTIFIED RIGHT.
           05  G3 PIC X(2) VALUE "VV".
       01  HW.
           05  H1 PIC 9(4) USAGE PACKED-DECIMAL.
           05  H2 PIC X(4) JUSTIFIED RIGHT.
           05  H3 PIC X(2) VALUE "VV".
       01  LEN-CR PIC 99.
       01  LEN-HW PIC 99.
       COMMUNICATION SECTION.
       CD  CDOUT FOR OUTPUT
           TEXT LENGTH IS CO-LEN
           STATUS KEY IS CO-STAT.
       01  CR TYPE GT.
       PROCEDURE DIVISION.
           DISPLAY "G3=" G3 OF CR.
           MOVE 1234 TO G1 OF CR.
           MOVE "AB" TO G2 OF CR.
           DISPLAY "G1=" G1 OF CR " G2=[" G2 OF CR "]".
           MOVE FUNCTION LENGTH(CR) TO LEN-CR.
           MOVE FUNCTION LENGTH(HW) TO LEN-HW.
           IF LEN-CR = LEN-HW
               DISPLAY "CR LEN SAME AS HAND-CODED"
           ELSE
               DISPLAY "CR LEN " LEN-CR " HW LEN " LEN-HW
           END-IF.
           CALL "G2TDF06N".
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2TDF06N.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  NW TYPE GT.
       PROCEDURE DIVISION.
           MOVE 56 TO G1 OF NW.
           MOVE "C" TO G2 OF NW.
           DISPLAY "NG1=" G1 OF NW " NG2=[" G2 OF NW "] NG3=" G3 OF NW.
           GOBACK.
       END PROGRAM G2TDF06N.
       END PROGRAM G2TDF06.
