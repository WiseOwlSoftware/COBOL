      *>--- conformance-case
      *> id: pb2103_repo13_across_repository
      *> rules: [GR-12.3.8.4-11]
      *> accept: [2002, 2014, 2023]
      *> files: [pb2103_r13_fx.cbl, pb2103_r13_fg.cbl]
      *> build: ["pb2103_r13_fx.cbl > P2103RF", "pb2103_r13_fg.cbl > P2103RG", "{self}"]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB2103, PB4455 (DESIGN-external-repository 8.3 determination
      *> D-R3, 17.1): the externalized name selects the function even when
      *> the function that only the WORD names is also in the external
      *> repository. 12.3.8.4 GR11 c): "the details are taken from the
      *> external repository for the function with the same name as the
      *> externalized name of the function prototype. That function is the
      *> one that will be activated" (cite.py --check 12.3.8.4 -> OK 11)
      *> b), the paragraph that holds c)). NOTE 2: a specifier with no AS
      *> phrase has the function-prototype-name itself as its externalized
      *> name.
      *>
      *> DERIVATION. Two libraries are compiled apart, each into the
      *> directory every later step searches. P2103RFX AS "P2103RF"
      *> (user-function-name P2103RFX, externalized P2103RF, adds 200) is
      *> the assembly P2103RF.dll. P2103RF AS "P2103RG" (user-function-name
      *> P2103RF, externalized P2103RG, adds 100) is P2103RG.dll. 8.4.6.7: a
      *> user-function-name "may be referenced in the REPOSITORY paragraph
      *> of ... any subsequently-compiled source unit that specifies that
      *> user-function-name as a function-prototype-name", so the word
      *> P2103RF reaches the second library too. FUNCTION P2103RF has no AS
      *> phrase: its externalized name is P2103RF, GR11 c) takes the
      *> function of that name from the repository, P2103RFX: 1 + 200 =
      *> 201, and the standard does not rank the other clause (the word
      *> reaching P2103RG), so the externalized name wins as in
      *> GnuCOBOL 3.2.0, with warning COBOL2969 (REPO-13; pinned by
      *> ExternalRepositoryResolutionTests). FUNCTION P2103RH AS "P2103RG"
      *> has the externalized name P2103RG: the function P2103RF of the
      *> second library, 1 + 100 = 101.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P2103RM.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           FUNCTION P2103RF
           FUNCTION P2103RH AS "P2103RG".
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-N PIC 9(4) VALUE 1.
       01 WS-R PIC 9(4).
       PROCEDURE DIVISION.
       MAIN.
           COMPUTE WS-R = FUNCTION P2103RF(WS-N).
           DISPLAY "F=" WS-R.
           COMPUTE WS-R = FUNCTION P2103RH(WS-N).
           DISPLAY "H=" WS-R.
           STOP RUN.
       END PROGRAM P2103RM.
