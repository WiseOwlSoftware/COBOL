// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolSharp.Compiler.Diagnostics;

namespace CobolSharp.Compiler;

/// <summary>
/// Result of a COBOL compilation: success/failure, output path, and diagnostics.
/// </summary>
public sealed record CompilationResult(
    bool Success,
    string OutputPath,
    IReadOnlyList<Diagnostic> Diagnostics);
