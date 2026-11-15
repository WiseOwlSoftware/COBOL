      *>--- conformance-case
      *> id: g2-search-varying-cd-noninteger
      *> rules: [2002-SR-14.8.34.2-3]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:14.8.34.2 SR3 - "Identifier-2 shall reference a data
      *> item whose usage is index or a data item that is an integer.
      *> Identifier-2 shall not be subscripted by the first or only
      *> index-name specified in the INDEXED phrase in the OCCURS
      *> clause specified in the data description entry for
      *> identifier-1." Operands in a record under an output CD.
      *> Leg: identifier-2 CN PIC 9V9 in the CD record is not an integer: refused.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2SRN04.
       DATA DIVISION.
       COMMUNICATION SECTION.
       CD COUT FOR OUTPUT.
       01 CREC.
          05 CN PIC 9V9.
          05 CTAB OCCURS 3 TIMES INDEXED BY CI.
             10 CV PIC X.
             10 CK PIC 9.
       PROCEDURE DIVISION.
       MAIN.
           SET CI TO 1
           SEARCH CTAB VARYING CN
             WHEN CV(CI) = "B" DISPLAY "FOUND"
           END-SEARCH
           STOP RUN.
