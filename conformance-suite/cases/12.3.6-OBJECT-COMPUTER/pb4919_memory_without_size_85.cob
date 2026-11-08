      *>--- conformance-case
      *> id: pb4919_memory_without_size_85
      *> rules: [FMT-12.3.6.2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4919 - the X3.23-1985 MEMORY SIZE clause with its
      *> optional word SIZE omitted. The printed general format
      *> (§1985:VI.4.4.2, page VI-11) underlines MEMORY and the unit
      *> words, NOT SIZE, so MEMORY 8 WORDS is the same clause as
      *> MEMORY SIZE 8 WORDS. The grammar demanded SIZE and refused
      *> this paragraph with COBOL1970 ("'MEMORY' is not a clause").
      *> The clause follows the computer-name XYZ, which 85 requires
      *> before any clause. Fails if SIZE is demanded again.
      *> Expected: the program compiles at 85 and displays its name.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P4919MS.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       OBJECT-COMPUTER. XYZ MEMORY 8 WORDS.
       PROCEDURE DIVISION.
       MAIN-PARA.
           DISPLAY "P4919MS".
           STOP RUN.
