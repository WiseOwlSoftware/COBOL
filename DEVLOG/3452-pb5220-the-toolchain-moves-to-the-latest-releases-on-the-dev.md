## Entry 3452 — 2026-11-10 18:18 PST — PB5220: the toolchain moves to the latest releases, on the dev box and in CI

The dev box updated to .NET SDK 10.0.404 at 16:49, and every local gate after that ended NOT RUN [toolchain] against
`toolchain.env`'s 10.0.403; the PB5158 fix sat ungated. On the owner's word ("We want the latest versions", "CI should
update to the same later versions") the manifest and its cloud copy name the newest releases, dotnet-sdk 10.0.404, pwsh
7.7.0 and semgrep 1.185.0, which CI installs from it. The dev box was brought to them with no gate running.

