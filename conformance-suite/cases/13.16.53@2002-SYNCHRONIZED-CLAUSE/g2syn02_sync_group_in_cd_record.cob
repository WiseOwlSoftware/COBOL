      *>--- conformance-case
      *> id: g2syn02_sync_group_in_cd_record
      *> rules: [2002-SR-13.16.53.2-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:13.16.53.2 SR1 - "The SYNCHRONIZED clause may be specified
      *>   only for an elementary item". CG is a GROUP item (it has the
      *>   subordinate CN) of a record under an input CD (§2002:A.1.2.2
      *>   SR5: the record is exactly 87 positions, 1+2+84), so its
      *>   SYNCHRONIZED violates SR1 and the program is refused at the
      *>   CG entry. The accepted half (SYNCHRONIZED on elementary items
      *>   of a CD record) is g2syn01_sync_cd_ws_ls_file_layout.
      *> cite.py --check: OK §2002:13.16.53.2 SR1; §2002:A.1.2.2 SR5.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2SYN02.
       DATA DIVISION.
       COMMUNICATION SECTION.
       CD  CD-IN FOR INPUT.
       01  CD-REC.
           05  CA  PIC X.
           05  CG  SYNCHRONIZED.
               10  CN  PIC 9(4) COMP.
           05  CZ  PIC X(84).
       PROCEDURE DIVISION.
           DISPLAY CA.
           STOP RUN.
