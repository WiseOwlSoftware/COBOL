      *>--- conformance-case
      *> id: g2-external-complete-redefines
      *> rules: [2002-GR-13.16.20.3-4]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:13.16.20.3 GR4 - "A source element that describes an
      *> external record may contain a data description entry including
      *> the REDEFINES clause that redefines the complete external
      *> record, and this complete redefinition need not occur
      *> identically in other source elements in the run unit."
      *> G2EXR1 and G2EXR2 describe the same external record G2-SHR
      *> with one externalized name, no VALUE and 8 bytes each (the
      *> first sentence of GR4). Only G2EXR1 redefines the complete
      *> record (ALT-R); G2EXR2 describes it as one PIC X(8).
      *> §2002:13.16.20.3 GR1: "the data contained in the record is
      *> external and may be accessed within the run unit" by each.
      *> Line 1: G2EXR1 sets SR-A "WXYZ", SR-B 1234; ALT-ALL redefines
      *> all 8 bytes, so "P1 ALT WXYZ1234". Line 2: G2EXR2 sees the same
      *> 8 bytes, "P2 SEES WXYZ1234", and moves "ABCD5678". Line 3:
      *> G2EXR1 sees SR-A "ABCD", SR-B 5678 and ALT-ALL "ABCD5678".
      *> Why it can fail: the REDEFINES in one element only is refused
      *> as a description mismatch, the redefinition is given its own
      *> storage, or the two elements do not share the record.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2EXR1.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  G2-SHR IS EXTERNAL.
           05  SR-A PIC X(4).
           05  SR-B PIC 9(4).
       01  ALT-R REDEFINES G2-SHR.
           05  ALT-ALL PIC X(8).
       PROCEDURE DIVISION.
       P1.
           MOVE "WXYZ" TO SR-A.
           MOVE 1234 TO SR-B.
           DISPLAY "P1 ALT " ALT-ALL.
           CALL "G2EXR2".
           DISPLAY "P1 AFTER " SR-A " " SR-B " " ALT-ALL.
           STOP RUN.
       END PROGRAM G2EXR1.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2EXR2.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  G2-SHR IS EXTERNAL.
           05  SR-X PIC X(8).
       PROCEDURE DIVISION.
       P1.
           DISPLAY "P2 SEES " SR-X.
           MOVE "ABCD5678" TO SR-X.
           GOBACK.
       END PROGRAM G2EXR2.
