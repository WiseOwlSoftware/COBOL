## Plan §0 records the owner's tool-version decision (PB4007)

§0 gains a bullet for the owner's decision of 2026-10-28, which PB4007 implemented in #748: CI and every host run
the tool versions `toolchain.env` names, and those are the latest releases. It names the manifest, the CI action and the
host installers that read it, the daily freshness check and the gate's `NOT RUN [toolchain]` stop, and it records
winget and the removal of older versions on Windows.
