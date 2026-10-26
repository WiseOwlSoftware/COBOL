"""Which shell commands start a desktop GUI application — the model behind forbidden_commands.py rule 8 (kb/Work PB3798).

Owner order 2026-10-25 ("Stop fucking running Obsidian"): a dispatched agent (w20261703b, PB3789) ran `Start-Process`
on Obsidian over and over to read a Bases formula, and each launch put a window on the owner's desktop. An agent never
starts a desktop application: a window steals the focus of whoever is working there, and nothing a dispatched agent
reads needs one. Console tools run unchanged.

A step launches a GUI when it is, in this order of check:
  1. a launcher VERB that opens whatever the shell associates with its operand: `Invoke-Item`/`ii`, `explorer`,
     `xdg-open`, `open`, `wslview`, `gio`, `rundll32`, `mshta`, `wscript` and kin (`LAUNCHERS`);
  2. a desktop APPLICATION named in command position (`GUI_APPS`: Obsidian, a browser, an editor, an Office program, ...),
     by name or by path (`C:/.../Obsidian.exe`), also behind a prefix command (`timeout 5 obsidian`, `& obsidian`,
     `wsl -e explorer.exe`) and behind `cmd /c`, `pwsh -Command` and `bash -c`, which the shared parser has already
     walked into steps of their own;
  3. `Start-Process` / `saps` / PowerShell's `start` alias / cmd.exe's `start`, which launch ANYTHING (a document or a URL
     opens its associated application): refused unless the target is a console tool (`CONSOLE_TOOLS`) AND the command
     says it opens no window (`-NoNewWindow`, `-WindowStyle Hidden`, cmd's `/b`), has no `-Verb` (`-Verb RunAs` is a UAC
     prompt on the owner's screen), and hands no GUI program to the console tool through its arguments;
  4. the library forms of the same act: `[System.Diagnostics.Process]::Start`, `New-Object -ComObject Shell.Application`
     (and `WScript.Shell`), Python's `os.startfile` / `webbrowser.open` among a Python command's words.

This is a deny list for what is known to open a window plus a default-deny for the shell's generic launchers; an
unknown GUI executable run DIRECTLY by path is not caught (its name joins `GUI_APPS` when one is seen). The model reads
`shell_location.Step`s, never raw text, so a commit message, a `grep obsidian` and a heredoc body that mention these
words are no launch (the PB3198 lesson).
"""
from __future__ import annotations

import re

import shell_location as sl

# verbs that open their operand with the associated application
LAUNCHERS = {"invoke-item", "ii", "explorer", "xdg-open", "open", "wslview", "gio", "gnome-open", "kde-open", "rundll32",
             "mshta", "wscript", "control", "msiexec"}
# desktop applications (a browser, an editor, a viewer, an Office program, a chat client, a system tool with a window)
GUI_APPS = {"obsidian", "chrome", "msedge", "firefox", "brave", "opera", "vivaldi", "iexplore", "safari",
            "notepad", "notepad++", "wordpad", "mspaint", "calc", "mmc", "taskmgr", "regedit", "charmap", "snippingtool",
            "code", "code-insiders", "cursor", "windsurf", "devenv", "rider", "rider64", "idea64", "webstorm64",
            "sublime_text", "atom", "gedit", "kate", "nautilus", "dolphin", "thunar",
            "winword", "excel", "powerpnt", "outlook", "onenote", "acrord32", "acrobat",
            "slack", "discord", "teams", "ms-teams", "zoom", "spotify", "vlc", "steam", "mstsc", "gimp", "inkscape"}
# the browsers among them: run with `--headless` they open no window
BROWSERS = {"chrome", "msedge", "firefox", "brave", "opera", "vivaldi"}
# console programs: they write to the stream they are given, so a `Start-Process` of one that opens no window is a
# background job, not a desktop launch
CONSOLE_TOOLS = {"pwsh", "powershell", "cmd", "bash", "sh", "wsl", "dotnet", "python", "python3", "py", "git", "gh", "node",
                 "npm", "npx", "cobol", "cobc", "curl", "robocopy", "ping", "msbuild", "nuget", "pip", "pytest", "semgrep",
                 "sleep", "timeout"}
# commands that run another command named among their words
_PREFIX = {"timeout", "env", "sudo", "doas", "nohup", "nice", "ionice", "time", "xargs", "exec", "command", "builtin",
           "stdbuf", "watch", "wsl", "&", "call"}
_START = {"start-process", "saps", "start"}
_ASSIGN = re.compile(r"^[A-Za-z_][A-Za-z0-9_]*=")
_NUMBER = re.compile(r"^[0-9][0-9.]*[smhd]?$")
# Start-Process's parameters, by full name: a word `-x` names the ONE parameter it prefixes (PowerShell binds an
# unambiguous prefix); an ambiguous or unknown prefix is read as "cannot tell", which refuses
_START_VALUE = ("filepath", "argumentlist", "credential", "workingdirectory", "redirectstandarderror",
                "redirectstandardinput", "redirectstandardoutput", "verb", "windowstyle")
_START_SWITCH = ("loaduserprofile", "nonewwindow", "passthru", "wait", "usenewenvironment")
_PS_DOTNET_START = re.compile(r"\[(?:system\.)?diagnostics\.process\]::start\b", re.I)
_PY_LAUNCH = re.compile(r"\bos\.startfile\b|\bwebbrowser\.(?:open|get)\b")
_QUOTED = re.compile(r"'[^']*'|\"[^\"]*\"")

HOW = ("A dispatched agent starts no desktop application (owner order 2026-10-25, kb/Work PB3798: an agent launched "
       "Obsidian over and over on the owner's screen). Read the file or run the console tool directly; a background "
       "console job is `Start-Process <console tool> -NoNewWindow` (or `-WindowStyle Hidden`), and a long job in this "
       "repository goes through its script (build-local.ps1, enqueue.py). To open something for the owner, give them "
       "the path.")


def _command_word(words: tuple[str, ...]) -> int | None:
    """The index of the word naming the program a step runs: past `NAME=value` prefixes, the call operator and the
    prefix commands (`timeout 5 x`, `env A=1 x`, `wsl -e x`) with their options and numeric operands; None for an empty
    step."""
    k = 0
    while k < len(words):
        w = words[k]
        if _ASSIGN.match(w):
            k += 1
            continue
        name = sl.program(w)
        if name not in _PREFIX:
            return k
        k += 1
        if name in ("command", "builtin") and k < len(words) and words[k] in ("-v", "-V"):
            return None                         # `command -v obsidian` looks a program up; it runs nothing
        if name == "wsl":                       # `wsl [-d distro] -e x` / `wsl -- x` / `wsl x`: the command follows -e/--
            j = next((i for i in range(k, len(words)) if words[i] in ("-e", "--exec", "--")), None)
            if j is not None:
                k = j + 1
            continue
        while k < len(words) and (words[k].startswith("-") or _NUMBER.match(words[k]) or _ASSIGN.match(words[k])):
            k += 1
    return None


def _parameter(word: str, names: tuple[str, ...]) -> str | None:
    """The Start-Process parameter `-word` names: the only one it is a prefix of, else None."""
    low = word.lstrip("-").lower()
    hits = [n for n in names if n.startswith(low)] if low else []
    return hits[0] if len(hits) == 1 else None


def _start_refusal(words: tuple[str, ...]) -> str | None:
    """Why this Start-Process / `start` command may open a window or run a GUI, or None when it is a console tool opened
    in no window (arguments: the words after the verb)."""
    target: str | None = None
    quiet = False
    i = 0
    rest: list[str] = []
    while i < len(words):
        w = words[i]
        if w.startswith("/") and len(w) == 2 and w[1].lower() == "b":      # cmd.exe `start /b`: the current console
            quiet = True
        elif w.startswith("-") and len(w) > 1 and not w[1].isdigit():
            value = _parameter(w, _START_VALUE)
            switch = _parameter(w, _START_SWITCH)
            if value is None and switch is None:
                return f"`{w}` is not a recognizable Start-Process parameter, so whether it opens a window cannot be told"
            if switch == "nonewwindow":
                quiet = True
            elif value == "verb":
                return "Start-Process -Verb runs the target through the shell (`-Verb RunAs` is a UAC prompt on the owner's screen)"
            elif value is not None:
                if i + 1 >= len(words):
                    return f"`{w}` has no value"
                v = words[i + 1]
                i += 1
                if value == "filepath":
                    target = v
                elif value == "windowstyle":
                    quiet = quiet or v.lower() in ("hidden", "h", "hi", "hid", "hidd", "hidde")
                elif value == "argumentlist":
                    rest.append(v)
        elif w and w != '""' and w != "''" and target is None and not w.startswith("/"):
            target = w
        elif w.startswith("/") and len(w) > 2:
            pass                                                            # cmd.exe's other flags (`/min`, `/d dir`)
        else:
            rest.append(w)
        i += 1
    if target is None:
        return "no target program is named"
    name = sl.program(target)
    if name not in CONSOLE_TOOLS:
        return f"`{target}` is not a console tool, so it opens its own window or its associated application"
    if not quiet:
        return (f"`{target}` is started without `-NoNewWindow` / `-WindowStyle Hidden` / cmd's `/b`, so it gets a console "
                "window of its own")
    for token in re.split(r"[\s,;'\"@()]+", " ".join(rest)):
        if token and (sl.program(token) in GUI_APPS or sl.program(token) in LAUNCHERS
                      or re.match(r"^[a-z][a-z0-9+.-]*://", token, re.I)):
            return f"its arguments hand `{token}` to the console tool, which opens a desktop application"
    return None


def refusal(step: sl.Step) -> str | None:
    """Why this step starts a desktop GUI application, or None. The text names the offending form."""
    words = step.words
    k = _command_word(words)
    if k is None:
        return None
    name = sl.program(words[k])
    if name in LAUNCHERS:
        return f"`{words[k]}` opens its operand in the associated desktop application"
    if name in GUI_APPS:
        if name in BROWSERS and any(w.lower() in ("--headless", "-headless") or w.lower().startswith("--headless=")
                                    for w in words[k + 1:]):
            return None                         # a headless browser draws no window (print-to-pdf, screenshots)
        return f"`{words[k]}` is a desktop application"
    if name in _START:
        why = _start_refusal(words[k + 1:])
        if why:
            return f"`{words[k]}`: {why}"
    if name == "new-object" and any(w.lower() in ("shell.application", "wscript.shell") for w in words[k + 1:]):
        return "a Shell.Application / WScript.Shell COM object runs and opens programs through the desktop shell"
    if name in ("python", "python3", "py") and any(_PY_LAUNCH.search(w) for w in words[k + 1:]):
        return "`os.startfile` / `webbrowser.open` open the desktop application associated with their operand"
    if _PS_DOTNET_START.search(_QUOTED.sub("", step.text)):
        return "`[System.Diagnostics.Process]::Start` runs the program through the shell, which may open a window"
    return None
