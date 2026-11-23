#!/usr/bin/env python3
"""Self-test for forbidden_commands.py — every rule fires on its shape AND stays silent on the legitimate neighbour.

A guard hook is a gate, and a gate is only trusted once its failure branch has fired (feedback: prove the watchdog
fails). Run: python scripts/hooks/test_forbidden_commands.py   (every gate and CI run it through scripts/self_tests.py.)
"""
import base64
import json
import os
import pathlib
import subprocess
import sys
sys.path.append(str(pathlib.Path(__file__).resolve().parents[1]))  # scripts/, for standard_streams
from standard_streams import ensure_utf8  # noqa: E402
from scratch_repo import ScratchRepo, scratch_dir  # noqa: E402  (scripts/: the one scratch fixture)

ensure_utf8()

HOOK = pathlib.Path(__file__).with_name("forbidden_commands.py")
REPO = HOOK.resolve().parents[2]
# another repository, for the unit-push rule: a real git directory whose origin is not this repository's
OTHER_DIR = scratch_dir("fc-other-")
ScratchRepo.init(OTHER_DIR).git("remote", "add", "origin", "https://example.invalid/other.git")
OTHER = OTHER_DIR.as_posix()
# THIS repository's origin URL (read through the fixture, which only reads): the push destinations a unit must reach
# through whatever git configuration names them (kb/Work PB5269). Each scratch repository's `origin` IS this repository;
# the extra setting makes git publish somewhere else, or (the twins) to origin again.
THIS_ORIGIN = ScratchRepo(REPO).git("remote", "get-url", "origin")
ELSEWHERE = "https://example.invalid/elsewhere.git"


def same_origin(prefix: str, *settings: tuple[str, ...], branch: str = "main") -> str:
    """A scratch repository on `branch` whose `origin` is this repository's, plus a remote `other` at ELSEWHERE and
    each `git config` setting; its posix path."""
    d = scratch_dir(prefix)
    r = ScratchRepo.init(d, branch=branch)
    r.git("remote", "add", "origin", THIS_ORIGIN)
    r.git("remote", "add", "other", ELSEWHERE)
    for s in settings:
        r.git("config", *s)
    return d.as_posix()


SAME = same_origin("fc-same-")
PUSHURL = same_origin("fc-pushurl-", ("remote.origin.pushurl", ELSEWHERE))
PUSHURL_SAME = same_origin("fc-pushurl-same-", ("remote.origin.pushurl", THIS_ORIGIN))
PUSH_DEFAULT = same_origin("fc-pushdefault-", ("remote.pushDefault", "other"))
PUSH_DEFAULT_SAME = same_origin("fc-pushdefault-same-", ("remote.pushDefault", "origin"))
BRANCH_PUSH = same_origin("fc-branchpush-", ("branch.main.pushRemote", "other"), ("remote.pushDefault", "origin"))
BRANCH_REMOTE = same_origin("fc-branchremote-", ("branch.main.remote", "other"))
REWRITE = same_origin("fc-rewrite-", (f"url.{ELSEWHERE}.pushInsteadOf", THIS_ORIGIN))
# THE SESSION'S DIRECTORY every case runs the hook from (the payload's `cwd`, which is how Claude Code tells the hook
# where the session is): a scratch repository on a branch this test names, its origin this repository's. A push that
# names no repository is resolved from ITS branch and configuration, so no verdict depends on which branch the
# developer's or the gate's checkout is on (kb/Work PB5227: the hook asked the real checkout `git symbolic-ref HEAD`).
SESSION = same_origin("fc-session-", branch="unit-work")
# kb/Work PB6136, rule 2c: scratch repositories whose origin is this repository, each with a commit a push would publish.
# The planted stamp is built at run time, so this file holds no stamp of its own.
PLANTED = "Licensed to " + "Jane" + " Doe"


def with_commits(prefix: str, *commits: tuple[str, dict[str, str]], published: int = 0) -> str:
    """A `same_origin` repository with `commits` (message, files) on main; its first `published` commits are already
    on origin (its `refs/remotes/origin/main` stands at the last of them)."""
    d = same_origin(prefix)
    r = ScratchRepo(d)
    for k, (message, files) in enumerate(commits, 1):
        r.commit(message, files)
        if k == published:
            r.git("update-ref", "refs/remotes/origin/main", "HEAD")
    return d


STAMPED = with_commits("fc-stamped-", ("base", {"a.txt": "one\n"}), ("ocr", {"ocr.json": '{"line": "' + PLANTED + '"}\n'}))
STAMPED_MESSAGE = with_commits("fc-stamped-msg-", ("quotes " + PLANTED, {"a.txt": "one\n"}))
CLEAN = with_commits("fc-clean-", ("base", {"a.txt": "one\n"}), ("more", {"b.txt": "two\n"}))
ON_ORIGIN = with_commits("fc-on-origin-", ("ocr", {"ocr.json": PLANTED + "\n"}), ("more", {"b.txt": "two\n"}), published=1)
SPLIT = with_commits("fc-split-", ("split", {"g.txt": "x\norder " + "#\n" + "1234" + "56\n"}))   # review R1-2
TAGGED = with_commits("fc-tagged-", ("base", {"a.txt": "one\n"}))                               # review R1-3
ScratchRepo(TAGGED).git("tag", "-a", "v-stamped", "-m", "release " + PLANTED)
BS = chr(92)  # a backslash, spelled without one
NL = chr(10)
# kb/Work PB3198: every rule reads the ONE model of the command line (shell_location.steps), so each rule is checked
# AFTER a heredoc (the rules that scanned only the text before the first `<<` passed these) and after a QUOTED `<<`
# (which made the rest of the line a "heredoc body" for the rules that did not).
HD = "cat > m.txt <<'EOF'" + NL + "hello" + NL + "EOF" + NL
QS = 'python -c "print(1<<2)"; '

CASES = [
    # (command, expect_block)
    ("git stash push -m wip", True),
    ("git stash", True),
    ("git stash pop", True),
    ("git -C E:/COBOL stash apply", True),
    ("git stash list", False),
    ("git rebase --autostash origin/main", True),
    ("git rebase origin/main", False),
    ("git push origin HEAD:main", True),
    ("git push origin main", True),
    ("git push -q origin HEAD:refs/heads/main", True),
    ("git push origin HEAD:worktree-agent-abc123", False),                # a work branch, never main
    ("python scripts/orchestrator/enqueue.py land worktree-agent-abc123", False),   # the landing script itself
    ("cd /e/claude-skills && git push -q origin main", False),
    ("cd /e/COBOL && git push origin HEAD:main", True),
    ("cd E:/COBOL && git push origin HEAD:main", True),     # the Windows form of this repo's path
    ("cd scripts && git push origin main", True),               # a relative cd never leaves the repo
    ("cd /e/COBOL-private && git push origin main", True),      # a sibling of this repo's name (fail closed)
    ("cd /e/Sites/wiseowlsoftware.com && git push origin main", False),
    ("git -C /e/COBOL/tools/claude-skills push -q origin main --follow-tags", False),  # a registered submodule is another repo
    ("cd tools/claude-skills && git push origin main", False),                         # relative, same submodule
    ("cd /e/COBOL/tools && git push origin main", True),                               # its parent folder is still this repo
    # every location change the shared parser reads (kb/Work PB2599), not only a bare `cd`
    ("pushd /e/claude-skills && git push -q origin main", False),
    ('cd "/e/Sites/wise owl" && git push origin main', False),                          # a quoted path with a space
    ("ls; cd /e/claude-skills && git push origin main", False),
    ("pushd /e/COBOL && git push origin main", True),
    ("python - <<'EOF'\nprint('a" + BS + "nb')\nEOF", False),          # one backslash arrives intact (kb/Work PB5234)
    ("python - <<'EOF'\nprint('a" + BS + BS + "nb')\nEOF", True),      # a run of two is halved by the Bash tool
    ("cat > x.txt <<'EOF'" + NL + "C:" + BS + "Users" + BS + "x " + BS + "d+" + NL + "EOF", False),   # paths, regexes
    ("cat > x.txt <<'EOF'" + NL + "a" + BS + BS + BS + BS + "b" + NL + "EOF", True),
    ("cat > x.txt <<'EOF'" + NL + "say " + BS + BS + '"hi' + NL + "EOF", False),   # a run before a quote is kept
    ("python - <<'EOF'\nprint('plain')\nEOF", False),
    ('dotnet test x.csproj --filter "~Drift|~EditionGate" > log.txt', True),
    ('dotnet test x.csproj --filter "FullyQualifiedName~Drift|FullyQualifiedName~EditionGate" > log.txt', False),
    ('dotnet test x.csproj --filter "FullyQualifiedName~Drift"', False),   # unredirected output is not refused (kb/Work PB5234)
    ('dotnet test x.csproj --filter "FullyQualifiedName~Drift" 2>&1 | tail -5', False),
    ("dotnet test x.csproj", False),
    # a command chained after a verdict command is no longer refused (kb/Work PB5234: 1 true refusal of 127, and
    # enqueue.py refuses a tip no GREEN gate record covers); each of these was refused by the deleted rule
    ("dotnet test x.csproj > t.log 2>&1 && git commit -m x", False),
    ("dotnet build WiseOwl.Cobol.sln -c Debug && dotnet test x.csproj > t.log", False),
    ("python scripts/orchestrator/enqueue.py land b > l.log 2>&1; git log -1", False),
    ("pwsh -File scripts/build-local.ps1 -Priority BelowNormal *> b.log || true", False),
    # 5. WSL lifecycle commands are the owner's to run
    ("wsl --terminate Ubuntu", True),
    ("wsl.exe --export Ubuntu E:/x.tar", True),
    ("wsl --shutdown", True),
    ("wsl --update --web-download", True),
    ("wsl -t Ubuntu", True),
    ("wsl --unregister Ubuntu-26.04", True),
    ("wsl --install Ubuntu-26.04 --no-launch", True),
    ("wsl --terminate Ubuntu | Out-Null; wsl --export Ubuntu E:/x.tar", True),
    ("wsl -d Ubuntu -e bash -lc 'ls ~'", False),
    ("wsl -d Ubuntu --cd /mnt/e/COBOL -- bash -lc 'bash scripts/linux-gate.sh'", False),
    ("wsl -l -v", False),
    ("git commit -q -m 'document wsl --terminate and wsl --export for the owner'", False),
    ("grep -n 'wsl --export' docs/x.md", False),
    ("bash -c 'wsl --shutdown'", True),                                  # a -c payload's command is a command
    ("echo y | wsl --terminate Ubuntu", True),                           # so is a pipeline's later side
    # 6. a `cd` inside a BACKGROUNDED list with another command after its `&` (MANDATORY-PRACTICES P14(b), kb/Work PB3095)
    ("cd /e/x && git log -1 > a.txt & git status > b.txt &", True),     # the 2026-10-11 shape: b ran in the caller's dir
    ("cd /e/x && a & b", True),
    ("pushd /e/x && a & b", True),
    ("cd /e/x || exit 1 && a & wait", True),
    ("cd /e/x && git log -1", False),                                    # a plain chain: the cd binds what follows
    ("cd /e/x && git log -1 &", False),                                  # backgrounded, nothing after the `&`
    ("cd /e/x; a & b", False),                                           # the cd is its own list: it moves the caller
    ("git -C /e/x log -1 > a.txt & git -C /e/y status > b.txt &", False),
    ("cd /e/x && dotnet build x.sln > b.log 2>&1", False),               # `2>&1` is a redirection, not a background `&`
    ("cd /e/x && a &> l.log", False),                                    # `&>` too
    ("cd /e/x && a |& tee l.log", False),                                # and `|&`
    ("echo 'cd /e/x && a & b'", False),                                  # quoted text is text
    # review R1-1: the rule reads the SHARED parser, so a comment's apostrophe or an escaped quote cannot hide the list
    ("git status  # don't worry" + chr(10) + "cd /e/x && a & b", True),
    ("echo it" + BS + "'s here && cd /e/x && a & b", True),
    ("cat > m.txt <<'EOF'" + chr(10) + "cd /e/x && a & b" + chr(10) + "EOF" + chr(10) + "git status", False),   # heredoc body
    ("bash -c 'cd /e/x && a & b'", True),                                # a -c payload is a command line
    ("(cd /e/x && a) & b", False),                                       # the subshell already scopes the cd
    ("{ cd /e/x; a; } & b", True),                                       # a { } group is in the backgrounded list
    # PB3198: every rule after a heredoc, after a quoted `<<`, and silent on its words inside a body or a quote
    (HD + "git stash", True),
    (QS + "git stash", True),
    ("cat > m.txt <<'EOF'" + NL + "git stash" + NL + "EOF" + NL + "git status", False),   # the body is text
    ('git commit -m "never git stash"', False),                          # so is a quoted message
    (HD + "git pull --autostash", True),
    ("git -c rebase.autoStash=true rebase origin/main", True),           # autostash switched on through -c
    ("git -c rebase.autostash=false rebase origin/main", False),
    (HD + "git push origin main", True),
    (QS + "git push origin HEAD:main", True),
    ("git push origin +HEAD:main", True),                                # a forced refspec is still main
    ("git push origin +main", True),
    ("cd /e/claude-skills && git push origin x; git -C /e/COBOL push origin main", True),   # each push in its own dir
    ("cat > m.txt <<'EOF'" + NL + "git push origin main" + NL + "EOF" + NL + "git status", False),
    (QS + 'printf "a' + BS + 'n"', False),                               # a quoted `<<` is no heredoc: no body
    (HD + 'printf "a' + BS + 'n"', False),                               # the command after a body is no body
    ("bash -c 'cat' <<'EOF'" + NL + "a" + BS + BS + "nb" + NL + "EOF", True),  # a body feeding a -c payload is a body
    ("bash -c 'cat' <<'EOF'" + NL + "a" + BS + "nb" + NL + "EOF", False),
    (HD + 'dotnet test x.csproj --filter "~Foo" > log.txt', True),
    (QS + 'dotnet test x.csproj --filter "~Foo" > log.txt', True),
    (HD + 'dotnet test x.csproj --filter "FullyQualifiedName~Foo"', False),   # unredirected output is not refused (kb/Work PB5234)
    ('dotnet test x.csproj --filter=~Foo > log.txt', True),
    ('dotnet test x.csproj --filter "FullyQualifiedName~Foo" & echo x > y.txt', False),   # unredirected output is not refused (kb/Work PB5234)
    ('git commit -m "dotnet test --filter ~X"', False),                  # quoted text is no test run
    (QS + "dotnet build x.sln > b.log && git commit -m x", False),
    (HD + "wsl --shutdown", True),
    (QS + "wsl --shutdown", True),
    ("cat > m.txt <<'EOF'" + NL + "wsl --shutdown" + NL + "EOF", False),
    (HD + "cd /e/x && a & b", True),
    (QS + "cd /e/x && a & b", True),
    # a heredoc a shell reads as its script is a command line, so every rule reads it (the w1098c lead)
    ("bash <<'EOF'" + NL + "git stash" + NL + "EOF", True),
    ("bash -s <<'EOF'" + NL + "git push origin main" + NL + "EOF", True),
    ("sh <<'EOF'" + NL + 'dotnet test x.csproj --filter "~Foo" > log.txt' + NL + "EOF", True),
    ("pwsh -NoProfile -Command - <<'EOF'" + NL + "git stash" + NL + "EOF", True),
    ("bash run.sh <<'EOF'" + NL + "git stash" + NL + "EOF", False),   # stdin of a script is that script's data
    # the branch review of PB3198, R1-1: a command substitution is a command line, in a word or inside double quotes
    ("x=$(git stash)", True),
    ("echo `git stash`", True),
    ('echo "$(git stash)"', True),
    ("cat <(git stash)", True),
    ("x=$(git stash list)", False),
    ("r=$(git push origin HEAD:main 2>&1)", True),
    ('r=$(dotnet test x.csproj --filter "~Foo")', True),
    ("git commit -m \"$(cat <<'EOF'" + NL + "fix a" + BS + BS + "nb" + NL + "EOF" + NL + ')"', True),   # the commit shape
    ("git commit -m \"$(cat <<'EOF'" + NL + "fix a" + BS + "nb" + NL + "EOF" + NL + ')"', False),
    ("git commit -m \"$(cat <<'EOF'" + NL + "fix a" + NL + "EOF" + NL + ')"', False),
    # a quote or a bracket in the body never ends the substitution, so the command after it is still read
    ("git commit -m \"$(cat <<'EOF'" + NL + 'say "hi" (and \') too' + NL + "EOF" + NL + ')" && git stash', True),
    ("git commit -m \"$(cat <<'EOF'" + NL + 'say "hi" (and \') too' + NL + "EOF" + NL + ')" && git status', False),
    ("dotnet test x.csproj > t.log 2>&1; echo \"EXIT=$? $(date)\"", False),   # a substitution is its echo's own
    ('dotnet test x.csproj --filter "FullyQualifiedName~X" | tail $(echo -5)', False),   # the reader is still tail
    ('r=$(dotnet test x.csproj --filter "FullyQualifiedName~Foo" | tail -5)', False),
    ("bash -c 'r=$(dotnet test x.csproj --filter FullyQualifiedName~Foo | tail -5)'", False),
    ('r=$(dotnet test x.csproj --filter "FullyQualifiedName~Foo" | grep $(echo Passed))', False),
    ('r=$(dotnet test x.csproj --filter "FullyQualifiedName~Foo" | sort)', False),   # unredirected output is not refused (kb/Work PB5234)
    ('r=$(dotnet test x.csproj --filter "FullyQualifiedName~Foo" | sort $(echo x | tail -1))', False),   # unredirected output is not refused (kb/Work PB5234)
    ('dotnet test x.csproj --filter "FullyQualifiedName~Foo" | ( tail -5 )', False),    # a grouped reader is a reader
    # a group is a pipeline stage: it reads the stage before it, and the stage after it reads the group
    ("( echo 'git stash' ) | bash", True),
    ("echo 'git stash' | ( bash )", True),
    ("echo 'git stash list' | ( bash )", False),
    # R3-1: reading a pipe never makes a group's FIRST command a pipeline side, so its cd moves the group's later steps
    # (at c2bdc1b8d the first two passed, a push to this repository's main, and the third was a false block)
    ("cd /e/claude-skills; echo y | ( cd /e/COBOL && git push origin HEAD:main )", True),
    ("cd /e/claude-skills; true | { cd /e/COBOL; git push origin main; }", True),
    ("echo y | ( cd /e/claude-skills && git push -q origin main )", False),
    # and the group's stdin reaches each of its commands, a -c payload's and a substitution's
    ("echo 'git stash' | ( cd /e/W; bash )", True),
    ("echo 'git stash list' | ( cd /e/W; bash )", False),
    ("echo 'git stash' | bash -c 'cd /e/W; bash'", True),
    ("echo 'git stash' | x=$(bash)", True),
    # R1-2: every other way a shell is handed a command line as a string or through stdin
    ('eval "git stash"', True),
    ('eval "git stash list"', False),
    ('cmd /c "git stash"', True),
    ('cmd //c "git push origin main"', True),
    ('cmd /c "cd /d E:/COBOL & git push origin main"', True),
    ('cmd /c "cd /d E:/claude-skills & git push origin main"', False),      # another repository, read as cmd.exe does
    ("echo 'git stash' | bash", True),
    ("printf 'git stash" + BS + "n' | sh", True),
    ("cat <<EOF | bash" + NL + "git stash" + NL + "EOF", True),
    ("echo 'git stash' | grep stash", False),                              # piped into a reader, not a shell
    ("cat run.sh | bash", False),                                          # what a file holds is not this line's
    ('bash <<< "git stash"', True),
    ('grep x <<< "git stash"', False),
    ("timeout 5 bash -c 'git stash'", True),                               # behind a command that runs another
    ("env A=1 sh -c 'git stash'", True),
    ("xargs -n1 sh -c 'git stash' < x.txt", True),
    ("wsl -d Ubuntu -e bash -lc 'git stash'", True),
    ("bash -euc 'git stash'", True),                                       # `-c` among single-letter options
    ("bash -C run.sh", False),                                             # `-C` is noclobber, not a payload
    # 7. a recursive delete whose target the shell expands (kb/Work PB5764, MANDATORY-PRACTICES P17): each spelling of
    # the recursive flag, each kind of expansion, each place a command line comes from; and the forms that stay allowed
    ('rd=$R/$id; rm -rf "$rd"; mkdir -p "$rd"', True),                    # the shape the probing agents wrote
    ("rm -rf $B/$id", True),                                              # unquoted
    ("rm -r $d", True),
    ('rm -R "${d}"', True),
    ('rm -fr "$R/$id"', True),
    ('rm --recursive "$d"', True),
    ('rm -rf "$d"/*', True),
    ("rm -rf $(cat dirs.txt)", True),                                     # a command substitution
    ("rm -rf `pwd`/x", True),                                             # a backquote
    ('rm "$d" -rf', True),                                                # rm reads an option after an operand
    ('rd=/e/scratch/x; rm -rf "$rd"', True),                              # a variable set to a literal is still a variable
    ('sudo rm -rf "$d"', True),
    ('X=1 rm -rf "$d"', True),
    ('for d in a b; do rm -rf "$d"; done', True),                         # a loop's body
    ("bash -c 'rm -rf \"$d\"'", True),                                    # a -c payload expands it when it runs
    ('cmd /c "rmdir /s /q %D%"', True),                                   # cmd.exe's own variable
    ('cmd /c "rd /S %TEMP%' + BS + 'x"', True),
    ("rm -rf /e/scratch/runs/x", False),                                  # a literal path
    ("rm -rf 'E:/x/$y'", False),                                          # a `$` in single quotes is literal
    ('rm -rf "' + BS + '$d"', False),                                     # and so is an escaped one
    ('rm "$f"', False),                                                   # one file, not recursive
    ('rm -f "$f"', False),
    ('rm -- -r "$f"', False),                                             # after `--`, `-r` is a file name
    ('d=$R/x; ls "$d"; rm -rf /e/scratch/x', False),                      # the variable is another command's
    ('echo rm -rf "$d"', False),                                          # the command is echo
    ("grep -n 'rm -rf \"$d\"' run.sh", False),                            # quoted text is text
    ('git rm -r "$d"', False),                                            # git's own removal, one commit from recovery
    ('cmd /c "rmdir /q %D%"', False),                                     # not recursive
    ('cmd /c "rmdir /s /q E:' + BS + 'x"', False),                        # literal
    # 8. a kill by process name in Bash (kb/Work PB6034)
    ("pkill -f cobol", True),
    ("killall dotnet", True),
    ("taskkill //IM cobol.exe //F", True),                                # Git Bash's spelling of /IM
    ("taskkill /IM cobol.exe /F", True),
    ("kill 1234", False),                                                 # a pid
    ('kill "$pid"', False),
    ("taskkill /PID 1234 /F", False),
    # the branch review of PB6034 (train-1985-c R1-1): a name ENUMERATION and a KILL anywhere in one command line, in
    # any order or pipe, through a substitution, xargs or a loop
    ("kill $(pgrep cobol)", True),
    ("pgrep cobol | xargs kill", True),
    ("pgrep -f cobol | xargs -r kill -9", True),
    ("ps aux | grep cobol | awk '{print $2}' | xargs kill", True),
    ("for p in $(pidof cobol); do kill $p; done", True),
    ("kill `pgrep cobol`", True),
    ("wmic process where name='cobol.exe' delete", True),
    ("wmic process where \"name='cobol.exe'\" call terminate", True),
    ('taskkill /F /FI "IMAGENAME eq cobol.exe"', True),
    ('tasklist /FI "IMAGENAME eq cobol.exe"', False),                     # listing kills nothing
    ("pgrep -a cobol", False),
    ("ps -p 1234 && kill 1234", False),                                   # a pid, looked up and killed
    ('kill -0 "$pid"', False),
    ('wmic process where processid=1234 delete', False),
    ("git log --grep 'pgrep cobol | xargs kill'", False),                 # quoted text is text
]


# The PowerShell tool's commands: the same rules, read with PowerShell's location changes (kb/Work PB2599).
PS_CASES = [
    ("Set-Location E:/claude-skills; git push -q origin main", False),
    ("sl E:/claude-skills; git push -q origin main", False),
    ("Push-Location -Path 'E:/Sites/wise owl'; git push origin main", False),
    ("Set-Location E:/COBOL; git push origin main", True),
    ("git push origin HEAD:main", True),
    ("git stash", True),
    ("Set-Location E:/x && a & b", True),                                # the same backgrounded list in PowerShell 7
    ("Set-Location E:/x; & pwsh -File y.ps1", False),                    # `&` that starts a command is the call operator
    ("& git stash pop", True),                                           # the call operator before git (PB3198)
    ("git commit -m 'never git stash'", False),
    # the branch review of PB3198: PowerShell's subexpressions, expressions and script blocks are command lines
    ("echo $(git stash)", True),
    ('Write-Output "x $(git stash)"', True),
    ("Write-Output (git stash)", True),
    ("Get-ChildItem | ForEach-Object { git stash }", True),
    ("if ($x) { git stash }", True),
    # R3-1's PowerShell forms: a script block reading the pipeline runs in this runspace, so its Set-Location moves it
    ("Set-Location E:/claude-skills; 'y' | & { Set-Location E:/COBOL; git push origin main }", True),
    ("Set-Location E:/claude-skills; 'y' | ForEach-Object { Set-Location E:/COBOL; git push origin main }", True),
    ("'y' | & { Set-Location E:/claude-skills; git push origin main }", False),
    ("$m = (git log -1 --format=%s)", False),
    ("Write-Output 'git stash'", False),
    ('$r = (dotnet test x.csproj --filter "FullyQualifiedName~Foo" | Select-String Passed)', False),
    ('$r = $(dotnet test x.csproj --filter "FullyQualifiedName~Foo" | Select-String Passed)', False),
    ('$r = (dotnet test x.csproj --filter "FullyQualifiedName~Foo" | Sort-Object)', False),   # unredirected output is not refused (kb/Work PB5234)
    # and every interpreter handed a string
    ('Invoke-Expression "git stash"', True),
    ("iex 'git stash'", True),
    ("'git stash' | Invoke-Expression", True),
    ("'git stash' | Write-Output", False),
    ("& pwsh -NoProfile -c 'git stash'", True),
    ('cmd /c "git stash"', True),
    ("pwsh -EncodedCommand " + base64.b64encode("git stash".encode("utf-16-le")).decode(), True),
    ("pwsh -EncodedCommand " + base64.b64encode("git status".encode("utf-16-le")).decode(), False),
    # 7. PowerShell's recursive delete of an expanded path (kb/Work PB5764): every alias, every prefix of -Recurse
    ("Remove-Item -Recurse -Force $d", True),
    ('Remove-Item -Recurse "$root' + BS + 'x"', True),                   # a variable in double quotes
    ("rm -r $d", True),
    ("ri -Recurse -Path $d", True),
    ("del $env:TEMP" + BS + "x -Recurse", True),
    ("rd -rec $(Join-Path $a b)", True),
    ("rmdir -Recurse ${d}", True),
    ("Remove-Item -LiteralPath:$d -Recurse", True),
    ('cmd /c "rd /s /q %TEMP%' + BS + 'x"', True),
    ("Remove-Item -Recurse -Force 'E:/x/y'", False),                     # literal
    ("Remove-Item -Recurse 'E:/x/$y'", False),                           # single quotes are literal
    ("Remove-Item $f", False),                                           # not recursive
    ("Remove-Item -Recurse:$false $d", False),
    ("Remove-Item -Recurse E:/x -Filter $pat", False),                   # the variable is a filter, not the target
    ('Write-Output "rm -r $d"', False),
    # 8. a kill by process name (kb/Work PB6034, MANDATORY-PRACTICES P18): the command that killed another gate's
    # compiles, each spelling of a name selection, and the kills of an agent's own pid that stay allowed
    ('Get-Process dotnet-stack,cobol -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue; "ok"',
     True),
    ("Stop-Process -Name cobol -Force", True),
    ("Stop-Process -n cobol", True),
    ("spps -ProcessName dotnet", True),
    ("kill -Name cobol", True),
    ("gps cobol | spps", True),
    ("Get-Process | Stop-Process", True),                                # every process
    ("Get-Process -Name cobol | Stop-Process -Force", True),
    ("taskkill /IM cobol.exe /F", True),
    ('cmd /c "taskkill /f /im dotnet.exe"', True),
    ("Stop-Process -Id 1234", False),                                    # one pid
    ("Stop-Process -Id $p.Id -Force", False),
    ("Stop-Process 1234 -Force", False),                                 # a positional id
    ("$p | Stop-Process", False),                                        # the agent's own process object
    ("Get-Process -Id $p.Id | Stop-Process", False),
    ("$p.Kill()", False),
    ("taskkill /PID 1234 /T /F", False),
    ("Get-Process cobol", False),                                        # listing kills nothing
    ("Write-Output 'Stop-Process -Name cobol'", False),
    # the branch review of PB6034 (train-1985-c R1-1): every form that ENUMERATES by name and KILLS in one command line
    ("Get-Process cobol | Where-Object { $_.StartTime -lt (Get-Date).AddMinutes(-5) } | Stop-Process -Force", True),
    ("Stop-Process -Id (Get-Process cobol).Id -Force", True),
    ("Get-Process -Name cobol | ForEach-Object { $_.Kill() }", True),
    ("foreach ($p in Get-Process cobol) { $p.Kill() }", True),
    ("Get-Process | Where-Object Name -eq cobol | Stop-Process", True),
    ("$procs = Get-Process cobol; $procs | Stop-Process", True),
    ("Get-Process cobol | ForEach-Object { Stop-Process -Id $_.Id }", True),
    ("Get-CimInstance Win32_Process -Filter \"Name='cobol.exe'\" | Invoke-CimMethod -MethodName Terminate", True),
    ("gwmi Win32_Process | Where-Object Name -eq cobol.exe | Remove-WmiObject", True),
    ("[System.Diagnostics.Process]::GetProcessesByName('cobol') | ForEach-Object { $_.Kill() }", True),
    ("Get-Process cobol | ForEach-Object { taskkill /PID $_.Id /F }", True),
    ("$p = Start-Process cobol -ArgumentList x.cob -PassThru; Stop-Process -Id $p.Id", False),   # its own pid
    ("$p = Start-Process cobol -PassThru; $p.WaitForExit(5000); $p.Kill()", False),
    ("Get-Process -Id $p.Id -ErrorAction SilentlyContinue | Stop-Process", False),
    ("Get-CimInstance Win32_Process -Filter \"ProcessId=$($p.Id)\" | Invoke-CimMethod -MethodName Terminate", False),
    ("Get-Process cobol | Select-Object Id, StartTime", False),         # listing kills nothing
]


# A guard against a hung hook, never a time expectation: a case's verdict is its exit code. At 30 s (the hook's own
# settings.json timeout) a unit-push case, whose hook asks git for the push's remote, timed out on a host at 100 % CPU
# inside a gate (run 000001-20261116T025348Z of the PB5764 branch): a RED no change of the hook caused.
HANG_GUARD = 300


def blocked(cmd: str, unit: bool = False, tool: str = "Bash") -> bool:
    return hook(cmd, unit, tool).returncode == 2


def reason(cmd: str) -> str:
    """The hook's refusal of `cmd` (its stderr), attended, in the Bash tool."""
    return hook(cmd).stderr


def hook(cmd: str, unit: bool = False, tool: str = "Bash") -> subprocess.CompletedProcess:
    # The environment is part of the case: an orchestrator unit has COBOL_COORD_DIR, an attended session never does. Set or
    # removed EXPLICITLY, so the result never depends on who runs this test.
    env = {k: v for k, v in os.environ.items() if k != "COBOL_COORD_DIR"}
    if unit:
        env["COBOL_COORD_DIR"] = "E:/COBOL-coord"
    return subprocess.run([sys.executable, str(HOOK)], input=json.dumps({"tool_name": tool, "tool_input": {"command": cmd},
                                                                        "cwd": SESSION}),
                          capture_output=True, text=True, encoding="utf-8", errors="replace", timeout=HANG_GUARD,
                          env=env)


# Inside an orchestrator unit a `git push` to ANOTHER repository is refused, whatever the flags (PB2044: a unit pushed
# the public skills repo with `git push -q origin HEAD:main`), judged by the remote's URL; a push of the unit's own
# branch of THIS repository passes (kb/Work PB5234). The directories that exist nowhere (`/e/claude-skills`) have no
# remote to read, so the rule fails closed on them; OTHER is a real repository with another remote.
UNIT_CASES = [
    ("git push -q origin HEAD:main", True),
    ("cd /e/claude-skills && git push -q origin main", True),
    ("git -C E:/claude-skills push origin main", True),
    ("git -c core.askpass=x push origin v1.18.0", False),   # this repository (kb/Work PB5234)
    ("git --no-pager push --quiet origin HEAD:refs/heads/main", True),
    ("git push origin HEAD:worktree-agent-abc123", False),                   # its own branch, this repository
    ("git push -u origin claude/batch-7", False),
    ("git push -q origin HEAD 2>&1 | tail -1", False),
    ("git push origin HEAD:main", True),                                     # rule 2: this repository's main
    ("cd /e/Sites/wiseowlsoftware.com; git push", True),                     # no repository there: fails closed
    ("cd " + OTHER + " && git push -u origin x", True),                      # another repository's branch
    ("git -C " + OTHER + " push origin v1.18.0", True),                      # and its tag
    ("git push https://github.com/WiseOwlSoftware/claude-skills.git x", True),  # a URL that is another repository
    ("git push https://github.com/WiseOwlSoftware/COBOL x", False),          # the same repository spelled as a URL
    ("python scripts/orchestrator/enqueue.py land worktree-agent-abc123", False),   # its push is the script's
    ("python scripts/orchestrator/enqueue.py queue --title T --body-file m.txt", False),   # its push is the script's
    ("git log --grep=push", False),
    ("git log --oneline -3 -- scripts/orchestrator/enqueue.py", False),
    ("git commit -q -m 'document git push for the owner'", False),
    ('git commit -q -m "PB2044: a unit ran git push -q origin HEAD:main"', False),
    ("bash -c 'cd /e/claude-skills && git push -q origin main'", True),       # the payload of bash -c IS a command line
    ('pwsh -Command "git -C E:/claude-skills push origin main"', True),
    ("git status -s", False),
    # THE SHAPE THE UNIT ACTUALLY USED: a heredoc, then the commit and the push after its terminator
    ("cd /x/scratch; git add -A; cat > ../m.txt <<'EOF'" + chr(10) + "v1.17.1: a fix" + chr(10) + "EOF" + chr(10)
     + "git commit -q -F ../m.txt; git push -q origin HEAD:main", True),
    ("cat > ../m.txt <<'EOF'" + chr(10) + "the unit ran git push -q origin HEAD:main" + chr(10) + "EOF" + chr(10)
     + "git commit -q -F ../m.txt", False),                                    # the word inside the heredoc BODY is text
    # a quoted `<<` hides nothing (PB3198): the push after it is read; each of these shapes, pointed at another repository
    # (blocked) and at this one (passes), so a push that went unseen would fail the first of each pair (kb/Work PB5269)
    (QS + "git -C " + OTHER + " push", True),
    (QS + "git push", False),
    ("git commit -q -m 'x' && xargs -n1 git -C " + OTHER + " push origin < refs.txt", True),   # git wherever it stands in the step
    # this repository, but its refspecs come from xargs, unreadable to the scan, after a commit: rule 2c (kb/Work PB6136)
    ("git commit -q -m 'x' && xargs -n1 git push origin < refs.txt", True),
    ("git status && git push origin HEAD", False),
    # the branch review of PB3198, R1-1: a push inside a command substitution is a push
    ("out=$(git -C /e/claude-skills push origin main 2>&1)", True),
    ("out=$(git push -q origin HEAD:main 2>&1)", True),
    ("echo `git -C " + OTHER + " push`", True),
    ("echo `git push`", False),
    # kb/Work PB5269: the destination is resolved as git resolves it, not read from the first word that has no dash
    ("git push --repo=https://github.com/WiseOwlSoftware/claude-skills.git", True),   # no refspec: the old rule read origin
    ("git push -q --force --repo=https://github.com/WiseOwlSoftware/claude-skills.git", True),
    ("git push --repo=https://github.com/WiseOwlSoftware/claude-skills.git HEAD", True),
    ("git push --repo https://github.com/WiseOwlSoftware/claude-skills.git HEAD", True),
    ("git push --repo=https://github.com/WiseOwlSoftware/COBOL", False),   # the same repository
    ("git push --repo origin", False),
    # with --repo AND a positional word git takes the positional word as the repository (HEAD is no remote: refused)
    ("git push --repo=https://github.com/WiseOwlSoftware/COBOL HEAD", True),
    # git prefers the positional repository to --repo (measured with two bare repositories), so the positional word decides
    ("git push --repo=origin https://github.com/WiseOwlSoftware/claude-skills.git HEAD", True),
    ("git push --repo origin https://github.com/WiseOwlSoftware/claude-skills.git HEAD", True),
    ("git push --repo=https://github.com/WiseOwlSoftware/claude-skills.git origin HEAD", False),   # origin wins: this repository
    # git's own inputs that move a push out of the remote's configuration are refused (the twins are the plain pushes)
    ("git --git-dir=" + OTHER + "/.git push origin HEAD", True),
    ("git --git-dir " + OTHER + "/.git push", True),
    ("GIT_DIR=" + OTHER + "/.git git push origin HEAD", True),
    ("export GIT_DIR=" + OTHER + "/.git; git push origin HEAD", True),
    ("env GIT_DIR=" + OTHER + "/.git git push", True),
    ("GIT_CONFIG_COUNT=1 GIT_CONFIG_KEY_0=url." + ELSEWHERE + ".pushInsteadOf GIT_CONFIG_VALUE_0=" + THIS_ORIGIN
     + " git push " + THIS_ORIGIN + " HEAD", True),
    ("git -c url." + ELSEWHERE + ".pushInsteadOf=" + THIS_ORIGIN + " push " + THIS_ORIGIN + " HEAD", True),
    ("git -c url." + ELSEWHERE + ".insteadOf=" + THIS_ORIGIN + " push origin HEAD", True),
    ("cd " + REWRITE + " && git push " + THIS_ORIGIN + " HEAD", True),     # a configured rewrite applies to a literal URL
    ("cd " + REWRITE + " && git push origin HEAD", True),                  # and to a remote's URL
    ("git push " + THIS_ORIGIN + " HEAD", False),
    # `-C` to a repository whose origin is this one (a scratch one: rule 2c scans what a push of THIS repository's own
    # checkout would publish, which no self-test may read, kb/Work PB6136)
    ("git -C " + SAME + " push origin HEAD", False),
    ("git commit -q -m 'GIT_DIR=x is why'", False),                        # the name in a message is text
    ("git push --repo=" + OTHER + " HEAD", True),                              # a path to another repository
    ("git push -o ci.skip https://github.com/WiseOwlSoftware/claude-skills.git x", True),   # `-o`'s value is no repository
    ("git push --receive-pack=x origin HEAD", False),
    ("git -c remote.origin.pushurl=" + ELSEWHERE + " push origin HEAD", True),   # the call's own configuration
    ("cd " + SAME + " && git push origin HEAD", False),
    ("cd " + SAME + " && git push", False),                                    # no repository named: origin
    ("cd " + SAME + " && git push other HEAD", True),                          # a second remote that is not this one
    ("cd " + SAME + " && git push -u other x", True),
    ("cd " + PUSHURL + " && git push origin HEAD", True),                      # origin fetches here, pushes elsewhere
    ("cd " + PUSHURL + " && git push", True),
    ("cd " + PUSHURL_SAME + " && git push origin HEAD", False),
    ("cd " + PUSH_DEFAULT + " && git push", True),                             # remote.pushDefault
    ("cd " + PUSH_DEFAULT + " && git push origin HEAD", False),                # an explicit remote overrides it
    ("cd " + PUSH_DEFAULT_SAME + " && git push", False),
    ("cd " + BRANCH_PUSH + " && git push", True),                              # branch.<b>.pushRemote beats pushDefault
    ("cd " + BRANCH_REMOTE + " && git push", True),                            # branch.<b>.remote beats origin
]

fails =[(c, e) for c, e in CASES if blocked(c) != e]
fails += [(f"[unit] {c}", e) for c, e in UNIT_CASES if blocked(c, unit=True) != e]
fails += [(f"[PowerShell] {c}", e) for c, e in PS_CASES if blocked(c, tool="PowerShell") != e]
# The same pushes in an ATTENDED session (no COBOL_COORD_DIR): rule 2 alone decides.
ATTENDED = [("git push -u origin claude/batch-7", False), ("git -C E:/claude-skills push origin v1.18.0", False),
            ("git push -q origin HEAD:main", True)]
fails += [(f"[attended] {c}", e) for c, e in ATTENDED if blocked(c) != e]
# kb/Work PB6136, rule 2c: a push to this repository that would publish a licence stamp, attended or in a unit
STAMP_CASES = [
    ("cd " + STAMPED + " && git push origin HEAD:refs/heads/pb-topic", True),
    ("git -C " + STAMPED + " push -u origin pb-topic", False),               # names no commit: git refuses it
    ("git -C " + STAMPED + " push -u origin main:pb-topic", True),
    ("cd " + STAMPED + " && git push", True),                                # no refspec: HEAD
    ("cd " + STAMPED + " && git push --all origin", True),
    ("cd " + STAMPED + " && git push " + THIS_ORIGIN + " HEAD:pb-topic", True),   # this repository spelled as its URL
    ("cd " + STAMPED + " && git push other HEAD:pb-topic", False),           # another repository: not judged here
    ("cd " + STAMPED + " && git push origin :pb-old", False),                # a deletion publishes nothing
    ("cd " + STAMPED + " && git push --delete origin pb-old", False),
    ("cd " + STAMPED + " && git push --stdin origin", True),                 # refspecs it cannot read: fails closed
    ("cd " + STAMPED_MESSAGE + " && git push origin HEAD:pb-topic", True),   # the stamp in a commit message
    ("cd " + CLEAN + " && git push origin HEAD:pb-topic", False),            # the clean neighbour
    ("cd " + ON_ORIGIN + " && git push origin HEAD:pb-topic", False),        # the stamp is already on origin
    ("cd /e/COBOL/no-such-directory && git push origin HEAD:pb-topic", True),   # this repository, unreadable: fails closed
]
fails += [(f"[stamp] {c}", e) for c, e in STAMP_CASES if blocked(c) != e]
fails += [(f"[unit stamp] {c}", e) for c, e in STAMP_CASES[:1] + STAMP_CASES[-3:-1] if blocked(c, unit=True) != e]
fails += [(f"[PowerShell stamp] {c}", True) for c in ["git -C " + STAMPED + " push origin HEAD:pb-topic"]
          if not blocked(c, tool="PowerShell")]
_said = reason("cd " + STAMPED + " && git push origin HEAD:pb-topic")
if not ("ocr.json:1" in _said and "per-copy licence stamp naming a purchaser" in _said and "Jane" not in _said):
    fails.append(("[stamp] the refusal names the file, line and shape and never the matched text: " + _said, True))
# kb/Work PB6136 branch review: R1-1, a push after a step that creates or moves commits on the same line is refused (the
# scan runs before the line does; the reviewer's probe is the first case), while reads and staging before it are not;
# R1-2, a shape broken across a line break; R1-3, an annotated tag's message, by name and through `--follow-tags`
LATER_CASES = [
    ("cd " + CLEAN + " && echo x > f.txt && git add f.txt && git commit -q -m x && git push -u origin HEAD", True),
    ("cd " + CLEAN + " && git add f.txt && git status && git push origin HEAD:pb-topic", False),
    ("cd " + CLEAN + " && git fetch -q origin && git push origin HEAD:pb-topic", False),
    ("cd " + CLEAN + " && git checkout -q main && git push origin HEAD:pb-topic", True),
    ("cd " + CLEAN + " && git ci -m x && git push origin HEAD:pb-topic", True),              # an alias: fails closed
    ("git -C " + CLEAN + " merge x; git -C " + CLEAN + " push origin HEAD:pb-topic", True),
    ("cd " + CLEAN + " && git commit -q -m x && git push other HEAD:pb-topic", False),       # another repository
    ("cd " + SPLIT + " && git push origin HEAD:pb-topic", True),
    ("cd " + TAGGED + " && git push origin v-stamped", True),
    ("cd " + TAGGED + " && git push --follow-tags origin HEAD:pb-topic", True),
    ("cd " + TAGGED + " && git push origin HEAD:pb-topic", False),
]
fails += [(f"[stamp, review] {c}", e) for c, e in LATER_CASES if blocked(c) != e]
_said = reason(LATER_CASES[0][0])
if "follows `git commit`" not in _said or "as its own command" not in _said:
    fails.append(("[stamp, review] the refusal names the step and says to push in its own command: " + _said, True))
STAMP_TOTAL = len(STAMP_CASES) + 3 + 1 + 1 + len(LATER_CASES) + 1
# the PowerShell tool inside a unit: its environment assignments redirect git the same way (kb/Work PB5269)
UNIT_PS = [("$env:GIT_DIR = '" + OTHER + "/.git'; git push origin HEAD", True),
           ("$env:GIT_DIR='" + OTHER + "/.git'; git push", True),
           ("Set-Item env:GIT_DIR '" + OTHER + "/.git'; git push", True),
           ("git push origin HEAD", False),
           ("git -C " + OTHER + " push origin HEAD", True)]
fails += [(f"[unit PowerShell] {c}", e) for c, e in UNIT_PS if blocked(c, unit=True, tool="PowerShell") != e]
for c, e in fails:
    print(f"FAIL: expected {'BLOCK' if e else 'PASS'}: {c!r}")
total = len(CASES) + len(UNIT_CASES) + len(PS_CASES) + len(ATTENDED) + len(UNIT_PS) + STAMP_TOTAL
print(f"forbidden_commands self-test: {total - len(fails)}/{total} " + ("GREEN" if not fails else "RED"))
sys.exit(1 if fails else 0)
