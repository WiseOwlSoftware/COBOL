      *> reject-at: 2023
      *> The A.4.9 LOCALE keyword phrase of the otherwise-supported case/numeric functions — LOWER-CASE
      *> §15.57, UPPER-CASE §15.97, NUMVAL-C §15.68, TEST-NUMVAL-C §15.94 — is documented non-support
      *> (COBOLNET1518), while the SAME functions WITHOUT a LOCALE phrase remain fully supported (§15.57.4
      *> rule 4 implementor correspondence; NUMVAL-C without LOCALE). NUMVAL-C's LOCALE keyword is a spec
      *> Annex-A list omission disposed identically (PHASE-11-scout-notes.md spec:locale). LOCALE is not a
      *> reserved word, so the phrase parses as extra arguments and is detected by name.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P11LOCKW.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 X  PIC X(3) VALUE "AbC".
       01 R  PIC X(3).
       01 RN PIC 9(4)V99.
       01 RT PIC 9(2).
       PROCEDURE DIVISION.
       MAIN.
           MOVE FUNCTION LOWER-CASE(X LOCALE LOC1) TO R
           MOVE FUNCTION UPPER-CASE(X LOCALE LOC1) TO R
           COMPUTE RN = FUNCTION NUMVAL-C("12,34" LOCALE LOC1)
           COMPUTE RT = FUNCTION TEST-NUMVAL-C("12" LOCALE LOC1)
           STOP RUN.
