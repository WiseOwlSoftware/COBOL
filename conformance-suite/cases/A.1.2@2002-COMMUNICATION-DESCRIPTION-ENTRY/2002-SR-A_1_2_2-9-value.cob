      *>--- conformance-case
      *> id: 2002-SR-A_1_2_2-9-value
      *> rules: [2002-SR-A.1.2.2-9]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:A.1.2.2 SR9 - a refusal beside the accepted case
      *> 2002-SR-A_1_2_2-9: "only the first redefinition may contain
      *> VALUE clauses", so the VALUE clause of O2-ALL in OREC2, the
      *> second record under the output CD (line 10 of the program),
      *> is outside the rule. A compiler that accepts it fails.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2CDS9V.
       DATA DIVISION.
       COMMUNICATION SECTION.
       CD COUT FOR OUTPUT.
       01 OREC1.
          02 O1-DC PIC 9(4) VALUE 1.
          02 FILLER PIC X(19).
       01 OREC2.
          02 O2-ALL PIC X(23) VALUE SPACES.
       PROCEDURE DIVISION.
       P1.
           STOP RUN.
