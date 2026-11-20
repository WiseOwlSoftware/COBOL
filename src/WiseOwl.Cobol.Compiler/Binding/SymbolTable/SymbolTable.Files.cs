// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Binding.Model;
using WiseOwl.Cobol.Symbols;

namespace WiseOwl.Cobol.Binding;

using Core = WiseOwl.Cobol.Frontend.Generated.CobolParserCore;

/// <summary>How far a file-name lookup reaches: <see cref="Visible"/>, the element's own file-names and its containers'
/// GLOBAL ones (§2023:13.18.27.4 GR1); <see cref="Own"/>, the element's own alone, for a position that names an entry of
/// this element's own FILE-CONTROL (an FD or SD's file-name, §2023:13.4.5.3 SR1, §2023:13.4.6.3 SR1; the I-O-CONTROL
/// clauses and APPLY COMMIT's file-names).</summary>
internal enum NameReach
{
    Visible,
    Own,
}

public sealed partial class SymbolTable
{
    /// <summary>This element's own file-names: every SELECT's, and the model an FD or SD with no SELECT synthesizes (the last
    /// declaration of a spelling is the one a reference finds).</summary>
    private readonly Dictionary<CobolName, FileModel> _ownFiles = [];

    /// <summary>The containers' GLOBAL file-names, nearest container first (the first of a spelling stands), known before
    /// the element binds (<see cref="InheritFiles"/>).</summary>
    private readonly Dictionary<CobolName, FileModel> _inheritedFiles = [];

    /// <summary>Every file of every containing program, GLOBAL or not, nearest container first (<see cref="InheritFiles"/>):
    /// the chain <see cref="FilesOfContainingChain"/> reads.</summary>
    private readonly List<FileModel> _containerFiles = [];

    /// <summary>The inherited file-names a data-name, condition-name or index-name of the spelling declared in a nearer source
    /// element hides (§2023:8.4.6.2.1 3) a); §1985:X.1.3.8.2 GR3a; kb/Work PB5786).</summary>
    private readonly HashSet<CobolName> _hiddenFileNames = [];

    /// <summary>The files this element's OWN file control entries select, by file-name: the subjects of §2023:12.4.5.2 SR2 (one
    /// SELECT per file-name) and SR3 (each needs an FD or SD). Not <see cref="_ownFiles"/>, which also holds the models an FD
    /// or SD synthesizes; a class-level FILE-CONTROL, which §2023:12.4.3 SR1 forbids, is not screened (kb/Work PB1076, PB1077).</summary>
    private readonly Dictionary<CobolName, FileModel> _selectedFiles = [];

    /// <summary>Declares a file-name of this element: the file joins the slice in the order written and the table under
    /// <paramref name="name"/>; <paramref name="selected"/> marks a SELECT that §2023:12.4.5.2 SR2 and SR3 screen.</summary>
    internal void Declare(FileModel file, string name, bool selected)
    {
        // The entry stands at the file control entry (FileModel.EntryAt; none for the model an FD or SD with no SELECT synthesizes).
        Enter(NameClass.File, file.SelectName, PositionOf(file.EntryAt), file);
        _data.State.Files.Declare(file);
        _ownFiles[Key(name)] = file;
        if (selected) _selectedFiles[Key(name)] = file;
    }

    /// <summary>The file a file-name denotes here: the element's own, which hides a container's of the same spelling
    /// (§2023:8.4.6.2.1 3) a)), else the nearest container's GLOBAL file unless a nearer declaration hides it.</summary>
    private FileModel? FileNamed(CobolName name, NameReach reach) =>
        _ownFiles.TryGetValue(name, out var own) ? own
        : reach == NameReach.Visible && !_hiddenFileNames.Contains(name) && _inheritedFiles.TryGetValue(name, out var inherited) ? inherited
        : null;

    /// <summary>Every file visible in this element: its own, then each inherited one whose spelling it does not declare itself.</summary>
    internal IEnumerable<FileModel> VisibleFiles
    {
        get
        {
            foreach (var file in _ownFiles.Values) yield return file;
            foreach (var (name, file) in _inheritedFiles)
                if (!_ownFiles.ContainsKey(name))
                    yield return file;
        }
    }

    /// <summary>The file whose <see cref="FileModel.Records"/> contain <paramref name="record"/> (reference identity, never its
    /// spelling): this element's own files in the order declared, then the visible containers' GLOBAL ones (§2023:13.18.27.4
    /// GR1 — the record-names of a GLOBAL FD are global names), so a contained program's WRITE or REWRITE of a container's
    /// record resolves to the container's one connector. The one record-to-file question: the binder's record-name-1 rule
    /// (<c>StatementValidation.ResolveRecordName</c>) and the flag pass's probe of the same WRITE both ask it here, in the
    /// element's scope, a method's included (kb/Work PB5932).</summary>
    internal FileModel? FileWhoseRecordIs(DataItem record)
    {
        foreach (var file in _data.State.Files.Declared)
            if (file.Records.Contains(record)) return file;
        foreach (var file in VisibleFiles)
            if (file.Records.Contains(record)) return file;
        return null;
    }

    /// <summary>Every file of this element and of each containing program, GLOBAL or not, shadowed or not: this element's own
    /// files, then each container's, nearest first. A container's file need not be GLOBAL for an item it names
    /// (its FILE STATUS item, made visible by the item's own GLOBAL clause) to be referenced here, so a question about such
    /// an item's ROLE reads this chain, not <see cref="VisibleFiles"/>.</summary>
    internal IEnumerable<FileModel> FilesOfContainingChain =>
        _data.State.Files.Declared.Concat(_containerFiles);

    /// <summary>The element's own files, one per file-name (the last declared of a spelling).</summary>
    internal IEnumerable<FileModel> OwnFiles => _ownFiles.Values;

    /// <summary>The source position of each file-name's first file description entry or sort-merge file description entry,
    /// across both kinds (<see cref="DeclareFileDescriptions"/>): the entry at that position is the one that stands.</summary>
    private readonly Dictionary<CobolName, int> _firstFileDescriptions = [];

    /// <summary>Declare where each file-name of the FILE SECTION <paramref name="fs"/> is first described, before any of its entries
    /// binds (<c>FileControlBinder.BindFileSection</c>).</summary>
    internal void DeclareFileDescriptions(Core.FileSectionContext fs)
    {
        void Note(Core.FileNameContext? name, Antlr4.Runtime.IToken start)
        {
            if (name is null) return;
            var key = Key(name.GetText());
            if (!_firstFileDescriptions.TryGetValue(key, out var at) || start.StartIndex < at) _firstFileDescriptions[key] = start.StartIndex;
        }
        foreach (var fd in fs.fileDescriptionEntry()) Note(fd.fileName(), fd.Start);
        foreach (var sd in fs.sortMergeDescriptionEntry()) Note(sd.fileName(), sd.Start);
    }

    /// <summary>Is the entry starting at <paramref name="entryStart"/> a second description of the file-name it writes,
    /// <paramref name="name"/>? No qualifier can tell two descriptions of one file-name apart (a declaration question; the first
    /// stands).</summary>
    internal bool DescribedEarlier(Core.FileNameContext name, Antlr4.Runtime.IToken entryStart) =>
        _firstFileDescriptions[Key(name.GetText())] != entryStart.StartIndex;

    /// <summary>How many distinct file-names the SAME clause <paramref name="same"/> writes, whatever each identifies: the
    /// clause is defined over two or more files (§2023:12.4.6.4.2, §2023:12.4.6.4.4 GR1).</summary>
    internal static int DistinctFileNamesWritten(Core.SameClauseContext same) =>
        same.fileName().Select(f => Key(f.GetText())).Distinct().Count();

    /// <summary>The level-01 record among <paramref name="records"/> (one file description entry's) that the DATA RECORDS clause's
    /// data-name-1 <paramref name="written"/> names (§1985:VII.3.5.3 SR1); null when none does. The clause is the reference itself,
    /// so its occurrence is recorded.</summary>
    internal DataItem? IdentifyDataRecord(IReadOnlyList<DataItem> records, Core.CobolWordContext written)
    {
        var key = Key(written.GetText());
        var item = records.FirstOrDefault(r => r.Level == 1 && r.CobolName is { } n && Key(n) == key);
        if (item is not null) RecordData(item, written, report: true);
        return item;
    }

    /// <summary>The SELECTed files SR3 screens, with the file-name as the SELECT wrote it.</summary>
    internal IEnumerable<(string Name, FileModel File)> SelectedFiles => _selectedFiles.Select(p => (p.Key.Word, p.Value));

    /// <summary>The file of a CONTAINING source element, described with no GLOBAL clause, that an unqualified clause operand spells
    /// (§2023:12.4.6.3.3 SR1: the APPLY COMMIT clause's file-name-1 "shall be specified in the FILE-CONTROL paragraph of the source
    /// element that contains this clause"), or null. Such a file is not visible here (§2023:8.4.6.2.2), so it names nothing: the
    /// question only words the refusal of an operand that reached no data item.</summary>
    internal FileModel? ContainerLocalFileSpelled(WrittenDataName operand)
    {
        if (operand.Qualifiers.Count != 0) return null;
        var key = Key(operand.Name);
        foreach (var f in _data.State.Inherited.ContainerLocalFiles)
            if (Key(f.CobolName) == key) return f;
        return null;
    }

    /// <summary>Whether <paramref name="qualifier"/>, the outermost qualifier of a data reference, is a visible file-name whose
    /// file holds the record <paramref name="root"/> (the file-report-qualifier, §2023:8.4.2.2.2 Format 1).</summary>
    internal bool FileQualifies(string qualifier, DataItem root) =>
        FileNamed(Key(qualifier), NameReach.Visible) is { } file && file.Records.Contains(root);

    /// <summary>The containers' GLOBAL file-names join the table (§2023:13.18.27.4 GR1), the nearest first; their depths, record
    /// areas and non-GLOBAL files are the inheritance slice's (<see cref="InheritedGlobals.AddContainerFiles"/>).</summary>
    internal void InheritFiles(IReadOnlyList<FileModel> files, IReadOnlyCollection<DataItem> globalRoots, int depth)
    {
        _containerFiles.AddRange(files);
        foreach (var file in _data.State.Inherited.AddContainerFiles(files, globalRoots, depth))
            if (_inheritedFiles.TryAdd(Key(file.CobolName), file))
                _data.State.Inherited.SetFileDepth(file, depth);
    }

    /// <summary>Hides each inherited file-name that a nearer declaration of its spelling hides (§2023:8.4.6.2.1 3) a)).</summary>
    internal void HideShadowedFiles(Func<string, int, bool> declaredNearerThan)
    {
        foreach (var (name, file) in _inheritedFiles)
            if (declaredNearerThan(name.Word, _data.State.Inherited.FileDepth(file)))
                _hiddenFileNames.Add(name);
    }

    /// <summary>Does the file-name a <c>&gt;&gt;TURN</c> directive's FILE phrase writes name <paramref name="file"/>, the file a
    /// statement references (§2023:7.3.25.4 GR6, GR8: a file-scoped directive enables or disables checking for the statements that
    /// reference that file)? The directive stands outside every source element, so its word is no reference this element's table
    /// identifies; the statement's file is already identified, and the question is answered with the table's one name equality
    /// (docs/rearchitecture/DESIGN-listing.md section 6.7.1, "Equality of COBOL names is private too"; step 6b's remainder slice 5).</summary>
    internal static bool DirectiveNamesFile(string directiveWord, FileModel file) => Key(directiveWord) == Key(file.CobolName);
}
