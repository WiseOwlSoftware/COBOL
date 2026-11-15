**2026-11-13, lead filed by the notes batch 20261113d.** UNVERIFIED LEAD: re-probe before fixing.

Further reports of the same red, each a Linux-only (WSL) self-test failure at `scripts/hooks/test_forbidden_commands.py` 265/267 with the same two cases ("git push https://github.com/WiseOwlSoftware/COBOL x" and "git push --repo=https://github.com/WiseOwlSoftware/COBOL"), each at a tree that touched no script or hook:

- wave 20262020 group C finisher (`w20262020c-PB5210-report.md`, "New defects"): "Linux-only self-test red: `scripts/hooks/test_forbidden_commands.py` expects PASS for `git push https://github.com/WiseOwlSoftware/COBOL x` and `git push --repo=https://github.com/WiseOwlSoftware/COBOL`, and gets a refusal under WSL (265/267). process_only. Repro: `bash scripts/linux-gate.sh --legs unit,conformance --nice`, `TestResults/linux-gate/selftests.log`. Site: `scripts/hooks/forbidden_commands.py` (the URL-push arm) or the self-test's scratch-repository remote. Its root is separate from PB5210's."
- wave 20262020 group F (`w20262020f-PB5337-report.md` item 1) at 050c7eef2d: "Site: scripts/hooks/forbidden_commands.py, URL-form push classification on Linux. Not yet checked against main."
- wave 20262020 journal, a group's lead at 92856558b2: "Site: the forbidden-commands URL-push arm, or the self-test's scratch-repo remote (PB5227, 513fc4e11a)."
- wave 20262022 group E (`w20262022e-PB5364-report.md`) at HEAD 168074671: "GREEN 267/267 on Windows at the same tree ... Site: `scripts/hooks/forbidden_commands.py` (the unit-push destination resolution of PB5269 and PB5227, which runs the hook from a scratch repository). It probably fails on Linux's URL or remote resolution in the scratch repository. Not verified."
- the same red appears in several other wave 20262020 and 20262022 result lines of the two journals (workflows wf_52dcfb9a-25b and wf_979d09fa-90c).

Open notes naming this red: PB5367 (the first report) and this one, PB5390 (the mechanism: `_pushes_elsewhere` compares a push URL's key with this clone's origin).
