      *>--- conformance-case
      *> id: pb2104_import_class_refused_at_85
      *> rules: [SR-12.3.8.3-6, GR-12.3.8.4-6]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB2104. The class-specifier and the object reference to
      *> a class another group defines are COBOL-2002 introductions
      *> (12.3.8.2 class-specifier; 13.18.60 USAGE OBJECT REFERENCE),
      *> so the program that imports a class in pb2104_import_class_under_
      *> another_word is refused below that edition: wiseowl/
      *> diagnostics/pb2104_import_class_refused_at_85.json.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P2104R85.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS ACCT AS "P2104-Ledger".
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A OBJECT REFERENCE ACCT.
       PROCEDURE DIVISION.
           STOP RUN.
       END PROGRAM P2104R85.
