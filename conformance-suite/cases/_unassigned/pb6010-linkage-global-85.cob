      *>--- conformance-case
      *> id: pb6010-linkage-global-85
      *> rules: []
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB6010 - the GLOBAL clause in the linkage section, a COBOL-2002 relaxation
      *> (Annex F.2 item 61), breaks §1985:X.4.6.3 SR1 at 85, and the edition gate cites it
      *> there (the construct row's rule1985) where it cited §2023:13.18.27.3 SR1.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB6010LK.
       DATA DIVISION.
       LINKAGE SECTION.
       01  L PIC X GLOBAL.
       PROCEDURE DIVISION USING L.
       MAIN-P.
           EXIT PROGRAM.
