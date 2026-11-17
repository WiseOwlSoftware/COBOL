// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Reflection;
using System.Text.RegularExpressions;
using WiseOwl.Cobol.Runtime;
using WiseOwl.Cobol.Runtime.Exceptions;
using WiseOwl.Cobol.Runtime.IO;
using WiseOwl.Cobol.Tests.Shared;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// ⛔ A COBOL-85 PROGRAM IS TOLD ONLY THE I-O STATUS VALUES THE 1985 TEXT LISTS, OR A 9x THIS IMPLEMENTATION DOCUMENTS
/// (kb/Work PB5882; docs/FILES_DESIGN.md D33; the conformance statement's section 3.13). §1985:VII.1.3.5, §1985:VIII.1.3.4
/// and §1985:IX.1.3.4 each give "a list of the values placed in the I-O status for the previously named conditions", and a
/// condition the implementor adds is an "Implementor-Defined Condition With Unsuccessful Completion", 9x.
/// <para>The guard has three parts: the module lists are re-derived from <c>specs/ANSI_COBOL_1985.md</c> on every run;
/// EVERY <see cref="FileStatusCode"/> constant, found by reflection, must be listed by a 1985 module or have its own
/// 1985 value (so a status a later change adds fails here until it has its 1985 answer, and cannot leak); and every
/// projection, on every organization, is a 1985 value or a documented 9x that keeps the value's class and its
/// criticality, so the runtime's returned status and the program's status always steer the same phrase and hook.</para>
/// </summary>
public sealed class IoStatus1985Tests
{
    private static readonly string[] Organizations =
        [FixedFileAttributes.Sequential, FixedFileAttributes.Relative, FixedFileAttributes.Indexed];

    /// <summary>Every <see cref="FileStatusCode"/> constant: the runtime's whole status vocabulary.</summary>
    private static IEnumerable<string> RuntimeValues() =>
        typeof(FileStatusCode).GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(f => f.IsLiteral && f.FieldType == typeof(string))
            .Select(f => (string)f.GetRawConstantValue()!);

    /// <summary>The values a 1985 module's I-O status subclause lists, read from the transcription: the items
    /// "I-O Status = NN." from the subclause's heading to its 9x item, the last of the list.</summary>
    private static HashSet<string> ListedBy1985(string heading, int occurrence)
    {
        var lines = File.ReadAllLines(TestRepo.Specs("ANSI_COBOL_1985.md"));
        int at = -1;
        for (int i = 0, seen = 0; i < lines.Length; i++)
            if (lines[i].TrimStart('#', ' ').TrimEnd() == heading && seen++ == occurrence) { at = i; break; }   // the indexed chapter's heading carries no markdown marks
        Assert.True(at >= 0, $"the 1985 transcription has no occurrence {occurrence} of '{heading}'");
        var values = new HashSet<string>(StringComparer.Ordinal);
        for (int i = at + 1; i < lines.Length; i++)
        {
            var m = Regex.Match(lines[i].Replace("║", ""), @"^[a-h]\. I-O Status = (?<v>\d\d|9x)\.");
            if (!m.Success) continue;
            if (m.Groups["v"].Value == "9x") return values;
            values.Add(m.Groups["v"].Value);
        }
        throw new InvalidOperationException($"'{heading}' ({occurrence}) has no 9x item");
    }

    [Fact]
    public void TheModuleLists_AreThe1985TextsOwn()
    {
        Assert.Equal(ListedBy1985("1.3.5 I-O Status", 0).Order(), IoStatus1985.ModuleList(FixedFileAttributes.Sequential).Order());
        Assert.Equal(ListedBy1985("1.3.4 I-O Status", 0).Order(), IoStatus1985.ModuleList(FixedFileAttributes.Relative).Order());
        Assert.Equal(ListedBy1985("1.3.4 I-O Status", 1).Order(), IoStatus1985.ModuleList(FixedFileAttributes.Indexed).Order());
    }

    [Fact]
    public void EveryRuntimeStatus_IsListedBy1985_OrHasIts1985Value()
    {
        var listed = Organizations.SelectMany(IoStatus1985.ModuleList).ToHashSet(StringComparer.Ordinal);
        foreach (string value in RuntimeValues())
            Assert.True(listed.Contains(value) || IoStatus1985.Defined9x.ContainsKey(value) || IoStatus1985.OwnValues.ContainsKey(value),
                $"I-O status '{value}' is in no 1985 list and has no 1985 value: give it one in IoStatus1985.OwnValues (kb/Work PB5882)");
    }

    [Fact]
    public void EveryProjection_IsA1985Value_OfTheSameClassAndCriticality()
    {
        var listed = Organizations.SelectMany(IoStatus1985.ModuleList).ToHashSet(StringComparer.Ordinal);
        static char Class(string s) => s[0] is '0' or '1' or '2' ? s[0] : 'u';   // successful · at end · invalid key · other
        foreach (string value in RuntimeValues())
            foreach (string organization in Organizations)
            {
                string reported = IoStatus1985.Report(value, organization);
                Assert.True(listed.Contains(reported) || IoStatus1985.Defined9x.ContainsKey(reported),
                    $"'{value}' on a {organization} file is reported '{reported}', which no 1985 module lists and no 9x of this implementation is");
                if (reported[0] != '9' && !IoStatus1985.ModuleList(organization).Contains(reported))
                    Assert.True(reported[0] is '1' or '2',
                        $"'{value}' on a {organization} file is reported '{reported}', a value the file's own module does not list");
                Assert.Equal(Class(value), Class(reported));
                Assert.Equal(ExceptionCatalog.IsFatalIoStatus(value), ExceptionCatalog.IsFatalIoStatus(reported));
            }
    }

    [Fact]
    public void Each9x_NamesOneCondition()
    {
        var targets = IoStatus1985.OwnValues.Values.Where(v => v[0] == '9').ToList();
        Assert.Equal(targets.Count, targets.Distinct(StringComparer.Ordinal).Count());
        foreach (string nineX in targets)
        {
            Assert.True(IoStatus1985.Defined9x.ContainsKey(nineX), $"'{nineX}' is not a documented 9x");
            Assert.False(RuntimeValues().Contains(nineX), $"'{nineX}' is also a condition of its own at the later editions");
        }
        foreach (string own in IoStatus1985.OwnValues.Keys)
            Assert.False(Organizations.Any(o => IoStatus1985.ModuleList(o).Contains(own)), $"'{own}' is listed by 1985 and needs no projection");
        // Every documented 9x is either the runtime's own ('90', '91') or the projection of exactly one condition.
        foreach (string nineX in IoStatus1985.Defined9x.Keys)
            Assert.True(RuntimeValues().Contains(nineX) ^ targets.Contains(nineX), $"'{nineX}' is documented but names no condition, or names two");
    }

    [Theory]
    [InlineData("71", "99")]   // a line sequential record holding a character outside the line set: its own 9x
    [InlineData("06", "04")]   // §1985:VII.4.4.4 GR13's record length conflict
    [InlineData("09", "00")]   // a successful READ of which 1985 says no more
    [InlineData("00", "00")]
    [InlineData("10", "10")]
    public void TheProgramStatus_At85_IsThe1985Value_AndFrom2002_TheCondition(string condition, string at85)
    {
        Assert.Equal(at85, IoStatus1985.Report(condition, FixedFileAttributes.Sequential));
        Assert.False(DialectBehaviors.IsActive(DialectBehavior.IoStatusValuesOutside1985Lists, 85));
        Assert.True(DialectBehaviors.IsActive(DialectBehavior.IoStatusValuesOutside1985Lists, 2002));
    }

    [Theory]
    [InlineData("34", "30")]   // a permanent error the relative and indexed modules name only as "no further information"
    [InlineData("07", "00")]   // a successful completion the relative and indexed modules do not list
    [InlineData("24", "24")]
    [InlineData("14", "14")]
    public void AKeyedFileValue_IsItsModulesOwn(string condition, string onRelative)
    {
        Assert.Equal(onRelative, IoStatus1985.Report(condition, FixedFileAttributes.Relative));
    }
}
