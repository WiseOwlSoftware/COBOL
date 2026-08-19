      *> reject-at: 2002 2014 2023
      *> ISO 12.3.7.3 SR24: "locale-name-2 shall be a locale-name defined by the LOCALE clause" - the ALPHABET clause's
      *> named IS LOCALE form referencing a name no LOCALE clause declares: the same COBOLNET1664 every reference site
      *> draws, citing its own rule (kb/Work PB64 T1). The program otherwise binds (the alphabet falls back to the
      *> current-locale form) so the diagnostic is the only thing between this source and a compiled program.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB64T1AUND.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           LOCALE ES IS "es-ES"
           ALPHABET SWE IS LOCALE SV.
       PROCEDURE DIVISION.
           STOP RUN.
