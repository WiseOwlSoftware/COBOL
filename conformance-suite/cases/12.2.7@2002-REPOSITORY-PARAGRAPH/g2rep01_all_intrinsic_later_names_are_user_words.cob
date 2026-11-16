      *>--- conformance-case
      *> id: g2rep01_all_intrinsic_later_names_are_user_words
      *> rules: [2002-SR-12.2.7.2-13]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:12.2.7.2 SR13 - under FUNCTION ALL INTRINSIC only "the
      *> names of the intrinsic functions" are barred as user words.
      *>   cite.py --check "§2002:12.2.7.2 SR13" "none of the names of the
      *>   intrinsic functions may be specified as a user-defined word
      *>   within the scope of this REPOSITORY paragraph" -> OK
      *>   cite.py --check "§2002:12.2.7.3 GR14" "it is as if each of the
      *>   intrinsic-function-names defined in 8.11, Intrinsic function
      *>   names, were specified" -> OK
      *> "The intrinsic functions" are the 2002 list, §2002:8.11 ("The
      *> following is the list of intrinsic function names" -> cite.py
      *> OK). TRIM and FORMATTED-DATE (added in 2014) and CONCAT (added
      *> in 2023) are not on it and are not 2002 reserved words, so in
      *> this program, whose REPOSITORY paragraph specifies FUNCTION ALL
      *> INTRINSIC, they are ordinary data-names. The refusal leg (a
      *> 2002 name, SQRT, as a data-name) is
      *> g2rep02-all-intrinsic-2002-name-as-user-word.
      *> DERIVATION OF THE EXPECTED OUTPUT:
      *>   NAMES - TRIM=7, FORMATTED-DATE=8, CONCAT=9, each PIC 9
      *>     => NAMES=789
      *>   UC - §2002:12.2.7 the paragraph "allows declaration of
      *>     intrinsic-function-names that may be used without specifying
      *>     the word FUNCTION" (cite.py OK); with ALL, UPPER-CASE (on
      *>     the 2002 list) is so declared, so UPPER-CASE("abc") is the
      *>     function: "ABC" => UC=ABC. This leg shows the ALL specifier
      *>     is in force, so NAMES is not admitted by ignoring it.
      *> A leg fails if a post-2002 name is refused (applying a later
      *> edition's list at 2002) or if ALL INTRINSIC is not applied.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2REP01.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           FUNCTION ALL INTRINSIC.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  TRIM            PIC 9 VALUE 7.
       01  FORMATTED-DATE  PIC 9 VALUE 8.
       01  CONCAT          PIC 9 VALUE 9.
       01  W               PIC X(3).
       PROCEDURE DIVISION.
       MAIN-P.
           DISPLAY "NAMES=" TRIM FORMATTED-DATE CONCAT.
           MOVE UPPER-CASE("abc") TO W.
           DISPLAY "UC=" W.
           STOP RUN.
