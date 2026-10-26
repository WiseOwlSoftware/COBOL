*>--- conformance-case
*> id: pb3355-call-target-alphanumeric-edited
*> rules: [SR-14.9.4.3-1]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB3355 - ISO 14.9.4.3 SR1: "Identifier-1 shall be defined as an alphanumeric, national, or
      *> program-pointer data item" (cite.py --check 14.9.4.3 "Identifier-1 shall be defined as an alphanumeric,
      *> national, or program-pointer data item" -> OK). 8.5.2.1: "Use of the name of a data class or data category in
      *> the rules of COBOL refers to the category unless class is specifically indicated" (cite.py --check -> OK), and
      *> SR1 names no class, so the rule admits the CATEGORY alphanumeric; Table 2 lists alphanumeric-edited as a
      *> category of its own (class alphanumeric, category alphanumeric-edited). W-EDITED is of category
      *> alphanumeric-edited and is refused (COBOL1681), exactly as ASSIGN USING (12.4.5.2 SR7: "Data-name-1 shall
      *> reference an alphanumeric data item") and the record keys refuse it (kb/Work PB850).
      *> Why this can fail: the screen asked the class lattice, where alphanumeric-edited answers class alphanumeric.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3355CALL2.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W-EDITED PIC XXBXXXXX VALUE "PB 355SB".
       PROCEDURE DIVISION.
       MAIN.
           CALL W-EDITED
           STOP RUN.
