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
  3. a DOCUMENT run in command position (`& ./x.md`, `.\report.html`, `cmd /c x.md`): PowerShell and cmd.exe hand a file
     that is no program to ShellExecute, which opens its associated application exactly as `Invoke-Item` does
     (`DOCUMENT_EXTENSIONS`; branch review R1-1). The step's own shell decides: Bash execs the file (a shebang script
     `./app.cs` is no launch) and a PowerShell statement that starts with a variable (`$resp.json`) is an expression, not a
     command, unless the call operator runs it (R2-3). The call and dot-source operators also run a parenthesized string
     as the command NAME (`& ("obsidian")`, `. ('C:/.../Obsidian.exe')`), so the string stands in the command word (R2-1);
  4. `Start-Process` / `saps` / PowerShell's `start` alias / cmd.exe's `start`, which launch ANYTHING (a document or a URL
     opens its associated application): refused unless the target is a console tool (`CONSOLE_TOOLS`) AND the command
     says it opens no window (`-NoNewWindow`, `-WindowStyle Hidden`, cmd's `/b`), has no `-Verb` (`-Verb RunAs` is a UAC
     prompt on the owner's screen), and the command line it hands the tool is itself no launch: the tool and its
     arguments are re-read as a command (`Start-Process cmd -ArgumentList '/c','start','x.md'` is `cmd /c start x.md`,
     whose `start` is refused), and a SHELL (`cmd`, `pwsh`, `bash` ...) is also refused when any of its arguments names a
     launcher, a start verb or a desktop application, because a shell may take its arguments as a command in a form the
     parser cannot tell (`pwsh Start-Process x.md`; branch review R1-2). Start-Process's positional second argument is its
     -ArgumentList, so `Start-Process cmd -NoNewWindow "/c start x.md"` is read like the named form (R2-2). A non-shell console tool (`git clone <url>`,
     `python x.py open`) is handed data, so its arguments are no launch;
  5. the library forms of the same act: `[System.Diagnostics.Process]::Start`, `New-Object -ComObject Shell.Application`
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
# files that are no program: named in command position, PowerShell and cmd.exe open them in the application the desktop
# associates with the extension (documents, media, project files an IDE owns, shortcuts and Windows Script Host files)
DOCUMENT_EXTENSIONS = {"md", "markdown", "txt", "text", "log", "rtf", "pdf", "doc", "docx", "docm", "odt", "xls", "xlsx", "xlsm",
                       "ods", "csv", "tsv", "ppt", "pptx", "odp", "html", "htm", "xhtml", "mht", "mhtml", "svg", "xml", "json",
                       "yaml", "yml", "toml", "ini", "cfg", "conf", "base", "canvas", "png", "jpg", "jpeg", "gif", "bmp", "ico",
                       "webp", "tif", "tiff", "mp3", "mp4", "wav", "avi", "mkv", "mov", "zip", "7z", "rar", "sln", "slnx",
                       "csproj", "vcxproj", "ipynb", "trx", "binlog", "cob", "cbl", "cpy", "cs", "c", "h", "cpp", "java", "ts",
                       "lnk", "url", "vbs", "vbe", "wsf", "jse", "js", "hta", "msi", "msc", "cpl", "scr"}
# console programs: they write to the stream they are given, so a `Start-Process` of one that opens no window is a
# background job, not a desktop launch
CONSOLE_TOOLS = {"pwsh", "powershell", "cmd", "bash", "sh", "wsl", "dotnet", "python", "python3", "py", "git", "gh", "node",
                 "npm", "npx", "cobol", "cobc", "curl", "robocopy", "ping", "msbuild", "nuget", "pip", "pytest", "semgrep",
                 "sleep", "timeout"}
# commands that run another command named among their words
_PREFIX = {"timeout", "env", "sudo", "doas", "nohup", "nice", "ionice", "time", "xargs", "exec", "command", "builtin",
           "stdbuf", "watch", "wsl", "&", "call"}
_START = {"start-process", "saps", "start"}
# console programs that take their arguments as a command line of their own
_SHELLS = {"cmd", "pwsh", "powershell", "bash", "sh", "wsl"}
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
# the words an argument string holds, as the console tool will see them (`'/c','start','x.md'`, `"/c start x.md"`)
_ARG_WORDS = re.compile(r"[\s,;'\"@()]+")
# a PowerShell expression that is one quoted string, `("obsidian")` / `$("obsidian")`: the command NAME when the call or
# dot-source operator runs it (`& ("obsidian")`), a plain string anywhere else
_PS_STRING_EXPR = re.compile(r"^\$?\(+\s*(['\"])(.*)\1\s*\)+$", re.S)

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


def _start_refusal(words: tuple[str, ...], shell: str) -> str | None:
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
                    rest.extend(t for t in _ARG_WORDS.split(v) if t)
        elif w and w != '""' and w != "''" and target is None and not w.startswith("/"):
            target = w
        elif w.startswith("/") and len(w) > 2 and target is None:
            pass                                                            # cmd.exe's other flags (`/min`, `/d dir`)
        else:
            # past the target a word is the tool's positional -ArgumentList (position 1: `Start-Process cmd "/c start x.md"`),
            # whatever it starts with, so it is re-read below like the named -ArgumentList
            rest.extend(t for t in _ARG_WORDS.split(w) if t)
        i += 1
    if target is None:
        return "no target program is named"
    name = sl.program(target)
    if name not in CONSOLE_TOOLS:
        return f"`{target}` is not a console tool, so it opens its own window or its associated application"
    if not quiet:
        return (f"`{target}` is started without `-NoNewWindow` / `-WindowStyle Hidden` / cmd's `/b`, so it gets a console "
                "window of its own")
    if name in _SHELLS:
        for token in rest:
            if sl.program(token) in GUI_APPS or sl.program(token) in LAUNCHERS or sl.program(token) in _START:
                return f"its arguments hand `{token}` to the shell `{target}`, which opens a desktop application"
    # the tool and its arguments, read as the command the tool runs (`cmd /c x.md`, `pwsh -c Start-Process x.md`)
    for st in sl.steps(" ".join([target, *rest]), None, shell):
        why = refusal(st)
        if why:
            return f"handed to `{target}` it is a launch: {why}"
    return None


def _extension(word: str) -> str:
    """The lower-case extension of the file a command word names ('' for none)."""
    base = word.replace("\\", "/").rsplit("/", 1)[-1].lower()
    return base.rsplit(".", 1)[1] if "." in base.strip(".") else ""


def refusal(step: sl.Step) -> str | None:
    """Why this step starts a desktop GUI application, or None. The text names the offending form. The step's own
    `shell` decides the syntax rules that differ (a payload of `cmd /c` inside a Bash call is cmd.exe's)."""
    shell = step.shell
    words = step.words
    if shell == sl.POWERSHELL and step.substitution and step.text.lstrip()[:1] in ("'", '"'):
        return None                             # `('obsidian')`: a string in a PowerShell expression, no command ...
    k = _command_word(words)
    if k is None:
        return None
    called = k > 0 and words[k - 1] == "&"
    if shell == sl.POWERSHELL and words[k] == "." and k + 1 < len(words):
        k, called = k + 1, True                 # dot-source operator: the next word is the command it runs
    if shell == sl.POWERSHELL and called and (m := _PS_STRING_EXPR.match(words[k])):
        # ... unless the call / dot-source operator runs it: `& ("obsidian")` and `. ('C:/.../Obsidian.exe')` name the
        # program (branch review R2-1), so the string stands in the command word
        words = (*words[:k], m.group(2), *words[k + 1:])
    elif shell == sl.POWERSHELL and not called and words[k].startswith("$"):
        return None                             # `$resp.json`: an expression statement (a property read), no command
    name = sl.program(words[k])
    if shell != sl.BASH and _extension(words[k]) in DOCUMENT_EXTENSIONS:
        # PowerShell and cmd.exe hand a non-program to ShellExecute; Bash execs the file (a shebang script, or an error)
        return f"`{words[k]}` is a document, and running it opens the application associated with its extension"
    if name in LAUNCHERS:
        return f"`{words[k]}` opens its operand in the associated desktop application"
    if name in GUI_APPS:
        if name in BROWSERS and any(w.lower() in ("--headless", "-headless") or w.lower().startswith("--headless=")
                                    for w in words[k + 1:]):
            return None                         # a headless browser draws no window (print-to-pdf, screenshots)
        return f"`{words[k]}` is a desktop application"
    if name in _START:
        why = _start_refusal(words[k + 1:], shell)
        if why:
            return f"`{words[k]}`: {why}"
    if name == "new-object" and any(w.lower() in ("shell.application", "wscript.shell") for w in words[k + 1:]):
        return "a Shell.Application / WScript.Shell COM object runs and opens programs through the desktop shell"
    if name in ("python", "python3", "py") and any(_PY_LAUNCH.search(w) for w in words[k + 1:]):
        return "`os.startfile` / `webbrowser.open` open the desktop application associated with their operand"
    if _PS_DOTNET_START.search(_QUOTED.sub("", step.text)):
        return "`[System.Diagnostics.Process]::Start` runs the program through the shell, which may open a window"
    return None
