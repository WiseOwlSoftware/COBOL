*>--- conformance-case
*> id: pb3682-same-as-group-with-usage-not-level-1
*> rules: [SR-13.18.49.3-7]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB3682 - ISO 13.18.49.3 SR7: "Data-name-1 shall reference an elementary item or a level 1 group item" (cite.py: OK  13.18.49.3 7)).
      *> H is a level 5 GROUP whose USAGE clause entry bind answered with a PICTURE profile before its subordinates were
      *> parsed; the clause is copied before 13.18.60.4 GR1 sheds that profile, so the test read H as elementary and accepted it.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P3682I.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 G.
          05 H USAGE BINARY-LONG.
             10 A.
             10 B.
       01 S SAME AS H.
       PROCEDURE DIVISION.
           STOP RUN.
