*>--- conformance-case
*> id: pb2103_function_prototype_ignores_entry
*> rules: [GR-12.3.8.4-11]
*> accept: [2002, 2014, 2023]
*> files: [pb2103_fp_lib.cbl]
*> build: ["pb2103_fp_lib.cbl > P2103FP", "{self}"]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB2103 (DESIGN-external-repository 8.3). 12.3.8.4 GR11 b): "if the externalized name of the function
      *> prototype is the externalized name of a function prototype definition specified in the same compilation group,
      *> the details are taken from that function prototype definition and the details in the external repository are
      *> ignored. The function that will be activated is the one with the same externalized name as the externalized
      *> name of the function prototype" (cite.py --check 12.3.8.4 "the details in the external repository are ignored"
      *> -> OK 11) b)).
      *>
      *> DERIVATION. The REPOSITORY paragraph names FUNCTION P2103FP, and this group holds the function prototype
      *> definition P2103FP, so GR11 b) takes the details from it and ignores the external repository. The library
      *> P2103FP.dll is built beside it with --repository-update off (wiseowl/options/<id>.pb2103_fp_lib.cbl.json), so
      *> its entry carries no signature: were the repository consulted for P2103FP, the compilation would be refused
      *> for that entry (REPO-8). It is not, so the compilation succeeds, and the function activated at run time is the
      *> one externalized P2103FP, the library's: 21 * 2 = 42, displayed in its PIC 9(4) result as 0042.
      IDENTIFICATION DIVISION.
      FUNCTION-ID. P2103FP IS PROTOTYPE.
      DATA DIVISION.
      LINKAGE SECTION.
      01 L-X PIC 9(4).
      01 L-R PIC 9(4).
      PROCEDURE DIVISION USING L-X RETURNING L-R.
      END FUNCTION P2103FP.

      IDENTIFICATION DIVISION.
      PROGRAM-ID. P2103FPM.
      ENVIRONMENT DIVISION.
      CONFIGURATION SECTION.
      REPOSITORY.
          FUNCTION P2103FP.
      DATA DIVISION.
      WORKING-STORAGE SECTION.
      01 N PIC 9(4) VALUE 21.
      PROCEDURE DIVISION.
          DISPLAY FUNCTION P2103FP(N)
          STOP RUN.
      END PROGRAM P2103FPM.
