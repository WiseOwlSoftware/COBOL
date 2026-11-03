      *>--- conformance-case
      *> id: pb2104_import_interface_refused_at_85
      *> rules: [SR-12.3.8.3-9, SR-12.3.8.3-16]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB2104. The interface-specifier and the property-specifier
      *> are COBOL-2002 introductions (12.3.8.2), so the program that
      *> imports an interface and a property in pb2104_import_interface_
      *> and_property is refused below that edition: wiseowl/diagnostics/
      *> pb2104_import_interface_refused_at_85.json.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P2104I85.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           INTERFACE SP AS "P2104-Speaker"
           PROPERTY BAL.
       PROCEDURE DIVISION.
           STOP RUN.
       END PROGRAM P2104I85.
