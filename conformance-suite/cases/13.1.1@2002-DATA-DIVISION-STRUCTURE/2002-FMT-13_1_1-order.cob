      *>--- conformance-case
      *> id: 2002-FMT-13_1_1-order
      *> rules: [2002-FMT-13.1.1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:13.1.1 - a refusal beside the accepted case
      *> 2002-FMT-13_1_1: the general format places the COMMUNICATION
      *> SECTION after the LINKAGE SECTION, so a LINKAGE SECTION header
      *> written after the COMMUNICATION SECTION (line 9 of the program)
      *> is outside the format. A compiler that accepts it fails.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2DDS2.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W-X PIC X(4) VALUE "WSOK".
       COMMUNICATION SECTION.
       CD CIN FOR INPUT
           STATUS KEY IS CST.
       LINKAGE SECTION.
       01 K-X PIC X(4).
       PROCEDURE DIVISION.
       P1.
           DISPLAY W-X.
           STOP RUN.
