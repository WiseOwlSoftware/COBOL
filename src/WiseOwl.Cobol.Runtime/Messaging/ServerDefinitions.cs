// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace WiseOwl.Cobol.Runtime.Messaging;

/// <summary>
/// ⛔ THE READER OF <c>mcs.json</c>'S <c>servers</c> MEMBER (docs/rearchitecture/DESIGN-message-control.md §0 Q3, §5.4.5; the communication facility reads the file's other
/// members by name and ignores this one, DESIGN-communication-facility.md §6). Each entry is <c>{ "name": "BALANCE", "start": "...", "maxLength": 4096 }</c>: <c>name</c>
/// is required and is a server name as <see cref="McsServerName.Normalize"/> spells it (two entries naming one server are refused), <c>start</c> is optional and is the command
/// the MCS starts when a request finds no live server (a command line, split at spaces with double quotes grouping, or an array of strings: the program, then its
/// arguments), and <c>maxLength</c> is optional and is a positive whole number, the longest message the server allows (§2023:14.9.38.4 GR1).
/// <para>A file that is absent defines nothing. A file that cannot be read, is not JSON, or whose <c>servers</c> member is of the wrong shape is an
/// <see cref="IOException"/>, which is EC-MCS-IMP at the run unit's first messaging statement (§0 Q8: "an unusable <c>mcs.json</c>").</para>
/// </summary>
internal static class ServerDefinitions
{
    /// <summary>The definition file of the MCS directory.</summary>
    internal const string FileName = "mcs.json";

    /// <summary>The definitions of the MCS directory <paramref name="mcsDirectory"/>, as a lookup by normalized server name.</summary>
    internal static Func<string, ServerDefinition?> Read(string mcsDirectory)
    {
        string path = Path.Combine(mcsDirectory, FileName);
        byte[] bytes;
        try { bytes = File.ReadAllBytes(path); }
        catch (Exception e) when (e is FileNotFoundException or DirectoryNotFoundException) { return static _ => null; }
        try
        {
            if (JsonNode.Parse(bytes, nodeOptions: null, new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip }) is not JsonObject root)
                throw new FormatException("the top level is not an object");
            return From(root);
        }
        catch (Exception e) when (e is JsonException or FormatException or InvalidOperationException)
        {
            throw new IOException($"cannot use '{path}': {e.Message}", e);
        }
    }

    /// <summary>The definitions of an already parsed <c>mcs.json</c> (a <see cref="FormatException"/> for a member of the wrong shape).</summary>
    internal static Func<string, ServerDefinition?> From(JsonObject root)
    {
        if (root["servers"] is null) return static _ => null;
        if (root["servers"] is not JsonArray servers) throw new FormatException("'servers' is not an array");
        Dictionary<string, ServerDefinition> byName = new(StringComparer.Ordinal);
        foreach (JsonNode? entry in servers)
        {
            if (entry is not JsonObject server) throw new FormatException("a 'servers' entry is not an object");
            string name = McsServerName.Normalize(Text(server, "name") ?? throw new FormatException("a 'servers' entry has no 'name'"));
            if (name.Length == 0) throw new FormatException("a 'servers' entry has a blank 'name'");
            if (byName.ContainsKey(name)) throw new FormatException($"server '{name}' is defined twice (names are compared without regard to case, trailing spaces removed)");
            byName[name] = new ServerDefinition(MaxLength(server, name), Start(server, name));
        }
        return name => byName.GetValueOrDefault(name);
    }

    private static string? Text(JsonObject owner, string member) => owner[member] switch
    {
        null => null,
        JsonValue value when value.TryGetValue(out string? text) => text,
        _ => throw new FormatException($"'{member}' is not a string"),
    };

    private static int? MaxLength(JsonObject server, string name)
    {
        if (server["maxLength"] is not { } node) return null;
        if (node is not JsonValue value || !value.TryGetValue(out int length) || length < 1)
            throw new FormatException($"server '{name}': 'maxLength' is not a positive whole number");
        return length;
    }

    private static IReadOnlyList<string>? Start(JsonObject server, string name)
    {
        if (server["start"] is not { } node) return null;
        IReadOnlyList<string> command = node switch
        {
            JsonArray array => [.. array.Select(item => item is JsonValue v && v.TryGetValue(out string? text) ? text : throw new FormatException($"server '{name}': 'start' holds a non-string"))],
            JsonValue value when value.TryGetValue(out string? line) => SplitCommandLine(line),
            _ => throw new FormatException($"server '{name}': 'start' is neither a string nor an array of strings"),
        };
        if (command.Count == 0 || command[0].Length == 0) throw new FormatException($"server '{name}': 'start' names no program");
        return command;
    }

    /// <summary>A command line split at spaces and tabs; a double quote groups what it encloses (and may enclose spaces), and is not part of the argument.</summary>
    internal static List<string> SplitCommandLine(string line)
    {
        List<string> arguments = [];
        StringBuilder current = new();
        bool quoted = false, any = false;
        foreach (char c in line)
        {
            if (c == '"') { quoted = !quoted; any = true; }
            else if (!quoted && c is ' ' or '\t')
            {
                if (any) arguments.Add(current.ToString());
                current.Clear();
                any = false;
            }
            else { current.Append(c); any = true; }
        }
        if (quoted) throw new FormatException("a double quote in 'start' is not closed");
        if (any) arguments.Add(current.ToString());
        return arguments;
    }
}
