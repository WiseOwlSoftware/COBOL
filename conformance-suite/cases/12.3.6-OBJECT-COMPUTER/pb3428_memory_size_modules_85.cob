      *>--- conformance-case
      *> id: pb3428_memory_size_modules_85
      *> rules: [FMT-12.3.6.2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB3428 - the positive control of
      *> pb3428-memory-size-unit-word: the X3.23-1985 MEMORY SIZE clause
      *> takes MODULES as its unit word, recognized by the word's TEXT
      *> through the same predicate that refuses FOO (pb3133's control
      *> spells WORDS). DERIVED, NOT QUOTED (VERSION_CHANGE_REFERENCE row
      *> 7.7). The program compiles at 85 and displays its name.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P3428MM.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       OBJECT-COMPUTER. XYZ MEMORY SIZE 12 MODULES.
       PROCEDURE DIVISION.
       MAIN-PARA.
           DISPLAY "P3428MM".
           STOP RUN.
