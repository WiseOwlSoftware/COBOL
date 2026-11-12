## PB5457 filed: the wave unit's wait path names a directory the harness no longer writes

Wave unit 20262009 found that the prompt's `{TASKS_DIR}` (built in `orchestrate.ps1#Build-Prompt` from the `E--COBOL` project
slug) does not exist: the harness writes the Workflow's output, and every background Bash output, under a hashed project
directory (`E:/Temp/claude/a398860f1e7c5585/<session>/tasks/`). A unit that follows wave.md step 4 literally polls a missing
file and never sees its Workflow return. Filed as kb/Work PB5457 (process only); this unit waited on the real path instead.
