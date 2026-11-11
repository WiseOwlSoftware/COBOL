## PB5225: the line-ending audit checks only what a branch changes and CI keeps one full sweep

The owner asked why `line_endings.py --check` was still needed when `.gitattributes` says `* text=auto eol=lf`. The
attributes miss three cases: a lone CR makes git store the file as binary, a Windows tool can leave a CRLF checkout, and
a commit made through the API skips the attributes. So the rule stays, but the gate no longer re-reads all 35,241 tracked
files (13.2 s) to apply it. `gate.py` hands the LINE ENDINGS audit the change's base and measured tree, and the script
reads only the paths `inert_lane.changed` lists plus the checkout's modified and untracked files. WRITERS parses only the
changed live Python files. On this host the change form took 1.5 s against 10 to 30 s for the sweep.

CI keeps one full sweep, in the Linux `audits` job. The Windows-job step and the nightly step are gone: a CI checkout is a
fresh checkout of blobs that sweep just read, LF on every OS while the attributes hold. So a new ATTRIBUTES arm asks
`git check-attr text eol` for every file read and refuses any that is neither `-text` nor text with `eol=lf`: a narrower
rule such as `*.cob -eol`, which only the Windows step would have noticed before (review R1-4). A change that edits a
`.gitattributes` or the check's own modules re-classifies files it does not list, so it runs the whole sweep (R1-3).
