      *>--- conformance-case
      *> id: g2-same-as-cycle-ancestor
      *> rules: [2002-SR-13.16.47.2-3]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:13.16.47.2 SR3 - "The description of data-name-1, including
      *> its subordinate data items, shall not contain a SAME AS clause that
      *> references ... any group item to which this entry is subordinate."
      *> The subject A is subordinate to G; its data-name-1 B has a
      *> subordinate C described SAME AS G, so the entry for A violates
      *> SR3; symmetrically, for the subject C (subordinate to B) data-name-1
      *> G has the subordinate A described SAME AS B. The program is refused
      *> (the compiler reports it at C).
      *> Every other SAME AS rule holds: B and G are level 1 groups in
      *> working storage (SR7), neither SAME AS entry is followed by a
      *> subordinate (SR2), and G and B carry no USAGE, SIGN or GROUP-USAGE
      *> (SR9). Fails if the program is accepted.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2SAC06.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  G.
           05 A SAME AS B.
       01  B.
           05 C SAME AS G.
       PROCEDURE DIVISION.
           DISPLAY "RAN"
           STOP RUN.
