      *>--- conformance-case
      *> id: pb2500_locale_constant_name
      *> rules: [SR-13.10.3-2, GR-13.10.4-1, SR-12.3.7.3-10,
      *>     GR-12.3.7.4-5, SR-8.3.2.1-4, DOC-A.1-181]
      *> accept: [2002, 2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB2500 lead B - a word that is BOTH a declared
      *> constant-name and an implementor system-name, written where the
      *> LOCALE clause admits either ({external-locale-name-1 |
      *> literal-4}), names the CONSTANT'S locale.
      *>   cite.py --check 13.10.3 "constant-name-1 may be used anywhere
      *>     that a format specifies a literal" -> OK 13.10.3 2)
      *>   cite.py --check 13.10.4 "as if literal-1" -> OK 13.10.4 1)
      *>   cite.py --check 8.3.2.1 "A given word may be used as a
      *>     system-name and as a user-defined word" -> OK 8.3.2.1 4)
      *>   cite.py --check 12.3.7.4 "external-locale-name-1"
      *>     -> OK 12.3.7.4 5) (the implementor specifies the allowable
      *>     external-locale-names and literal-4 values)
      *>   cite.py --check 12.3.7.3 "Literal-4 and literal-9 shall be
      *>     alphanumeric or national literals" -> OK 12.3.7.3 10)
      *> The standard does not say which reading such a word gets; the
      *> implementor's determination (docs/CONFORMANCE.md DOC-A.1-181,
      *> the same one DOC-A.1-71 makes for ASSIGN) is that the program's
      *> own declaration of the word wins.
      *> Each leg can fail:
      *>   LK - FR_FR is a constant standing for "ja-JP", so the date is
      *>     the ja-JP form 2026/08/19; read as the system-name fr_FR it
      *>     would be the fr-FR form of leg LF.
      *>   LF - the literal "fr-FR": 19/08/2026, the answer leg LK would
      *>     give if the constant were never consulted.
      *>   LW - ja_JP names no constant, so it is the system-name
      *>     external-locale-name-1: the ja-JP form 2026/08/19 (the
      *>     root form 08/19/2026 if the word were ignored).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2500LC.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           LOCALE LK IS FR_FR
           LOCALE LF IS "fr-FR"
           LOCALE LW IS ja_JP.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  FR_FR CONSTANT AS "ja-JP".
       01  W-D   PIC X(8) VALUE "20260819".
       PROCEDURE DIVISION.
       MAIN-P.
           DISPLAY "LK=[" FUNCTION LOCALE-DATE(W-D LK) "]".
           DISPLAY "LF=[" FUNCTION LOCALE-DATE(W-D LF) "]".
           DISPLAY "LW=[" FUNCTION LOCALE-DATE(W-D LW) "]".
           STOP RUN.
