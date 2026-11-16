      *>--- conformance-case
      *> id: g2002-hex-national-literal-misgrouped
      *> rules: [2002-SR-8.3.1.2.4.2-5]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:8.3.1.2.4.2 SR5 - "Each hex-character-sequence-1 shall
      *> consist of the number of hexadecimal digits that the
      *> implementor
      *> has specified as the number of hexadecimal digits that map to a
      *> national character": four here (docs/CONFORMANCE.md
      *> DOC-A.1-97),
      *> so NX"004" (three digits) violates SR5 and is refused.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2NAT05.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
           01 N1 PIC N(2).
       PROCEDURE DIVISION.
           MOVE NX"004" TO N1
           STOP RUN.
