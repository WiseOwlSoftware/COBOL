**2026-11-18: the merge group of pb4950-sweep went red on main's new src project (run 44281451776); fixed in the branch.**

- **The red.** `EditionCiteOneDeciderDriftTests` built its Roslyn compilations from a fixed list of five src projects. Main #1865
  (49ef04bf25) added `src/WiseOwl.Cobol.Listing`, which `WiseOwl.Cobol.Compiler` references. The merged tree's Compiler reached a
  reference the list never built, and `built[reference]` threw `KeyNotFoundException`. The branch's own gate could not see it:
  the project was not on the branch until the merge group merged main in.
- **The root fix: no fixed list remains.**
  - `DiscoverSrcProjects` reads every `src/**/*.csproj`. From each file it takes the properties MSBuild would use: AssemblyName
    and TargetFramework (or the file's name and the `Directory.Build.props` default), `IsRoslynComponent`, its project
    references, and its analyzer references (`OutputItemType="Analyzer"`). Every path goes through `TestRepo.MsBuildPath`.
  - The product compilations are the non-Roslyn-component projects, built in reference order: each project after every project
    it references. A cycle fails with its members named.
  - A reference to a project outside the discovered set fails with an assertion that names both files. Planted here: a
    ProjectReference added to the Listing csproj gave that message, not an exception. The csproj was then restored, and
    `git status` showed only the test changed.
  - The generator list is gone too. A project's source generators are its analyzer references, loaded from the referenced
    project's own `bin/<config>/<its TargetFramework>/<its AssemblyName>.dll`. Nothing names `WiseOwl.Cobol.Compiler.SourceGen`
    or `WiseOwl.Cobol.Compiler` any more.
- **The sweep for the same assumption.** Every test class that enumerates src projects passed on the merged tree: RepoDrift
  175/175, Unit 41504/41504, Characterization `RuntimeApiGuardTests`, and the Conformance classes CompiledProgramCache,
  EditionGateArm, EditionMuted and UsageInheritance (230/230).
  - **One sibling assumption remains, outside this branch's scope:** `CompiledProgramCacheDriftTests`. Its
    `EveryAmbientReadInTheCompiler_IsRecorded` and `TheCompiler_HasNoPublicMutableStatic` name the compiler as Compiler,
    Frontend and Editions. Listing is now in the compiler's closure, and neither scan reads it.
  - It has no ambient read today, so nothing is wrong yet. A clock or file read added to Listing would still run unkeyed.
  - Fix: derive the scanned projects from `CompiledProgramCache.CompilerClosure()` (its `WiseOwl.Cobol.*` product assemblies
    less the runtime), not from a list. That is a change to the cache's contract test, and it belongs to its owner.
