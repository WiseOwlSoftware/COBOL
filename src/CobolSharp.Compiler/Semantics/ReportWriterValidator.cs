// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolSharp.Compiler.Common;
using CobolSharp.Compiler.Diagnostics;

namespace CobolSharp.Compiler.Semantics;

/// <summary>
/// Validates Report Writer declarations: TYPE, SUM, CONTROL fields.
/// Currently a stub — full validation deferred until Report Writer codegen is implemented.
/// </summary>
public static class ReportWriterValidator
{
    public static void Validate(SemanticModel model, DiagnosticBag diagnostics)
    {
        // Report Writer validation is deferred until Report Writer statements
        // (INITIATE, GENERATE, TERMINATE) are fully bound and lowered.
        // The diagnostic descriptors CBL3401-3406 are defined and ready.
    }
}
