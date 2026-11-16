      *>--- conformance-case
      *> id: g2-value-sr1-cd-strong
      *> rules: [2002-SR-13.16.61.2-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:13.16.61.2 SR1 - "The subject of the entry shall not be
      *> a strongly-typed group item", reached in the communication
      *> section. CR, a level 1 record under an output CD, is described
      *> with a TYPE clause referencing ST, which specifies the STRONG
      *> phrase, so CR is a strongly-typed group item (§2002:8.5.3) and
      *> it carries a format 1 VALUE. The literal also fails
      *> §2002:13.16.61.2 SR11 (the category of a strongly-typed group is
      *> its type-name, §2002:8.5.2). The compiler also reports COBOL5991
      *> (its reading of §2002:13.16.55.2 SR4 with §2002:A.1.2.2 SR9);
      *> this case does not depend on that diagnostic and pins SR1 by
      *> requiring its own diagnostic, COBOL1703, at the VALUE entry, so
      *> it fails if the SR1 check does not reach CD records.
      *> The communication facility draws the obsolete warning COBOL0903.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2V01C.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  ST TYPEDEF STRONG.
           05 S1 PIC X(2).
       COMMUNICATION SECTION.
       CD  CDOUT FOR OUTPUT.
       01  CR TYPE ST VALUE "AB".
       PROCEDURE DIVISION.
       MAIN-P.
           DISPLAY "NOT REFUSED".
           STOP RUN.
