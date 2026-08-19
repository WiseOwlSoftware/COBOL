      *> reject-at: 2023
      *> The A.4.9 LOCALE keyword phrase of the otherwise-supported numeric functions — NUMVAL-C §15.68 and
      *> TEST-NUMVAL-C §15.94 — is documented non-support (COBOLNET1518) until kb/Work PB64 T6 lands PICTURE
      *> format 2 / the LOCALE-aware currency scan, while the SAME functions WITHOUT a LOCALE phrase remain fully
      *> supported (NUMVAL-C without LOCALE uses the compilation unit's currency). NUMVAL-C's LOCALE keyword is a
      *> spec Annex-A list omission disposed identically (PHASE-11-scout-notes.md spec:locale). The LOCALE phrase
      *> of LOWER-CASE §15.57 / UPPER-CASE §15.97 is LIVE since PB64 T5 (an undeclared locale-name there is
      *> COBOLNET1664 — negative/pb64t5-case-phrase-name-undeclared), so those lines left this fixture. LOCALE is
      *> not a LEXER TOKEN here, so the phrase parses as extra arguments and is detected BY NAME (it IS a reserved
      *> word from 2002 per §8.9 / reserved-words.json; not tokenizing it is a deliberate choice, because a token
      *> would break that by-name detection. Fix-queue PB25.)
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P11LOCKW.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 RN PIC 9(4)V99.
       01 RT PIC 9(2).
       PROCEDURE DIVISION.
       MAIN.
           COMPUTE RN = FUNCTION NUMVAL-C("12,34" LOCALE LOC1)
           COMPUTE RT = FUNCTION TEST-NUMVAL-C("12" LOCALE LOC1)
           STOP RUN.
