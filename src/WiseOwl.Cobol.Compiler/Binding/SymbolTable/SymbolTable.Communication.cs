// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Binding.Model;
using WiseOwl.Cobol.Symbols;

namespace WiseOwl.Cobol.Binding;

public sealed partial class SymbolTable
{
    /// <summary>The cd-names of this source element (a cd-name is the element's alone, ISO/IEC 1989:2002 8.4.5), keyed by
    /// name; the first declaration of a spelling stands, the second being refused COBOL4250 at its declaration (kb/Work
    /// PB5307). The slice keeps the order written (<see cref="CommunicationSlice.Declared"/>).</summary>
    private readonly Dictionary<CobolName, CommunicationDescription> _cds = [];

    /// <summary>Declares a communication description entry: its cd-name joins the table and the entry the slice.</summary>
    internal void Declare(CommunicationDescription description)
    {
        // The entry stands where the data map places the CD: the start of its entry, which its record area's description carries.
        Enter(NameClass.CommunicationDescription, description.Name, PositionOf(description.Area.DeclaredAt), description);
        _data.State.Communication.Declare(description);
        _cds.TryAdd(Key(description.Name), description);
    }

    /// <summary>Whether <paramref name="qualifier"/>, written as the outermost qualifier of a data reference, is a cd-name
    /// (ISO/IEC 1989:2002 8.4.1.1.1's file-cd-report-qualifier), and if so whether its communication description describes
    /// the record <paramref name="root"/>; null when the word is no cd-name.</summary>
    internal bool? CdQualifies(string qualifier, DataItem root) =>
        _cds.TryGetValue(Key(qualifier), out var cd) ? cd.Describes(root) : null;
}
