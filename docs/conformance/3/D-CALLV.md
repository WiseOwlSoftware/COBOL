- **D-CALLV — the `--permissive` vendor spellings of a Format 1 CALL's USING phrase** (kb/Work PB162, PB3996, PB4012;
  owner decision 2026-10-28). §2023:14.9.4.2 Format 1 prints `[ BY REFERENCE ] { identifier-2 } …` and
  `BY CONTENT { identifier-2 } …` and nothing else (`cite.py --check 14.9.4.2 "Format 1 (Program)"` → OK); BY VALUE,
  a literal or expression argument and OMITTED belong to Format 2, which the AS phrase selects. **Strict** refuses each
  with COBOL0899, the standard's verdict. **`--permissive`** reports the same code as a warning, names the spelling, and
  binds it with the Format-2 meaning it spells, for exactly the spellings the CLAUDE.md rule-1 survey finds the
  vendors accept on a CALL naming no prototype: BY VALUE (the Value mode), a BY CONTENT literal or expression and a
  bare literal or expression (a detached BY CONTENT value) — GnuCOBOL accepts all five, IBM Enterprise COBOL and
  Micro Focus BY VALUE and a BY CONTENT literal — and **OMITTED passed BY REFERENCE**, bare (`USING W OMITTED`) or
  written `USING BY REFERENCE OMITTED`, the §2023:14.9.4.4 GR11 omitted argument ("If an OMITTED phrase is specified …
  the omitted-argument condition for that parameter evaluates to true in the called program", OK 11)), which
  GnuCOBOL (`tests/external/gnucobol/tests/testsuite.src/run_misc.at:4415`, `run_file.at:1217`), IBM (its BY
  REFERENCE OMITTED for callable services) and Micro Focus accept. Asked by the owner whether `--permissive` accepts
  OMITTED there, the owner answered on 2026-10-28: "Yes, permissive accepts". An OMITTED that a BY CONTENT phrase
  carries over by §2023:14.9.4.4 GR5's transitivity (`USING BY CONTENT W OMITTED`) is refused under both modes: an omitted
  argument has no BY CONTENT value, and cobc 3.2 refuses that spelling too. **At `--std 85`** there is one CALL
  format and no program-prototype CALL to point to, and each of the six spellings is a COBOL-2002 introduction (BY VALUE
  `call-by-value-2002`, OMITTED `omitted-arguments-2002`, a literal or expression `call-literal-expression-argument-2002`,
  the last QUOTED from ISO/IEC 1989:2002 Annex F.2 item 15): strict reports that COBOL0900, once, and `--permissive`
  accepts the vendor spelling there too. IBM Enterprise COBOL lists OMITTED and BY VALUE in CALL among its extensions
  over its ISO 1989:1985 baseline, and HP documents both as HP extensions; asked whether `--std 85 --permissive` accepts
  OMITTED, the owner answered on 2026-11-03: "permissive accepts it at 85 too" (kb/Work PB4503). Implemented once, in
  `CallBinder.Format2OnlySpellingOf` (which spellings, reading the carried mode) and `CallBinder.Format1Narrowing`
  (the framing, at every edition: the edition-conformance pass gates no CALL argument spelling); pinned by `conformance-test:FormatOneVendorCallPermissiveTests` and the strict refusals
  `conformance:cases/14.9.4-CALL/pb3996-call-format1-omitted-*`.
