*>--- conformance-case
*> id: message-tag-refused-renames
*> rules: [SR-13.18.45.3-5, SR-13.18.45.3-8]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB4740 review R1-5 - 13.18.45.3 SR8: "None of the items within the range, including data-name-2 and
      *> data-name-3, if specified, shall be of class object, message-tag, or pointer, a strongly-typed group item, ..."
      *> (cite.py --check 13.18.45.3 OK, SR 8.) A message tag can sit only at level 1 or under a strongly-typed group
      *> (13.18.60.3 SR14), and SR5 bars a level-1 entry from RENAMES ("Neither data-name-2 nor data-name-3 shall refer to
      *> an entry that is described with level-number 1, 66, 77, or 88." cite.py --check 13.18.45.3 OK, SR 5) while SR8's
      *> own list bars the strong group's members. So every message tag is refused at a RENAMES, and this program draws the
      *> level-1 form (COBOL2740).
      *> Refused at 2023, where USAGE MESSAGE-TAG exists (8.9; Annex E.2 item 25); below it the entry draws the COBOL0900 introduction gate (pb916-message-tag-introduction).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. MTRENAMES.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 T USAGE MESSAGE-TAG.
       66 RN RENAMES T.
       PROCEDURE DIVISION.
       MAIN.
           STOP RUN.
       END PROGRAM MTRENAMES.
