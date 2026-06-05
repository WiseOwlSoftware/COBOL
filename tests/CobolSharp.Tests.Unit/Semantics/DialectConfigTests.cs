// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.

using CobolSharp.Compiler.Semantics;
using Xunit;

namespace CobolSharp.Tests.Unit.Semantics;

/// <summary>
/// Pins the per-version feature matrix exposed by <see cref="DialectConfig"/> — the M0 version engine's single
/// source of truth (docs/MULTIVERSION_ROADMAP.md §3). A change to any version's feature set must update these.
/// </summary>
public sealed class DialectConfigTests
{
    [Fact]
    public void Default_is_permissive_and_parses_as_85()
    {
        var c = DialectConfig.For(DialectMode.Default);
        Assert.False(c.IsStrict);
        Assert.Equal(85, c.ParserLevel);
        Assert.Equal("default", c.DisplayName);
        Assert.False(c.IsCobol2002OrLater);
        Assert.False(c.FlagsFeaturesRemovedAfter85);
        Assert.False(c.SupportsFreeFormSource);
        Assert.False(c.SupportsObjectOrientation);
    }

    [Fact]
    public void StrictCobol85_is_strict_but_still_85_level()
    {
        var c = DialectConfig.For(DialectMode.StrictCobol85);
        Assert.True(c.IsStrict);
        Assert.Equal(85, c.ParserLevel);
        Assert.Equal("COBOL-85", c.DisplayName);
        Assert.False(c.IsCobol2002OrLater);
        Assert.False(c.FlagsFeaturesRemovedAfter85);
        Assert.False(c.SupportsFreeFormSource);
    }

    [Fact]
    public void Cobol2002_enables_the_2002_surface_and_flags_removed_features()
    {
        var c = DialectConfig.For(DialectMode.Cobol2002);
        Assert.True(c.IsStrict);
        Assert.Equal(2002, c.ParserLevel);
        Assert.Equal("COBOL-2002", c.DisplayName);
        Assert.True(c.IsCobol2002OrLater);
        Assert.True(c.FlagsFeaturesRemovedAfter85);
        Assert.True(c.SupportsFreeFormSource);
        Assert.True(c.SupportsCompilerDirectives);
        Assert.True(c.SupportsUserDefinedFunctions);
        Assert.True(c.SupportsObjectOrientation);
        Assert.True(c.SupportsNationalData);
        Assert.True(c.SupportsBitAndBooleanData);
        Assert.True(c.SupportsPointers);
        Assert.True(c.SupportsValidate);
        // 2014 features are NOT yet available at 2002:
        Assert.False(c.IsCobol2014OrLater);
        Assert.False(c.SupportsDynamicTables);
        Assert.False(c.SupportsTypedef);
    }

    [Fact]
    public void Cobol2014_adds_dynamic_tables_and_typedef()
    {
        var c = DialectConfig.For(DialectMode.Cobol2014);
        Assert.Equal(2014, c.ParserLevel);
        Assert.Equal("COBOL-2014", c.DisplayName);
        Assert.True(c.IsCobol2002OrLater);
        Assert.True(c.IsCobol2014OrLater);
        Assert.False(c.IsCobol2023OrLater);
        Assert.True(c.SupportsDynamicTables);
        Assert.True(c.SupportsTypedef);
    }

    [Fact]
    public void Cobol2023_is_the_top_level()
    {
        var c = DialectConfig.For(DialectMode.Cobol2023);
        Assert.Equal(2023, c.ParserLevel);
        Assert.Equal("COBOL-2023", c.DisplayName);
        Assert.True(c.IsCobol2002OrLater);
        Assert.True(c.IsCobol2014OrLater);
        Assert.True(c.IsCobol2023OrLater);
    }

    [Fact]
    public void For_returns_cached_singletons()
    {
        Assert.Same(DialectConfig.For(DialectMode.Cobol2023), DialectConfig.For(DialectMode.Cobol2023));
        Assert.Same(DialectConfig.For(DialectMode.Default), DialectConfig.For(DialectMode.Default));
    }

    [Fact]
    public void Options_Config_reflects_the_selected_dialect()
    {
        var opt = new CompilationOptions { Dialect = DialectMode.Cobol2014 };
        Assert.Same(DialectConfig.For(DialectMode.Cobol2014), opt.Config);
        opt.Dialect = DialectMode.Default;
        Assert.False(opt.Config.IsStrict);
    }
}
