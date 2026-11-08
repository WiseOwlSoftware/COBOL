## PB4900: implementers write the report with the Write tool, because the worktree guard refuses a shell write

Three rolling-wave implementers returned without writing their report: wave 20261887 group E, wave 20261915 group B and
wave 20261921 group H. Each time `plan_wave.py --finished` read UNREADABLE, no review started, and the operator rebuilt
the report by hand. All three transcripts show the same cause. The agent wrote the report with one shell command,
`mkdir -p ... && cat > <report> <<'EOF'`, into the reports directory, which lies outside its worktree. Claude Code's
worktree isolation refused the command as "too complex to verify that it stays inside the worktree". The agent's next
turn was its structured return, naming the unwritten file.

The implementer spec now says, beside the report path, to write the report with the Write tool, `test -f` it, and return
only once it exists. `check_practices.py` requires that line in every rendered spec. MANDATORY-PRACTICES P12 names the
isolation refusal alongside the hooks: a refused command did not run. PB4900 records the cause and is landed.
