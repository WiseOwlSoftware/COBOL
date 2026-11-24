// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Binding.Model;
using WiseOwl.Cobol.Frontend.Generated;

namespace WiseOwl.Cobol.Binding;

using Core = CobolParserCore;

/// <summary>
/// ⛔ <b>WHAT A REFERENCE RESOLVER ASKS OF THE STATEMENT IT RESOLVES FOR</b> (kb/Work PB3230;
/// docs/rearchitecture/DESIGN-architecture-review.md §8.3 (4)). A procedure-division reference can carry what only the
/// statement binder can bind: a function-bearing subscript or reference-modifier position (D18), the occurs-depending
/// length a reference modifier's pre-ops must not overtake (§2023:14.6.4 steps 6 then 7), the EC-BOUND-REF-MOD checking
/// state at a line, and the object of an object property or object view that no word spells (§2023:8.4.3.1.2 Format 7,
/// §2023:8.4.3.5). The resolver is a lower layer than the statement binder, so the dependency runs ONE way: the
/// statement binder implements this interface and hands itself to the resolver it constructs
/// (<c>new ReferenceResolver(data, site)</c>); a DATA-division resolver is constructed with none.
/// <para>It replaced five settable delegates the statement binder installed on the resolver and three of its parts
/// after construction, each separately nullable, so one resolver object was a data-division resolver until a binder
/// mutated it into a procedure one. The two are now two constructions, and the site cannot change under a resolver.</para>
/// </summary>
internal interface IStatementSite
{
    /// <summary>Bind a subscript or reference-modifier position the resolver's direct walk cannot read (the D18 route) and
    /// answer the §2023:15.4 temporary its value lands in, the store registered as a statement pre-op.
    /// <para>⚠ Binding happens at RESOLVE time, not at the drain. That is load-bearing for nesting: an inner segment's
    /// store must register before its consumer's — <c>W-E(FUNCTION INTEGER(W-F(FUNCTION INTEGER(2))))</c> — which only
    /// binding while the reference resolves gives.</para>
    /// <para>⛔ The POSITION is part of the question (kb/Work PB170/PB172): §2023:13.18.38.3 SR7 lists "as a subscript" among
    /// the contexts an index-name may be referenced in and does not list a reference-modification position, so the two
    /// bind in different contexts.</para></summary>
    SegmentValue MaterializeSegment(Core.ArithmeticExpressionContext expression, SegmentPosition position, int line);

    /// <summary>Pin every OCCURS DEPENDING ON object <paramref name="inner"/>'s identification reads into a compiler temp at
    /// <paramref name="preOpMark"/> in the pending pre-ops, and answer the place reading the temps (§2023:14.6.4 steps 6
    /// then 7; kb/Work PB1123).</summary>
    Place FreezeOdoExtent(Place inner, int preOpMark, int line);

    /// <summary>True when checking for EC-BOUND-REF-MOD is enabled at <paramref name="line"/>, an exception-checking
    /// PERFORM's overlay included (kb/Work PB1707 part 1).</summary>
    bool RefModCheckingAt(int line);

    /// <summary>The receiver of an object property whose object no word spells (SELF, SUPER, an object view, a function,
    /// NULL; kb/Work PB1425): null having reported, or — under a <paramref name="probe"/> — null when the answer would need
    /// a side effect or a diagnostic.</summary>
    PropertyReceiver? BindPropertyObject(Core.PropertyObjectContext propertyObject, Core.CobolWordContext? superQualifier,
        bool probe);

    /// <summary>The object view (§2023:8.4.3.5) of an already resolved <paramref name="subject"/>, written as <paramref name="written"/>;
    /// null having reported.</summary>
    Place? ApplyObjectView(Place subject, Core.ObjectViewContext view, Core.DataReferenceContext written);
}

/// <summary>The statement site's answer to <see cref="IStatementSite.MaterializeSegment"/>: the temporary, or none, and
/// whether none means the position's own operand is a DEFERRED shape (its expression came back unbuilt), so the
/// reference is deferred rather than refused (kb/Work PB1030).</summary>
internal readonly record struct SegmentValue(DataItem? Temp, bool Unbuilt);
