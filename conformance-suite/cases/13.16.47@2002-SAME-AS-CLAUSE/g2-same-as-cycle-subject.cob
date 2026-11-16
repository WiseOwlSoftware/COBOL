      *>--- conformance-case
      *> id: g2-same-as-cycle-subject
      *> rules: [2002-SR-13.16.47.2-3]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:13.16.47.2 SR3 - "The description of data-name-1, including
      *> its subordinate data items, shall not contain a SAME AS clause that
      *> references the subject of the entry". For the subject A1,
      *> data-name-1 is B1, whose own description is SAME AS A1: it
      *> references the subject, so the pair is refused (each entry
      *> violates SR3 with the other as its data-name-1). The ancestor leg
      *> is g2-same-as-cycle-ancestor; accepted SAME AS controls are
      *> g2-same-as-qualified and g2-same-as-77-elementary. Fails if the
      *> program is accepted.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2SAC05.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  A1 SAME AS B1.
       01  B1 SAME AS A1.
       PROCEDURE DIVISION.
           DISPLAY "RAN"
           STOP RUN.
