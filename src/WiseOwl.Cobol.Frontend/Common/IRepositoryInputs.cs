// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace WiseOwl.Cobol.Frontend.Common;

/// <summary>
/// The part of a compilation's ambient-input record (<see cref="CompilationInputs"/>, the one gateway, kb/Work PB985) that
/// the external repository's reader uses (ISO §2023:8.13; DESIGN-external-repository §8.0, §16): the existence probes of the
/// places named on the command line, each recorded with its answer, and the mark that the compilation consulted the
/// repository. The binder sees the record through this interface because it sits below the front end's pipeline
/// (docs/rearchitecture/architecture-edges.json: compiler.binding may read frontend.common, never frontend.pipeline).
/// </summary>
public interface IRepositoryInputs
{
    /// <summary>Probe whether a FILE exists, recording the answer (<see cref="CompilationInputs.FileExists"/>).</summary>
    bool FileExists(string path);

    /// <summary>Probe whether a DIRECTORY exists, recording the answer (<see cref="CompilationInputs.DirectoryExists"/>).</summary>
    bool DirectoryExists(string path);

    /// <summary>Record that the compilation consulted the external repository
    /// (<see cref="CompilationInputs.ReadsRepository"/>).</summary>
    void RecordRepositoryRead();
}
