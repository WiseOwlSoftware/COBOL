      *>--- conformance-case
      *> id: g2-external-strong-group
      *> rules: [2002-SR-13.16.20.2-4]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:13.16.20.2 SR4 - "The EXTERNAL clause shall not be
      *> specified for a data item of class object or pointer or for a
      *> strongly-typed group item." S-REC is described with a TYPE
      *> clause that references S-T, a type declaration specifying the
      *> STRONG phrase, so it is strongly typed (§2002:8.5.3: "A typed
      *> group item is strongly typed in any of the following cases").
      *> EXTERNAL written for S-REC is refused at its entry. COBOL-2002
      *> has no exception for an external strong type (that is a 2023
      *> addition). Why it can fail: the screen bans only object and
      *> pointer items, or accepts the item because it is a record.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2EXT11.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  S-T IS TYPEDEF STRONG.
           05  S-A PIC X(4).
       01  S-REC TYPE S-T IS EXTERNAL.
       PROCEDURE DIVISION.
       P1.
           STOP RUN.
