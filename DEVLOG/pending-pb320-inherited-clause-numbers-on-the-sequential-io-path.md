## PB320: the inherited clause numbers on the sequential I/O path are repaired where they still stood

kb/Work PB320 listed sixteen sites that name MOVE, COMMIT, MULTIPLY, RESUME, SET, UNLOCK and START for rules about
OPEN, CLOSE, RELEASE, RETURN, WRITE and SORT. Re-probed against the current tree, eleven were already repaired by
earlier landings (the `CobolIO.g4` headers, the `BoundTree.cs` OPEN and CLOSE arms, `constructs.json` and the registry
generated from it, the SORT golden's header, both RELEASE-size comments, and the deleted legacy `SpecFixTests`). Seven
sites still carried the wrong number and are repaired here, each replacement checked with `cite.py --check` rather than
copied: the `FileOpenMode` summary and `FileSupport.cs` (OPEN is `§14.9.27`, not MOVE's `§14.9.25`), the `BoundClose`
summary, the sequential CLOSE body and `SequentialIoBinder.cs` (CLOSE is `§14.9.6`, not COMMIT's `§14.9.7`), the
sequential OPEN body, and `SequentialIoEmitter.LinesExpr`, whose WRITE ADVANCING comment cited UNLOCK's `§14.9.47.3`
for the rule that is WRITE's `§14.9.51.3` SR14. Two knowledge-base rows carried the same lineage (`Construct
Catalogue.md` OPEN REVERSED at `§14.9.26`, `IR Mapping.md` `BoundClose` at `§14.9.7`) and are fixed, and the IR Mapping
banner that described the old cross-wired source comments is removed because the comments are no longer wrong.

The sweep is the finding. `audit_code_citations.py --check` read 0 findings before the repair, so the audit that was
built for exactly this failure mode could not see these seven. SUBJECT suppresses a finding when the cited clause's
construct is named anywhere within thirty lines. An adjacency check (a statement name directly beside a clause-level
statement citation of a different statement) was prototyped and not added: its precise form catches one of the six
stale shapes, since most name the construct in prose ("the sequential OPEN body (ISO §14.9.25"), and its wide form
reports 99 sites, none of them defects. Adding either would have been a gate that looks stronger than it is.

No inventory row closes (`closes_rows: []`, with the reason in the note). The change is comments and documents only;
no golden applies. Gate: the one gate, leg 1.

Review round 1 found an eighth site, in the file this entry repairs: the summary of `SequentialConnector.WriteCore` cited
WRITE at `§14.9.46` (TERMINATE; WRITE is `§14.9.51`), the very cross-wiring whose banner this entry deletes. Fixed. The
sweep for WRITE beside `§14.9.46` found two siblings that cite it for WRITE's rules: `FileIoDifferentialTests` (the SQ207M
mnemonic test, now `§14.9.51.4` GR25d and GR4) and the SQ207M note in `tests/nist/corpus.tsv`. Each cite was
`cite.py --check`ed; comments and a note column only.

Review round 2 found that the round-1 fix cited GR4 (the SAME RECORD AREA availability rule) for "the WRITE releases the
record"; the release rule is `§14.9.51.4` GR12 (`cite.py --check`ed). Both sites (the SQ207M test summary and the corpus
note) now say GR12; the remaining GR4 citations in the tree are all record-area availability and are correct.
