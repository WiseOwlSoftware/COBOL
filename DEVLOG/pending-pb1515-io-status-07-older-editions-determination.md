## PB1515: I-O status '07' at the older editions is a recorded determination, pinned by goldens at 85 to 2023 and a setter-site drift test

PB1515 (wave 1115, group C; the file I/O campaign, PB3353) owed the pre-2023 setters of I-O status '07': the
refuter had struck the inventory row `GR-9.1.13.2-6` PARTIAL because its editions field claimed 85 to 2014 on the
ground that OPEN/CLOSE "were already the only setters at every edition", while Annex E.2 item 16 says the setting
"is now restricted to the execution of the OPEN and CLOSE statements" (`cite.py --check E.2` OK).

**Re-probe.** On the branch build, record sequential, relative and indexed files at `--std 85`, `2002`, `2014` and
`2023` give one behaviour: `OPEN ... WITH NO REWIND` answers '07' only when it succeeds, `CLOSE ... WITH NO REWIND`,
`REEL` and `UNIT [FOR REMOVAL]` only on an open file, no READ, WRITE, REWRITE, START or DELETE ever, and an
unsuccessful OPEN or CLOSE keeps its own status ('35', '41', '42', '61'). GnuCOBOL 3.2.0 (WSL) answers '07' from
CLOSE alone and from no other statement; it does not implement `OPEN ... NO REWIND`.

**What the root is.** Nothing in the compiler is wrong, and there is nothing to implement behind the edition axis:
E.2 names neither the statements that set '07' before 2023 nor the condition they reported, and the repository holds
no 1985, 2002 or 2014 text. VERSION_CHANGE_REFERENCE's own admission test (an E.2 item earns a `behavior:` gate only
when it says what the prior rule STATED) therefore fails for row 22, which becomes `ref-only` with the determination
written in its cell (the rows 25 and 26 shape), following rule 1's order: the standard has nothing for those
editions, then GnuCOBOL. `docs/CONFORMANCE.md` section 3 no longer says "already met at all editions" as if it had
been read from the older text; it separates what is verified at 2023 from what is a determination below it, and names
the way out (a text that states a distinct rule makes row 22 a `behavior:` row in `DialectBehaviors`).

**What now keeps it true.** `IoStatus07SetterDriftTests` (Unit): the named constant `FileStatusCode.PhraseOnNonReelMedium`
is read in `FileRegistry.CloseByFormat` and `FileRegistry.NoRewindPhraseEffect` only, and the value "07" is spelled
nowhere else in the runtime or the code generator, so a third setter (the "other I-O statements" of the older
editions) cannot appear without failing a test that states the rule. Four conformance goldens (the owner's
2026-10-17 requirement that file I/O tests cover the abnormal and exception paths as fully as the success paths):
`pb1515_io_status_07_open_close_only` (85, 2002, 2014, 2023; sequential: '35' on an absent file, '41' on a re-OPEN,
'42' on CLOSE of a closed file for each of NO REWIND, REEL and UNIT FOR REMOVAL, '10' with and without an AT END
phrase, '46', with a FILE STATUS clause and a Format 1 USE declarative counted), `pb1515_io_status_07_keyed_organizations`
(85 to 2023; relative and indexed: '35', '22', '23', '10' with INVALID KEY, AT END and neither),
`pb1515_io_status_07_sharing_conflict` (2002 to 2023; a refused `OPEN ... NO REWIND` answers '61', never '07', and the
refused file is not affected) and `pb1515_io_warning_07_raises_ec` (2023; '07' is EC-I-O-WARNING under `>>TURN`, raised
by OPEN and CLOSE alone, with and without a FILE STATUS clause, and '42' is not a warning) with its negative
`pb1515-io-warning-07-below-2023` (COBOLNET0878 at 2002 and 2014). Every expected value is derived in the golden's
comment from its clause; the run matched the derivation on the first pass at every edition.

**Row.** `GR-9.1.13.2-6` PARTIAL to CONFORMS (GAP 56 to 55), editions kept at 85 to 2023 because the conformance suite
refuses a test accepted at an edition outside a row's band and the older-edition claim is now stated as what it is.
No diagnostic codes used (COBOLNET4307 to 4309 returned).
