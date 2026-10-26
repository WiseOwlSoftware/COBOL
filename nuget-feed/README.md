# nuget-feed — the repo-local NuGet feed

This directory is a NuGet package source (`nuget.config`, key `wiseowl-local`). Package source mapping sends exactly
one package here; every other package comes from nuget.org.

## Antlr4.Runtime.Standard 4.13.2-wiseowl.1

The ANTLR 4.13.2 C# runtime, built from our fork of ANTLR, with one upstream defect fixed.

- **Source:** https://github.com/WiseOwlSoftware/antlr4, branch `wiseowl/4.13.2-sll-predicate-merge-cache`: the
  upstream tag `4.13.2` plus one commit. The package's own `.nuspec` records the commit (`repository commit=`).
- **Why a fork:** the C# runtime on nuget.org stops at 4.13.1 (upstream never published 4.13.2 for C#), and both
  4.13.1 and 4.13.2 carry the defect below. The C# runtime of 4.13.2 differs from 4.13.1 only in its version
  strings, so this build also matches the 4.13.2 tool jar the parser is generated with
  (`src/WiseOwl.Cobol.Frontend/ANTLR4/antlr-4.13.2-complete.jar`).
- **The defect (kb/Work PB3504):** `PredictionMode.HasSLLConflictTerminatingPrediction` (pure SLL mode, configs
  with semantic predicates) copies the configuration set with the predicates removed and adds each configuration
  WITHOUT a merge cache. Merging the prediction contexts of configurations that differ only by predicate then
  re-merges every shared subgraph once per path, and the paths multiply with nesting: an EVALUATE operand holding
  four nested FUNCTION calls never finished. The fix gives that copy loop a `MergeCache`, as every other merge in
  the simulator has. The same commit fixes the Java runtime (the reference runtime ran out of memory at depth 5).
- **Identity:** the assembly is `Antlr4.Runtime.Standard`, version 4.13.2.0, strong-named with ANTLR's own key
  (`runtime/CSharp/src/Antlr4.snk`), so it is a drop-in for the upstream assembly. Only the package version
  (`-wiseowl.1`) marks it as ours.

### Rebuild it

```
git clone https://github.com/WiseOwlSoftware/antlr4.git
cd antlr4
git checkout wiseowl/4.13.2-sll-predicate-merge-cache
cd runtime/CSharp/src
dotnet build Antlr4.csproj -c Release -p:TargetFrameworks=netstandard2.0 -p:GeneratePackageOnBuild=false -p:Version=4.13.2-wiseowl.1
dotnet pack Antlr4.csproj --no-build -c Release -p:TargetFrameworks=netstandard2.0 -p:Version=4.13.2-wiseowl.1 \
  -p:RepositoryUrl=https://github.com/WiseOwlSoftware/antlr4 -p:RepositoryCommit=<the branch's commit> -o <this directory>
```

A change to the fork's branch is a new package version (`-wiseowl.2`, …) and a new pin in
`Directory.Packages.props`; never overwrite a published version's file.

### Leaving the fork

When an upstream ANTLR release carries the fix AND its C# runtime is on nuget.org, pin that version in
`Directory.Packages.props`, delete this directory and the `wiseowl-local` source and mapping from `nuget.config`, and
close the tracking note named in kb/Work PB3504.
