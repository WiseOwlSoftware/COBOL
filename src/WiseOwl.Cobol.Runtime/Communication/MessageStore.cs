// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.Json;
using System.Text.Json.Nodes;
using WiseOwl.Cobol.Runtime.IO;

namespace WiseOwl.Cobol.Runtime.Communication;

/// <summary>How a statement waits for the lock another run unit holds: this many retries, each after <see cref="Wait"/>, and then status 92.</summary>
internal sealed record LockWait(int Retries, Action Wait)
{
    internal static readonly LockWait Standard = new(500, () => Thread.Sleep(TimeSpan.FromMilliseconds(20)));
}

/// <summary>
/// ⛔ THE MESSAGE CONTROL SYSTEM (ISO/IEC 1989:2002 E.5.1), WiseOwl COBOL's own: "The structures, formats, and symbolic names of the
/// queues are defined by the user to the message control system at some time prior to the execution of the run unit." The user defines
/// them in the file <c>mcs.json</c> of the MCS directory (the environment variable <c>WISEOWL_COBOL_MCS</c>, else the current directory);
/// what the MCS holds lives beside it in <c>mcs-state.json</c>, so run units on one machine communicate through it and a message outlives
/// the run unit that sent it (E.5.5.1). docs/rearchitecture/DESIGN-communication-facility.md gives the file's format, and
/// docs/CONFORMANCE.md the determinations: with no definition file every symbolic name is unknown (status 20 or 21); an unreadable
/// definition file is status 91 on every statement and a store the host refuses is status 92 (Table A.1's implementor-defined 9x).
/// <para>Every statement takes the store's lock, reads the state, acts and commits (<see cref="Commit"/>), so two run units never see a
/// half-applied statement. An obsolete facility's traffic does not justify anything cleverer.</para>
/// </summary>
internal sealed class MessageStore
{
    /// <summary>The environment variable that names the MCS directory.</summary>
    internal const string DirectoryVariable = "WISEOWL_COBOL_MCS";

    /// <summary>The environment variable that says what the MCS scheduled the run unit for (ISO/IEC 1989:2002 A.1.2.3 GR6, GR28).</summary>
    internal const string ScheduledVariable = "WISEOWL_COBOL_MCS_SCHEDULED";

    private const string DefinitionFile = "mcs.json", StateFile = "mcs-state.json", LockFile = "mcs.lock";
    internal const string UnreadableDefinitions = "91", StoreRefused = "92";

    private readonly string? _root;
    private readonly JsonObject? _definitions;
    private readonly bool _broken;
    private readonly LockWait _lockWait;

    /// <summary>A.1.2.3 GR6, GR28: what the MCS scheduled the run unit for, the levels of a queue structure for an input CD or a terminal for an
    /// input-output CD; empty when anything else started the run unit.</summary>
    internal string[] Scheduled { get; }

    private MessageStore(string? root, JsonObject? definitions, bool broken, string[] scheduled, LockWait lockWait)
    {
        _root = root;
        _definitions = definitions;
        _broken = broken;
        Scheduled = scheduled;
        _lockWait = lockWait;
    }

    /// <summary>The store this run unit's environment names: the directory <c>WISEOWL_COBOL_MCS</c>, else the current one, and the levels of
    /// <c>WISEOWL_COBOL_MCS_SCHEDULED</c> separated by '/'.</summary>
    internal static MessageStore Open() => OpenAt(
        Environment.GetEnvironmentVariable(DirectoryVariable) is { Length: > 0 } named ? named : Directory.GetCurrentDirectory(),
        Environment.GetEnvironmentVariable(ScheduledVariable) is { Length: > 0 } scheduled ? scheduled.Split('/') : []);

    /// <summary>The store of one MCS directory. The definition file is read once: it is defined "prior to the execution".</summary>
    internal static MessageStore OpenAt(string root, string[] scheduled, LockWait? lockWait = null)
    {
        lockWait ??= LockWait.Standard;
        string path = Path.Combine(root, DefinitionFile);
        if (!File.Exists(path)) return new MessageStore(null, null, broken: false, scheduled, lockWait);
        try
        {
            var definitions = MessageJson.Object(JsonNode.Parse(File.ReadAllText(path)));
            MessageStoreState.FromDefinitions(definitions, DateTimeOffset.UnixEpoch);   // a definition that cannot be used is refused here, once
            return new MessageStore(root, definitions, broken: false, scheduled, lockWait);
        }
        catch (Exception e) when (IsUnusable(e))
        {
            return new MessageStore(root, null, broken: true, scheduled, lockWait);
        }
    }

    /// <summary>What a file the host refuses, or one the MCS cannot use, throws: the file system's two and the JSON reader's, among them an
    /// <see cref="ArgumentException"/> for an object that writes a member twice. The one list, for the definitions (91) and the store (92).</summary>
    private static bool IsUnusable(Exception e) =>
        e is IOException or UnauthorizedAccessException or JsonException or InvalidOperationException or FormatException or ArgumentException;

    /// <summary>Whether a status is this store's refusal, which says the statement took no action, and not an action's answer.</summary>
    internal static bool IsRefusal(string status) => status is UnreadableDefinitions or StoreRefused;

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
            if (state.Dirty) Commit(state);
            return status;
        }
        catch (Exception e) when (IsUnusable(e))
        {
            return StoreRefused;
        }
    }

    /// <summary>The lock another run unit holds for the length of one statement; a sharing violation is that, and is waited out.</summary>
    private FileStream AcquireLock()
    {
        string path = Path.Combine(_root!, LockFile);
        for (int retry = 0; ; retry++)
        {
            try
            {
                return new FileStream(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
            }
            catch (IOException e) when (HostFile.IsSharingRefusal(e) && retry < _lockWait.Retries)
            {
                _lockWait.Wait();
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

    /// <summary>⛔ THE COMMIT, the one place a statement takes effect outside the process, and why a status 92 means no action (Table A.1, docs/CONFORMANCE.md): the state goes to a
    /// temporary file, the devices take their messages, and only then does the state file move into place. A refusal at any step takes back what the devices took, so a program that
    /// retries on 9x delivers once. A crash between the devices and the move leaves the device's message and the old state, the one window left by a file system with no two-file
    /// transaction: it can repeat a message and never loses one.</summary>
    private void Commit(MessageStoreState state)
    {
        var transmissions = state.Deliver();
        string path = Path.Combine(_root!, StateFile);
        string temporary = path + ".tmp";
        var taken = new Dictionary<string, long?>(StringComparer.Ordinal);
        try
        {
            File.WriteAllText(temporary, state.ToJson().ToJsonString());
            foreach (var (device, message) in transmissions)
            {
                string file = Path.Combine(_root!, device + ".out");
                if (!taken.ContainsKey(file)) taken[file] = File.Exists(file) ? new FileInfo(file).Length : null;
                Transmit(file, message);
            }
            File.Move(temporary, path, overwrite: true);
        }
        catch (Exception e) when (IsUnusable(e))
        {
            foreach (var (file, length) in taken) TakeBack(file, length);
            TakeBack(temporary, null);
            throw;
        }
    }

    /// <summary>A message a device takes: the communication device of this MCS is the file <c>&lt;name&gt;.out</c> of the MCS directory, oriented to
    /// variable-length messages (A.2.6.3 GR2), so vertical positioning "is not applicable" and the MCS ignores it (GR12).</summary>
    private static void Transmit(string file, StoredMessage message) =>
        File.AppendAllText(file, string.Concat(message.Segments) + Environment.NewLine);

    /// <summary>Restore a file to its length before this statement, or to absent. The statement already answers 92, so a host that refuses this too changes nothing it says:
    /// that device keeps what it was given, and the other devices are still restored.</summary>
    private static void TakeBack(string file, long? length)
    {
        try
        {
            if (length is { } written)
            {
                using var device = new FileStream(file, FileMode.Open, FileAccess.Write, FileShare.None);
                device.SetLength(written);
            }
            else
            {
                File.Delete(file);
            }
        }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }
}
