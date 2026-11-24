      *>--- conformance-case
      *> id: pb5705-address-of-strong-group-own-type
      *> rules: [2002-SR-8.4.2.11.2-4]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5705. §2002:8.4.2.11.2 SR4: "If identifier-1 references a
      *> strongly-typed group item, identifier-1 shall be defined with a TYPE
      *> clause referencing a type declaration described with the STRONG
      *> phrase." S and its subgroup G each carry their own TYPE clause, so
      *> ADDRESS OF either is legal; the same program with the operand a group
      *> that has no TYPE clause of its own is refused
      *> (pb5705-address-of-strong-subgroup-no-type). Why it can fail: the
      *> new refusal also catches a subgroup that has its own TYPE clause.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5705A.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  T-IN IS TYPEDEF STRONG.
           05 T-A PIC X(4).
       01  T-REC IS TYPEDEF STRONG.
           05 T-X PIC X(2).
           05 T-G TYPE T-IN.
       01  S TYPE T-REC.
       PROCEDURE DIVISION.
           IF ADDRESS OF S NOT = NULL DISPLAY "S-SET".
           IF ADDRESS OF T-G OF S NOT = NULL DISPLAY "G-SET".
           STOP RUN.
