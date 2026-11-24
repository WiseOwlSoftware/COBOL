## Entry 4026 — 2026-11-23 18:47 PST — The Tier-A REDEFINES alias view rule has one home on DataItem (PB6141)

**Mechanism.** A Tier-A (`RedefinesTier.Alias`) class keeps one typed field, its canonical's, and every other member is a view
that owns no field and renders the canonical's (§2023:13.18.44.4 GR1: one storage area). The test "a non-canonical member of an
Alias class" was spelled inline at thirteen sites: `LinkageBinder.AnchorOf`, `PlaceBuilder.PlaceForItem` (twice),
`RecordLayout.ExactPhysicalAdvance`, `MemberCursor.Child`, `CorrespondingBinder.CorrAccess.CorrPlainMember`, `StorageFormPass`
(`ComputePromotedSet`, `PromoteNegativeZeroReceivers`, `Classify`), `PhysicalModel.BuildPhysicals`,
`RecordStructEmitter.EmitStaticReset`, `ProgramEmitter.EmitCallMethod` and `OoEmitter.EmitMethod` (twice). The guard shapes
differed: `RecordLayout` also asked `alias.Members.Contains(child)`, and `MemberCursor` and `CorrPlainMember` wrote the
canonical's side as `Tier is Alias && IsCanonical`.

**Re-probe.** A grep of `RedefinesTier.Alias` over `src/` at 6a6f45e332 listed the thirteen sites plus the enum and the classifier.

**Fix.** `DataItem.AliasCanonical` (the canonical a Tier-A view forwards to, else null), `DataItem.IsAliasView`, and
`DataItem.IsPlainMember` (no class, or a Tier-A canonical) are the rule; every site reads them and `CorrPlainMember` is deleted.
The membership guard was redundant: `ClassifyRedefinesClassesPass.ComputeTier` admits Tier A only when every member is
elementary, so no subordinate carries an Alias class. `PlaceForItem`'s "non-canonical and not Alias" guard became the
Rejected-tier test it meant, since Tier B returns earlier. `AliasViewRuleDriftTests` pins that only `RedefinesModel.cs`,
`ClassifyRedefinesClassesPass.cs` and `DataItem.cs` name the Alias tier, that the retired copy stays gone, and the rule's answer
for a standalone item, a Tier-A pair and a Tier-B pair. DESIGN-data-model §2.3 names the home.

**Behavior-neutral.** `python scripts/arch/compare_oracle.py`: `=== ARCH-ORACLE: IDENTICAL — 23575 cases (baseline 6a6f45e332fa,
new f9f0d80c744b-dirty) ===`. No goldens: no behavior changed. No inventory row (`closes_rows: []`).

**Self-review.** Architecture: one predicate per question, on the model type every phase already reads. Duplication: the grep
above now hits only the owners. Performance: the predicates are field reads; `RecordLayout` lost a `List.Contains`.

