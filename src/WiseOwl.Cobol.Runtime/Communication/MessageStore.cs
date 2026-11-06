// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using WiseOwl.Cobol.Runtime.Mcs;

namespace WiseOwl.Cobol.Runtime.Communication;

/// <summary>
/// ⛔ THE MESSAGE CONTROL SYSTEM (ISO/IEC 1989:2002 E.5.1), WiseOwl COBOL's own: "The structures, formats, and symbolic names of the
/// queues are defined by the user to the message control system at some time prior to the execution of the run unit." The user defines
/// them in the file <c>mcs.json</c> of the MCS directory (the environment variable <c>WISEOWL_COBOL_MCS</c>, else the current directory);
/// what the MCS holds lives beside it in the paged store <c>mcs.store</c> (<see cref="McsStore"/>), so run units on one machine communicate through it and a message
/// outlives the run unit that sent it (E.5.5.1). docs/rearchitecture/DESIGN-communication-facility.md gives the file's format, and
/// docs/CONFORMANCE.md the determinations: with no definition file every symbolic name is unknown (status 20 or 21); an unreadable
/// definition file is status 91 on every statement; a store the host refuses, or whose locks the host cannot carry, is status 92 and takes no action, and a statement whose
/// outcome the store cannot tell is status 93 (Table A.1's implementor-defined 9x).
/// <para>Every statement is one statement of the store (<see cref="McsStore.Statement"/>): it takes the store's mutex, reads the rows it needs, acts and lands them as one page
/// statement, so two run units never see a half-applied statement. This class is the statement's boundary: the definitions, and what a refusal answers.</para>
/// </summary>
internal sealed class MessageStore : IDisposable
{
    /// <summary>The environment variable that says what the MCS scheduled the run unit for (ISO/IEC 1989:2002 A.1.2.3 GR6, GR28).</summary>
    internal const string ScheduledVariable = "WISEOWL_COBOL_MCS_SCHEDULED";

    private const string DefinitionFile = "mcs.json";

    /// <summary>The paged store of the MCS directory.</summary>
    internal const string StoreFile = "mcs.store";

    /// <summary>The 9x statuses (Table A.1's implementor-defined values): 91 the definition file is unusable, 92 the store refused the statement and no action was taken, 93 the
    /// store refused the statement and cannot tell whether it landed.</summary>
    internal const string UnreadableDefinitions = "91", StoreRefused = "92", UnknownOutcome = "93";

    private readonly string? _root;
    private readonly DefinitionFile? _definitions;
    private readonly bool _broken;
    private McsStore? _store;

    /// <summary>A.1.2.3 GR6, GR28: what the MCS scheduled the run unit for, the levels of a queue structure for an input CD or a terminal for an
    /// input-output CD; empty when anything else started the run unit.</summary>
    internal string[] Scheduled { get; }

    private MessageStore(string? root, DefinitionFile? definitions, bool broken, string[] scheduled)
    {
        _root = root;
        _definitions = definitions;
        _broken = broken;
        Scheduled = scheduled;
    }

    /// <summary>The store this run unit's environment names: the directory <c>WISEOWL_COBOL_MCS</c>, else the current one, and the levels of
    /// <c>WISEOWL_COBOL_MCS_SCHEDULED</c> separated by '/'.</summary>
    internal static MessageStore Open() => OpenAt(
        McsDirectory.Resolve(null).Path,
        Environment.GetEnvironmentVariable(ScheduledVariable) is { Length: > 0 } scheduled ? scheduled.Split('/') : []);

    /// <summary>The store of one MCS directory. The definition file is read once: it is defined "prior to the execution".</summary>
    internal static MessageStore OpenAt(string root, string[] scheduled)
    {
        string path = Path.Combine(root, DefinitionFile);
        if (!File.Exists(path)) return new MessageStore(null, null, broken: false, scheduled);
        try
        {
            byte[] bytes = File.ReadAllBytes(path);
            using var reader = new StreamReader(new MemoryStream(bytes), Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
            var json = MessageJson.Object(JsonNode.Parse(reader.ReadToEnd()));
            var parsed = MessageDefinitions.Parse(json, DateTimeOffset.UnixEpoch);   // a definition that cannot be used is refused here, once
            return new MessageStore(root, new DefinitionFile(json, SHA256.HashData(bytes), parsed), broken: false, scheduled);
        }
        catch (Exception e) when (IsUnusable(e))
        {
            return new MessageStore(root, null, broken: true, scheduled);
        }
    }

    /// <summary>What a file the host refuses, or one the MCS cannot use, throws: the file system's two, the JSON reader's, and the store's own refusals
    /// (<see cref="McsRefusalException"/>, an <see cref="IOException"/>), among them an <see cref="ArgumentException"/> for an object that writes a member twice. The one list, for
    /// the definitions (91) and the store (92).</summary>
    internal static bool IsUnusable(Exception e) =>
        e is IOException or UnauthorizedAccessException or JsonException or InvalidOperationException or FormatException or ArgumentException;

    /// <summary>Whether a status is this store's refusal, which says the statement took no action the run unit may rely on, and not an action's answer.</summary>
    internal static bool IsRefusal(string status) => status is UnreadableDefinitions or StoreRefused or UnknownOutcome;

    /// <summary>One statement's transaction. Returns the action's status key, or the 9x this store reports instead of acting.</summary>
    internal string Run(Func<MessageStoreState, string> action)
    {
        if (_broken) return UnreadableDefinitions;
        if (_root is null) return action(MessageStoreState.Undefined());
        try
        {
            _store ??= McsStore.Open(_root);
            return _store.Statement(_definitions!, action);
        }
        catch (Exception e) when (IsUnusable(e))
        {
            return StoreRefused;   // the store's file could not be opened: nothing was read, nothing acted
        }
    }

    /// <summary>The end of the run unit's dialogue with the store: <see cref="McsStore.Dispose"/>. A later statement opens the store again.</summary>
    public void Dispose()
    {
        var store = _store;
        _store = null;
        store?.Dispose();
    }
}
