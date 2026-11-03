// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Binding.Model;
using WiseOwl.Cobol.Compiler.Oo;

namespace WiseOwl.Cobol.Binding;

/// <summary>
/// The compiler-synthesized TEMPORARY items of one <see cref="DataBinder"/> (docs/rearchitecture/DESIGN-architecture-review.md §8.3
/// (1); kb/Work PB2360): the level-1 items the procedure binders mint — a property reference's, an inline method invocation's, a
/// user function's result, an object view's, SELF's, a sending value's — each CLONED from a model item's description and
/// appended to the binder's roots so the FieldEmitter declares it like any other item. <see cref="Create"/> is THE ONE
/// constructor; the <c>For…</c> members name the object-reference families that go through it.
/// </summary>
internal sealed class CompilerTemps(DataBinder data)
{
    /// <summary>Synthesize the GR1/GR2/GR3 compiler temp for one property reference: a level-1 elementary
    /// item CLONED from the accessor's crossing description (<paramref name="model"/> = the GET RETURNING
    /// item or the SET formal — identical by the §13.18.42 clone rule / the 0842 §11.7.3 SR7 single-USING accessor
    /// check). One temp per REFERENCE (GR1 temp-1 / GR2 temp-2; GR3 reuses one — the caller decides).</summary>
    internal DataItem ForProperty(DataItem model, string prop) =>
        Create(model, "__PROP-TEMP-", "__prop", prop);

    /// <summary>Synthesize the §8.4.3.4.4 GR1 b)/c) temporary of one INLINE METHOD INVOCATION (kb/Work
    /// PB428): "temp-identifier has the same description, class, and category as the RETURNING parameter in
    /// the specification of the method identified by literal-1", and it "is a temporary item that exists for
    /// the purpose of effecting the inline invocation in this way and for no other purpose". <paramref
    /// name="model"/> is that RETURNING item, so the delivery into the temp is an IDENTITY crossing —
    /// which is why the §14.8.3.3 conformance check the written-RETURNING form runs has nothing to say here.
    /// The SAME <see cref="Create"/> the property and user-function temps use: one temp
    /// constructor, three clients.</summary>
    internal DataItem ForInvocation(DataItem model, string method) =>
        Create(model, "__INV-TEMP-", "__inv", method);

    /// <summary>Synthesize the temporary an OBJECT-VIEW references (§8.4.3.5.4 GR1, kb/Work PB1425): "This reference
    /// of identifier-1 is treated at compile-time as though it had the description specified by the AS phrase", so the
    /// temporary is a USAGE OBJECT REFERENCE item described <paramref name="view"/>.</summary>
    internal DataItem ForObjectView(ObjectRefDescriptor view) =>
        ForObjectReference(view, "__VIEW-TEMP-", "__view");

    /// <summary>Synthesize the temporary SELF references when it is written as an operand (kb/Work PB1425): §8.4.3.8.3
    /// SR7 makes SELF class object and category object reference, not universal, and §8.4.3.8.4 GR1 the object the
    /// method was invoked on — the description ACTIVE-CLASS spells (§13.18.60.4 GR22 e)), FACTORY OF in a factory
    /// method.</summary>
    internal DataItem ForSelf(string containingClass, bool factory) =>
        ForObjectReference(ObjectRefDescriptor.ActiveClass(containingClass, factory), "__SELF-TEMP-", "__self");

    /// <summary>Synthesize the temporary that holds the object a RECEIVING object-property reference identified (kb/Work
    /// PB2078): identifier-3 is evaluated into it once, when the statement identifies the receiver, and the property's GET
    /// and SET both reach that object (§8.4.3.9.4 GR3 — one temporary, one property of one object), however the
    /// statement changes identifier-3's subscripts in between. Described as identifier-3 is.</summary>
    internal DataItem ForIdentifiedObject(ObjectRefDescriptor description) =>
        ForObjectReference(description, "__OBJ-TEMP-", "__obj");

    /// <summary>The ONE object-reference temporary: a USAGE OBJECT REFERENCE item described
    /// <paramref name="description"/>, the description every object-reference rule then asks of it. The same
    /// <see cref="Create"/> as the other temporaries; each client names its own fixed family.</summary>
    private DataItem ForObjectReference(ObjectRefDescriptor description, string cobolPrefix, string csPrefix)
    {
        var model = new DataItem
        {
            Level = 1, CobolName = "__OREF", ClrName = "__oref",
            Pic = PicInfo.ObjectReferenceItem(description),
        };
        return Create(model, cobolPrefix, csPrefix, description.Name ?? "UNIVERSAL");
    }

    /// <summary>The (temp, model) clone pairs <see cref="Create"/> produced — consumed by the
    /// run-unit emitter's post-bind re-sync: <c>StoreAsImage</c> is still MUTABLE while procedure bodies
    /// bind (a ref-mod store or a figurative MOVE inside the MODEL's own unit flips it AFTER a temp cloned
    /// it — the M2-UDF-1 review's unit-order desync), so the frozen copy must be re-read once every
    /// procedure has bound.</summary>
    internal List<(DataItem Temp, DataItem Model)> Clones { get; } = [];

    /// <summary>The ONE synthesized-compiler-temp constructor (property-reference temps, user-function result
    /// temps): a level-1 item cloned from <paramref name="model"/>'s description, appended to
    /// <see cref="ForestSlice.Roots"/> so the FieldEmitter declares it like any other item. A GROUP model deep-clones its
    /// subtree (the §8.4.3.2.4 GR1 "description … is that specified by the description in the linkage
    /// section" for a group RETURNING item) via <see cref="CloneNode"/> — structurally like the TYPEDEF
    /// <c>CloneItem</c> but UNREGISTERED: a temp's subordinates are never referenceable (a function result is
    /// only ever a whole sending operand, §8.4.3.2.3 SR1), and registering the callee's LINKAGE member names
    /// in the CALLER's scope would collide/ambiguate legal caller names. The pair is recorded for the
    /// post-bind <c>StoreAsImage</c> re-sync (see <see cref="Clones"/>); a group temp's
    /// numeric-DISPLAY leaves are promoted by the <c>UsageCollectionPass</c> whole-group collection instead
    /// (the temp is a <c>BoundCallProgram.Returning</c> whole-group operand).
    /// <para>⛔ THE DESCRIPTION IS NOT LISTED HERE (kb/Work PB888). §8.4.3.2.4 GR1 gives the temporary "the
    /// description, class, and category" of the RETURNING item — the whole entry, not a chosen subset — and
    /// this constructor used to spell five clauses of it (PICTURE, SIGN, JUSTIFIED, BLANK WHEN ZERO, plus USAGE
    /// on a subordinate), so GROUP-USAGE was lost and a <c>GROUP-USAGE NATIONAL</c> result measured 8 where
    /// the byte-identical working-storage item measures 4. Every clause now goes through
    /// <see cref="DataBinder.CopyDescriptionToTemp"/> in its <see cref="DescriptionCopyScope.CompilerTemp"/> scope, and
    /// <c>DescriptionCopyCompletenessDriftTests</c> fails if a hand copy reappears.</para></summary>
    internal DataItem Create(DataItem model, string cobolPrefix, string csPrefix, string tag)
    {
        var t = new DataItem
        {
            Level = 1,
            CobolName = cobolPrefix + data.NextUid,
            ClrName = csPrefix + data.NextUid + "_" + DataItem.WordIdentifier(tag),
            IsCompilerTemp = true,   // kb/Work R26 — the positive is-a-declared-item discrimination
            // (P5.7: the clone-time StoreAsImage seed is gone — StorageFormPass's promoted-set re-sync derives
            //  the temp's storage from its model's PRE-whole-group facts, the fused pipeline's re-sync ordering.)
        };
        DataBinder.CopyDescriptionToTemp(model, t);
        t.Uid = data.TakeUid();
        if (model.IsGroup)
            foreach (var child in model.Children)
                t.AddMember(CloneNode(child, t));
        data.State.Forest.AddRoot(t);
        Clones.Add((t, model));
        return t;
    }

    /// <summary>A compiler-temp TABLE (kb/Work PB4162): a level-1 group holding ONE table whose element is a clone of
    /// <paramref name="element"/>'s description - the held form of a table(ALL) function argument (<c>BoundHoldTableAll</c>). The
    /// table has <paramref name="occurs"/> occurrences, or - when that is null - is a dynamic-capacity table (§8.5.1.9) whose
    /// capacity the holder sets. It goes through <see cref="Create"/> like every other temporary, over a scratch group
    /// model whose one member is <see cref="CloneNode"/>'s clone of the element with this table's OCCURS clause in place of the
    /// element's own, so the description clauses are copied by the one <see cref="DataBinder.CopyDescriptionToTemp"/> and none is listed here.
    /// <paramref name="cell"/> is the table's member, the item an occurrence of it is addressed by; a dynamic table's CAPACITY
    /// register (the view <c>DynamicResolve</c> mints for every declared one, §13.18.38.4 GR15) is minted here the same way.</summary>
    internal DataItem CreateTable(DataItem element, int? occurs, string cobolPrefix, string csPrefix, string tag, out DataItem cell)
    {
        var model = new DataItem { Level = 1, CobolName = cobolPrefix + "MODEL", ClrName = csPrefix + "model" };
        // the element's own OCCURS clause belongs to the table it came from, not to this one
        (int? Occurs, OccursSpec? Spec) table = occurs is null
            ? (null, new OccursSpec { Min = 0, Max = 0, IsDynamic = true })
            : (occurs, null);
        model.AddMember(CloneNode(element, model, table));
        var temp = Create(model, cobolPrefix, csPrefix, tag);
        cell = temp.Children[0];
        if (cell.OccursSpec is { IsDynamic: true } spec)
            spec.CapacityRegister = new DataItem
            {
                Level = 49,
                ClrName = NamingConvention.CapacityRegisterName(cell.ClrName),
                Pic = PicInfo.BinaryItem(Usage.BinaryLong, signed: false),
                Parent = cell.Parent,
                Uid = data.TakeUid(),
            };
        Clones.Add((cell, element));   // the member's storage form follows the element's (StorageFormPass)
        return temp;
    }

    /// <summary>Deep-clone one description node under a compiler temp (see <see cref="Create"/>):
    /// fresh <see cref="DataItem.Uid"/> (StructName/ProfileName ride on it), the immutable
    /// <see cref="DataItem.Pic"/> shared, the <see cref="DataItem.ClrName"/> uniquified among siblings — and,
    /// unlike the TYPEDEF <c>CloneItem</c>, NOT registered (no by-name entry, no 88s, no index-names: a temp's
    /// subordinates are unreachable by reference). The initializer carries only identity and the
    /// <see cref="DescriptionCopyKind.MemberOnly"/> fields a subordinate reproduces (level, name, OCCURS); every
    /// data description CLAUSE goes through <see cref="DataBinder.CopyDescriptionToTemp"/>, the same ONE copy the TYPE /
    /// SAME AS clone uses, so the temp's subtree cannot lose a clause the model's has (kb/Work PB888 — it had
    /// lost GROUP-USAGE, SYNCHRONIZED and ALIGNED, the last two of which decide a group's slack bytes).</summary>
    /// <param name="table">When given, the clone carries this OCCURS clause in place of the source's own
    /// (<see cref="CreateTable"/>); a subordinate always keeps its own.</param>
    private DataItem CloneNode(DataItem src, DataItem newParent, (int? Occurs, OccursSpec? Spec)? table = null)
    {
        var clone = new DataItem
        {
            Level = src.Level,
            DeclaredAt = src.DeclaredAt,
            CobolName = src.CobolName,
            ClrName = src.ClrName,   // the base name: newParent.AddMember allocates it among the NEW siblings (PB2093)
            Occurs = table is { } t ? t.Occurs : src.Occurs,
            OccursSpec = table is { } u ? u.Spec : src.OccursSpec is { } os ? DataBinder.CloneOccursSpec(os) : null,
        };
        DataBinder.CopyDescriptionToTemp(src, clone);
        clone.Uid = data.TakeUid();
        clone.Parent = newParent;
        foreach (var child in src.Children)
            clone.AddMember(CloneNode(child, clone));
        return clone;
    }
}
