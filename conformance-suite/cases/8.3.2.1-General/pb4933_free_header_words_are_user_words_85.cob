      *>--- conformance-case
      *> id: pb4933_free_header_words_are_user_words_85
      *> rules: [SR-8.3.2.1-1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> SCREEN, LOCAL-STORAGE, REPOSITORY AND OPTIONS ARE USER-DEFINED WORDS AT COBOL-85
      *> (kb/Work PB4933). The COBOL-85 reserved word list has none of them (ISO 8.9 reserves
      *> them from COBOL-2002), and only a reserved word is refused as a user-defined word
      *> (§2023:8.3.2.1 1): "Reserved words shall not be used as user-defined words or
      *> system-names"). Each heads a COBOL-2002 header whose keyword the reservation gate now
      *> keeps where a parse read it as one; this program writes no header, so every occurrence
      *> is the data item and is retyped as one. MOVE puts "A" in each one-character item and
      *> DISPLAY shows the four in order: AAAA. Fails if a reference keeps its keyword token
      *> (a syntax error) or a declaration is refused.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4933U.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 SCREEN PIC X.
       01 LOCAL-STORAGE PIC X.
       01 REPOSITORY PIC X.
       01 OPTIONS PIC X.
       PROCEDURE DIVISION.
       P1.
           MOVE "A" TO SCREEN LOCAL-STORAGE REPOSITORY OPTIONS
           DISPLAY SCREEN LOCAL-STORAGE REPOSITORY OPTIONS.
           STOP RUN.
