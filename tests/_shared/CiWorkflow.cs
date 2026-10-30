// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.

using System.Text.RegularExpressions;

namespace WiseOwl.Cobol.Tests.Shared;

/// <summary>
/// ⭐ THE ONE READER OF A CI WORKFLOW'S SHAPE (kb/Work PB3819): the jobs under <c>jobs:</c>, each job's steps and the
/// comments between them, for every drift test that audits <c>.github/workflows/*.yml</c> (or a composite action's
/// <c>steps:</c>). Ten classes each split the workflow their own way — one over the whole file (so the <c>on:</c>
/// triggers read as jobs), one that hung a job's banner on the job ABOVE, one that dropped comments inside a step and one
/// that kept them — and a workflow edit one reader tolerated another misread, so a drift test passed vacuously or went red
/// on a legal change. This is the single definition; the tests ask it, and none splits a workflow itself.
/// <para>
/// The shape it reads is the one GitHub's workflow syntax gives these files: a top-level <c>jobs:</c> key; job ids two
/// spaces deep; a job's <c>steps:</c> list whose <c>- </c> items sit deeper than the key. It is a line reader, not a YAML
/// parser: block scalars (<c>run: |</c>) are kept as the lines they are, and a flow-style job or step list is not
/// understood — it is REFUSED (<see cref="InvalidDataException"/>) where it can be told apart, never read as empty,
/// because a scan that looked at nothing must not report clean.
/// </para>
/// <para>
/// Comments: a comment or blank line is attributed to what FOLLOWS it. A banner above a job key documents that job, and a
/// comment above a step documents that step; the trailing comments of a job or step belong to the next, so a banner naming
/// <c>--list-tests</c> is never read as part of the job above it. A comment between two lines of a job or step stays in
/// its lines (<see cref="CiJob.Lines"/>, <see cref="CiStep.Lines"/>) and <see cref="CiJob.Code"/> / <see cref="CiStep.Code"/>
/// drop it.
/// </para>
/// </summary>
internal static class CiWorkflow
{
    private static readonly Regex JobsKey = new(@"^jobs:[ \t]*(?:#.*)?$", RegexOptions.NonBacktracking);
    private static readonly Regex JobHeader = new(@"^  (?<id>[A-Za-z0-9_.-]+):[ \t]*(?:#.*)?$", RegexOptions.NonBacktracking);
    private static readonly Regex StepsKey = new(@"^\s*steps:[ \t]*(?:#.*)?$", RegexOptions.NonBacktracking);
    private static readonly Regex InlineSteps = new(@"^\s*steps:[ \t]*[^\s#]", RegexOptions.NonBacktracking);
    private static readonly Regex Key = new(@"^(?<key>[A-Za-z0-9_.-]+):(?:[ \t]+(?<value>.*))?$", RegexOptions.NonBacktracking);
    private static readonly Regex TrailingComment = new(@"[ \t]+#.*$", RegexOptions.NonBacktracking);

    /// <summary>Whether <paramref name="text"/> has a top-level <c>jobs:</c> key: a workflow, as against a composite
    /// action, which has none.</summary>
    internal static bool HasJobs(string text) => Lines(text).Any(l => JobsKey.IsMatch(l));

    /// <summary>The jobs under the workflow's top-level <c>jobs:</c> key, in file order. Refuses a text with no
    /// <c>jobs:</c> key, or with a <c>jobs:</c> key that holds no job: an empty answer would be indistinguishable from
    /// "every job is clean".</summary>
    internal static IReadOnlyList<CiJob> Jobs(string workflow)
    {
        string[] lines = Lines(workflow);
        int start = Array.FindIndex(lines, l => JobsKey.IsMatch(l));
        if (start < 0)
            throw new InvalidDataException("the workflow has no top-level `jobs:` key, so no job reader can have found anything");

        var jobs = new List<CiJob>();
        string? id = null;
        List<string> banner = [], body = [], pending = [];   // pending: comment and blank lines not yet attributed
        for (int i = start + 1; i < lines.Length; i++)
        {
            string line = lines[i];
            if (IsTrivia(line))
            {
                pending.Add(line);
                continue;
            }
            if (line[0] != ' ')
                break;   // a top-level key after `jobs:` ends the jobs block
            Match header = JobHeader.Match(line);
            if (header.Success)
            {
                if (id is not null)
                    jobs.Add(new CiJob(id, banner, body));
                (id, banner, body, pending) = (header.Groups["id"].Value, pending, [], []);
                continue;
            }
            if (id is null)
                throw new InvalidDataException($"line {i + 1} of the workflow sits under `jobs:` before any job id: `{line.Trim()}` "
                    + "(only block-style job ids two spaces deep are read)");
            body.AddRange(pending);
            pending.Clear();
            body.Add(line);
        }
        if (id is not null)
            jobs.Add(new CiJob(id, banner, body));
        if (jobs.Count == 0)
            throw new InvalidDataException("the workflow's `jobs:` key holds no job");
        return jobs;
    }

    /// <summary>The job named <paramref name="id"/>; refuses a workflow without it.</summary>
    internal static CiJob Job(string workflow, string id) =>
        Jobs(workflow).FirstOrDefault(j => j.Id == id)
        ?? throw new InvalidDataException($"the workflow has no `{id}` job (jobs: {string.Join(", ", Jobs(workflow).Select(j => j.Id))})");

    /// <summary>
    /// The items of the first <c>steps:</c> list in <paramref name="lines"/> (a job's, or a composite action's).
    /// The list is the run of lines deeper than the key; its items are the <c>- </c> lines at the indentation of the
    /// first one. Comment and blank lines never end it and are attributed to the item that follows them.
    /// </summary>
    internal static IReadOnlyList<CiStep> Steps(IReadOnlyList<string> lines)
    {
        int at = -1;
        for (int i = 0; i < lines.Count && at < 0; i++)
        {
            if (InlineSteps.IsMatch(lines[i]))
                throw new InvalidDataException($"`{lines[i].Trim()}` is a flow-style step list (only block-style `steps:` lists are read)");
            if (StepsKey.IsMatch(lines[i]))
                at = i;
        }
        if (at < 0)
            return [];

        int key = Indent(lines[at]), item = -1;
        var steps = new List<CiStep>();
        List<string> current = [], pending = [];
        for (int i = at + 1; i < lines.Count; i++)
        {
            string line = lines[i];
            if (IsTrivia(line))
            {
                pending.Add(line);
                continue;
            }
            int indent = Indent(line);
            bool isItem = line.TrimStart().StartsWith("- ", StringComparison.Ordinal) || line.Trim() == "-";
            if (indent < key || (indent == key && !(isItem && (item < 0 || item == key))))
                break;   // as shallow as `steps:` itself (a list may sit at the key's own indentation): the list ended
            if (isItem && (item < 0 || indent == item))
            {
                item = indent;
                if (current.Count > 0)
                    steps.Add(new CiStep(current, item));
                current = [line];
                pending.Clear();   // the comments above this item document it, and are not part of its lines
                continue;
            }
            if (current.Count == 0)
                throw new InvalidDataException($"`steps:` is followed by `{line.Trim()}`, not a `- ` item (only block-style step lists are read)");
            current.AddRange(pending);
            pending.Clear();
            current.Add(line);
        }
        if (current.Count > 0)
            steps.Add(new CiStep(current, item));
        return steps;
    }

    /// <summary>The same as <see cref="Steps(IReadOnlyList{string})"/> over a text (a composite action file).</summary>
    internal static IReadOnlyList<CiStep> Steps(string text) => Steps(Lines(text));

    /// <summary>A comment-only or blank line.</summary>
    internal static bool IsTrivia(string line)
    {
        string t = line.TrimStart();
        return t.Length == 0 || t[0] == '#';
    }

    internal static int Indent(string line) => line.Length - line.TrimStart().Length;

    internal static string[] Lines(string text) => text.ReplaceLineEndings("\n").Split('\n');

    /// <summary>The key/value pairs a step or job has at ONE indentation: the item line's own key (past its <c>- </c>) and
    /// every key of the step's mapping. A key nested deeper (a <c>with:</c> value) is not the step's own.</summary>
    internal static IEnumerable<(string Key, string Value)> OwnKeys(IEnumerable<string> lines, int mappingIndent)
    {
        foreach (string raw in lines)
        {
            string line = raw;
            int indent = Indent(line);
            if (line.TrimStart().StartsWith("- ", StringComparison.Ordinal))
            {
                line = line.TrimStart()[2..];
                indent += 2;
            }
            if (indent != mappingIndent)
                continue;
            Match m = Key.Match(line.TrimStart());
            if (m.Success)
                yield return (m.Groups["key"].Value, TrailingComment.Replace(m.Groups["value"].Value, "").Trim());
        }
    }

    internal static string Joined(IEnumerable<string> lines) => string.Concat(lines.Select(l => l + "\n"));
}

/// <summary>A job of a workflow: its id, the comments above its key and its body.</summary>
/// <param name="Id">The job's key under <c>jobs:</c>.</param>
/// <param name="Banner">The comment and blank lines directly above the job's key: they document THIS job.</param>
/// <param name="Lines">Every line under the key, comments inside the job included, the banner and the next job's banner
/// excluded.</param>
internal sealed record CiJob(string Id, IReadOnlyList<string> Banner, IReadOnlyList<string> Lines)
{
    /// <summary>The body, each line ending in <c>\n</c>.</summary>
    internal string Text => CiWorkflow.Joined(Lines);

    /// <summary>The body without comment-only lines, each line ending in <c>\n</c>.</summary>
    internal string Code => CiWorkflow.Joined(CodeLines);

    /// <summary>The body's lines but the comment-only ones (blank lines stay).</summary>
    internal IEnumerable<string> CodeLines => Lines.Where(l => !l.TrimStart().StartsWith('#'));

    /// <summary>The job's <c>steps:</c> list.</summary>
    internal IReadOnlyList<CiStep> Steps => CiWorkflow.Steps(Lines);

    /// <summary>The value of the job's own key <paramref name="key"/> (<c>runs-on</c>, <c>if</c>, <c>needs</c>), or null:
    /// a key two levels under <c>jobs:</c>, comment stripped.</summary>
    internal string? Value(string key) =>
        CiWorkflow.OwnKeys(Lines, 4).Where(k => k.Key == key).Select(k => k.Value).FirstOrDefault();
}

/// <summary>A step of a <c>steps:</c> list.</summary>
/// <param name="Lines">The step's lines from its <c>- </c> line to the line before the next item, comments inside it
/// included.</param>
/// <param name="ItemIndent">The indentation of its <c>- </c>.</param>
internal sealed record CiStep(IReadOnlyList<string> Lines, int ItemIndent)
{
    /// <summary>The step's text, each line ending in <c>\n</c>.</summary>
    internal string Text => CiWorkflow.Joined(Lines);

    /// <summary>The step's lines but the comment-only ones.</summary>
    internal IEnumerable<string> CodeLines => Lines.Where(l => !l.TrimStart().StartsWith('#'));

    /// <summary>The step's own keys in order (<c>id</c>, <c>name</c>, <c>uses</c>, <c>run</c>, <c>env</c>, <c>with</c> …), each
    /// with its inline value (empty for a block value), a trailing comment stripped.</summary>
    internal IEnumerable<(string Key, string Value)> Keys => CiWorkflow.OwnKeys(Lines, ItemIndent + 2);

    /// <summary>The inline value of the step's own key <paramref name="key"/>, or null when it has none: a key nested
    /// deeper (a <c>with:</c> value) is not the step's.</summary>
    internal string? Value(string key) => Keys.Where(k => k.Key == key).Select(k => k.Value).FirstOrDefault();

    /// <summary>The inline value of the input <paramref name="name"/> in the step's <c>with:</c> mapping (<c>overwrite</c>,
    /// <c>path</c>), or null: only a key one level under the step's own <c>with:</c> key counts, never the same key under
    /// <c>env:</c> or at the step's own level.</summary>
    internal string? Input(string name)
    {
        bool inWith = false;
        foreach (string raw in CodeLines)
        {
            string line = raw.TrimStart().StartsWith("- ", StringComparison.Ordinal) ? new string(' ', CiWorkflow.Indent(raw) + 2) + raw.TrimStart()[2..] : raw;
            int indent = CiWorkflow.Indent(line);
            if (indent == ItemIndent + 2)
                inWith = line.Trim().StartsWith("with:", StringComparison.Ordinal);
            else if (inWith && indent == ItemIndent + 4)
            {
                var key = CiWorkflow.OwnKeys([line], indent).FirstOrDefault();
                if (key.Key == name)
                    return key.Value;
            }
        }
        return null;
    }

    /// <summary>How the step is named in a message: its first <c>name</c>, <c>id</c> or <c>uses</c> key, else its first
    /// line.</summary>
    internal string Label => Keys.Where(k => k.Key is "name" or "id" or "uses").Select(k => k.Value).FirstOrDefault()
                             ?? Lines[0].Trim();
}
