      *>--- conformance-case
      *> id: pb2103_program_of_85_through_prototype
      *> rules: [GR-12.3.8.4-10]
      *> accept: [2002, 2014, 2023]
      *> files: [pb2103_legacy85.cbl]
      *> build: ["pb2103_legacy85.cbl > P2103L85", "{self}"]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB2103, PB4455 (DESIGN-external-repository 17.1, "an 85
      *> program called through a 2023 prototype"): the program called was
      *> compiled as COBOL-85 (wiseowl/options/<id>.pb2103_legacy85.cbl.json
      *> names the step's own edition, 85), and the program that calls it
      *> was compiled at the edition the test runs at, through a program
      *> prototype, a COBOL-2002 introduction (11.10.2 Format 2).
      *> 12.3.8.4 GR10 c): "the details are taken from the external
      *> repository for the program with the same name as the externalized
      *> name of the program prototype. That program is the one that will
      *> be called" (cite.py --check 12.3.8.4 -> OK 10) c)).
      *>
      *> DERIVATION. No program definition and no other prototype of the
      *> group is externalized P2103L85, so GR10 a) and b) do not apply and
      *> c) takes the program from the repository: the library compiled at
      *> 85. The prototype's USING formal PIC 9(4) matches the library's
      *> (13.7.3 SR2). The CALL passes W-N = 41 BY REFERENCE; the library
      *> displays it, adds 1 and returns by EXIT PROGRAM (GOBACK is not a
      *> COBOL-85 statement), so the caller sees W-N = 42.
      *> A repository entry carries no edition gate on its caller: the
      *> prototype is 2002 syntax and is refused at 85 before any REPO-*
      *> diagnostic could arise (the refusal pb2103_prototype_refused_at_85).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P2103L85 IS PROTOTYPE.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L-N PIC 9(4).
       PROCEDURE DIVISION USING L-N.
       END PROGRAM P2103L85.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P2103L8M.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           PROGRAM P2103L85.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W-N PIC 9(4) VALUE 41.
       PROCEDURE DIVISION.
       MAIN-PARA.
           CALL P2103L85 USING W-N
           DISPLAY "W-N=" W-N
           STOP RUN.
       END PROGRAM P2103L8M.
