      *>--- conformance-case
      *> id: pb4512-initialize-to-value-below-2002
      *> rules: [GR-14.9.20.4-6]
      *> source-format: fixed
      *> portable: unverified
      *>---
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4512NG.
      *> kb/Work PB4512 -- the VALUE phrase of INITIALIZE (ISO 14.9.20.4 GR6 a) 3.) is a COBOL-2002 addition: the
      *> COBOL-85 INITIALIZE has only the REPLACING phrase, so INITIALIZE ... ALL TO VALUE over an edited item is
      *> refused at 85 (COBOL0831).
      *>   cite.py: OK  §2023:14.9.20.4 6) a) 3.  (General rules)
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 E1 PIC XXBXX VALUE "AB CD".
       PROCEDURE DIVISION.
       MAIN-PARA.
           INITIALIZE E1 ALL TO VALUE
           STOP RUN.
