      *>--- conformance-case
      *> id: pb1673-null-undeclared-at-85
      *> rules: [SR-8.3.2.1-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB1673 - at COBOL-85 NULL is not a reserved word (the
      *> predefined NULL of ISO §8.4.3.10 and its §8.9 reservation are
      *> COBOL-2002 additions; reserved-words.json r85 false), so it is
      *> a user-defined word there (the positive twin is
      *> pb655_post85_words_user_words, which DECLARES a data item NULL).
      *> This program declares none, so every NULL below - a VALUE
      *> operand, a condition-name's VALUE operand, a DISPLAY operand and
      *> a relation operand - names nothing declared: the §8.4.2.1
      *> undefined reference ("a statement shall contain a reference that
      *> uniquely identifies that resource"), COBOL1639, never the 2002
      *> §8.4.3.10.3 SR1 context rule (COBOL2576) of a NULL the edition
      *> does not have. At 2002 and later the same source is the SR1
      *> refusal (_unassigned/w69y-pb939-value-null).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1673N.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A PIC X(4) VALUE NULL.
       01 C PIC X.
          88 C-NULL VALUE NULL.
       PROCEDURE DIVISION.
           DISPLAY "A=" NULL
           IF A = NULL DISPLAY "EQ" END-IF
           STOP RUN.
