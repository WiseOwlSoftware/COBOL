      *>--- conformance-case
      *> id: g2-renames-cd-thru-subordinate
      *> rules: [2002-SR-13.16.43.2-10]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:13.16.43.2 SR10 - "The end of the storage area described by
      *> data-name-3 shall follow the end of the storage area described by
      *> data-name-2." G THRU B, where B is subordinate to G and ends with
      *> it (the NOTE), in a record under a CD: refused.
      *> cite.py --check of the quoted rule: OK. Refused at --std 2002.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2RENNB.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  W PIC X.
       COMMUNICATION SECTION.
       CD  CDOUT FOR OUTPUT
           DESTINATION COUNT IS DC
           TEXT LENGTH IS TL
           STATUS KEY IS SK
           ERROR KEY IS EK
           SYMBOLIC DESTINATION IS SD1.
       01  CDREC.
           05 G.
              10 A PIC XX.
              10 B PIC XX.
           05 C PIC XX.
       66  AB RENAMES G THRU B.
       PROCEDURE DIVISION.
       MAIN-PARA.
           DISPLAY "SHOULD NOT COMPILE"
           STOP RUN.
