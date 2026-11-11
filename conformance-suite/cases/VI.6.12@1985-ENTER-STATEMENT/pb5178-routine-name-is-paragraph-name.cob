      *>--- conformance-case
      *> id: pb5178-routine-name-is-paragraph-name
      *> rules: [1985-SR-VI.6.12.3-2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      * X3.23-1985 IV 4.2.2.1.1: "All user-defined words, except
      * segment-numbers and level-numbers, can belong to one and only
      * one of these disjoint sets", routine-names among them; VI
      * 6.12.3 SR2 lets a routine-name be referred to only in an ENTER
      * sentence. SUBX is both routine-name-1 and a paragraph-name;
      * it compiled clean (kb/Work PB5178). It is refused with
      * COBOL2692, the one-type-per-word rule.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5178P.
       PROCEDURE DIVISION.
       P1.
           ENTER FORTRAN SUBX.
           DISPLAY "END".
           STOP RUN.
       SUBX.
           STOP RUN.
