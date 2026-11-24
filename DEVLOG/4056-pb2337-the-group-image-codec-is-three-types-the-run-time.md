## Entry 4056 — 2026-11-24 15:42 PST — PB2337: the group image codec is three types, the run-time faces, the initial-image seed and the bit faces

kb/Work PB2337 (census R0-0042, DESIGN-architecture-review §8.3) planned an extract of `GroupImageCodec`, a type of
983 lines at the census and 1,403 lines at this branch's base (f6ad06877c). It had three jobs: the run-time image a record
struct carries (`AsImage`/`FromImage`, the national face, and the variable-length group's `CurrentImage`,
`AsVarImage`/`FromVarImage` and record layout), the compile-time INITIAL image a byte-addressed area starts at (a Tier-B
REDEFINES backing, an EXTERNAL, BASED or ADDRESS-OF cell, an OO backing), and the USAGE BIT faces.

**Re-probe.** This is a restructuring note, so there is no wrong answer to reproduce. Its precondition held: every blocker
(PB2118, PB2417, PB2099/PB2100) and every external-repository slice PB2097-PB2104 has `status: landed`, so §8.6 no longer
freezes the file.

**Shape.** Every member moved unchanged, with one exception: the bit-run arm of `EmitMemberFromImage` and the bit-group
arm of `EmitImageMethods` became two `BitImageCodec` entries, `EmitRunFromImage` and `EmitBitGroupFaces`, with the same
lines in the same order.
- `GroupImageCodec(PhysicalModel)` (797 lines): the run-time faces and the variable-length carrier. It no longer takes
  the `EmitContext` or the `ValueInitializer`, because it reads no VALUE clause.
- `ImageInitializer(EmitContext, ValueInitializer)` (423): `ImageInitOf`, `BackingSeedOver`, `CellDynSeeds` and their
  helpers, including the bit seed's `InitialBitCarrierOf`/`OneBitCarrierOf` and `SlotSeedOf`. It emits no member.
- static `BitImageCodec` (235): `EmitBitGroupFaces`, `EmitBitMethods`, `BitAreaOf`, `PackedRunOf`, `EmitRunFromImage`
  and the stored-filler readers. `BitAreaOf` is still the one §2023:8.5.1.6.3 placement that both the emitted `AsBits()`
  face and the seed compose over.
The callers changed in the same change, with no forwarder: `DataEmitter` wires the three, `PhysicalModel.Codec` became
`PhysicalModel.Images` (the Tier-B backing seed), `GroupValueSlicer` seeds a member-rooted backing through it, and
`RecordStructEmitter` takes the codec and the initializer separately.

**Drift test.** `GroupImageCodecBoundaryDriftTests` checks the seams by type rather than by member name:
- the codec declares nothing of the seed's types (`ValueInitializer`, `SeedRecipe`, `Subscripts`);
- the initializer takes no `CodeWriter`;
- `BitImageCodec` is static and is the only one of the three that names `BitFiller`.
`BitRunImageDriftTests` and `ConstantRecordSeedRecipeDriftTests` now read `ImageInitializer.cs`, where the run law and the
recipe chooser moved.

**Evidence and docs.** Eleven inventory rows had code-locations that named a moved member, or the bare file where the
witnessed code was the VALUE seed. They were re-sited by `record_verdicts` batches (2023: 9, 1985: 1, 2002: 1). Each
batch was accepted whole (11 rows changed), and the GAP count did not change in any edition: 1, 0 and 1130. The four
1985 code reads that name the old sites are pinned evidence, owed a re-read and not a red. Comments and design docs
(DESIGN.md, DATA_MODEL_DESIGN.md, DESIGN-data-model.md, DESIGN-codegen-backend.md, DESIGN-module-topology.md,
PROJECT_ORG_DESIGN.md, D-USAGE-BIT.md, the §8.3 row) now name the new homes.

**Neutrality.** `python scripts/arch/compare_oracle.py --no-build`:
`=== ARCH-ORACLE: IDENTICAL — 23650 cases (baseline f6ad06877c76, new f6ad06877c76-dirty) ===`.
`python scripts/semgrep/verify.py`: PASS, no rule's count moved.

**Gate.** `pwsh scripts/build-local.ps1 -Priority BelowNormal` at e3540158df, run 000001-20261124T232631Z:
`=== BUILD-LOCAL GATE: GREEN — Conformance 29,412/29,412 ran · Unit 42,990/42,990 ran · RepoDrift 245/245 ran ·
Characterization 37/37 ran (skipped 0) …`. All three `GroupImageCodecBoundaryDriftTests` facts passed (Unit.trx).

