      *>--- conformance-case
      *> id: g2-external-name-not-unique
      *> rules: [2002-SR-13.16.20.2-2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:13.16.20.2 SR2 - "In the same source element, the
      *> externalized name of the subject of the entry that includes the
      *> EXTERNAL clause shall not be the same as the externalized name
      *> of any other entry that includes the EXTERNAL clause."
      *> G2-A is externalized as "G2-B" (its AS literal) and G2-B, with
      *> no AS phrase, is externalized by its own data-name
      *> (§2002:13.16.20.3 GR3: "If literal-1 is not specified, the
      *> externalized name of the file connector or record is the name
      *> specified"), so both level-1 working-storage records, each a
      *> legal EXTERNAL subject (§2002:13.16.20.2 SR1), carry the name
      *> G2-B in one source element: refused at G2-B. G2-C, with a
      *> distinct name, is legal. Why it can fail: the check compares
      *> data-names instead of externalized names, or ignores AS.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2EXT09.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  G2-A PIC X(4) IS EXTERNAL AS "G2-B".
       01  G2-B PIC X(4) IS EXTERNAL.
       01  G2-C PIC X(4) IS EXTERNAL AS "G2-OTHER".
       PROCEDURE DIVISION.
       P1.
           STOP RUN.
