## Nightly 2026-11-18: the toolchain bump (gh, uv, semgrep)

The nightly (run 44365192584) was red on `latest`: toolchain.env was BEHIND by git-windows 2.56.0 -> 2.56.1, gh 2.104.0 ->
2.105.0, uv 0.13.8 -> 0.13.9, semgrep 1.185.0 -> 1.186.0. `python scripts/toolchain.py bump` moved toolchain.env and
scripts/cloud/setup-env.sh together (dotnet-sdk 11 left alone: the owner's decision). The same nightly's Shard spread red
landed separately (branch shard-weights-1118, PR #1878).

This landing moves gh, uv and semgrep only. The owner's `winget upgrade Git.Git` on the dev box failed with installer exit
code 1, and winget does not yet carry Git for Windows 2.56.1, so the host stays at git 2.56.0. GIT_WINDOWS_VERSION keeps
2.56.0 so that every gate on this host can run (`toolchain.py check` is GREEN at this tree), and the nightly's `latest`
stays BEHIND on git-windows alone until a later change moves it together with the host install.

The same gate found a host dependence (kb/Work PB5942): .NET 11 preview and RC SDKs appeared on the dev box mid-gate, and
PlatformAssumptionTests' child, run with `dotnet run` in a scratch directory outside the repository, was built by the newest
SDK. The child directory now carries the repository's global.json.
