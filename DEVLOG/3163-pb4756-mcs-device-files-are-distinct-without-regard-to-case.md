## Entry 3163 — 2026-11-05 14:56 PST — PB4756: MCS device files are distinct without regard to case

Re-probe: destinations `D1` and `d1` in one `mcs.json` were both accepted; on Windows both append to `D1.out`, so each SEND answered 00 for a device the other also feeds. The same held for two terminals, and for a terminal and a destination of one name (any host).

Fix: `MessageStoreState.FromDefinitions` now runs `RefuseSharedDeviceFiles` over the endpoints that own a device file (destinations with no `deliverTo`, and terminals) and refuses a definition file in which two are equal under `OrdinalIgnoreCase`; the file is then status 91 on every statement, like every other unusable definition, on every host so one definition file works on both OSes. Queues, sources and destinations that deliver to a queue own no file and may differ only in case.

Tests: four refusal cases added to `UnusableDefinitionFile_IsStatus91_OnEveryStatement` and the positive `NamesThatShareNoDeviceFile_DifferingOnlyInCaseAreDefined` in `MessageControlTests`. Design doc and CONFORMANCE.md determination updated. Rows closed: none (the MCS is implementor-defined).

