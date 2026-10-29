## Entry 2724 — 2026-10-29 10:32 PDT — Two PB2101 leads filed (PB4047, PB4048)

Wave 20261747 A's PB2101 report left three leads; two are filed here and the third is already in PB2103. Each was
searched in `kb/Work/` first (CoreLib, CS0012, ILLink, PublishTrimmed, TPA, trimmed, toolchain.env), and no open note
covers either mechanism.

- **PB4047.** `RoslynBackend.BuildReferenceAssemblies` compiles the generated program against the host's trusted-platform
  assemblies, so an emitted assembly references `System.Private.CoreLib` and an ordinary MSBuild host that references it
  fails CS0012. It blocks the trimmed host test in DESIGN-external-repository 17.5.
- **PB4048.** `toolchain.env` does not pin the SDK-matched `Microsoft.NET.ILLink.Tasks`; under SDK 10.0.403 publish hangs
  on NuGet on this box, so no automated `PublishTrimmed` witness exists. It depends on PB4047 for the composed host form.

