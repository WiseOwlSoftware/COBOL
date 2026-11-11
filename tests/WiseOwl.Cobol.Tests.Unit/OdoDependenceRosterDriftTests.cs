// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.

using System;
using System.Collections.Generic;
using System.Linq;
using WiseOwl.Cobol.Binding.Model;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// ⛔ EVERY <c>Place</c> KIND STATES HOW ITS IDENTIFICATION READS AN OCCURS DEPENDING ON OBJECT (kb/Work PB5119 review
/// R1-1).
/// <para>ISO §2023:14.6.4 makes "length evaluation for an occurs-depending group item" step 6 of item identification, and a
/// format 3 INSPECT identifies identifier-1 "only once before executing the format 1 statement" (§2023:14.9.22.4 GR19).
/// The first fix pinned the DEPENDING ON object only when identifier-1 was a plain <c>OdoGroupPlace</c>, so
/// <c>INSPECT G (2:) TALLYING N FOR ALL "A" REPLACING CHARACTERS BY "Z"</c> (G occurs-depending on N) still wrote the image
/// back at the length re-read after the counter changed N, and space-filled a position. The defect's SHAPE is a place
/// kind the pin did not name. <c>OdoDependence.Roles</c> now classifies EVERY kind and <c>OdoDependence.Rebind</c>
/// is the one walk; this test asserts the roster against the assembly, so a new kind is a failing test rather than an
/// operand form the pin silently skips.</para>
/// </summary>
[Trait("Sweep", "tree")]
public sealed class OdoDependenceRosterDriftTests
{
    private static IEnumerable<Type> ConcretePlaceKinds() =>
        typeof(Place).Assembly.GetTypes().Where(t => !t.IsAbstract && typeof(Place).IsAssignableFrom(t));

    /// <summary>A kind with no row has never been asked whether its identification reads an occurs-depending
    /// object.</summary>
    [Fact]
    public void EveryPlaceKindHasARole()
    {
        string[] missing =
        [
            .. ConcretePlaceKinds().Where(t => !OdoDependence.Roles.ContainsKey(t))
                .Select(t => t.Name).OrderBy(n => n, StringComparer.Ordinal)
        ];
        Assert.True(missing.Length == 0,
            "These Place kinds have no row in OdoDependence.Roles. State how each one's identification reads an "
            + "occurs-depending object (ISO §2023:14.6.4 step 6; kb/Work PB5119):\n  " + string.Join("\n  ", missing));
    }

    /// <summary>The role must match the shape: a decoration is a <see cref="PlaceDecorator"/> (its inner is walked), a
    /// leaf is not (nothing beneath it would be walked), and only <see cref="OdoGroupPlace"/> carries a group extent.
    /// Every row carries its reason.</summary>
    [Fact]
    public void EveryRoleMatchesItsKind()
    {
        var wrong = new List<string>();
        foreach (var (type, (role, reason)) in OdoDependence.Roles)
        {
            bool decorator = typeof(PlaceDecorator).IsAssignableFrom(type);
            bool fits = role switch
            {
                OdoDependence.Role.GroupExtent => type == typeof(OdoGroupPlace),
                OdoDependence.Role.Decoration => decorator && type != typeof(OdoGroupPlace),
                OdoDependence.Role.AccessPath or OdoDependence.Role.NoObject => !decorator,
                _ => false,
            };
            if (!fits) wrong.Add($"{type.Name} is rostered as {role}");
            if (string.IsNullOrWhiteSpace(reason)) wrong.Add($"{type.Name} has no reason");
            if (type.IsAbstract) wrong.Add($"{type.Name} is abstract");
        }
        Assert.True(wrong.Count == 0, string.Join("\n  ", wrong));
    }

    private static readonly DataItem NItem = new() { Level = 1, CobolName = "N", ClrName = "N" };
    private static readonly DataItem GItem = new() { Level = 5, CobolName = "G", ClrName = "G" };
    private static readonly MemberPlace N = new(new AccessPath([new RootFieldSegment("N")]), NItem);
    private static readonly MemberPlace Pin = new(new AccessPath([new RootFieldSegment("__refodo")]), NItem);

    private static OdoGroupPlace OdoG() =>
        new(new MemberPlace(new AccessPath([new RootFieldSegment("G")]), GItem), N, 0, 1, 1, 6,
            DependingInside: false, PositionsPerCharacter: 1, TrailingUnits: 0);

    /// <summary>The review's form, <c>G (2:)</c>: the reference modifier's inner extent reads the pin.</summary>
    [Fact]
    public void AReferenceModifiedGroupReadsThePin()
    {
        var place = new RefModPlace(OdoG(), new PositionConstant(2), null);
        Assert.True(OdoDependence.ReadsDependingObject(place));
        var rebound = Assert.IsType<RefModPlace>(OdoDependence.Rebind(place, d => d == N ? Pin : d));
        Assert.Same(Pin, Assert.IsType<OdoGroupPlace>(rebound.Inner).Depending);
    }

    /// <summary>An element of the table whose reference carries the §2023:13.18.38.4 GR7 bound test reads the pin in
    /// that test; a place with no object is returned as it was.</summary>
    [Fact]
    public void ATableStepsBoundTestReadsThePinAndAPlainItemReadsNothing()
    {
        var element = new MemberPlace(new AccessPath([new RootFieldSegment("T"),
            new FixedTableSegment(new PositionConstant(1), new OdoReferenceCheck(N, 1, 6))]), GItem);
        var rebound = Assert.IsType<MemberPlace>(OdoDependence.Rebind(element, d => d == N ? Pin : d));
        var step = Assert.IsType<FixedTableSegment>(rebound.Path.Segments[1]);
        Assert.Same(Pin, step.Odo!.Depending);

        var plain = new MemberPlace(new AccessPath([new RootFieldSegment("X")]), GItem);
        Assert.False(OdoDependence.ReadsDependingObject(plain));
        Assert.Equal(plain, OdoDependence.Rebind(plain, _ => Pin));
    }
}
