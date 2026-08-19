      *> reject-at: 2002 2014 2023
      *> ISO 14.9.39.3 SR26: "Locale-name-1 shall be specified in the LOCALE clause of the SPECIAL-NAMES paragraph."
      *> FR is neither a declared locale-name nor a data item - the ONE undeclared-locale-name diagnostic, naming the
      *> citing site: COBOLNET1664 (kb/Work PB64 T1; DESIGN-locale-facility 7 rule a).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB64T1UND.
       PROCEDURE DIVISION.
           SET LOCALE LC_COLLATE TO FR.
           STOP RUN.
