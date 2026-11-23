// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
//
// THE TEST FRAMEWORK of every test host: xunit's own, with the gate's executor and assembly runner, which order every
// run (GateOrderer.cs; kb/Work PB4328, docs/rearchitecture/DESIGN-test-build-ci.md section 3.14.3) and run CI's shard
// of it (GateShard.cs, TestWeights.cs; kb/Work PB4329, the same document's section 1.4). tests/Directory.Build.props
// links tests/_shared/ into every test project under tests/ and names GateTestFramework in each one's
// [assembly: Xunit.TestFramework].
#nullable enable
using System.Reflection;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace WiseOwl.Cobol.Tests.Shared.Gate;

/// <summary>
/// xunit's own framework, with an executor that ORDERS every run (<see cref="GateOrderer"/>) and runs one weighted shard
/// of it when, and only when, CI's merge group names one (<see cref="GateShard"/>, kb/Work PB4329).
/// </summary>
/// <remarks>
/// <para>Ordering changes WHEN a case runs, never WHETHER (owner, 2026-09-28: "order, don't skip"): with no shard every
/// discovered case runs, and the gate's population check (<c>scripts/test_population.py</c>) proves it by name.</para>
/// <para>⛔ WHY A REFUSAL IS AN EXECUTION ERROR AND NOT A THROW (evidence <c>b3-order-probe</c>): xunit catches a
/// framework constructor that throws and silently falls back to its default framework, so every case runs and the
/// run is green. An executor that throws is a "Catastrophic failure" with no verdict line and no trx result. An
/// executor that reports every case as xunit's <see cref="ExecutionErrorTestCase"/> gives a failed result per case,
/// a <c>Failed!</c> verdict line and exit 1, so a shard variable no host can run never passes as a run.</para>
/// </remarks>
public class GateTestFramework(IMessageSink messageSink) : XunitTestFramework(messageSink)
{
    protected sealed override ITestFrameworkExecutor CreateExecutor(AssemblyName assemblyName) =>
        new GateExecutor(assemblyName, SourceInformationProvider, DiagnosticMessageSink);
}

/// <summary>The executor: resolves the shard once per run and hands the runner either every case or the shard's, in the
/// orderer's order, or every case as an execution error when the shard cannot be run.</summary>
internal sealed class GateExecutor(AssemblyName assemblyName, ISourceInformationProvider sourceInformationProvider,
    IMessageSink diagnosticMessageSink)
    : XunitTestFrameworkExecutor(assemblyName, sourceInformationProvider, diagnosticMessageSink)
{
    // xunit's executor contract is `async void` (XunitTestFrameworkExecutor.RunTestCases); the runner reports the
    // run's end through the execution sink, never through a returned task.
    protected override async void RunTestCases(IEnumerable<IXunitTestCase> testCases,
        IMessageSink executionMessageSink, ITestFrameworkExecutionOptions executionOptions)
    {
        var cases = testCases.ToList();
        IReadOnlyList<IXunitTestCase> selection = Select(cases, out GateOrderer? orderer);
        using var runner = new GateAssemblyRunner(TestAssembly, selection, DiagnosticMessageSink,
            executionMessageSink, executionOptions, orderer);
        await runner.RunAsync();
    }

    /// <summary>What this run executes, and the orderer that orders it (null for a refusal). A shard that cannot be read
    /// or dealt refuses the run: every case becomes an execution error naming the cause. An orderer that cannot be made
    /// (an unreadable first-classes file) costs the ORDER, never a case: xunit's order is kept and the cause is a
    /// diagnostic message.</summary>
    private IReadOnlyList<IXunitTestCase> Select(List<IXunitTestCase> cases, out GateOrderer? orderer)
    {
        orderer = null;
        string assemblyKey;
        GateShard? shard;
        try
        {
            assemblyKey = TestWeights.AssemblyKey(TestAssembly.Assembly.Name);
            shard = GateShard.Read(Environment.GetEnvironmentVariable);
        }
        catch (Exception ex)
        {
            return Refuse(cases, ex is GateShardException ? ex.Message : $"the shard could not be read: {ex}");
        }

        List<IXunitTestCase> mine;
        try
        {
            mine = shard is null ? cases : shard.CasesOf(cases, assemblyKey, TestWeights.Embedded());
        }
        catch (Exception ex)
        {
            return Refuse(cases, $"shard {shard} could not be dealt: {ex}");
        }

        try
        {
            GateOnly? only = GateOnly.Read(Environment.GetEnvironmentVariable);
            mine = only is null ? mine : only.CasesOf(mine);
        }
        catch (Exception ex)
        {
            return Refuse(cases, ex is GateOnlyException ? ex.Message : $"the retry's case names could not be read: {ex}");
        }

        try
        {
            orderer = GateOrderer.For(assemblyKey, TestWeights.Embedded(), Environment.GetEnvironmentVariable);
        }
        catch (Exception ex)
        {
            DiagnosticMessageSink.OnMessage(new DiagnosticMessage($"GATE ORDER: xunit's order is kept ({ex.Message})"));
        }

        return mine;
    }

    private List<IXunitTestCase> Refuse(List<IXunitTestCase> cases, string cause) =>
        cases.Select(c => (IXunitTestCase)new ExecutionErrorTestCase(DiagnosticMessageSink,
                TestMethodDisplay.ClassAndMethod, TestMethodDisplayOptions.None, c.TestMethod,
                $"{GateShard.RefusalPrefix}{cause} (case {c.DisplayName})")).ToList();
}

/// <summary>xunit's assembly runner with the gate's collection orderer installed.</summary>
internal sealed class GateAssemblyRunner(ITestAssembly testAssembly, IEnumerable<IXunitTestCase> testCases,
    IMessageSink diagnosticMessageSink, IMessageSink executionMessageSink,
    ITestFrameworkExecutionOptions executionOptions, GateOrderer? orderer)
    : XunitTestAssemblyRunner(testAssembly, testCases, diagnosticMessageSink, executionMessageSink, executionOptions)
{
    /// <summary>xunit reads assembly-level <c>[TestCollectionOrderer]</c> attributes in
    /// <see cref="XunitTestAssemblyRunner.AfterTestAssemblyStartingAsync"/>; the gate's orderer is installed after it.</summary>
    protected override async Task AfterTestAssemblyStartingAsync()
    {
        await base.AfterTestAssemblyStartingAsync();
        if (orderer is not null)
        {
            TestCollectionOrderer = orderer.Collections(TestCases);
        }
    }
}
