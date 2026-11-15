      *>--- conformance-case
      *> id: g2rep02-all-intrinsic-2002-name-as-user-word
      *> rules: [2002-SR-12.2.7.2-13]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:12.2.7.2 SR13 - a 2002 intrinsic-function-name is refused
      *> as a user-defined word under FUNCTION ALL INTRINSIC.
      *>   cite.py --check "§2002:12.2.7.2 SR13" "none of the names of the
      *>   intrinsic functions may be specified as a user-defined word
      *>   within the scope of this REPOSITORY paragraph" -> OK
      *> SQRT is on the §2002:8.11 list; declaring it as a data-name in
      *> the scope of FUNCTION ALL INTRINSIC is the violation. The same
      *> program without the REPOSITORY paragraph is legal (SQRT is not a
      *> reserved word), so the only defect is this rule's. Expected: a
      *> diagnostic at the SQRT data description entry.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2REP02.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           FUNCTION ALL INTRINSIC.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  SQRT            PIC 9 VALUE 7.
       PROCEDURE DIVISION.
       MAIN-P.
           DISPLAY SQRT.
           STOP RUN.
