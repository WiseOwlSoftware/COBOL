// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Binding.Model;
using WiseOwl.Cobol.Binding.Passes;

namespace WiseOwl.Cobol.Binding;

/// <summary>The data binder and reference resolver of one bound source element: the binder-side working state the bound
/// model does not carry (kb/Work PB2361).</summary>
/// <param name="Data">The element's data binder.</param>
/// <param name="Refs">The element's reference resolver, made over <paramref name="Data"/>.</param>
internal sealed record ElementBinding(DataBinder Data, ReferenceResolver Refs);

/// <summary>
/// ⛔ THE BINDERS OF THE GROUP'S SOURCE ELEMENTS, KEPT ON THE BINDER'S SIDE OF THE SEAM (docs/rearchitecture/DESIGN-architecture-review.md
/// §8.3 (1), step 11; kb/Work PB2361). A <see cref="BoundUnit"/> and an <see cref="OoClassUnit"/> are the bound model a backend
/// reads, so they carry the frozen <see cref="DataBindResult"/> and never a binder; the binding phases, which need each element's
/// <see cref="DataBinder"/> and <see cref="ReferenceResolver"/> (a container's GLOBAL names, the repository's signatures, the
/// statement walk, the group tail), ask this table, which <see cref="BindSession.Binders"/> holds for the length of one bind.
/// A unit is added when its data division binds (<c>BinderDriver.BindUnitData</c>), a class's two halves when its data binds
/// (<c>OoDriver.BindClassData</c>, and for a parameterized skeleton <c>OoDriver.BindParameterized</c>).
/// </summary>
internal sealed class ElementBinders
{
    private readonly Dictionary<BoundUnit, ElementBinding> _units = new(ReferenceEqualityComparer.Instance);
    private readonly Dictionary<OoClassUnit, ElementBinding> _objects = new(ReferenceEqualityComparer.Instance);
    private readonly Dictionary<OoClassUnit, ElementBinding> _factories = new(ReferenceEqualityComparer.Instance);
    private readonly Dictionary<OoInterfaceSymbol, DataBinder> _interfaces = [];

    /// <summary>The data binder of each interface the group defines (its prototypes' formals, ISO §10.6.2 SR4), in the order
    /// they bound. An interface has no procedure division, so it has no resolver.</summary>
    public IReadOnlyDictionary<OoInterfaceSymbol, DataBinder> Interfaces => _interfaces;

    /// <summary>Record the data binder of <paramref name="iface"/> (once).</summary>
    public void AddInterface(OoInterfaceSymbol iface, DataBinder data) => _interfaces.Add(iface, data);

    /// <summary>Record <paramref name="unit"/>'s binders (once).</summary>
    public ElementBinding Add(BoundUnit unit, DataBinder data)
    {
        var binding = new ElementBinding(data, new ReferenceResolver(data));
        _units.Add(unit, binding);
        return binding;
    }

    /// <summary>Record the OBJECT half's binders of <paramref name="cls"/> (once).</summary>
    public ElementBinding AddObject(OoClassUnit cls, DataBinder data)
    {
        var binding = new ElementBinding(data, new ReferenceResolver(data));
        _objects.Add(cls, binding);
        return binding;
    }

    /// <summary>Record the FACTORY half's binders of <paramref name="cls"/> (once).</summary>
    public ElementBinding AddFactory(OoClassUnit cls, DataBinder data)
    {
        var binding = new ElementBinding(data, new ReferenceResolver(data));
        _factories.Add(cls, binding);
        return binding;
    }

    /// <summary>The binders of <paramref name="unit"/>; a unit whose data has not bound is an internal error.</summary>
    public ElementBinding Of(BoundUnit unit) =>
        _units.TryGetValue(unit, out var b) ? b
            : throw new InvalidOperationException($"ElementBinders: program unit '{unit.Name}' was read before its data division bound");

    /// <summary>The signature of the program or function <paramref name="unit"/> (its USING formals and RETURNING item,
    /// ISO §14.8.2, §14.8.3), which the group's repository search, the AS NESTED table and the prototype-pair screens compare.</summary>
    public CalleeSignature SignatureOf(BoundUnit unit)
    {
        var linkage = Of(unit).Data.Linkage;
        return new CalleeSignature(linkage.Formals, linkage.Returning);
    }

    /// <summary>The binders of <paramref name="cls"/>'s OBJECT half.</summary>
    public ElementBinding ObjectOf(OoClassUnit cls) =>
        _objects.TryGetValue(cls, out var b) ? b
            : throw new InvalidOperationException($"ElementBinders: class '{cls.Name}' was read before its OBJECT data bound");

    /// <summary>The binders of <paramref name="cls"/>'s FACTORY half.</summary>
    public ElementBinding FactoryOf(OoClassUnit cls) =>
        _factories.TryGetValue(cls, out var b) ? b
            : throw new InvalidOperationException($"ElementBinders: class '{cls.Name}' was read before its FACTORY data bound");

    /// <summary>Freeze the data binding of the group's <paramref name="units"/> and <paramref name="classes"/> into the bound
    /// model: each unit's and each class half's <see cref="DataBindResult"/> (<see cref="DataBinder.Result"/>). ⛔ A binder the
    /// group tail has not finished is refused, never frozen: the watermark of each must be the tail's terminal phase
    /// (<see cref="PassPhase.EditionConformanceChecked"/>, the P6 Step-6 gate), so a result is never made over a forest a
    /// pass still writes. The unexpanded parameterized classes bind for their own rules and are never emitted, so they are not
    /// frozen. Returns each interface's result (<see cref="BoundCompilation.InterfaceData"/>): an interface binds only its
    /// prototypes' formals in the data phases (ISO §10.6.2 SR4) and no group-tail pass runs over it, so its binder is complete
    /// when its data has bound and carries no watermark to require.</summary>
    public IReadOnlyDictionary<OoInterfaceSymbol, DataBindResult> Freeze(IReadOnlyList<BoundUnit> units, IReadOnlyList<OoClassUnit> classes)
    {
        foreach (var unit in units) unit.Data = Frozen(Of(unit).Data);
        foreach (var cls in classes)
        {
            cls.Data = Frozen(ObjectOf(cls).Data);
            cls.FactoryData = Frozen(FactoryOf(cls).Data);
        }
        return _interfaces.ToDictionary(static kv => kv.Key, static kv => kv.Value.Result());

        static DataBindResult Frozen(DataBinder data)
        {
            data.State.Require(PassPhase.EditionConformanceChecked, nameof(DataBindResult));
            return data.Result();
        }
    }
}
