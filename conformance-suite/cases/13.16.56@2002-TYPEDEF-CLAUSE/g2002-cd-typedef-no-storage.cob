      *>--- conformance-case
      *> id: g2002-cd-typedef-no-storage
      *> rules: [2002-GR-13.16.56.3-2]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:13.16.56.3 GR2 - "A type declaration has no storage
      *>   associated with it." The type declaration TC is written
      *>   first under an output CD entry (§2002:A.1.1.2: the entries
      *>   after a CD are ordinary data description entries).
      *> §2002:A.1.2.2 SR9 - "Record description entries subordinate to
      *>   an output CD entry implicitly redefine the record area
      *>   established by the output CD entry." and "only the first
      *>   redefinition may contain VALUE clauses". TC has no storage,
      *>   so it is no redefinition of the record area: CR1 is the
      *>   first, and its VALUE clauses (on C1, and on TA as assumed by
      *>   CX per §2002:13.16.56.3 GR3) are permitted. They take effect
      *>   at the initial state (§2002:13.16.61.3 GR4): lines C1=V1 and
      *>   TA=TV. CR2 is the second redefinition of the same area, so C2
      *>   holds the four positions of CR1: line C2=V1TV.
      *> The program fails to compile if TC is given storage as a CD
      *> record (CR1 would then be a second redefinition with VALUE
      *> clauses), and the C2 line fails if the entry of TC occupied the
      *> record area or CR2 did not overlay CR1.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2TDF02.
       DATA DIVISION.
       COMMUNICATION SECTION.
       CD  CO FOR OUTPUT
           TEXT LENGTH IS CO-LEN
           STATUS KEY IS CO-STAT.
       01  TC TYPEDEF.
           05  TA PIC X(2) VALUE "TV".
       01  CR1.
           05  C1 PIC X(2) VALUE "V1".
           05  CX TYPE TC.
       01  CR2.
           05  C2 PIC X(4).
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY "C1=" C1
           DISPLAY "TA=" TA OF CX
           DISPLAY "C2=" C2
           STOP RUN.
