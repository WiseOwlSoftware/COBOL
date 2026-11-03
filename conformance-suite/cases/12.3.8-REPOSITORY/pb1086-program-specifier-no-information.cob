      *>--- conformance-case
      *> id: pb1086-program-specifier-no-information
      *> rules: [SR-12.3.8.3-14]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB1086. ISO 1989:2023 12.3.8.3 syntax rule 14: "Literal-
      *> 3, if specified, or program-prototype-name-1, if literal-3 is
      *> not specified, shall be one of the following: the name of a
      *> program prototype specified in this compilation group; the name
      *> of a program definition specified previously in this
      *> compilation group; the name of a program for which information
      *> exists in the external repository" (cite.py --check 12.3.8.3 ->
      *> OK 14)). PB1086NOSUCH is none of them: the group defines
      *> neither a prototype nor a definition of it, and an external
      *> repository that holds no information for it (this compilation
      *> reads no other build of that name) cannot supply any. The
      *> specifier is therefore nonconforming wherever it is written,
      *> used by a CALL or not: refused with COBOL5962
      *> (wiseowl/diagnostics/pb1086-program-specifier-no-information.json).
      *> The positive twins are the program prototype of
      *> 8.3.2.2-User-defined-words/pb237_program_prototype (a prototype
      *> of this group) and wiseowl/cases/12.3.8-REPOSITORY/
      *> pb2103_prototype_group_split_across_units (information in the
      *> external repository).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1086SP.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           PROGRAM PB1086NOSUCH.
       PROCEDURE DIVISION.
       MAIN-PARA.
           DISPLAY "RAN".
           STOP RUN.
       END PROGRAM PB1086SP.
