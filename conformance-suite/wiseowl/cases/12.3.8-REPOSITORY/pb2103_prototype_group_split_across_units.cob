      *>--- conformance-case
      *> id: pb2103_prototype_group_split_across_units
      *> rules: [SR-13.7.3-2, GR-12.3.8.4-10]
      *> accept: [2002, 2014, 2023]
      *> files: [pb2103_split_def.cbl]
      *> build: ["pb2103_split_def.cbl > P2103SQ", "{self}"]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB1115, PB2103, PB4455 (DESIGN-external-repository 17.1,
      *> "the pb1115 shape split across two units"): the program prototype
      *> and the program definition are in different compilation groups, so
      *> the match 13.7.3 SR2 asks of their descriptions is made through
      *> the external repository. 13.7.3 SR2: "The description of the
      *> formal parameters and the returning item that appear in the
      *> linkage section of a function prototype or a program prototype
      *> shall match the description of the formal parameters and the
      *> returning item in the corresponding function definition or program
      *> definition" (cite.py --check 13.7.3 -> OK 2)). What a description
      *> is, for that match, is 8.5.3.1's: "The essential characteristics
      *> of a type, which is identified by its type-name, are the relative
      *> positions and lengths of the elementary items defined in the type
      *> declaration, and the ALIGNED, BLANK WHEN ZERO, DYNAMIC LENGTH,
      *> JUSTIFIED, PICTURE, SIGN, SYNCHRONIZED, and USAGE clauses
      *> specified for each of these elementary items" (cite.py --check
      *> 8.5.3.1 -> OK); intermediate grouping and the data-names are not
      *> among them.
      *>
      *> DERIVATION. The prototype P2103SQ declares 01 L-G (05 PIC 9(2),
      *> 05 PIC X(2)). The definition P2103SDEF AS "P2103SQ", compiled
      *> apart (the build step), declares 01 D-G whose intermediate group
      *> D-S holds the PIC 9(2) and whose PIC X(2) follows it: the same
      *> elementary layout under other names and another grouping. The
      *> descriptions match, so the compilation against the repository
      *> entry succeeds with no warning that the prototype differs from the
      *> published one (--repository-check, REPO-9; pinned by
      *> ExternalRepositoryResolutionTests), and, with
      *> EC-PROGRAM-ARG-MISMATCH checking on in both units
      *> (14.9.4.4 GR3 d)), the activation finds the argument conforming
      *> to the formal, so ON EXCEPTION does not execute. 12.3.8.4 GR10 b):
      *> the details are the prototype's and the program called is the one
      *> with its externalized name, P2103SQ, the library. The library
      *> displays its view of the group, A=07 B=XY, adds 5 to D-A and moves
      *> "ZZ" to D-B; BY REFERENCE shares the storage, so the caller sees
      *> W-A=12 W-B=ZZ.
       >>TURN EC-PROGRAM-ARG-MISMATCH CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P2103SQ IS PROTOTYPE.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L-G.
          05 L-A PIC 9(2).
          05 L-B PIC X(2).
       PROCEDURE DIVISION USING L-G.
       END PROGRAM P2103SQ.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P2103SM.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           PROGRAM P2103SQ.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W-G.
          05 W-A PIC 9(2) VALUE 7.
          05 W-B PIC X(2) VALUE "XY".
       PROCEDURE DIVISION.
       MAIN-PARA.
           CALL P2103SQ USING W-G
               ON EXCEPTION DISPLAY "EXCEPTION"
           END-CALL
           DISPLAY "W-A=" W-A " W-B=" W-B
           STOP RUN.
       END PROGRAM P2103SM.
