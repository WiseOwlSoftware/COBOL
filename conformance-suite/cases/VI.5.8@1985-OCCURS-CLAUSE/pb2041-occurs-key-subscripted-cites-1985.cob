      *>--- conformance-case
      *> id: pb2041-occurs-key-subscripted-cites-1985
      *> rules: [1985-SR-VI.5.8.3-4]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB2041. §1985:VI.5.8.3 SR4: "Data-name-2 must be
      *> specified without the subscripting normally required", and
      *> §1985:III.2 p69: a data-name in a general format "represents a
      *> word which must not be reference-modified, subscripted, or
      *> qualified unless specifically permitted by the rules of the
      *> format". K(1) below is refused (COBOL2024) naming both; the
      *> message used to close with "the operand shall not be subject
      *> to any OCCURS clauses", false for a key, which lies under the
      *> table's OCCURS by definition.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P2041K.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  T.
           05  E OCCURS 3 ASCENDING KEY K(1) INDEXED BY I.
               10  K PIC X.
       PROCEDURE DIVISION.
       MAIN-P.
           DISPLAY "P2041K ACCEPTED".
           STOP RUN.