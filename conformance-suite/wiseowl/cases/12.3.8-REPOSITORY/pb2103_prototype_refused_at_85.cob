      *>--- conformance-case
      *> id: pb2103_prototype_refused_at_85
      *> rules: [GR-12.3.8.4-10]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB2103, PB4455. The program prototype (11.10.2 Format 2) and
      *> the REPOSITORY paragraph's program-specifier (12.3.8.2) are COBOL-
      *> 2002 introductions, so the caller of pb2103_program_of_85_through_
      *> prototype is refused at 85, with COBOL0900, before the compilation
      *> consults the external repository (no REPO-* diagnostic precedes
      *> it; DESIGN-external-repository 17.1): wiseowl/diagnostics/
      *> pb2103_prototype_refused_at_85.json.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P2103R85 IS PROTOTYPE.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L-N PIC 9(4).
       PROCEDURE DIVISION USING L-N.
       END PROGRAM P2103R85.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P2103R8M.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           PROGRAM P2103R85.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W-N PIC 9(4) VALUE 41.
       PROCEDURE DIVISION.
       MAIN-PARA.
           CALL P2103R85 USING W-N
           STOP RUN.
       END PROGRAM P2103R8M.
