      *>--- conformance-case
      *> id: g2-type-subject-value-overrides
      *> rules: [2002-GR-13.16.55.3-3]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:13.16.55.3 GR3 - "If a VALUE clause is specified in the
      *> data description of the subject of the entry, that VALUE clause
      *> applies and any VALUE clause specified in the description of
      *> type-name-1 is ignored for this entry."
      *> Every leg pairs a subject that writes its own VALUE with a twin
      *> that does not, so the twin shows type-name-1's VALUE is copied
      *> (§2002:13.16.55.3 GR1) and the subject shows it is ignored:
      *>  - working-storage, elementary (X / Y of TYPE T VALUE "AAA") and
      *>    group (Z / W of TYPE GV VALUE "QQ12"), at initial state
      *>    (§2002:13.16.61.3 GR4 c);
      *>  - a record under an output CD (CR), at initial state; CR is the
      *>    CD's only record, so it may carry a VALUE (§2002:A.1.2.2 SR9,
      *>    "only the first redefinition may contain VALUE clauses");
      *>  - INITIALIZE ... ALL TO VALUE, whose receiving items are those
      *>    whose entry has a VALUE clause (§2002:14.8.19.3 GR5c), in
      *>    working-storage, the CD record, and the linkage section,
      *>    where VALUE takes effect only by INITIALIZE (§2002:13.16.61.3
      *>    GR3).
      *> DERIVATION
      *>  initial: X=BBB (own) Y=AAA (type) Z=RR34 (own) W=QQ12 (type)
      *>  CR=CCC (own)        -> "X=BBB Y=AAA Z=RR34 W=QQ12 CR=CCC"
      *>  MOVE "ZZZ" then INITIALIZE X Y CR ALL TO VALUE
      *>                      -> "X=BBB Y=AAA CR=CCC"
      *>  the contained G2TYV01L INITIALIZEs its linkage items L1 (own
      *>  VALUE "LLL") and L2 (type VALUE) over the caller's "ZZZ"
      *>                      -> "L1=LLL L2=AAA"
      *> A leg fails if the subject takes the type's VALUE (X, Z, CR or
      *> L1 would show AAA/QQ12) or if the type's VALUE is dropped for
      *> the twins (Y, W, L2).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2TYV01.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  T TYPEDEF GLOBAL PIC X(3) VALUE "AAA".
       01  GV TYPEDEF VALUE "QQ12".
           05  H1 PIC X(2).
           05  H2 PIC 9(2).
       01  X TYPE T VALUE "BBB".
       01  Y TYPE T.
       01  Z TYPE GV VALUE "RR34".
       01  W TYPE GV.
       01  ARG1 PIC X(3) VALUE "ZZZ".
       01  ARG2 PIC X(3) VALUE "ZZZ".
       COMMUNICATION SECTION.
       CD  CDOUT FOR OUTPUT
           TEXT LENGTH IS CO-LEN
           STATUS KEY IS CO-STAT.
       01  CR TYPE T VALUE "CCC".
       PROCEDURE DIVISION.
           DISPLAY "X=" X " Y=" Y " Z=" Z " W=" W " CR=" CR.
           MOVE "ZZZ" TO X Y CR.
           INITIALIZE X Y CR ALL TO VALUE.
           DISPLAY "X=" X " Y=" Y " CR=" CR.
           CALL "G2TYV01L" USING ARG1 ARG2.
           DISPLAY "L1=" ARG1 " L2=" ARG2.
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2TYV01L.
       DATA DIVISION.
       LINKAGE SECTION.
       01  L1 TYPE T VALUE "LLL".
       01  L2 TYPE T.
       PROCEDURE DIVISION USING L1 L2.
           INITIALIZE L1 L2 ALL TO VALUE.
           GOBACK.
       END PROGRAM G2TYV01L.
       END PROGRAM G2TYV01.
