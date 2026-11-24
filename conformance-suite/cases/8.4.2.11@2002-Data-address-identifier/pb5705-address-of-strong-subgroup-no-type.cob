      *>--- conformance-case
      *> id: pb5705-address-of-strong-subgroup-no-type
      *> rules: [2002-SR-8.4.2.11.2-4]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5705. §2002:8.4.2.11.2 SR4: "If identifier-1 references a
      *> strongly-typed group item, identifier-1 shall be defined with a TYPE
      *> clause referencing a type declaration described with the STRONG
      *> phrase." T-G is a group subordinate to the strong record S and has no
      *> TYPE clause of its own, so ADDRESS OF T-G OF S is refused (COBOL2785).
      *> Control: pb5705-address-of-strong-group-own-type.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5705B.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  T-REC IS TYPEDEF STRONG.
           05 T-X PIC X(2).
           05 T-G.
              10 T-A PIC X(4).
       01  S TYPE T-REC.
       PROCEDURE DIVISION.
           IF ADDRESS OF T-G OF S NOT = NULL DISPLAY "ADDR-TAKEN".
           STOP RUN.
