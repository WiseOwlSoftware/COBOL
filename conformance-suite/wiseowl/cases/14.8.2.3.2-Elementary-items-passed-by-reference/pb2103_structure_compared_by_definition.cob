      *>--- conformance-case
      *> id: pb2103_structure_compared_by_definition
      *> rules: [GR-14.8.2.3.2-L2.1]
      *> accept: [2014]
      *> files: [pb2103_dq_callee.cbl, pb2103_dr_callee_v1.cbl, pb2103_dr_callee_v2.cbl]
      *> build: ["pb2103_dq_callee.cbl > P2103DQ", "pb2103_dr_callee_v1.cbl > P2103DR", "{self}", "pb2103_dr_callee_v2.cbl > P2103DR"]
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
      *> JUSTIFIED, PICTURE, SIGN, and USAGE clauses". Each callee is
      *> compiled apart (the build steps), so each program-specifier's
      *> details come from the external repository (12.3.8.4 GR10 c)):
      *> the CALLs are checked against the records at compile time and,
      *> because each callee is compiled elsewhere, again at the
      *> activation by 14.9.4.4 GR3 d) (DESIGN-external-repository 4.2).
      *> P2103DQ names its SHORT PREFIXED structure DS-CALLEE where the
      *> caller's is DS-CALLER: one layout, so case 01 conforms at
      *> both checks and the callee's store comes back. P2103DR was
      *> SHORT PREFIXED when this program was compiled against it, and
      *> is rebuilt after it (the last step) naming DS-CALLER PREFIXED:
      *> another layout, so at the activation case 02 is not successful
      *> and ON EXCEPTION executes (the stale caller of 17.1).
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
