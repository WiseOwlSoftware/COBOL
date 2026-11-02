## Entry 2998 — 2026-11-02 08:24 PST — A contained program's own condition-name outranks its container's GLOBAL index-name, constant-name and capacity register (PB4432)

ISO/IEC 1989:2023 8.4.6.2.1 3) a) (`cite.py --check` OK): "If the name is declared in source element B, the item in
source element B is the referenced item." PB4219 had ranked a file-name against a condition-name by the depth of the
element that declares each; the constant-name and index-name answers of `DataBinder.NonDataDeclarationOf` still returned
before the condition-name check whatever their depth.

**Re-probe (base 101ded510, Debug build).** A contained program with `88 RELF` under a container whose GLOBAL table is
`INDEXED BY RELF` (or whose GLOBAL `01 RELF CONSTANT`): `OCCURS DEPENDING ON RELF` was refused as "declared as an
index-name" / "a constant-name" (COBOL0851); `DISPLAY RELF` as an index-name (COBOL1637). The note's unprobed second half
reproduced and is worse than a wrong word: `SET RELF TO 2` and `PERFORM VARYING RELF FROM 1 BY 1 ...` compiled clean and bound
the CONTAINER's index. The sibling the note did not name did too: a GLOBAL `OCCURS DYNAMIC CAPACITY IN CAP` register and a
contained `88 CAP` - `DISPLAY CAP` compiled clean and read the container's register.

**Fix.** `NonDataDeclarationOf` collects the constant-name, index-name, file-name and condition-name declarations of the
spelling through one `Offer` ranking by `DeclaringDepth` (ties keep the old order constant, index, file, condition, the
§8.3.2.2 conflict being reported at the declaration). `SymbolTable.NearerDataOrConditionNameHides` is the one question the
classes resolved before the data-name lookup ask - `IndexCandidates` and `ReferenceResolver.CapacityRegisterFor` - over
`NearerDataNameHides` and `NearerConditionNameHides`. The procedure division's constant arm was already right
(`DropShadowedConstants` asks `DeclaredNearerThan`, which includes conditions); only the data division's clause operands
meet the inherited constant before that runs, and the ranking now covers them.

**Goldens.** `conformance-suite/cases/8.4.6.2-Local-and-global-names/pb4432_*`: ODO over index-name (85..2023), ODO over
constant-name (2002..2023), PERFORM VARYING over index-name (COBOL1639), DISPLAY over capacity register (2014, 2023), and
an accepted control (SET ... TO TRUE / IF with the container's index intact, 85..2023). `GlobalNameClassDriftTests`
holds `IndexCandidates` and `CapacityRegisterFor` to the one predicate and `NonDataDeclarationOf` to the ranking.

**Rows.** PB4432 claimed none; it closes none.

**Review round 1 (R1-1): the reverse arm.** The reviewer refuted "No other arm": `SymbolTable.NearestDeclaringConditions`
asked only `NearerDataNameHides`, so a contained program's OWN `INDEXED BY RELF`, `01 RELF CONSTANT`, `CAPACITY IN RELF` or
`SELECT RELF` did not hide the container's GLOBAL `88 RELF`: `IF RELF` and `SET RELF TO TRUE` compiled clean and used the
container's condition-name (ISO 8.4.6.2.1 3) a)). Re-probed on the branch head, all six shapes reproduced. The fix gives the
constant-name, index-name and file-name classes ONE list, `DataBinder.NonConditionDeclarationsOf`, which
`NonDataDeclarationOf` ranks beside the condition-name and `NonConditionDeclarationNearerThan` asks for the condition
lookup. The CAPACITY register keeps its ONE lookup (`CapacityRegisterReferenceDriftTests`): `ConditionBinder.ConditionOf`
asks `CapacityRegisterFor` of it. (The first gate was red: the ODO pass asks `NonDataDeclarationOf` before the registers
exist, and a second reader of them also broke that drift test and the SymbolTable layering row, so the register stayed out
of the list.) A condition-only position (SET ... TO TRUE) names the hiding class through `NonDataDeclaration.ClassPhrase`
or `ReferenceResolver.CapacityRegisterClassPhrase`; a bare IF operand leaves it to the value arm (COBOL1637 for the
index-name, COBOL2318 for a constant or register); SEARCH ALL's WHEN screen skips its condition-name verdict. Goldens:
`pb4432_if_names_own_{index_name,constant_name,file_name,capacity_register}_*`,
`pb4432_set_to_true_names_own_{index_name,capacity_register}_*` and the control
`pb4432_container_global_condition_name_beside_other_index_name`. Drift test extended: both directions ask the one list.

Review round 2 (R2-1, R2-2, R2-3). The reverse arm counted a nearer declaration the written qualifiers do not reach:
an own `INDEXED BY RELF` (an empty `IndexCandidates` set, ranked depth 0) hid `RELF OF X` from the container's GLOBAL
`88 RELF`, and an own `CAPACITY IN CAP` (`CapacityRegisterFor`'s `CapacityRefFault.Qualifiers` answer) hid `CAP OF X`,
though ISO 8.4.6.2.1 2) identifies the level-88 as the only item. Both reproduced on the reviewed head, both accepted now:
an unreached index-name ranks `int.MaxValue` in `NonConditionDeclarationsOf` and `ConditionBinder.CapacityRegisterHides`
requires `Reached.Count > 0`. Goldens `pb4432_qualified_condition_name_beside_own_{index_name,capacity_register}`; the
`NonDataDeclarationOf` comment no longer lists the register among the offered classes.

