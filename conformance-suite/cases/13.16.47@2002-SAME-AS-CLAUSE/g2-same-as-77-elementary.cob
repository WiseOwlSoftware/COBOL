      *>--- conformance-case
      *> id: g2-same-as-77-elementary
      *> rules: [2002-SR-13.16.47.2-8]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:13.16.47.2 SR8 - "If the subject of the entry is a level 77
      *> item, data-name-1 shall reference an elementary item." E1 is an
      *> elementary PIC X(3) item, so 77 E2 SAME AS E1 is legal and, by
      *> §2002:13.16.47.3 GR1, E2 is as though PIC X(3) were coded: it holds
      *> "ABC" and FUNCTION LENGTH(E2) is 3 (§2002:15.39.3 RV3). The control
      *> for g2-same-as-77-group (77 SAME AS a group, refused); it fails if
      *> the legal elementary target is refused or the copy loses its size.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2SA771.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       77  E1 PIC X(3) VALUE "ABC".
       77  E2 SAME AS E1.
       77  N1 PIC 9.
       PROCEDURE DIVISION.
           MOVE E1 TO E2
           MOVE FUNCTION LENGTH(E2) TO N1
           DISPLAY "[" E2 "] " N1.
           STOP RUN.
