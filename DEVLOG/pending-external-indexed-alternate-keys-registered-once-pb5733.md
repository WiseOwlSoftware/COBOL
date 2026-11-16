## External indexed file: a second describing program no longer re-appends the alternate keys (PB5733)

Two programs of one run unit that describe one EXTERNAL indexed file with the same alternate record keys shared one
`IndexedConnector` (`RegisterIndexed` returns early for an existing `::EXT::` name), but each describer's emitted
`AddAlternateKey` calls still ran, so the connector's key table held every alternate key twice and the second program's
OPEN got I-O status 39 (`§2023:12.4.5.3 GR1`: one connector, the same number of alternate keys).

Re-probe on this build before the fix: the note's repro ended in status 39. After: `P1 DONE`, `0001AAAAxxxx` at `--std 85`,
`2002` and `2023`. Fix: `FileRegistry.RegisterIndexed` records the name of an existing external connector it skipped
(`_alternateKeysDescribed`) and clears it when it creates one; `AddAlternateKey` for that name returns. The emitter writes
each registration then its keys contiguously (`KeyedIoEmitter`), so the field is exact. Siblings swept: `Register`,
`RegisterReport` and `RegisterRelative` already return early and have no follow-on registration calls.

Golden: `conformance:cases/12.4.5-File-control/pb5733_external_alternate_keys_agree` (two alternate keys, one WITH
DUPLICATES, READ by the second key), accepted at 85, 2002, 2014 and 2023. No refused-below program: the rule exists in
every edition. Row `GR-12.4.5.3-1` re-verdicted PARTIAL to CONFORMS.
