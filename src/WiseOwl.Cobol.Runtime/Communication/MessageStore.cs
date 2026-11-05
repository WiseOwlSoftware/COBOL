// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.Json;
using System.Text.Json.Nodes;

namespace WiseOwl.Cobol.Runtime.Communication;

/// <summary>
/// ⛔ THE MESSAGE CONTROL SYSTEM (ISO/IEC 1989:2002 E.5.1), WiseOwl COBOL's own: "The structures, formats, and symbolic names of the
/// queues are defined by the user to the message control system at some time prior to the execution of the run unit." The user defines
/// them in the file <c>mcs.json</c> of the MCS directory (the environment variable <c>WISEOWL_COBOL_MCS</c>, else the current directory);
/// what the MCS holds lives beside it in <c>mcs-state.json</c>, so run units on one machine communicate through it and a message outlives
/// the run unit that sent it (E.5.5.1). docs/rearchitecture/DESIGN-communication-facility.md gives the file's format, and
/// docs/CONFORMANCE.md the determinations: with no definition file every symbolic name is unknown (status 20 or 21); an unreadable
/// definition file is status 91 on every statement and a store the host refuses is status 92 (Table A.1's implementor-defined 9x).
/// <para>Every statement takes the store's lock, reads the state, acts and writes the state back atomically, so two run units never
/// see a half-applied statement and a crash leaves the state of the last completed statement. An obsolete facility's traffic does not
/// justify anything cleverer.</para>
/// </summary>
internal sealed class MessageStore
{
    /// <summary>The environment variable that names the MCS directory.</summary>
    internal const string DirectoryVariable = "WISEOWL_COBOL_MCS";

    /// <summary>The environment variable that says what the MCS scheduled the run unit for (ISO/IEC 1989:2002 A.1.2.3 GR6, GR28).</summary>
    internal const string ScheduledVariable = "WISEOWL_COBOL_MCS_SCHEDULED";

    private const string DefinitionFile = "mcs.json", StateFile = "mcs-state.json", LockFile = "mcs.lock";
    internal const string UnreadableDefinitions = "91", StoreRefused = "92";
    private static readonly TimeSpan LockRetry = TimeSpan.FromMilliseconds(20);

    private readonly string? _root;
    private readonly JsonObject? _definitions;
    private readonly bool _broken;

    /// <summary>A.1.2.3 GR6, GR28: what the MCS scheduled the run unit for, the levels of a queue structure for an input CD or a terminal for an
    /// input-output CD; empty when anything else started the run unit.</summary>
    internal string[] Scheduled { get; }

    private MessageStore(string? root, JsonObject? definitions, bool broken, string[] scheduled)
    {
        _root = root;
        _definitions = definitions;
        _broken = broken;
        Scheduled = scheduled;
    }

    /// <summary>The store this run unit's environment names: the directory <c>WISEOWL_COBOL_MCS</c>, else the current one, and the levels of
    /// <c>WISEOWL_COBOL_MCS_SCHEDULED</c> separated by '/'.</summary>
    internal static MessageStore Open() => OpenAt(
        Environment.GetEnvironmentVariable(DirectoryVariable) is { Length: > 0 } named ? named : Directory.GetCurrentDirectory(),
        Environment.GetEnvironmentVariable(ScheduledVariable) is { Length: > 0 } scheduled ? scheduled.Split('/') : []);

    /// <summary>The store of one MCS directory. The definition file is read once: it is defined "prior to the execution".</summary>
    internal static MessageStore OpenAt(string root, string[] scheduled)
    {
        string path = Path.Combine(root, DefinitionFile);
        if (!File.Exists(path)) return new MessageStore(null, null, broken: false, scheduled);
        try
        {
            var definitions = MessageJson.Object(JsonNode.Parse(File.ReadAllText(path)));
            MessageStoreState.FromDefinitions(definitions, DateTimeOffset.UnixEpoch);   // a definition that cannot be used is refused here, once
            return new MessageStore(root, definitions, broken: false, scheduled);
        }
        catch (Exception e) when (e is JsonException or InvalidOperationException or FormatException or IOException or UnauthorizedAccessException)
        {
            return new MessageStore(root, null, broken: true, scheduled);
        }
    }

    /// <summary>One statement's transaction. Returns the action's status key, or the 9x this store reports instead of acting.</summary>
    internal string Run(Func<MessageStoreState, string> action)
    {
        if (_broken) return UnreadableDefinitions;
        if (_root is null) return action(MessageStoreState.Empty());
        try
        {
            using var held = AcquireLock();
            var state = Load();
            string status = action(state);
            if (state.Dirty)
            {
                state.Deliver(Transmit);
                Save(state);
            }
            return status;
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or JsonException or InvalidOperationException or FormatException)
        {
            return StoreRefused;
        }
    }

    /// <summary>The lock another run unit holds for the length of one statement; a sharing violation is that, and is waited out.</summary>
    private FileStream AcquireLock()
    {
        string path = Path.Combine(_root!, LockFile);
        for (int attempt = 0; ; attempt++)
        {
            try
            {
                return new FileStream(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
            }
            catch (IOException) when (attempt < 500)
            {
                Thread.Sleep(LockRetry);
            }
        }
    }

    private MessageStoreState Load()
    {
        var now = RunUnit.Current.Clock.Now();
        var state = MessageStoreState.FromDefinitions(_definitions!, now);
        string path = Path.Combine(_root!, StateFile);
        if (File.Exists(path)) state.Overlay(MessageJson.Object(JsonNode.Parse(File.ReadAllText(path))));
        else state.Touch();   // the definitions' messages enter the queues once, when the store first answers
        return state;
    }

    private void Save(MessageStoreState state)
    {
        string path = Path.Combine(_root!, StateFile);
        string temporary = path + ".tmp";
        File.WriteAllText(temporary, state.ToJson().ToJsonString());
        File.Move(temporary, path, overwrite: true);
    }

    /// <summary>A message a device takes: the communication device of this MCS is the file <c>&lt;name&gt;.out</c> of the MCS directory, oriented to
    /// variable-length messages (A.2.6.3 GR2), so vertical positioning "is not applicable" and the MCS ignores it (GR12).</summary>
    private void Transmit(string device, StoredMessage message) =>
        File.AppendAllText(Path.Combine(_root!, device + ".out"), string.Concat(message.Segments) + Environment.NewLine);
}
