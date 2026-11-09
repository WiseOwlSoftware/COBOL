      *>--- conformance-case
      *> id: pb4958-typedef-clause-with-an-item-named-typedef
      *> rules: [FMT-13.18.58.2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4958 - at COBOL-85, where 8.9 leaves TYPEDEF free, a data item named
      *> TYPEDEF is a user-defined word, and the TYPEDEF clause (§2023:13.18.58), a COBOL-2002
      *> construct, draws the same named edition refusal COBOL0900 as without the item.
      *> The reservation gate retypes the declared free word to a user-defined word; in T's
      *> entry that reading leaves TYPEDEF no clause of the data description entry, so the
      *> gate gives the token back its keyword (kb/Work PB4958). Fails if the entry draws
      *> COBOL1941 "'TYPEDEF' is not a clause".
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4958T.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 TYPEDEF PIC X.
       01 T TYPEDEF PIC X.
       PROCEDURE DIVISION.
       P1.
           STOP RUN.
