*>--- conformance-case
*> id: pb3755-content-all-multichar-numeric-edited-2002
*> rules: [GR-14.8.2.3.3-L2.2, SR-8.3.3.6.3-3]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB3755 - the activation arm asks the MOVE rules, the WHOLE of them, and §8.3.3.6.3 SR3 is asked of every
      *> association a figurative constant takes (moved to it, compared with it, paired with it): "If the length of
      *> literal-1 is greater than one, it is not permitted to be associated with a numeric or numeric-edited item". So
      *> ALL "57" BY CONTENT into the PIC ZZ9 formal of a contained program activated with the NESTED phrase is the
      *> association MOVE ALL "57" TO <a numeric-edited item> is, and is COBOL0902 from 2002 (row
      *> all-literal-multichar-numeric-removed-2002, VCR row 7.13) - the edge the written MOVE has. Before this it
      *> compiled clean at every edition.
      *>   cite.py --check 8.3.3.6.3 "If the length of literal-1 is greater than one, it is not permitted to be
      *>     associated with a numeric or numeric-edited item" -> OK 8.3.3.6.3 3)
       IDENTIFICATION DIVISION.
       PROGRAM-ID. N3755MC.
       PROCEDURE DIVISION.
           CALL "N3755MA" AS NESTED USING BY CONTENT ALL "57"
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. N3755MA.
       DATA DIVISION.
       LINKAGE SECTION.
       01 E PIC ZZ9.
       PROCEDURE DIVISION USING E.
           DISPLAY "ENTERED"
           GOBACK.
       END PROGRAM N3755MA.
       END PROGRAM N3755MC.
