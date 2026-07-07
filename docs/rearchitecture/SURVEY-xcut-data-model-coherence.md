# SURVEY (cross-cutting) — Data-Model Coherence

> **Type:** rearchitecture AS-IS survey (backfill of the missing cross-cutting data-model trace).
> **Scope:** the typed-native value model spanning `DataItem` / `PicInfo` / `Place` / `NumProfile`, bind → emit →
> runtime. This is *the* central architecture question: **how many different physical representations of "one data
> item's value" exist, where the choice is made, and whether they can be unified.**
> **Method:** read the model files end-to-end; traced three representative items with the prebuilt CLI
> (`dotnet …/cobol.dll <src> --std 2002 -o <out> --run`) and inspected the emitted `<out>.g.cs`.
> **Hard invariants checked against:** typed-native only (byte[] confined to a genuine Tier-C / file boundary);
> spec-first (ISO/IEC 1989:2023); one canonical mechanism per job; no god classes.

---

## 0. Bottom line up front

- **One value can be materialised five structurally different ways for a *numeric* item**, and — for the pivotal
  case — *which one* is decided **late** (after PROCEDURE-DIVISION binding) and realised by **mutating a public
  `bool` from ~11 sites across all three layers**, with CodeGen writing back into the Binding data model.
- Counting the whole model there are **~10 physical storage forms** (below), but only the numeric family has the
  *competing-late-choice* pathology. **"Packed/binary" is NOT a distinct at-rest representation** — a `COMP-3` and a
  `DISPLAY` numeric are the *same* native `long`; the usage difference is folded into `NumProfile.Truncation` +
  the on-demand character/byte image (confirmed by trace 1).
- **Unification is feasible and correct — but only at the *classification/decision* layer, not the *physical* layer.**
  The right target is exactly what `DESIGN-data-model.md` proposes: one closed `StorageForm` discriminator computed
  **once** by one pass, read by everyone, replacing the late-mutated flag. The multiple physical forms are intrinsic
  (they exist for real §14.9 GR4 / §13.18.44 reasons) and should stay; the *defect* is the scattered late decision,
  not the plurality.
- **The roadmap (`DESIGN-data-model.md` + `PHASE-05-…md`) is strong and largely complete.** The gaps are narrow and
  listed in §9: (1) the OO override-harmonize *pairwise cross-unit* reconciliation is assumed to become a declarative
  per-item rule but is under-specified and is the single hardest parity risk; (2) the sanctioned Tier-C `byte[]`
  boundary — invariant #1's own escape hatch — stays *unimplemented*, so the most representative real mixed-usage
  REDEFINES still has no working representation; (3) the `Place`-from-`StorageForm` mapping is incomplete and the
  model does not distinguish "has a value representation" from "gets a physical field" (phantom items).

---

## 1. Responsibilities & pipeline place

| Layer | File(s) | Responsibility for the value model |
|---|---|---|
| **Bind — declared shape** | `Binding/PicInfo.cs` | `PicInfo.Analyze` (230 lines, `:333–562`) turns a PICTURE+USAGE into category / digits / scale / sign; `ClrType` (`:222`) maps it to a C# storage type. |
| **Bind — item tree** | `Binding/DataItem.cs` | The record-tree node. Holds the *declared* shape **and** ~10 pass-computed facts, incl. the pivotal mutable `StoreAsImage` (`:169`) and the recursive image predicates `IsCharacterImage`/`IsImageCapable`/`ImageWidth` (`:245–300`). Projects `ElementType`/`FieldType`/`ClrType` (`:304–314`). |
| **Bind — overlay model** | `Binding/RedefinesModel.cs`, `OdoModel.cs` | `RedefinesClass` + the A/B/C/D `RedefinesTier` lattice (`RedefinesModel.cs:37–53`); ODO group-image slicing (`OdoModel.cs`). |
| **Bind — lvalue** | `Binding/Place.cs`, `ReferenceResolver.cs` | The ONE lvalue contract `Place` (`Read()`/`Write()`), and the resolver that picks the concrete `Place` subtype per item. |
| **Emit — fields & codec** | `CodeGen/Emit/FieldEmitter.cs` | Emits the C# fields, the per-numeric `NumProfile`, and the group `AsImage()`/`FromImage()` record codec. |
| **Emit — value text** | `CodeGen/Emit/NumericRenderer.cs`, `OperandText.cs` | Render a bound operand to a scaled-integer expression (numeric) or a character-image string. |
| **Runtime** | `Runtime/Numeric/{CobolNum,NumProfile}.cs`, `Runtime/Text/CobolString.cs` | The value engine: scale/round/truncate (`CobolNum`), the runtime numeric profile (`NumProfile`), the fixed-width string substrate (`CobolString`). |

**Pipeline place of the decision.** The declared storage type is fixed early (PICTURE analysis → `PicInfo.ClrType`).
But the *character-image promotion* — the numeric→string flip — is decided **last**, from the whole-group-use set
collected *during* procedure binding, and is applied both inside the binder and by a CodeGen pass that **writes back
into the Binding model** (`CSharpEmitter.MarkStoreAsImage`, `CSharpEmitter.cs:50–68`, driven from
`ReferenceResolver`'s mid-resolve mutation of `WholeGroupReferenced`, `ReferenceResolver.cs:280,303`). *There is no
single owning phase for the value representation.*

---

## 2. Trace A — signed packed numeric `PIC S9(5)V99 COMP-3`

Source (`trace1_packed.cob`): `01 WS-PACKED PIC S9(5)V99 COMP-3 VALUE -123.45.` `ADD 1.55`, `MOVE` to a DISPLAY twin.

**End-to-end path**

1. **Grammar/parse** → picture string `S9(5)V99`, usage keyword `COMP-3`.
2. **`PicInfo.Analyze` / `ParseUsage`** (`PicInfo.cs:625` → `Usage.Packed`; `:507–561`): `Category=Numeric`,
   `Digits=7`, `Scale=2`, `Signed=true`. `SignKindFor(Packed, signed, …)` → `"BinaryMinus"` (`PicInfo.cs:571`).
3. **`PicInfo.ClrType`** (`:242–247`): usage is not float, `Digits ≤ 18` ⇒ **`"long"`**. → `DataItem.ElementType`
   (`DataItem.cs:304`) = `"long"`. **The packed usage does not change the storage type.**
4. **`NumProfile` projection** (`PicInfo.ProfileInitializer`, `:301–316`): `Truncation = PackedDecimal`,
   `StorageLength = Digits/2+1 = 4`, `SignKind = BinaryMinus`.
5. **`FieldEmitter`** emits one native field + one static profile.
6. **`NumericRenderer` / `CobolNum`** do the arithmetic on the unscaled `long`; the packed-ness lives only in the
   profile's truncation discipline.

**Confirmed generated C# (`trace1.g.cs`):**

```csharp
internal static readonly NumProfile _P_0 = new NumProfile { Digits = 7, FractionDigits = 2, Signed = true,
    SignKind = NumericSign.BinaryMinus, Truncation = NumericTruncation.PackedDecimal, StorageLength = 4 };
private long WS_PACKED = -12345L;                        // ← packed item IS a native long
…
WS_PACKED = (long)(CobolNum.Store(((Int128)(WS_PACKED) + (155L)), 2, _P_0, CobolRounding.Truncation));
System.Console.WriteLine("PACKED=" + CobolNum.FormatDisplay(WS_PACKED, _P_0));
```

Run output: `PACKED=-0012190` (BinaryMinus display), `DISP=001219}` (the DISPLAY twin's trailing-overpunch of the
same value).

**Finding.** Packed **is not** a distinct value representation. `long`/`Int128` is *the* fixed-point carrier for
DISPLAY, COMP, COMP-3, COMP-5, and the BINARY-CHAR family alike (`PicInfo.ClrType` `:242–247`; `CobolNum` remark
`:19–21`). The *only* place the packed nature ever materialises as bytes is the file/record codec (Tier-C /
`AsImage` — a §13.18.60 GR4 implementor-defined zoned image, `PicInfo.ImageSignKind` `:153`). So the task's candidate
"packed/binary" collapses into candidates "native long/Int128" + "the AsImage/FromImage codec." **This is a point in
the model's favour** and the design's `StorageForm.NativeInt` correctly keeps them one case.

---

## 3. Trace B — group with `OCCURS` + a `REDEFINES` view

Source (`trace2_redef.cob`):
```
01 WS-GRP.  05 WS-ROW OCCURS 3.  10 WS-A PIC X(2).  10 WS-N PIC 9(3).
01 WS-VIEW REDEFINES WS-GRP PIC X(15).
```

**End-to-end path**

1. **Bind** builds `WS-GRP` (group, 3× `WS-ROW`, each `WS-A` string + `WS-N` numeric-DISPLAY) and `WS-VIEW`
   (redefining alphanumeric).
2. **RedefinesClassifier** puts both in one `RedefinesClass`. The whole class is USAGE DISPLAY ⇒
   **`RedefinesTier.StringCanonical`** (Tier-B, `RedefinesModel.cs:44–46`). The class collapses to **one `string`
   backing** `_redef_WS_GRP` of width 15 (`RedefinesClass.BackingCsName` `:76`; `FieldEmitter.BuildPhysicals`
   `:103–110`).
3. **The numeric leaf `WS-N` is force-promoted to a character image** — a Tier-B numeric member must store its zoned
   image, `DataBinder.cs:1562/1577` sets `StoreAsImage = true`.
4. **`ReferenceResolver.PlaceForItem`** (`:255–281`) builds a **`RedefViewPlace`** for every member: a
   `(offset,width)` window over `_redef_WS_GRP` (`Place.cs:82–102`). Subscripted access adds
   `(idx−1)·stride` to the class offset (`:271–273`).
5. **`OperandText` / `NumericRenderer`** read a Tier-B view's `Read()` directly (it is already a string window,
   `OperandText.cs:68`); numeric use decodes via `CobolNum.ParseDisplay` (`NumericRenderer.cs:98`).

**Confirmed generated C# (`trace2.g.cs`):**

```csharp
private string _redef_WS_GRP = CobolString.Store(
    (CobolString.Repeat((new string(' ', 2) + CobolNum.FormatDisplay(0L, _P_3)), 3)), 15);   // ONE backing string
…
_redef_WS_GRP = CobolString.SpliceInto(_redef_WS_GRP, (int)(0 + 1), 15, CobolString.Store("AB123CD456EF789", 15));
"ROW2-A=" + CobolString.RefMod(_redef_WS_GRP, (int)(0 + (2 - 1) * 5 + 1), 2)   // WS-A(2) window
"ROW2-N=" + CobolString.RefMod(_redef_WS_GRP, (int)(2 + (2 - 1) * 5 + 1), 3)   // WS-N(2) window
```

Run output: `ROW2-A=CD`, `ROW2-N=456`, `VIEW=AB123CD456EF789` — correct.

**Findings.**
- Tier-B works and is coherent: **one stored backing, N typed windows** — a genuine single-mechanism win (invariant
  respected: no byte[]).
- **Smell (concrete):** the group's `record struct` types are **still emitted but dead**. `trace2.g.cs` contains
  `_T_0`/`_T_1` with `AsImage() => ""` / empty `FromImage`, never instantiated (the class became the string backing).
  `FieldEmitter.EmitStructTypeDecls` (`:187`) emits struct types by group regardless of whether the group is
  subsumed by a Tier-B class. Harmless output bloat, but it shows the field-vs-view decision is *not* threaded into
  type emission.
- **The offset arithmetic is assembled as emit-time C# text at bind time** (`Place.cs:94`,
  `ReferenceResolver.cs:271–273`) — `"CobolString.RefMod(_redef_WS_GRP, (int)(0 + (2 - 1) * 5 + 1), 2)"`. A
  bind/emit-boundary violation (the binder writes C#), already flagged for later phases (DESIGN §2.2 item 4, Q3).

---

## 4. Trace C — whole-group MOVE (the pivotal `StoreAsImage` case)

Source (`trace3_wholegroup.cob`): two groups each `05 …N PIC 9(3)` (numeric) + `05 …X PIC X(4)`; `MOVE WS-SRC TO
WS-DST`; then `MOVE SPACES TO WS-SRC`.

**End-to-end path**

1. **`ReferenceResolver`** resolves `WS-SRC`/`WS-DST` as whole-group operands → both added to
   `WholeGroupReferenced` (`ReferenceResolver.cs:303`).
2. **Late promotion.** After procedure binding, `CSharpEmitter.MarkStoreAsImage` (`CSharpEmitter.cs:50–68`) walks
   every numeric-DISPLAY leaf under a whole-group-referenced group and flips `StoreAsImage = true`. This is
   **CodeGen mutating the Binding data model.**
3. **`DataItem.ElementType`** (`:304`) then returns `"string"` for `WS-SN`/`WS-DN` instead of `"long"`.
4. **`FieldEmitter`** emits the group `AsImage()`/`FromImage()` codec (`:207–221`); the MOVE becomes
   `dst.FromImage(src.AsImage())`.

**Confirmed generated C# (`trace3.g.cs`):**

```csharp
private record struct _T_0 {
    public string WS_SN;   // ← numeric leaf promoted to string
    public string WS_SX;
    public readonly string AsImage() => WS_SN + WS_SX;
    public void FromImage(string __s) { __s = CobolString.Store(__s, 7); WS_SN = __s.Substring(0,3); WS_SX = __s.Substring(3,4); }
}
private _T_0 WS_SRC = new _T_0 { WS_SN = CobolNum.FormatDisplay(42L, _P_1), WS_SX = CobolString.Store("WXYZ", 4) };
…
WS_DST.FromImage(CobolString.Store(WS_SRC.AsImage(), 7));   // whole-group MOVE via the codec
WS_SRC.FromImage(new string(' ', 7));                      // MOVE SPACES — the numeric leaf now holds spaces
"SRC-N-AFTER-SPACES=[" + WS_SRC.WS_SN + "]"
```

Run output: `DST-N=042`, `DST-X=WXYZ`, `SRC-N-AFTER-SPACES=[   ]` — the numeric leaf legitimately holds spaces
(exactly why the promotion exists: §14.9 MOVE GR4 fills a group "without consideration for the individual
elementary items"). The **same source** compiled without the whole-group use would keep `WS_SN` a `long` (trace 1
shows the un-promoted shape).

**Finding.** This is the highest-risk defect. The *same* numeric leaf is `long` in one program and `string` in
another, decided by a fact only known after procedure binding, applied by mutation from three layers, and reconciled
by C# overload resolution at Roslyn time (see §6).

---

## 5. The enumeration — how many representations of "a value" exist

### 5.1 Physical storage forms (what a C# field can *be*)

| # | Form | C# realisation | Chosen by | Site |
|---|---|---|---|---|
| 1 | **Native `long`** (unscaled scaled-integer, ≤18 digits) | `private long X` | PICTURE analysis (early) | `PicInfo.cs:246`, `DataItem.cs:304` |
| 2 | **Native `Int128`** (19–38 digits) | `private Int128 X` | PICTURE analysis (early) | `PicInfo.cs:246` (`Digits>18`) |
| 3 | **Native `float`** (COMP-1/FLOAT-SHORT) | `private float X` | USAGE analysis (early) | `PicInfo.cs:244` |
| 4 | **Native `double`** (COMP-2/FLOAT-LONG/-EXTENDED) | `private double X` | USAGE analysis (early) | `PicInfo.cs:245` |
| 5 | **Fixed-width `string` image** — natural (alphanumeric/edited/national/boolean) OR **`StoreAsImage`-promoted numeric** | `private string X` | natural: early; **promoted: LATE + mutated** | `DataItem.cs:304`, `StoreAsImage :169` |
| 6 | **Tier-B window** over one shared `string` backing (REDEFINES class B) | `_redef_X` string + `CobolString.RefMod/SpliceInto` windows | RedefinesClassifier pass | `RedefinesModel.cs:45`, `Place.cs:82` |
| 7 | **Tier-C window** over one shared `byte[]` (mixed-USAGE REDEFINES) | *declared, unimplemented — rejected loud* | RedefinesClassifier pass | `RedefinesModel.cs:48` |
| 8 | **Out-of-line `CobolDynTable<T>`** (OCCURS DYNAMIC) | `private CobolDynTable<T> X` | OCCURS analysis (early) | `DataItem.cs:309` |
| 9 | **`ManagedPointer`** (USAGE POINTER) | `private ManagedPointer X` | USAGE analysis (early) | `PicInfo.cs:234` |
| 10 | **Object reference** (`CobolObject?` / typed class) | reference field | USAGE analysis (early) | `PicInfo.cs:230` |

Plus INDEX cells (a `long`, folded into #1) and the CAPACITY register (a *phantom* — a `PicInfo`-bearing `DataItem`
with **no field**, its value IS `CobolDynTable.Capacity`; `OdoModel.cs:455–464`).

### 5.2 The transient serialization form (not storage, but a representation the value passes through)

- **`AsImage()` / `FromImage()` group codec** (`FieldEmitter.cs:207–261`): a group's value as one flat character
  string, with each native fixed-point leaf encoded on demand via `CobolNum.FormatDisplay` (trailing-overpunch image
  per §13.18.60 GR4) and decoded via `CobolNum.ParseDisplay`. Reached by whole-group MOVE/compare/DISPLAY/WRITE/
  RELEASE and the FD/SD record codec. This is the `AsImage/FromImage codec` in the task's candidate list — a *sixth*
  face a numeric value wears, distinct from its at-rest form.

### 5.3 The crux: how many faces does **one numeric item's value** wear?

**Five**, reachable depending on context, all bridged by overload resolution / ParseDisplay-FormatDisplay:

1. native `long`/`Int128` field (default; incl. packed & binary — §2);
2. zoned-image `string` field (`StoreAsImage` promotion; §4);
3. a substring window in a shared string (Tier-B REDEFINES / ref-mod / RENAMES span — `RedefViewPlace`,
   `NumericImagePlace`, `RenamesPlace`, `RefModPlace`);
4. a slice of a group's `AsImage()` on the fly (whole-group ops; §5.2);
5. (float sibling) native IEEE `double` when a float operand is present (`NumericRenderer.cs:102`).

The `Place` layer already smooths most of this — `NumericImagePlace.Read/Write` (`Place.cs:147–165`) is explicitly a
"storage-form bridge" whose remark admits the form is *"decided … AFTER this expression text is produced"*.

### 5.4 `StoreAsImage` — the pivotal flag: read/write site census

Grepped over `src/Cobol.Net.Compiler`:

- **Declaration:** `DataItem.cs:169` — `public bool StoreAsImage { get; set; }` (public mutable).
- **Write sites (≈11, across all three layers):**
  - Binder *data* pass (6): `DataBinder.cs:255`, `:1562`, `:1577`; `DataBinder.Linkage.cs:275`;
    `DataBinder.Reports.cs:352`; `DataBinder.Oo.cs:373` (compiler-temp clone init).
  - Binder *procedure* pass (2): `StatementBinder.MoveFigurative.cs:123`, `:262`.
  - **CodeGen** (3): `CSharpEmitter.cs:65` (`MarkStoreAsImage`); `CSharpEmitter.Call.cs:118` (compiler-temp re-sync);
    `CSharpEmitter.Oo.cs:697` (OO override-harmonize re-sync).
- **Direct read sites (≈29):** `ReferenceResolver.cs:175,201`; `DataItem.cs:254,304`; `OperandText.cs:87`;
  `NumericRenderer.cs:75,105`; `FieldEmitter.cs:342,353,388,395`; `CSharpEmitter.cs:531,574,664,803,1119,1123`;
  `CSharpEmitter.Accept.cs:66,127`; `CSharpEmitter.StringUnstring.cs:154,160,211`; `CSharpEmitter.Inspect.cs:100`;
  `CSharpEmitter.Call.cs:530,940`; `CSharpEmitter.Oo.cs:662,821`; `CSharpEmitter.Sort.cs:225`.
- **Transitive readers:** every consumer of the derived props it feeds — `IsCharacterImage` (`DataItem.cs:245`),
  `IsImageCapable` (`:270`), `ImageWidth` (`:283`), `ElementType`/`FieldType` (`:304–311`) — recomputed at ~119 sites
  (DESIGN §1.1.3), each an O(subtree) walk silently sensitive to the last flip.

### 5.5 Can they be unified? — Yes, at the decision layer

- Forms 1–2 already unify (one axis, the `Wide` flag).
- Forms 5 (promoted) / 6 (Tier-B) / §5.2 (codec) are *physically distinct on purpose* (spec-mandated character-image
  behavior); collapsing them to one runtime carrier would be **wrong**, not cleaner.
- What CAN and SHOULD unify is the **decision**: replace the late-mutated `StoreAsImage` bool + the
  `(Pic, StoreAsImage, Class.Tier, IsDynamicTable)` scatter with **one closed discriminator computed once, read by
  all** — i.e. `StorageForm` (DESIGN §2.1). That removes the cross-layer write-back and the overload-resolution
  reconciliation while keeping the physical plurality. **Feasible; it is precisely the roadmap.**

---

## 6. Architecture smells (severity · file:line)

| Sev | Smell | Evidence |
|---|---|---|
| **CRITICAL** | **Value representation has no owning phase; decided by late mutation of a public bool from 3 layers, incl. CodeGen writing the Binding model.** | `CSharpEmitter.MarkStoreAsImage` `CSharpEmitter.cs:50–68`; ~11 write sites (§5.4); `ReferenceResolver` mutating `WholeGroupReferenced` mid-resolve `:280,303`. |
| **CRITICAL** | **Correctness reconciled by C# overload resolution — a representation bug is invisible until generated C# hits Roslyn.** The field type is undecided when the binder emits reference text, so the runtime carries polymorphic overloads so the SAME text compiles either way. | `CobolNum.FormatDisplay(Int128,…)` `:212` / `(string,…)` `:220`; `CobolNum.StoreDisplay(…,long/Int128/string)` `:227,231,235`; `NumericImagePlace` remark `Place.cs:156–159`. |
| **HIGH** | **`DataItem` is a mutable god-node** — declared shape + ~10 pass-computed `set`/`init` facts with no phase guard (`StoreAsImage`, `RedefinesTarget`, `Class`, `ClassOffset`, `IsCanonical`, `TypeName`, `StrongType`, `Uid`, `IsBased`). Validity is phase-dependent, unenforced. | `DataItem.cs:23,169,193,200,204,209,213,60,64,68`. |
| **HIGH** | **Recompute-on-read.** `IsCharacterImage`/`IsImageCapable`/`ImageWidth`/`StrongRoot` are recursive computed props re-walked at ~119 sites, each silently sensitive to the last `StoreAsImage` flip. | `DataItem.cs:74–83,245–300`. |
| **HIGH** | **Duplicated physical-width geometry that *must* agree.** `DataItem.ImageWidth` (`:283`), `OdoModel.PhysicalWidth` (`OdoModel.cs:155`, comment "mirrors FieldEmitter's physical layout"), `FieldEmitter.PhysicalImageWidth` (`:134`) each compute offsets/widths independently. | 3+ copies; drift hazard. |
| **HIGH** | **Tier-C declared but unimplemented** — the ONE sanctioned byte[] boundary is rejected loud; genuine mixed-USAGE REDEFINES type-punning has *no* working representation. | `RedefinesModel.cs:48`; ~10 scattered deferral guards (DESIGN §1.2). |
| **MED** | **Implicit pass ordering.** `DataBinder.BindResolve` orders ~15 passes by call sequence + comments; the *real* middle-end passes (incl. `MarkStoreAsImage`, `StorageForm` decision) are hidden inside `CSharpEmitter.CallEmitRunUnit`. No pass declares Requires/Produces. | `DataBinder.cs:210–232`; `CSharpEmitter.Call.cs:111–120`. |
| **MED** | **Dead struct-type emission.** A group subsumed by a Tier-B string backing still emits its `record struct` type (empty `AsImage()`/`FromImage`), never instantiated. | trace 2 `_T_0`/`_T_1`; `FieldEmitter.EmitStructTypeDecls:187`. |
| **MED** | **`PicInfo` is not a pure value record** — a 230-line `Analyze` scanner (`:333–562`) + dead skeleton scaffolding (`IsUnimplementedSkeleton => false` `:175`, `SkeletonReached` `:216`, three `ReferenceEquals` sentinel singletons `:691–705`). | `PicInfo.cs`. |
| **LOW** | **Identical `NumProfile`s not interned** — one static field per `Uid` even when byte-identical. | trace 3 `_P_1`/`_P_4` are identical; `ProfileName => "_P_"+Uid` `DataItem.cs:233`. |
| **LOW** | **Binder assembles emit-time C# strings** — `Place.Path`/`OffsetExpr` are raw C# text built at bind time. | `Place.cs:94`; `ReferenceResolver.cs:271–273`. |
| **LOW** | **Latent bug (confirmed by design):** apostrophe-delimited VALUE literals silently miscompile — the decode guards hard-code the `"` delimiter. | `FieldEmitter.cs:331`; DESIGN §2.8. |

---

## 7. Coupling / public mutable state / cross-layer reach

- **CodeGen → Binding write-back (the worst edge).** `CSharpEmitter.MarkStoreAsImage` (`CSharpEmitter.cs:50–68`),
  the compiler-temp re-sync (`CSharpEmitter.Call.cs:111–120`) and the OO harmonize re-sync
  (`CSharpEmitter.Oo.cs:694–697`) all **mutate `DataItem` from the emitter**. Emit correctness is therefore
  order-dependent on a bind-time side effect. Invariant "one canonical mechanism / no god class" is violated at the
  layer boundary itself.
- **`ReferenceResolver` mutates shared bind state mid-resolve.** `data.WholeGroupReferenced.Add(item)` inside
  `PlaceForItem` (`ReferenceResolver.cs:280,303`) — resolution has a side effect that a *later* pass depends on.
- **`DataBinder`'s ~9 public mutable collections** are an unencapsulated blackboard (DESIGN §1.2 / table row).
- **Runtime overload surface is load-bearing coupling, not convenience.** The `Occ`/`StoreDisplay`/`FormatDisplay`
  triplets (`CobolNum.cs:212–235`) exist *because* the compiler can't decide the field type before emitting text.
  They couple a representation decision to the C# type system rather than to a compiler pass.
- **Reach:** a single fact (`StoreAsImage`) is written in Binding (data + procedure passes) and CodeGen, and read in
  Binding, CodeGen/Emit, and (transitively) the Runtime overload set — a fact that touches all three layers with no
  owner.

---

## 8. Latent-bug risks

1. **Overload-resolution silent miscompile.** If a promoted-vs-native mismatch slips through (e.g. a new verb path
   emits `x.Read()` expecting a `long` but the field became a `string`), the failure is a *wrong overload*, surfacing
   as a Roslyn compile error or — worse — a silently-different conversion, not a bind diagnostic. The
   `NumericImagePlace` remark (`Place.cs:156–159`) is an admission of this hazard.
2. **Promotion-parity across the 11 write sites.** The whole-group promotion must fire identically from
   MoveFigurative, Linkage, Reports, OO clone/harmonize, and compiler-temps. A missed site = a numeric leaf that
   *should* be a string stays a `long` and cannot hold the spaces a group MOVE deposits → wrong output (the exact
   trace-3 behavior would regress).
3. **OO override-harmonize is a *pairwise cross-unit* repair** (`CSharpEmitter.Oo.cs:694–697`): given a caller/callee
   formal pair where one side got promoted, it picks the native side and flips it to match. This is not a per-item
   property — it is a reconciliation between two independently-bound units, run as a post-pass *repair loop*.
4. **Width-geometry drift.** Three independent width computations must agree (`DataItem.ImageWidth`,
   `OdoModel.PhysicalWidth`, `FieldEmitter`); an off-by-one in one produces a mis-sliced `FromImage` — a data
   corruption that passes bind and compiles clean.
5. **Phantom-item ambiguity.** The CAPACITY register and RENAMES alias carry a `PicInfo` (so a naive
   `StorageForm`-from-`Pic` would classify them `NativeInt`/`CharImage`) yet have **no field**. A pass that assumes
   "has a storage form ⇒ has a field" would emit a phantom field or read a nonexistent one.

---

## 9. Reorg / unification suggestions

These converge with `DESIGN-data-model.md`; where I add to it, it is flagged **[beyond roadmap]**.

1. **Adopt `StorageForm` (DESIGN §2.1) — the correct unification.** One closed discriminator, computed **once** by a
   `StorageFormPass` that runs after whole-group use is known, stored **init-only**. Delete `StoreAsImage` + the
   `MarkStoreAsImage` write-back. This directly kills smells CRITICAL-1/CRITICAL-2/HIGH-god-node.
2. **Move the whole-group-use collection out of `ReferenceResolver` into a `UsageCollectionPass` over the bound tree**
   (DESIGN §2.5 step 9). Removes the mid-resolve mutation.
3. **One `RecordLayout` width authority** (DESIGN §2.6) — deletes the 3+ divergent geometry copies.
4. **Slim `DataItem`; freeze facts per pass** (DESIGN §2.4) + explicit `IBindPass` DAG (DESIGN §2.5). Kills implicit
   ordering.
5. **[beyond roadmap] Prune dead struct types.** `StorageFormPass` already knows a Tier-B-subsumed group needs no
   `record struct`; gate `FieldEmitter.EmitStructTypeDecls` on that so trace-2's empty `_T_0`/`_T_1` disappear.
6. **[beyond roadmap] Intern identical `NumProfile`s** (or key by structural identity, not `Uid`) — removes the
   trace-3 `_P_1`/`_P_4` duplication.

---

## ROADMAP GAP CHECK

I read `DESIGN-data-model.md` and `PHASE-05-unified-data-model-storageform.md` against everything above. **The plan
is strong and its diagnosis matches this survey almost exactly** — it names the pivotal `StoreAsImage` defect, the
cross-layer write-back, the overload-resolution reconciliation, the recompute-on-read, the width duplication, the
Tier-C hole, the `PicInfo` scaffolding, and the apostrophe latent bug, and its `StorageForm`/`StorageFormPass`/
`RecordLayout`/pass-DAG solution is the right unification (decision-layer, not physical-layer — matching my §5.5
conclusion). The migration is genuinely safe (parallel-SSOT + corpus equivalence gate before any deletion). The
following are the concrete gaps / corrections the roadmap should address:

1. **[Gap — highest] The OO override-harmonize reconciliation is under-specified.** PHASE-05 Step 7.2 and DESIGN §5.1
   say the OO harmonize *"decision … moves into StorageFormPass as a rule"* and *"the compute-then-repair loop is
   deleted."* But `CSharpEmitter.Oo.cs:694–697` is not a per-item property — it is a **pairwise reconciliation across
   two independently-bound units** (it inspects `a.StoreAsImage` vs `b.StoreAsImage` for a caller/callee formal pair
   and flips the native one). A single per-item `StorageFormPass` producing `Storage` bottom-up does not obviously
   have both units' whole-group facts available, nor a place to express "these two formals must agree." The design
   should specify **how** cross-unit formal agreement becomes declarative — e.g. a distinct `OoFormalHarmonizePass`
   that runs after every unit's `UsageCollectionPass` but before `StorageFormPass`, contributing to the
   whole-group-referenced set of *both* sides — otherwise the promised deletion of the repair loop will either
   regress OO differential tests or quietly re-introduce a mutation. This is the single hardest parity risk and it is
   currently one sentence.

2. **[Gap] Tier-C `byte[]` — the sanctioned byte boundary — stays unimplemented, so the "unified" model still cannot
   represent the most representative real REDEFINES.** Both docs (DESIGN §2.3 / §6 Q1; PHASE-05 scope) choose
   "single-source the *rejection* now, implement the codec in P11." That is a reasonable sequencing, but the roadmap
   should be explicit that **after Phase 5 the model is *not* representationally complete** — a `COMP`/`COMP-3` leaf
   redefined as `PIC X` (an extremely common production idiom) is still a loud reject. Recommend the roadmap either
   (a) pull a *minimal* Tier-C read/write codec forward so `StorageForm.TierCWindow` is a live case the unified model
   can actually exercise (proving the discriminator generalises past string-only overlays), or (b) state plainly in
   the exit criteria that Tier-C remains quarantined and track the residual REDEFINES coverage gap explicitly. As
   written, "unified data model" over-claims while invariant #1's own escape hatch is empty.

3. **[Gap] The `Place`-from-`StorageForm` mapping is incomplete, and the model does not distinguish "has a value
   representation" from "gets a physical field."** DESIGN §2.2 item 2 maps only 5 shapes
   (`NativeInt`/`NativeFloat`/`CharImage`/`TierBWindow`/`DynamicTable` → their places) but the resolver builds **nine**
   `Place` subtypes — `OdoGroupPlace`, `RenamesPlace`, `RefModPlace`, `NumericImagePlace`, `CapacityRegisterPlace` are
   unmapped. In particular: the **CAPACITY register** and **level-66 RENAMES alias** are `DataItem`s that carry a
   `PicInfo` (so they'd naively get a `StorageForm`) but have **no field** (`OdoModel.cs:455–464`; RENAMES adds no
   storage). `StorageForm` as designed conflates "value representation" with "field emission." The roadmap should add
   either a `StorageForm` case or an explicit `HasField`/phantom predicate so `FieldEmitter` and `StorageFormPass`
   agree on which items are physical — otherwise a phantom field or a read of a nonexistent field is a latent
   StorageFormPass bug (my §8.5).

4. **[Minor correction] The equivalence gate proves parity only where the conformance corpus exercises the path.**
   PHASE-05 Step 2/6 assert `StorageForm`≡`StoreAsImage` over the 2028-test corpus. That is sound for the common
   paths, but the OO-harmonize (#1), compiler-temp clone, and Report-Writer promotion sites are only proven if the
   corpus covers them. The roadmap lists sentinels (`OoSpineTests`, whole-group/ODO/SORT goldens) — good — but should
   add an **explicit targeted fixture per write-site** (a program that forces each of the 11 promotion origins) so
   parity is proven by construction, not by corpus coincidence.

5. **[Minor addition] Dead struct-type pruning and `NumProfile` interning** (my §9.5/§9.6) are not in scope. Both are
   pure emit-size wins that `StorageFormPass` newly makes trivial (it knows Tier-B-subsumed groups and can key
   profiles structurally). Worth folding into Phase 5's "cached image facts" step or noting as a fast-follow — not a
   correctness gap, but the survey confirmed both in generated output (trace 2 `_T_0/_T_1`, trace 3 `_P_1/_P_4`).

**Net:** the roadmap's central thesis (one computed `StorageForm`, decided once, read by all) is the right and
sufficient unification for the pivotal defect, and the migration is safe. The material risk is concentrated in gap
#1 (OO cross-unit harmonize as a declarative rule) and the honesty gap #2 (Tier-C still empty). Neither invalidates
the plan; both should be written into the design before execution.
