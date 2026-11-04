      *>--- conformance-case
      *> id: pb4488_cobol_words_directing_statements_precede_the_directive
      *> rules: [GR-7.3.10.4-6, GR-7.3.10.4-2, GR-7.3.10.4-4, GR-7.3.10.4-5]
      *> accept: [2023]
      *> files: [PB4488RS.cpy, PB4488SH.cpy]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO 1989:2023 7.3.10.4 GR6 (kb/Work PB4488): "6) A COBOL-WORDS
      *> directive does not affect any Compiler directing statements or
      *> Compiler directives."
      *>   cite.py: OK  §7.3.10.4 6)  (General rules)
      *> The order of 7.2.1 says the same: "Step 1: Library text identified
      *> in COPY statements is incorporated" and "Step 3: The
      *> conditionally-processed compilation group is read and the
      *> replacing actions of REPLACE statements are applied in order."
      *> come first, and "Step 4: The results of Step 3 are read, and the
      *> actions of the COBOL-WORDS directive are applied in order."
      *> comes last.
      *>   cite.py: OK  §7.2.1   (General)  (each of the three)
      *> REPLACE matching: "Pseudo-text-1 matches the source text if, and
      *> only if, the ordered sequence of text-words that forms
      *> pseudo-text-1 is equal, character for character, to the ordered
      *> sequence of source text-words."
      *>   cite.py: OK  §7.2.4.4 8) b)  (General rules)
      *> The directives: SUBSTITUTE "COPY" BY "CPY" (GR4 takes COPY from
      *> the syntax of the compilation stage), RESERVE "PB4488RS" (GR5: a
      *> text-name the program may not use as a user-defined word),
      *> EQUATE "DISPLAY" WITH "SHOW" (GR2), and EQUATE of the INSPECT
      *> phrase words REPLACING and BY, which the COPY and REPLACE
      *> statements below spell with the reserved words themselves.
      *>
      *> DERIVATION of each expected line, in execution order.
      *>   COPY PB4488RS.  -> Step 1 incorporates the library text before
      *>                      the directive acts, so neither the
      *>                      SUBSTITUTE of COPY nor the RESERVE of its
      *>                      text-name reaches the statement (GR6). It
      *>                      declares RS-ITEM, VALUE "R" -> "R".
      *>   COPY PB4488SH.  -> the library text SHOW "LIBRARY". is
      *>                      incorporated (Step 1) and then read by
      *>                      Step 4, which makes SHOW a DISPLAY -> "LIBRARY".
      *>   SHOW "A".       -> REPLACE (Step 3) compares the text-words as
      *>                      written: SHOW is not DISPLAY, so the REPLACE
      *>                      does not match it; Step 4 then makes SHOW a
      *>                      DISPLAY -> "A".
      *>   DISPLAY "A".    -> the REPLACE statement matches DISPLAY "A" and
      *>                      writes DISPLAY "Z" -> "Z".
      *>   REPLACE ==YY== BY ==SHOW==  turns YY "REPLACED". into
      *>                      SHOW "REPLACED". and Step 4 reads that
      *>                      result -> "REPLACED".
      *>   INSPECT W RPLG ALL "A" BYX "C" -> GR2 makes RPLG REPLACING and
      *>                      BYX BY in the INSPECT statement, which the
      *>                      directive does reach -> W is "AAB", so "CCB".
      *> Legs that can fail: the COPY statement read through the map (the
      *> library text not incorporated, or a refusal at the SUBSTITUTEd
      *> COPY or the RESERVEd text-name); the REPLACE statement matching
      *> the synonym; Step 4 not reaching library text or replaced text;
      *> the directive not reaching the INSPECT phrase words.
000100 >>COBOL-WORDS SUBSTITUTE "COPY" BY "CPY"
000200 >>COBOL-WORDS RESERVE "PB4488RS"
000300 >>COBOL-WORDS EQUATE "DISPLAY" WITH "SHOW"
000400 >>COBOL-WORDS EQUATE "REPLACING" WITH "RPLG"
000500 >>COBOL-WORDS EQUATE "BY" WITH "BYX"
000600 IDENTIFICATION DIVISION.
000700 PROGRAM-ID. PB4488A.
000800 DATA DIVISION.
000900 WORKING-STORAGE SECTION.
001000 01 W PIC X(3) VALUE "AAB".
001100 COPY PB4488RS.
001200 PROCEDURE DIVISION.
001300 MAIN-PARA.
001400     DISPLAY RS-ITEM.
001500     COPY PB4488SH.
001600     REPLACE ==DISPLAY "A"== BY ==DISPLAY "Z"==.
001700     SHOW "A".
001800     DISPLAY "A".
001900     REPLACE OFF.
002000     REPLACE ==YY== BY ==SHOW==.
002100     YY "REPLACED".
002200     REPLACE OFF.
002300     INSPECT W RPLG ALL "A" BYX "C".
002400     SHOW W.
002500     STOP RUN.
