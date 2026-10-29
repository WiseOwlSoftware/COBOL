// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Runtime;
using WiseOwl.Cobol.Runtime.Repository;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// kb/Work PB2101 — <see cref="RepositoryLocator"/> over <see cref="RepositoryIndex"/> (DESIGN-external-repository §7.4,
/// §15.2): the named file first and nothing else read when it answers, the directory index on a miss (so a program whose
/// assembly is not named after it is found), a collision of two different assemblies, the word rows of §8.3, the schema
/// check, and the file rules (files only, no recursion, an exact <c>.dll</c> extension, the size cap). Every file here is a real
/// assembly built with Roslyn carrying a real <c>[CobolRepository]</c> record, as a compiled module does.
/// </summary>
public sealed class RepositoryLocatorTests : WiseOwlCobolTestBase
{
    /// <summary>A library whose record indexes <paramref name="rows"/> (<c>kind:name:word</c>, written by
    /// <see cref="RepositoryIndex.EncodeUnit"/>) at schema <paramref name="schema"/>. The build is deterministic, so two files
    /// of the same rows are the same build (one module version id) and two of different rows are two assemblies.</summary>
    private static byte[] Module(int schema, params (RepositoryUnitKind Kind, string Name, string Word)[] rows)
    {
        string units = string.Join(", ", rows.Select(r => $"\"{RepositoryIndex.EncodeUnit(r.Kind, r.Name, r.Word).Replace("\\", "\\\\", StringComparison.Ordinal)}\""));
        return RepositoryIndexTests.Compile(
            $"[assembly: WiseOwl.Cobol.Runtime.Repository.CobolRepository({schema}, \"{RuntimeAbi.Version}\", {RuntimeAbi.CallAbi}, "
            + $"Registrar = \"Cobol.M.__CobolModule\", Units = new string[] {{ {units} }})]");
    }

    private static byte[] Module(params (RepositoryUnitKind Kind, string Name, string Word)[] rows) =>
        Module(CobolRepositoryAttribute.CurrentSchemaVersion, rows);

    private string Put(string file, byte[] image)
    {
        string path = Path.Combine(TempDir, file);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllBytes(path, image);
        return path;
    }

    private static (RepositoryUnitKind, string, string) Program(string name) => (RepositoryUnitKind.Program, name, name);

    [Fact]
    public void TheNamedFile_IsTheAnswer_AndNothingElseIsRead()
    {
        string pay = Put("PAY.dll", Module(Program("PAY")));
        // Another build carrying PAY beside it: the named file answered, so the directory is never scanned and there is no collision.
        Put("PAY - Copy.dll", Module(Program("PAY"), Program("OTHER")));
        var found = RepositoryLocator.Find(TempDir, RepositoryUnitKind.Program, "PAY");
        Assert.NotNull(found.Entry);
        Assert.Equal(pay, found.Entry!.Path);
        Assert.Null(found.Collision);
        Assert.Null(found.Reason);
        Assert.Equal("Cobol.M.__CobolModule", found.Entry.Registrar);
        Assert.Equal(RuntimeAbi.CallAbi, found.Entry.CallAbi);
        Assert.Equal(RuntimeAbi.Version.ToString(), found.Entry.RuntimeVersion);
        Assert.Equal(CobolRepositoryAttribute.CurrentSchemaVersion, found.Entry.SchemaVersion);
        Assert.Equal(new RepositoryUnitRow(RepositoryUnitKind.Program, "PAY", "PAY"), found.Entry.Unit);
    }

    [Fact]
    public void AProgramWhoseAssemblyIsNotNamedAfterIt_IsFoundByTheDirectoryIndex_UnderTheAnnexCFold()
    {
        string lib = Put("Lib.dll", Module(Program("PAYROLL-CALC"), (RepositoryUnitKind.Function, "FUNC", "FUNC")));
        var found = RepositoryLocator.Find(TempDir, RepositoryUnitKind.Program, "payroll-calc");
        Assert.Equal(lib, found.Entry?.Path);
        Assert.Equal("PAYROLL-CALC", found.Entry!.Unit.ExternalizedName);
        // The kind is part of the question: the function is not a program, and the program is not a function.
        Assert.Null(RepositoryLocator.Find(TempDir, RepositoryUnitKind.Program, "FUNC").Entry);
        Assert.Null(RepositoryLocator.Find(TempDir, RepositoryUnitKind.Function, "PAYROLL-CALC").Entry);
        Assert.Equal(lib, RepositoryLocator.Find(TempDir, RepositoryUnitKind.Function, "func").Entry?.Path);
    }

    [Fact]
    public void TwoDifferentAssembliesCarryingOneName_AreACollision_AndTwoCopiesOfOneBuildAreNot()
    {
        string a = Put("A.dll", Module(Program("DUP")));
        string b = Put("B.dll", Module(Program("DUP"), Program("EXTRA")));
        var collision = RepositoryLocator.Find(TempDir, RepositoryUnitKind.Program, "DUP");
        Assert.Equal(a, collision.Entry?.Path);   // the first in ordinal path order
        Assert.Equal(b, collision.Collision);

        string dir = Path.Combine(TempDir, "copies");
        Directory.CreateDirectory(dir);
        File.WriteAllBytes(Path.Combine(dir, "One.dll"), Module(Program("DUP")));
        File.WriteAllBytes(Path.Combine(dir, "Two.dll"), Module(Program("DUP")));
        var same = RepositoryLocator.Find(dir, RepositoryUnitKind.Program, "DUP");
        Assert.Equal(Path.Combine(dir, "One.dll"), same.Entry?.Path);
        Assert.Null(same.Collision);
    }

    [Fact]
    public void AFunctionIsFoundByItsWord_TheWordRowsOfTheIndex()
    {
        string lib = Put("Fns.dll", Module((RepositoryUnitKind.Function, "EXT-NAME", "MY-WORD"), Program("MY-WORD")));
        var byWord = RepositoryLocator.FunctionByWord(TempDir, "my-word");
        Assert.Equal(lib, byWord.Entry?.Path);
        Assert.Equal("EXT-NAME", byWord.Entry!.Unit.ExternalizedName);   // the word is not the externalized name
        Assert.Null(RepositoryLocator.FunctionByWord(TempDir, "EXT-NAME").Entry);   // a word, not a name
        Assert.Null(RepositoryLocator.FunctionByWord(TempDir, "NO-SUCH").Entry);
        Assert.Equal(lib, RepositoryLocator.Find(TempDir, RepositoryUnitKind.Function, "ext-name").Entry?.Path);
    }

    [Fact]
    public void AnAssemblyOfAnotherSchema_IsNoAnswer_AndTheNamedFileSaysSo()
    {
        Put("OLD.dll", Module(1, Program("OLD")));
        var named = RepositoryLocator.Find(TempDir, RepositoryUnitKind.Program, "OLD");
        Assert.Null(named.Entry);
        Assert.Contains("schema v1", named.Reason, StringComparison.Ordinal);
        Assert.Contains($"reads v{CobolRepositoryAttribute.CurrentSchemaVersion}", named.Reason, StringComparison.Ordinal);
        Assert.Contains("recompile", named.Reason, StringComparison.Ordinal);

        // Not named after the program, it is skipped by the scan like any other file that is no entry.
        Put("Elsewhere.dll", Module(1, Program("ALSO-OLD")));
        Assert.Null(RepositoryLocator.Find(TempDir, RepositoryUnitKind.Program, "ALSO-OLD").Entry);
    }

    [Fact]
    public void AFileThatIsNoEntry_IsAReasonWhenNamedAfterTheUnit_AndSkippedByTheScan()
    {
        Put("NOTASM.dll", System.Text.Encoding.ASCII.GetBytes("this is not an assembly"));
        var named = RepositoryLocator.Find(TempDir, RepositoryUnitKind.Program, "NOTASM");
        Assert.Null(named.Entry);
        Assert.Contains("not a managed assembly", named.Reason, StringComparison.Ordinal);

        // An assembly with no repository record (the runtime itself is one) is no entry either; the scan skips it and a
        // later file still answers.
        File.Copy(typeof(RepositoryLocator).Assembly.Location, Path.Combine(TempDir, "WiseOwl.Cobol.Runtime.dll"));
        var norecord = RepositoryLocator.Find(TempDir, RepositoryUnitKind.Program, "WiseOwl.Cobol.Runtime");
        Assert.Null(norecord.Entry);
        Assert.Contains("carries no CobolRepository record", norecord.Reason, StringComparison.Ordinal);
        string zed = Put("Zed.dll", Module(Program("ZED-PROG")));
        Assert.Equal(zed, RepositoryLocator.Find(TempDir, RepositoryUnitKind.Program, "ZED-PROG").Entry?.Path);
    }

    [Fact]
    public void ANamedFileThatDoesNotCarryTheUnit_SaysSo_WhenNoOtherFileDoes()
    {
        Put("P2847Q.dll", Module(Program("P2847R")));
        var miss = RepositoryLocator.Find(TempDir, RepositoryUnitKind.Program, "P2847Q");
        Assert.Null(miss.Entry);
        Assert.Contains("does not carry the program P2847Q", miss.Reason, StringComparison.Ordinal);
        Assert.Contains("P2847Q.dll", miss.Reason, StringComparison.Ordinal);
        // The function kind of the same name is a different question with its own words.
        Assert.Contains("does not carry the function P2847Q",
            RepositoryLocator.Find(TempDir, RepositoryUnitKind.Function, "P2847Q").Reason, StringComparison.Ordinal);
        // Another file that does carry it is the answer, and then the named file's reason means nothing.
        string other = Put("Other.dll", Module(Program("P2847Q")));
        var hit = RepositoryLocator.Find(TempDir, RepositoryUnitKind.Program, "P2847Q");
        Assert.Equal(other, hit.Entry?.Path);
        Assert.Null(hit.Reason);
    }

    [Fact]
    public void TheDirectoryRules_FilesOnly_NoRecursion_AnExactExtension_AMissingDirectoryIsAMiss()
    {
        Put("sub/Deep.dll", Module(Program("DEEP")));         // a subdirectory is never read
        Put("Wrong.dll.bak", Module(Program("BAKED")));       // not a .dll
        Put("Wrong.dllx", Module(Program("DLLX")));           // "*.dll" matches this on some file systems; the extension is exact
        Directory.CreateDirectory(Path.Combine(TempDir, "folder.dll"));   // a directory named like a file
        Assert.Null(RepositoryLocator.Find(TempDir, RepositoryUnitKind.Program, "DEEP").Entry);
        Assert.Null(RepositoryLocator.Find(TempDir, RepositoryUnitKind.Program, "BAKED").Entry);
        Assert.Null(RepositoryLocator.Find(TempDir, RepositoryUnitKind.Program, "DLLX").Entry);
        var none = RepositoryLocator.Find(Path.Combine(TempDir, "no-such-directory"), RepositoryUnitKind.Program, "ANY");
        Assert.Null(none.Entry);
        Assert.Null(none.Reason);
        Assert.Null(RepositoryLocator.Find(TempDir, RepositoryUnitKind.Program, "").Entry);
    }

    [Fact]
    public void AnExternalizedNameThatCannotBeAFileName_IsFoundThroughItsIndexRow()
    {
        string lib = Put("Slashed.dll", Module(Program("A/B"), Program("C:D")));
        Assert.Equal(lib, RepositoryLocator.Find(TempDir, RepositoryUnitKind.Program, "A/B").Entry?.Path);
        Assert.Equal(lib, RepositoryLocator.Find(TempDir, RepositoryUnitKind.Program, "C:D").Entry?.Path);
        Assert.Null(RepositoryLocator.Find(TempDir, RepositoryUnitKind.Program, "..\\X").Entry);
    }

    [Fact]
    public void AFileOverTheSizeCap_IsSkippedUnread_AndListedForReport()
    {
        string huge = Path.Combine(TempDir, "Huge.dll");
        using (var stream = new FileStream(huge, FileMode.Create)) stream.SetLength(RepositoryLocator.MaxFileBytes + 1);
        string fine = Put("FINE.dll", Module(Program("FINE")));
        Assert.Equal([huge], RepositoryLocator.SkippedForSize(TempDir));
        Assert.Equal(fine, RepositoryLocator.Find(TempDir, RepositoryUnitKind.Program, "FINE").Entry?.Path);
        var named = RepositoryLocator.Find(TempDir, RepositoryUnitKind.Program, "Huge");
        Assert.Null(named.Entry);
        Assert.Contains("larger than", named.Reason, StringComparison.Ordinal);
    }

    /// <summary>A file is read once per process and remembered by its path, length and last write time (§7.4, §16): the next
    /// build of the same file is a new answer.</summary>
    [Fact]
    public void AReplacedFile_IsReadAgain()
    {
        string path = Put("Moving.dll", Module(Program("FIRST")));
        Assert.NotNull(RepositoryLocator.Find(TempDir, RepositoryUnitKind.Program, "FIRST").Entry);
        File.WriteAllBytes(path, Module(Program("SECOND"), Program("THIRD")));
        File.SetLastWriteTimeUtc(path, DateTime.UtcNow.AddMinutes(5));
        Assert.Null(RepositoryLocator.Find(TempDir, RepositoryUnitKind.Program, "FIRST").Entry);
        Assert.NotNull(RepositoryLocator.Find(TempDir, RepositoryUnitKind.Program, "THIRD").Entry);
    }
}
