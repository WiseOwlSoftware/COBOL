// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace WiseOwl.Cobol.Runtime;

/// <summary>Marks a member that answers an edition question (a <c>bool</c> or <c>int</c> derived from the edition in force) and that
/// another assembly can read. The build's analyzer (<c>src/WiseOwl.Cobol.Analyzers</c>, WOCA0002) requires the mark on every such member
/// and, in each compilation that reads it, treats the member as edition-derived, so a string chosen on it outside
/// <c>EditionInfo.Cite</c> is refused there too (WOCA0001; kb/Work PB4950, docs/VERSION_TEST_MATRIX_DESIGN.md P2.3a). It lives in the
/// runtime because the runtime is the one assembly every product project that reads an edition references (the editions project
/// sits above it), so one type serves them all; it carries no behavior and nothing reads it at run time.</summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Method | AttributeTargets.Field, Inherited = false)]
public sealed class EditionDerivedAttribute : Attribute;
