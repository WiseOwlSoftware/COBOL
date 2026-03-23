// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolSharp.Runtime;

/// <summary>
/// Thrown by STOP RUN to unwind the entire call stack.
/// Caught at the outermost Main method to terminate the run unit.
/// This is distinct from EXIT PROGRAM (which returns to the caller)
/// and GOBACK (which returns to the caller or terminates if in main).
/// </summary>
public sealed class StopRunException : Exception
{
    public StopRunException() : base("STOP RUN") { }
}
