      *>--- conformance-case
      *> id: pb5150-global-subordinate-name-reused-by-local-record
      *> rules: [GR-13.18.27.4-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5150 - a data-name subordinate to a GLOBAL record is a global name (§2023:13.18.27.4 GR1: "All data-names
      *>   subordinate to a global name are global names", cite.py: OK), so §2023:8.4.2.1 ("The same name shall not be used
      *>   both as the name of an item possessing the global attribute and as the name of any other data item described in the
      *>   source element", cite.py: OK) refuses CODE-X of the local record LOC beside CODE-X of the GLOBAL record GLB.
      *>   Nothing references CODE-X. conformance:pb5150_global_subordinate_name_unique_beside_other_names is the legal side.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5150N.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  GLB GLOBAL.
           02  CODE-X PIC X VALUE "G".
       01  LOC.
           02  CODE-X PIC X VALUE "L".
       PROCEDURE DIVISION.
       P-MAIN.
           STOP RUN.
