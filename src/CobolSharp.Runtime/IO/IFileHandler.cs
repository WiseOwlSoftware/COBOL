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

/// <summary>
/// COBOL OPEN statement modes (ISO §6.6.38).
/// </summary>
public enum FileOpenMode
{
    /// <summary>OPEN INPUT — read-only sequential or random access.</summary>
    Input,
    /// <summary>OPEN OUTPUT — creates or replaces the file for writing.</summary>
    Output,
    /// <summary>OPEN I-O — read and rewrite/delete access.</summary>
    InputOutput,
    /// <summary>OPEN EXTEND — append records to the end of an existing file.</summary>
    Extend,
}

/// <summary>
/// Key comparison condition for the COBOL START statement (ISO §6.6.54).
/// Positions the file cursor relative to the supplied key value.
/// </summary>
public enum StartCondition
{
    /// <summary>Position at the first record whose key equals the supplied value.</summary>
    Equal,
    /// <summary>Position at the first record whose key is greater than the supplied value.</summary>
    GreaterThan,
    /// <summary>Position at the first record whose key is greater than or equal to the supplied value.</summary>
    GreaterThanOrEqual,
    /// <summary>Position at the first record whose key is less than the supplied value.</summary>
    LessThan,
    /// <summary>Position at the first record whose key is less than or equal to the supplied value.</summary>
    LessThanOrEqual,
}
