      *>--- conformance-case
      *> id: pb2103_function_word_in_repository
      *> rules: [GR-12.3.8.4-11]
      *> accept: [2002, 2014, 2023]
      *> files: [pb2103_word_lib.cbl]
      *> build: ["pb2103_word_lib.cbl > P2103WG", "{self}"]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB2103, PB4455 (DESIGN-external-repository 8.3 determination
      *> D-R3 step 2, 17.1): the user-function-name of a function in the
      *> external repository reaches a later-compiled unit. 8.4.6.7: "A
      *> user-function-name may be referenced in the REPOSITORY paragraph
      *> of any source element that follows that function definition within
      *> the compilation group and, if the external repository is updated,
      *> in any subsequently-compiled source unit that specifies that
      *> user-function-name as a function-prototype-name in its REPOSITORY
      *> paragraph" (cite.py --check 8.4.6.7 -> OK).
      *>
      *> DERIVATION. The library function P2103WF AS "P2103WG" (user-
      *> function-name P2103WF, externalized P2103WG, adds 100) is compiled
      *> apart into the assembly P2103WG.dll, which the repository (the
      *> output directory, 9.3) publishes with the word P2103WF. This
      *> program's FUNCTION P2103WF has no AS phrase, so GR11's
      *> externalized name P2103WF names no function of the group or of the
      *> repository (NOTE 2); the specifier is legal all the same, because
      *> the word is the library function's user-function-name, so the
      *> details are the library function's and P2103WG is the function
      *> activated: 1 + 100 = 101, displayed in PIC 9(4) as 0101.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P2103WM.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           FUNCTION P2103WF.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-N PIC 9(4) VALUE 1.
       01 WS-R PIC 9(4).
       PROCEDURE DIVISION.
       MAIN.
           COMPUTE WS-R = FUNCTION P2103WF(WS-N).
           DISPLAY "W=" WS-R.
           STOP RUN.
       END PROGRAM P2103WM.
