      *>--- conformance-case
      *> id: g2002-cd-typedef-strong-elementary
      *> rules: [2002-SR-13.16.56.2-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:13.16.56.2 SR1 - "If the subject of the entry is an
      *> elementary item, the STRONG phrase shall not be specified."
      *> At 2002 the record description entries after a CD entry are
      *> ordinary data description entries (§2002:A.1.1.2) and TYPEDEF
      *> is admitted in any level-1 entry (§2002:13.14.2 SR14), so the
      *> rule governs a type declaration written under a CD: T2 is an
      *> elementary type declaration with STRONG and is refused. The
      *> accepted form (a TYPEDEF under a CD without STRONG) runs in
      *> g2002-cd-typedef-member-refs.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2TDF03.
       DATA DIVISION.
       COMMUNICATION SECTION.
       CD  CO FOR OUTPUT
           TEXT LENGTH IS CO-LEN
           STATUS KEY IS CO-STAT.
       01  T2 TYPEDEF STRONG PIC X(2).
       01  CR PIC X(2).
       PROCEDURE DIVISION.
           MOVE "AB" TO CR.
           STOP RUN.
