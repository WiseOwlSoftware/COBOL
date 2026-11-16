      *>--- conformance-case
      *> id: g2-same-as-cd-occurs-subordinate
      *> rules: [2002-SR-13.16.47.2-5]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:13.16.47.2 SR5 - "The description of data-name-1 shall
      *> not contain an OCCURS clause. However, items subordinate to
      *> data-name-1 may contain OCCURS clauses." The granted form, with
      *> the subject in a record description entry under an output CD:
      *> §2002:A.1.1 "The communication description entry is followed by
      *> none, one, or more record description entries" and
      *> §2002:A.1.1.2 "A record description consists of a set of data
      *> description entries"; §2002:13.16.47.2 SR7 restricts only
      *> data-name-1 ("described in the file, working-storage,
      *> local-storage, or linkage section"), so the subject X may sit
      *> in CR. Data-name-1 is WT, a level 1 working-storage group with
      *> no OCCURS of its own; its subordinate WE has OCCURS 3.
      *> By §2002:13.16.47.3 GR1 and GR2 a), X is a group whose
      *> subordinate WE OCCURS 3 has the same description: MOVE "ABC"
      *> TO X fills WE OF X (1) to (3) with A, B, C, so elements 1 and 3
      *> are "AC", FUNCTION LENGTH(X) is 3, and the record CR with Z =
      *> "-" is "ABC-". Fails if the subordinate OCCURS is refused, or
      *> if the copied table loses its occurrences or its size.
      *> The refused form is g2-same-as-cd-occurs-target.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2SAC01.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  WT.
           05 WE PIC X OCCURS 3.
       01  L   PIC 9.
       COMMUNICATION SECTION.
       CD  CDOUT FOR OUTPUT
           TEXT LENGTH IS TL
           STATUS KEY IS SK.
       01  CR.
           05 X SAME AS WT.
           05 Z PIC X.
       PROCEDURE DIVISION.
       MAIN-P.
           MOVE "ABC" TO X
           MOVE "-" TO Z
           MOVE FUNCTION LENGTH(X) TO L
           DISPLAY "[" WE OF X (1) WE OF X (3) "] " L
           DISPLAY "[" CR "]"
           STOP RUN.
