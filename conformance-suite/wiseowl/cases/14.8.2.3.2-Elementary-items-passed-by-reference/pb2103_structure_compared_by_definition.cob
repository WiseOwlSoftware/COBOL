      *>--- conformance-case
      *> id: pb2103_structure_compared_by_definition
      *> rules: [GR-14.8.2.3.2-L2.1]
      *> accept: [2014]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB2103 (docs/CONFORMANCE.md D-DL3; DESIGN-external-
      *> repository 6.4): two DYNAMIC LENGTH clauses name the same
      *> structure when the structures have the same DEFINITION, never
      *> by the dynamic-length-structure-name, which is a word of its
      *> own source element. 14.8.2.3.2 rule 2: "the definition of the
      *> formal parameter and the definition of the argument shall
      *> have the same ALIGN, BLANK WHEN ZERO, DYNAMIC LENGTH,
      *> JUSTIFIED, PICTURE, SIGN, and USAGE clauses", asked at run
      *> time by 14.9.4.4 GR3 d) because each CALL names a program
      *> specified later (12.3.8.4 GR10 c)). P2103DQ names its SHORT
      *> PREFIXED structure DS-CALLEE where the caller's is DS-CALLER:
      *> one layout, so case 01 conforms and the callee's store comes
      *> back. P2103DR names its structure DS-CALLER, as the caller
      *> does, but PREFIXED: another layout, so case 02 is not
      *> successful and ON EXCEPTION executes.
       >>TURN EC-PROGRAM-ARG-MISMATCH CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P2103DL.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           DYNAMIC LENGTH STRUCTURE DS-CALLER IS SHORT PREFIXED.
       REPOSITORY.
           PROGRAM P2103DQ
           PROGRAM P2103DR.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 D PIC X DYNAMIC LENGTH DS-CALLER.
       PROCEDURE DIVISION.
           MOVE "HELLO" TO D
           CALL P2103DQ USING BY REFERENCE D
               ON EXCEPTION DISPLAY "01 SAME LAYOUT EXC"
               NOT ON EXCEPTION DISPLAY "01 SAME LAYOUT OK " D
           END-CALL
           CALL P2103DR USING BY REFERENCE D
               ON EXCEPTION DISPLAY "02 OTHER LAYOUT EXC"
               NOT ON EXCEPTION DISPLAY "02 OTHER LAYOUT OK " D
           END-CALL
           STOP RUN.
       END PROGRAM P2103DL.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P2103DQ.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           DYNAMIC LENGTH STRUCTURE DS-CALLEE IS SHORT PREFIXED.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L PIC X DYNAMIC LENGTH DS-CALLEE.
       PROCEDURE DIVISION USING L.
           DISPLAY "   P2103DQ GOT " L
           MOVE "WORLD" TO L
           GOBACK.
       END PROGRAM P2103DQ.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P2103DR.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           DYNAMIC LENGTH STRUCTURE DS-CALLER IS PREFIXED.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L PIC X DYNAMIC LENGTH DS-CALLER.
       PROCEDURE DIVISION USING L.
           DISPLAY "   P2103DR GOT " L
           GOBACK.
       END PROGRAM P2103DR.
