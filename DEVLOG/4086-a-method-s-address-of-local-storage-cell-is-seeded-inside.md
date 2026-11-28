## Entry 4086 — 2026-11-27 21:21 PST — A method's ADDRESS-OF LOCAL-STORAGE cell is seeded inside the method's OPTIONS INITIALIZE scope

PB6336 (wrong answer, silent). In a method whose LOCAL-STORAGE record has its ADDRESS OF taken, the record lives in a
per-activation `StorageCell` with no other initializer. `OoEmitter.EmitMethod` computed that cell's initial image
(`ActivationPointerSeeds` -> `AddressableCellInit` -> `ImageInitOf` -> `InitialStateBackground.Seed`) before it pushed the method's
own OPTIONS model and swapped `Ctx.ActivationLocalRoots` to the method's roots (kb/Work PB1215). `FillFor` therefore saw the class's
options and the class forest's LOCAL-STORAGE roots, `OptionsInitialize.Governs` answered false, and the record started as the category
default instead of the fill (§2023:11.9.10.4 GR2, §2023:8.6.4).

Re-probe on this tree before the fix, `METHOD-ID. M1. OPTIONS. INITIALIZE LOCAL-STORAGE TO X"41".` with `01 R. 05 F PIC X(4).`
(ADDRESS OF taken) and a plain `01 Q. 05 G PIC X(4).`: `R=[    ] Q=[AAAA]`, twice. After: `R=[AAAA] Q=[AAAA]`.

Fix: the options push and the activation-roots swap move above the seed computation, one scope for every initializer of the activation;
the roots are restored after the LOCAL-STORAGE locals (the old comment said they stayed in for the body, which the code never did) and the
options scope after the body. Sibling sweep: the cached-singleton program arm (`ProgramEmitter.EmitCallMethod`) seeds inside the unit's own
options and roots already (probed: `R=[AAAA]` on both calls); `RecordStructEmitter` re-seeds ADDRESS-OF cells under the unit scope;
the class-member declarations of a per-activation cell are overwritten at entry.

Goldens: `11.9.10-INITIALIZE/pb6336_method_ls_address_of_options_initialize` (method's own clause, class-inherited clause, no clause;
the first method mutates R between two activations) and the refusal `pb6336-method-options-initialize-below-2023`.
No inventory row re-verdicted (the note carried none).

