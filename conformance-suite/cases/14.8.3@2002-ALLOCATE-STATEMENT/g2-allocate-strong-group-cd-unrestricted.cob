      *>--- conformance-case
      *> id: g2-allocate-strong-group-cd-unrestricted
      *> rules: [2002-SR-14.8.3.2-5]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:14.8.3.2 SR5 - "If both data-name-1 and data-name-2 are
      *> specified and data-name-1 references a strongly-typed group
      *> item, the data item referenced by data-name-2 shall be
      *> restricted to the type of data-name-1."
      *> V is a based item of the strong type TPT; data-name-2 is CP,
      *> an UNRESTRICTED data-pointer that is a level-1 record under
      *> an output CD (§2002:A.1.2.2 SR9: "Record description entries
      *> subordinate to an output CD entry implicitly redefine the
      *> record area"), the 2002-only place data-name-2 can live.
      *> CP is not restricted to TPT, so the ALLOCATE (line 33) is
      *> nonconforming and shall be refused. The CP declaration itself
      *> is legal: a plain USAGE POINTER record under an output CD.
      *> Fails if the compiler accepts the ALLOCATE, or refuses the
      *> CD record instead of the statement.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2ALC05.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 TPT TYPEDEF STRONG.
          05 F PIC 9(4).
       01 V TYPE TPT BASED.
       COMMUNICATION SECTION.
       CD COUT FOR OUTPUT.
       01 CP USAGE POINTER.
       PROCEDURE DIVISION.
       MAIN.
           ALLOCATE V RETURNING CP.
           STOP RUN.
