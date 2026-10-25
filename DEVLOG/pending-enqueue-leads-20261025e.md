## File PB3772: the landing lane's queue watch ends at IN_QUEUE

Filed kb/Work PB3772, "the landing lane's queue watch ENDS once it reads a pull request in the merge queue": a pull request the queue drops without a failed run reads NOT QUEUED and nothing acts on it. The witness is PR #593, which sat out of the queue with auto-merge off for over an hour on 2026-10-25. No code changed; the note carries the sites and the fix direction.
