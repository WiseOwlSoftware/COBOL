      *>--- conformance-case
      *> id: pb5073-global-data-name-duplicated
      *> rules: [SR-13.18.27.3-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5073 - 2023:8.4.2.1 (the same sentence in 1985:IV.4.3.8.1): "The same name shall not be used both as the name of
      *>   an item possessing the global attribute and as the name of any other data item described in the source element that
      *>   describes that global data item." (cite.py: OK). DUP-NAME is the name of the GLOBAL item and of an item of GRP, and no
      *>   statement references it, so no uniqueness-of-reference rule is involved. conformance:pb5073_global_name_unique_beside_
      *>   qualified_locals is the legal side.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5073N.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  DUP-NAME PIC X VALUE "A" GLOBAL.
       01  GRP.
           02  DUP-NAME PIC X.
       PROCEDURE DIVISION.
       P-MAIN.
           STOP RUN.
