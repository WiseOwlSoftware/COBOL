      *>--- conformance-case
      *> id: g2-value-sr1-f2-strong
      *> rules: [2002-SR-13.16.61.2-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:13.16.61.2 SR1 - "The subject of the entry shall not be
      *> a strongly-typed group item", reached through FORMAT 2 (table).
      *> E is described with a TYPE clause referencing ST, a type
      *> declaration specifying the STRONG phrase, so E is a strongly-
      *> typed group item (§2002:8.5.3, first bullet: "The item is
      *> described with a TYPE clause that references a type
      *> declaration specifying the STRONG phrase"). E carries a format 2
      *> VALUE: SR14 holds (E contains the OCCURS clause) and
      *> §2002:13.16.55.2 SR6 holds (E is subordinate to a type
      *> declaration that includes the STRONG phrase; that is why E sits
      *> inside the STRONG declaration OT). The literal also fails
      *> §2002:13.16.61.2 SR11, because the category of a strongly-typed
      *> group is its type-name (§2002:8.5.2) and no literal or
      *> figurative constant has that category (§2002:14.8.24.2 SR2).
      *> The case pins SR1 by requiring its own diagnostic, COBOL1703, at
      *> the VALUE entry (line 31), so it fails if format
      *> 2 escapes the SR1 check.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2V01T.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  ST TYPEDEF STRONG.
           05 S1 PIC X(2).
       01  OT TYPEDEF STRONG.
           05 E TYPE ST OCCURS 3 VALUES "AB" FROM (1).
       PROCEDURE DIVISION.
       MAIN-P.
           DISPLAY "NOT REFUSED".
           STOP RUN.
