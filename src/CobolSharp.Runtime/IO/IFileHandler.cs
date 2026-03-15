// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolSharp.Runtime.IO;

/// <summary>
/// Abstract interface for COBOL file operations. Each file organization
/// (sequential, indexed, relative) has its own implementation.
/// </summary>
public interface IFileHandler : IDisposable
{
    /// <summary>The external file name/path.</summary>
    string ExternalName { get; }

    /// <summary>Whether the file is currently open.</summary>
    bool IsOpen { get; }

    /// <summary>Open the file in the specified mode.</summary>
    string Open(FileOpenMode mode);

    /// <summary>Close the file.</summary>
    string Close();

    /// <summary>Read the next record (sequential access).</summary>
    string ReadNext(byte[] recordBuffer);

    /// <summary>Read a specific record by key (random/indexed access).</summary>
    string ReadByKey(byte[] recordBuffer, byte[] keyValue);

    /// <summary>Write a record.</summary>
    string Write(byte[] recordData);

    /// <summary>Rewrite the current record.</summary>
    string Rewrite(byte[] recordData);

    /// <summary>Delete the current record.</summary>
    string Delete();

    /// <summary>Position to a record by key for subsequent sequential reads.</summary>
    string Start(byte[] keyValue, StartCondition condition);
}

public enum FileOpenMode
{
    Input,
    Output,
    InputOutput,
    Extend,
}

public enum StartCondition
{
    Equal,
    GreaterThan,
    GreaterThanOrEqual,
    LessThan,
    LessThanOrEqual,
}
