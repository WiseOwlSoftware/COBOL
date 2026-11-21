- **D-IO-STATUS-07-OPEN-CLOSE — I-O status '07' restricted to OPEN/CLOSE (§2023:9.1.13.2 item 6; E.2 item 16)**: **VERIFIED AT EVERY EDITION FROM ITS OWN
  TEXT, THE SAME BEHAVIOUR AT EVERY EDITION** (kb/Work PB1515). *2023:* the '07' setters are exactly the three
  phrases item 6 names on the two statements it names, all reaching `FileStatusCode.PhraseOnNonReelMedium` on a
  non-reel/unit medium: `CLOSE … REEL/UNIT [FOR REMOVAL]` and `CLOSE … WITH NO REWIND` through
  `FileRegistry.CloseByFormat`, and `OPEN … WITH NO REWIND` through `FileRegistry.NoRewindPhraseEffect`. No
  READ/WRITE/START/REWRITE/DELETE path sets it, on any organization, and an unsuccessful OPEN or CLOSE keeps the
  status that made it unsuccessful ('35', '41', '42', '61': `NoRewindPhraseEffect` and `CloseByFormat` test the first
  digit). `IoStatus07SetterDriftTests` holds both facts structurally (the constant is read in those two methods only,
  and the value is spelled nowhere else). *Before 2023:* the earlier editions' own texts, read (kb/Work PB5321), name the same two setters. 1985:
  "I-O Status = 07. The input-output statement is successfully executed. However, for a CLOSE statement with the NO
  REWIND, REEL/UNIT, or FOR REMOVAL phrase or for an OPEN statement with the NO REWIND phrase, the referenced file is on
  a non-reel/unit medium" (§1985:VII.1.3.5, the sequential module's list; the relative and indexed lists carry no 07).
  2002 and 2014: "I-O status = 07. The input-output statement is successfully executed but a CLOSE statement with the NO
  REWIND, REEL/UNIT, or FOR REMOVAL phrase or for an OPEN statement with the NO REWIND phrase references a physical file
  on a non-reel/unit medium" (§2002:9.1.12.1, §2014:9.1.12.1). 2023 tightens only the lead-in, "An OPEN or CLOSE
  statement is successfully executed but ..." (§2023:9.1.13.2 GR6), and that is what E.2 item 16 records ("The setting of
  I-O Status '07' is now restricted to the execution of the OPEN and CLOSE statements", §2023:E.2): in every edition the
  condition clause already names only CLOSE and OPEN, so no earlier rule states another setter and no prior answer
  differs that a gate could serve (VERSION_CHANGE_REFERENCE's admission test, row 22). The 2014 E.2 item 7 says the 2002
  text "did not specify exactly when I-O status '07' was to be set" and that "The rules of the CLOSE statement have been
  updated to set the I-O status to '07'" (§2014:E.2); the 2002 OPEN rule already set it for an ignored NO REWIND ("If
  the NO REWIND phrase is ignored, the OPEN statement is successful", §2002:14.8.26.3 GR9). So the behaviour is
  edition-invariant on the editions' own text. Corroboration, below the text: GnuCOBOL 3.2.0 (probed in WSL,
  2026-10-17) answers '07' from `CLOSE … REEL/UNIT/NO REWIND` alone (it does not implement `OPEN … WITH NO REWIND`, which
  ISO §2023:14.9.27.4 GR11 requires to answer '07') and never from a READ, WRITE, REWRITE, START or DELETE. The
  EC-I-O-WARNING raising of '07' (§2023:9.1.13.1) is a separate 2023 rule, gated below.Witnesses: `conformance:cases/9.1.13.2-Successful-completion/pb1515_io_status_07_open_close_only`
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
