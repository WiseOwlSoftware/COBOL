// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Runtime.IO;
using WiseOwl.Cobol.Tests.Shared;
using Xunit;
using Xunit.Sdk;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// ⛔ A TEST THAT OPENS A FILE EXTEND MEASURES A PROGRAM THE COMPILER ACCEPTS (kb/Work PB3479). §14.9.27.3 SR2: <i>"The EXTEND
/// phrase shall be specified only if the access mode of the file connector referenced by file-name-1 is sequential"</i>, and
/// the compiler refuses the other declarations (COBOL2372). <see cref="FileHarness.Register"/> declares a RELATIVE or
/// INDEXED connector with DYNAMIC access unless told otherwise, so a theory that crossed the open modes and registered with
/// the default opened EXTEND over a declaration no program makes, and the Table 19 and '61' answers it pinned were measured
/// on that. <see cref="FileHarness.AccessFor"/> is the one place the rule is written and <see cref="FileHarness.Open"/> is
/// the OPEN that refuses the disagreeing declaration; these tests pin both, over every organization.
/// </summary>
public sealed class FileHarnessExtendDeclarationTests
{
    public static TheoryData<FileOrg, FileOpenMode> EveryOrgAndOpenMode()
    {
        var data = new TheoryData<FileOrg, FileOpenMode>();
        foreach (var org in Enum.GetValues<FileOrg>())
            foreach (var mode in Enum.GetValues<FileOpenMode>())
                data.Add(org, mode);
        return data;
    }

    /// <summary>EXTEND is sequential access's and no other mode forces it: the declaration an OPEN of each mode can make.</summary>
    [Theory]
    [InlineData(FileOpenMode.Input, KeyedAccess.Dynamic)]
    [InlineData(FileOpenMode.Output, KeyedAccess.Dynamic)]
    [InlineData(FileOpenMode.IO, KeyedAccess.Dynamic)]
    [InlineData(FileOpenMode.Extend, KeyedAccess.Sequential)]
    public void TheAccessAnOpenModeCanDeclare_IsSequentialForExtendOnly(FileOpenMode mode, KeyedAccess expected) =>
        Assert.Equal(expected, FileHarness.AccessFor(mode));

    /// <summary>The declaration <see cref="FileHarness.AccessFor"/> gives opens in its own mode, in every organization:
    /// the success-family status of the OPEN (§14.9.27.4 GR4 with Table 18, an available file not currently open; none of Table 19's refusals applies to a lone connector).</summary>
    [Theory]
    [MemberData(nameof(EveryOrgAndOpenMode))]
    public void TheDeclarationAnOpenModeCanMake_OpensInThatMode(FileOrg org, FileOpenMode mode)
    {
        string host = FileHarness.Tmp($"decl-{org}-{mode}");
        try
        {
            FileHarness.Seed(host, org, ["SEED"]);
            var reg = new FileRegistry();
            FileHarness.Register(reg, "F", host, org, sharing: null, access: FileHarness.AccessFor(mode));
            FileHarness.Open(reg, "F", mode);
            Assert.True(reg.Status("F")[0] == '0', $"({org} {mode}) OPEN answered '{reg.Status("F")}'.");
            reg.CloseAll();
        }
        finally { FileHarness.TryDelete(host); }
    }

    /// <summary>⛔ THE DEFECT, REFUSED: EXTEND over the harness's default declaration (DYNAMIC) or a RANDOM one fails the
    /// test that made it, in each organization that has an access mode to disagree with — and a SEQUENTIAL file, whose
    /// access mode is always sequential, opens.</summary>
    [Theory]
    [InlineData(FileOrg.Relative, KeyedAccess.Dynamic)]
    [InlineData(FileOrg.Relative, KeyedAccess.Random)]
    [InlineData(FileOrg.Indexed, KeyedAccess.Dynamic)]
    [InlineData(FileOrg.Indexed, KeyedAccess.Random)]
    public void OpenExtend_OverANonSequentialDeclaration_FailsTheTest(FileOrg org, KeyedAccess declared)
    {
        string host = FileHarness.Tmp($"refuse-{org}-{declared}");
        try
        {
            FileHarness.Seed(host, org, ["SEED"]);
            var reg = new FileRegistry();
            FileHarness.Register(reg, "F", host, org, sharing: null, access: declared);
            var failure = Assert.Throws<FailException>(() => FileHarness.Open(reg, "F", FileOpenMode.Extend));
            Assert.Contains("§14.9.27.3 SR2", failure.Message);
        }
        finally { FileHarness.TryDelete(host); }
    }

    /// <summary>The shorthand every runtime file test opens with does not route around the guard: a theory that registers
    /// through <see cref="FileHarness.Register"/>'s default declaration and opens EXTEND with <c>OpenStatic</c> fails too
    /// (review round 1, R1-2).</summary>
    [Theory]
    [InlineData(FileOrg.Relative)]
    [InlineData(FileOrg.Indexed)]
    public void OpenStaticExtend_OverTheHarnessDefaultDeclaration_FailsTheTest(FileOrg org)
    {
        string host = FileHarness.Tmp($"static-{org}");
        try
        {
            FileHarness.Seed(host, org, ["SEED"]);
            var reg = new FileRegistry();
            FileHarness.Register(reg, "F", host, org, sharing: null);
            var failure = Assert.Throws<FailException>(() => reg.OpenStatic("F", FileOpenMode.Extend));
            Assert.Contains("§14.9.27.3 SR2", failure.Message);
            reg.OpenStatic("F", FileOpenMode.IO);   // the same declaration opens in the modes DYNAMIC admits
            Assert.Equal("00", reg.Status("F"));
            reg.CloseAll();
        }
        finally { FileHarness.TryDelete(host); }
    }

    [Fact]
    public void OpenExtend_OverASequentialFile_IsNotRefused_WhateverAccessItWasHanded()
    {
        string host = FileHarness.Tmp("seq-extend");
        try
        {
            FileHarness.Seed(host, FileOrg.Sequential, ["SEED"]);
            var reg = new FileRegistry();
            FileHarness.Register(reg, "F", host, FileOrg.Sequential, sharing: null);   // access: Dynamic is ignored by a sequential file
            FileHarness.Open(reg, "F", FileOpenMode.Extend);
            Assert.Equal("00", reg.Status("F"));
            reg.CloseAll();
        }
        finally { FileHarness.TryDelete(host); }
    }
}
