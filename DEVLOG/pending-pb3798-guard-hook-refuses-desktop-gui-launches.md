## PB3798: the guard hook refuses a command that starts a desktop GUI application

The owner's order of 2026-10-25 ("Stop fucking running Obsidian") followed a PB1957 implementer (w20261703b, PB3789)
that ran `Start-Process` on Obsidian several times to read a Bases formula, so Obsidian windows kept opening on the
owner's desktop. Nothing in the guard hook refused a GUI launch.

Re-probe: before the change `forbidden_commands.py` passed `Start-Process obsidian`, `explorer .`, `start "" x.md`,
`Invoke-Item x.md`, `cmd /c start https://example.com` and a bare path to `Obsidian.exe` (exit 0 for each, fed as
command text; no case started anything).

Fix shape: rule 8 of `scripts/hooks/forbidden_commands.py`, with its model in the new `scripts/hooks/gui_launch.py`. The
model reads the shared parser's steps (`shell_location.steps`), as every other rule does since PB3198, so a `cmd /c`,
`pwsh -Command` or `bash -c` payload, a substitution and a script block are walked and a commit message, a `grep
obsidian` or a heredoc body that mentions the words is no launch. A step launches a GUI when it is (1) a launcher verb
(`Invoke-Item`/`ii`, `explorer`, `xdg-open`, `open`, `wslview`, `rundll32`, ...); (2) a named desktop application in
command position by name or path, also behind `timeout`, `env`, `nohup`, `xargs`, `wsl -e` and the call operator
(browsers run with `--headless` draw no window and pass); (3) `Start-Process`/`saps`/`start`, which are default-deny:
allowed only for a console tool started with `-NoNewWindow`, `-WindowStyle Hidden` or cmd's `/b`, with no `-Verb`
(`RunAs` is a UAC prompt), whose arguments hand no GUI program or URL to the console tool; a parameter prefix that is
ambiguous or unknown (`-W`) cannot be told to hide the window, so it refuses; (4) the library forms
`[System.Diagnostics.Process]::Start`, `New-Object -ComObject Shell.Application|WScript.Shell`, and Python's
`os.startfile` / `webbrowser.open`.

Tests: `scripts/hooks/test_forbidden_commands.py` gains `GUI_CASES` (Bash tool) and `GUI_PS_CASES` (PowerShell tool):
a refusal case for every form above and the neighbours that must still run (`Start-Process pwsh -NoNewWindow`, `dotnet`,
`start /b dotnet build`, `grep -rn obsidian`, `command -v obsidian`, a heredoc body, a commit message). The hook is
tested only by feeding it command text. 321/321 cases GREEN. `docs/DOC_INDEX.md` names the new rule and module.

Self-review: the one residue is inherent to a deny list. An unknown GUI executable run directly by path, or a GUI
launched from inside a script an agent writes and then runs, is not seen; the first is closed by adding the name to
`GUI_APPS`. A failure inside the model fails open (the hook never wedges a session) and the self-test is what proves the
rule fires. The MANDATORY-PRACTICES row for the rule stays with PB3797, per PB3798's acceptance.

Gate: see the landing record.
