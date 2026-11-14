- **D-CASE-MAPPING — Case mappings (UPPER-CASE / LOWER-CASE, §2023:15.97.4 r4 / §2023:15.57.4)**: absent a locale, the case correspondence is
  **implementor-defined** (§2023:15.97.4 r4). WiseOwl COBOL uses the .NET invariant Unicode case tables. Because the mapping
  is delegated to the implementor, the enumerated 2023 annex changes (E.2 item 14 deletions of DOTLESS I
  `(0131,0069)` and GREEK FINAL SIGMA `(03C2,03C3)`; E.3.3 item 6 additions) are not separately tuned — the invariant
  mapping is the determination (a corner case; the general Latin/basic repertoire matches exactly). Since kb/Work
  PB64 T5 this is the ELSE-arm it is meant to be: a `LOCALE locale-name-1` phrase (r2) or a CHARACTER CLASSIFICATION
  locale in effect for the operand's class (r3) maps through THAT locale's LC_CTYPE instead — see §4 item 5.
