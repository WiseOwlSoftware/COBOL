## PB675: the OPEN statement's RETRY-phrase scope is asserted on the bound tree, because no program can print a leak any more

`conformance:cases/14.9.27-OPEN/pb316_open_group_scope` parts (1) and (2) were written (PB316) to catch one group's RETRY
phrase leaking into its sibling file-name. Since PB321 arbitrates every OPEN against Table 19, they stopped doing that.
The re-probe on this tree: the golden prints its six pinned lines, and with the binder's per-group `retry` replaced by a
statement-scope one (the first RETRY written, applied to every group) it still prints the same six lines. So the pair
was green for a reason unrelated to what it asserts.

**Why no golden can discriminate it (measured, not assumed).** A leaked RETRY changes two things in the runtime, both
internal: `FileRegistry.OpenCore` registers the clause-less connector through `RegisterSharing(name, null,
FileLockMode.None, false)`, which is value-equal to `ImplementorDefaultShare`, and `AssociatePhysical(st, true)` makes
`SharedPhysical` non-null (the shared `OPEN EXTEND` release-ordinal mint and the read access of an OUTPUT/EXTEND
writer). A connector with `FileLockMode.None` sets no record lock, and every RETRY form lands the conflict's own '61'
within one run unit (`pb142_retry_conflict_class_ec`), so no status, exception name or record moves. The note's
candidate (a), a distinguishing clause in F-SEED's group, therefore does not exist.

**The fix is candidate (b), at the layer where the PB316 defect lived.** `OpenGroupScopeDriftTests` gains
`EachOpenEntry_CarriesOnlyItsOwnGroupsPhrases`: it parses and binds seven multi-group OPEN statements at COBOL-2023
(phrase-bearing group before and after a phrase-less one, one group with two file-names, two groups with different
RETRY kinds, RETRY with SHARING, SHARING alone), flattens the implicit-statement series, and requires every
per-file-name `BoundOpenFile` to carry exactly its own group's `Retry` kind and `Sharing` mode. Discrimination was
shown by mutating the binder twice and reverting: a RETRY hoisted to statement scope fails 7 of 7 rows, a RETRY that
only leaks forward fails 6 of 7. The golden's header now states what parts (1) and (2) still pin (GR20's equivalence of
the two written forms) and names the unit witness instead of the removed claim that a leak "changes what LATER opens
arbitrate against".

**Rows.** PB316's rows were re-measured. GR-14.9.27.4-24 (the RETRY phrase) had parts (1)/(2) as its only scope
witness; it and GR-14.9.27.4-20 and FMT-14.9.27.2 now name the unit theory, whose notes had claimed the golden's RETRY
parts discriminate (restated), and GR-14.9.27.4-22 and -23 gained it as the SHARING arm's bound-tree witness. No row
changed verdict and none was claimed by the note, so `closes_rows` is empty with its reason.

No compiler or runtime source changed. Self-review: the test's fixture declares unique PROGRAM-ID and temp file names,
deletes its temp source, asserts the bind produced no diagnostics and the expected entry count (so a dropped group or a
statement that stopped binding cannot read as a pass), and uses `FileModel.SelectName` because `CobolName` is
program-qualified at emission.
