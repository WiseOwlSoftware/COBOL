// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolNet.Cli;

/// <summary>The resolved <c>cobol</c> options a parse produces (the DTO handed to <see cref="CobolNet.CompilerDriver"/>).
/// Argument PARSING is owned by <see cref="Program.BuildRootCommand"/> via <c>System.CommandLine</c> — the
/// first-party parser handles the <c>--opt value</c> / <c>--opt=value</c> forms, arity, and help uniformly, so no
/// option can ever consume a following flag as its value (the class of bug a hand-rolled switch invited).</summary>
/// <param name="SourcePath">The COBOL source file to compile.</param>
/// <param name="OutputPath">Output assembly path (<c>-o</c>); null = the source path with a <c>.dll</c> extension.</param>
/// <param name="NistTestName">NIST CCVS test name (<c>--nist</c>) enabling placeholder preprocessing; null = off.</param>
/// <param name="DialectLevel">ISO dialect year (<c>--std</c>): 85 / 2002 / 2014 / 2023.</param>
/// <param name="CopyPaths">COPY copybook search directories (<c>--copy</c>), in order.</param>
/// <param name="Run">Run the compiled assembly after a successful compile (<c>--run</c>).</param>
/// <param name="Permissive">The strict/permissive severity axis (<c>--permissive</c>, orthogonal to
/// <c>--std</c>/<c>--nist</c>): accept constructs the targeted edition removed, warning instead of rejecting —
/// the documented migration mode (VERSION_TEST_MATRIX_DESIGN §10 #1; owner decision 4).</param>
internal sealed record CliOptions(
    string SourcePath,
    string? OutputPath,
    string? NistTestName,
    int DialectLevel,
    IReadOnlyList<string> CopyPaths,
    bool Run,
    bool Permissive);
