      *>--- conformance-case
      *> id: pb5195-alter-one-paragraph-section-85
      *> rules: [1985-SR-VI.6.7.3-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5195 - §1985:VI.6.7.3 SR1: "Procedure-name-1 is the
      *> name of a paragraph that contains a single sentence consisting
      *> of a GO TO statement without the DEPENDING phrase." (cite.py:
      *> OK). S7 is a SECTION whose one paragraph P7 is such a sentence;
      *> S7 is still the name of a section, not of a paragraph, so the
      *> ALTER is refused (COBOL1757). It used to compile and alter P7's
      *> GO TO through the section-name ("P3 ALTERED").
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5195AS.
       PROCEDURE DIVISION.
       S0 SECTION.
       P0.
           ALTER S7 TO PROCEED TO P3.
           GO TO S7.
       S7 SECTION.
       P7.
           GO TO P2.
       S8 SECTION.
       P2.
           DISPLAY "P2 NOT ALTERED".
           STOP RUN.
       P3.
           DISPLAY "P3 ALTERED".
           STOP RUN.
