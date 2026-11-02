*>--- conformance-case
*> id: pb2845-flag-option-word-spelled-with-dotted-capital-i
*> rules: []
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB2845, PB4240 - the option words of >>FLAG-14 are
      *> keywords of the directive (7.3.15 format), matched on the basic
      *> letters alone (ISO 8.1.3.2 3) a)): the Annex C fold of 4) b)
      *> governs user-defined words only (owner decision 2026-10-31), so
      *> "İ-O-STATUS-07", written with U+0130 LATIN CAPITAL LETTER I WITH
      *> DOT ABOVE, is NOT the option I-O-STATUS-07 and the directive is
      *> malformed. The sibling pb2845-flag-option-word-folded-by-host-
      *> case-mapping refuses the host's case mapping (U+017F) the same way.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2845P.
       PROCEDURE DIVISION.
       >>FLAG-14 İ-O-STATUS-07 ON
       MAIN-P.
           DISPLAY "ACCEPTED".
           STOP RUN.
