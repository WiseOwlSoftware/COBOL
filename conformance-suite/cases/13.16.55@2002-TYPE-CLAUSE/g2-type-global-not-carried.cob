      *>--- conformance-case
      *> id: g2-type-global-not-carried
      *> rules: [2002-GR-13.16.55.3-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:13.16.55.3 GR1 - the GLOBAL clause "specified for
      *> type-name-1" is excluded from the subject of a TYPE entry. T is
      *> GLOBAL, so the contained program may use the type-name (W TYPE
      *> T: "The GLOBAL clause applies to the scope of the type-name",
      *> §2002:13.16.56.3 GR3), but V TYPE T is not a global name, so
      *> DISPLAY V in the contained program references a name no entry
      *> of that program declares or makes available (§2002:13.16.25 "A
      *> global name is available to every program contained within the
      *> program that declares it"; V is not one). Refused at DISPLAY V.
      *> This refusal sits beside the accepted g2-type-excluded-clauses.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2TYX02.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  T TYPEDEF GLOBAL PIC X(4) VALUE "TTTT".
       01  V TYPE T.
       PROCEDURE DIVISION.
           CALL "G2TYX02I".
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2TYX02I.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  W TYPE T.
       PROCEDURE DIVISION.
           DISPLAY W.
           DISPLAY V.
           GOBACK.
       END PROGRAM G2TYX02I.
       END PROGRAM G2TYX02.
