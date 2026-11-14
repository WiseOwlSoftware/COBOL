- **D-IO-STATUS-07-OPEN-CLOSE — I-O status '07' restricted to OPEN/CLOSE (§2023:9.1.13.2 item 6; E.2 item 16)**: **VERIFIED AT 2023, A DETERMINATION
  BELOW IT, THE SAME BEHAVIOUR AT EVERY EDITION** (kb/Work PB1515). *2023:* the '07' setters are exactly the three
  phrases item 6 names on the two statements it names, all reaching `FileStatusCode.PhraseOnNonReelMedium` on a
  non-reel/unit medium: `CLOSE … REEL/UNIT [FOR REMOVAL]` and `CLOSE … WITH NO REWIND` through
  `FileRegistry.CloseByFormat`, and `OPEN … WITH NO REWIND` through `FileRegistry.NoRewindPhraseEffect`. No
  READ/WRITE/START/REWRITE/DELETE path sets it, on any organization, and an unsuccessful OPEN or CLOSE keeps the
  status that made it unsuccessful ('35', '41', '42', '61': `NoRewindPhraseEffect` and `CloseByFormat` test the first
  digit). `IoStatus07SetterDriftTests` holds both facts structurally (the constant is read in those two methods only,
  and the value is spelled nowhere else). *Before 2023:* E.2 item 16 says the setting "is now restricted to the
  execution of the OPEN and CLOSE statements", so an earlier edition let other input-output statements set it, but
  E.2 names neither the statements nor the condition they reported, and when this determination was made the repository
  held no earlier edition's text, so there was no prior answer a gate could serve (VERSION_CHANGE_REFERENCE's admission
  test, row 22). The repository now holds the texts of the editions `scripts/spec/spec_pdf.py`'s `EDITIONS` names (1985
  and 2002 among them), and the 1985 text's own status 07 names only a CLOSE and an OPEN on a non-reel/unit medium
  (§1985:VII.1.3.5); this determination has not yet been read against them (kb/Work PB5256, branch review R1-8). Under rule 1's order — the standard has nothing for those editions, then GnuCOBOL — the behaviour stays
  edition-invariant: GnuCOBOL 3.2.0 (probed in WSL, 2026-10-17) answers '07' from `CLOSE … REEL/UNIT/NO REWIND` alone
  (it does not implement `OPEN … WITH NO REWIND`, which ISO §2023:14.9.27.4 GR11 requires to answer '07') and never from a
  READ, WRITE, REWRITE, START or DELETE; IBM Enterprise COBOL and Micro Focus were not consulted, GnuCOBOL having
  answered. If a text of an earlier edition that states a distinct rule is ever obtained, the gate belongs in
  `DialectBehaviors` and row 22 becomes a `behavior:` row. Witnesses: `conformance:cases/9.1.13.2-Successful-completion/pb1515_io_status_07_open_close_only`
  and `…/pb1515_io_status_07_keyed_organizations` (the same output at 85, 2002, 2014 and 2023: each statement's
  status on its success and failure paths, with a FILE STATUS clause and a USE declarative),
  `…/pb1515_io_status_07_sharing_conflict` (a refused `OPEN … WITH NO REWIND` answers '61', never '07'; 2002+, the
  file sharing mechanism's edition) and `…/pb1515_io_warning_07_raises_ec` (2023: the '07' is the exception condition
  EC-I-O-WARNING, §2023:9.1.13.1, raised by OPEN and CLOSE alone, with and without a FILE STATUS clause), plus the negative
  `pb1515-io-warning-07-below-2023`. *(This bullet claimed until 2026-09-05 that CLOSE REEL/UNIT was the ONLY
  setter — untrue since kb/Work PB141 added the CLOSE NO REWIND arm, and doubly so since PB317 added the OPEN
  one — and, until PB1515, that "already met at all editions" had been derived from the 2023 text alone; a
  determination that undercounts its own sites, or claims editions it never read, cannot be checked against the
  code.)*
