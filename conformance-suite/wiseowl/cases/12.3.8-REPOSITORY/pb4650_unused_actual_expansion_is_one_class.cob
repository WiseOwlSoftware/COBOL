      *>--- conformance-case
      *> id: pb4650_unused_actual_expansion_is_one_class
      *> rules: [GR-12.3.8.4-5, DOC-A.1-138, DOC-A.1-201]
      *> accept: [2002, 2014, 2023]
      *> files: [pb4650_unused_actual_lib.cbl, pb4650_unused_actual_callee.cbl]
      *> build: ["pb4650_unused_actual_lib.cbl > P4650LIB", "pb4650_unused_actual_callee.cbl > P4650CAL", "{self}"]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4650 (DESIGN-external-repository 10.5). 9.3.12: "Within a
      *> run unit, two classes with the same externalized object-class-name
      *> that are created by expanding the same parameterized class with the
      *> same actual parameters are the same class instance." This program and
      *> the program it calls (the build step P4650CAL) are two compilation
      *> groups; each expands COUNTER over BOX, and the formal ELEM is named by
      *> no member of COUNTER, so the code of the expansion holds no reference
      *> to BOX's assembly. There is still ONE class COUNTER-BOX, and its
      *> factory has ONE count: this program bumps it (N=001), the callee bumps
      *> the same count (N=002), this program bumps it again (N=003). Two
      *> classes would read N=001, N=001, N=002.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P4650ONE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BOX AS "P4650-Box"
           CLASS COUNTER AS "P4650-Counter"
           CLASS COUNTER-BOX EXPANDS COUNTER USING BOX
           PROGRAM P4650CAL.
       PROCEDURE DIVISION.
           INVOKE COUNTER-BOX "BUMP"
           CALL P4650CAL
           INVOKE COUNTER-BOX "BUMP"
           STOP RUN.
       END PROGRAM P4650ONE.
