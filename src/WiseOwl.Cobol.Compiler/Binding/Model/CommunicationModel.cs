// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Runtime;
using WiseOwl.Cobol.Runtime.Communication;

namespace WiseOwl.Cobol.Binding.Model;

/// <summary>The positions of a CD's record area, by what the MCS keeps in them (ISO/IEC 1989:2002 A.1.2.3 GR2, GR16, GR24).</summary>
public enum CommunicationField
{
    Queue,
    SubQueue1,
    SubQueue2,
    SubQueue3,
    MessageDate,
    MessageTime,
    SymbolicSource,
    SymbolicTerminal,
    TextLength,
    EndKey,
    StatusKey,
    MessageCount,
    DestinationCount,
    ErrorKey,
    SymbolicDestination,
}

/// <summary>One communication description entry (ISO/IEC 1989:2002 A.1.2): its format, whether it has the INITIAL clause, and the record
/// area. <see cref="Area"/> is the implicit description the rules print, which is what the MCS reads and writes, so the statements of the
/// facility hand the runtime its image; <see cref="Records"/> are the entries written after the CD, which "implicitly redefine" it.</summary>
public sealed class CommunicationDescription(string name, CommunicationFormat format, bool isInitial, DataItem area, IReadOnlyList<DataItem> records)
{
    public string Name => name;

    /// <summary>The CD's name in the run unit, its source element's registry key before it (<c>BinderDriver</c>, as a file connector's): two programs may declare one cd-name.</summary>
    public string Key { get => field ?? name; internal set; }

    public CommunicationFormat Format => format;
    public bool IsInitial => isInitial;
    public DataItem Area => area;
    public IReadOnlyList<DataItem> Records => records;

    /// <summary>Whether a level-1 entry describes this CD's area: the qualifier walk's file-cd-report-qualifier (ISO/IEC 1989:2002 8.4.1.1.1).</summary>
    public bool Describes(DataItem root) => ReferenceEquals(root, area) || records.Any(r => ReferenceEquals(r, root));
}

/// <summary>The cd-names one source element declares. A cd-name "may be referenced only by statements and entries in the source element
/// that contains the communication section in which it is declared" (ISO/IEC 1989:2002 8.4.5), so there is no inherited half.</summary>
public sealed class CommunicationSlice
{
    private readonly List<CommunicationDescription> _declared = [];
    private readonly Dictionary<string, CommunicationDescription> _byName = new(CobolNames.Comparer);

    public IReadOnlyList<CommunicationDescription> Declared => _declared;

    public IReadOnlyDictionary<string, CommunicationDescription> ByName => _byName;

    internal void Declare(CommunicationDescription description)
    {
        _declared.Add(description);
        _byName[description.Name] = description;
    }
}
