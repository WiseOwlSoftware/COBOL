      *>--- conformance-case
      *> id: g2002-call-as-nested-scope
      *> rules: [2002-SR-14.8.4.2-13]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:14.8.4.2 SR13 sentence 2 - literal-1 shall name "a common
      *> program as specified in 8.4.5.2" or "a program that is directly
      *> contained in the calling program". G2CAL05B is neither for its
      *> sibling G2CAL05A: not common, and contained in G2CAL05, not in
      *> G2CAL05A (§2002:8.4.5.2 rule 1). The first CALL (G2CAL05 calling its
      *> directly contained G2CAL05A) is legal; the refusal is at the CALL
      *> inside G2CAL05A.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2CAL05.
       PROCEDURE DIVISION.
       MAIN.
           CALL "G2CAL05A" AS NESTED
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2CAL05A.
       PROCEDURE DIVISION.
       MAIN.
           CALL "G2CAL05B" AS NESTED
           GOBACK.
       END PROGRAM G2CAL05A.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2CAL05B.
       PROCEDURE DIVISION.
       MAIN.
           GOBACK.
       END PROGRAM G2CAL05B.
       END PROGRAM G2CAL05.
