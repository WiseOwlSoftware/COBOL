## Entry 2543 — 2026-10-25 11:34 PDT — PB3675 -- a transaction's holder that cannot be published is refused for the cause it had

`IHostMedium.PublishHolder` returned a bare holder-or-null, so `SequentialJournal.Begin` threw "the host cannot hold a transaction's
holder" for two different failures: a host that takes no lock here, and a publication every one of whose twelve tries lost its race to
another run unit's removal of the lock directory. The second is a failure to publish and says nothing about the host.

**Re-probe.** `Begin` over `HostMedium.Production` with `SidecarLockFiles.RaceInjector` losing every try threw the "cannot hold" text
(the new journal-level witness was red on the old code; its two sibling cases were green).

**Fix.** `PublishHolder` now returns the publication's `RegionLockResult` (the vocabulary PB3632 gave `SidecarLockFiles.Publish`) with the
holder in an `out` parameter; the interface summary says what each value means. `Begin` names the cause that happened. The disposition
stays one `IOException`, the statement's '30' with the file unaffected (§9.1.13.6 item 1, §14.9.27.4 GR25): Publish already retries
with pacing, and no other I-O status names a transient failure to begin a transaction. DESIGN-runtime-library 2.2.2.5 R1 carries the
rule. Sibling sweep: the other `Publish` callers took the result at PB3632; the handle host and `PhysicalFileTable` already use
`RegionLockResult`; nothing else collapses two causes into null.

**Witnesses.** `SequentialJournalHolderPublicationTests` (lost-race burst, a host with no directory beside the file, a publication that
loses three tries and wins the fourth) and `SequentialCommitControlTests.A_transactions_holder_that_cannot_be_published_refuses_the_statement_30_whatever_the_cause`
(OPEN OUTPUT of an existing and of an absent file and the first WRITE under EXTEND, each for `Held` and `Unavailable`: '30', the file
unaffected, nothing left in the lock directory), through the new `FaultHostMedium.PublicationAnswer`. No COBOL golden: the lost race is
not deterministically reachable from a COBOL program (it needs another run unit to remove the lock directory at that instant), and the '30' it answers is the same status as before.

**Rows.** None (the note claims no inventory row).

