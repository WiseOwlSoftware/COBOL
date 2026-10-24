## PB3654: three queue-lead notes repaired, and the register's reader drops a note it cannot parse without a word

Entry 2480 filed PB3651, PB3652 and PB3653 from the PB3170 and PB2974 reports, and they reached main (PR #513) unreadable:
every line of the three files began with a stray backtick, the frontmatter's opening line included (it read `` `--- ``),
inner backticks were escaped and one apostrophe was the literal text backslash-x27. The clerk's own "clean escaped
backticks" commit had not touched them. The enqueue unit found it while checking the pull request, repaired the notes on
the branch, and the queue merged #513's earlier head first, so the repair lands here: exactly one leading backtick is
removed per line and the escapes are undone, by a script that refuses any residue; the bodies are otherwise unchanged.

`work.py check` printed every item well-formed over the damaged files, and that is the defect PB3654 records. `load()`
skips any `kb/Work` file that `parse_frontmatter` answers `None` for, so a file named like a note whose frontmatter is
damaged leaves the register: `work.py next` never ranks it, the planner never reads its Sites paragraph, and `check`
counts the rest. Beside it, `check()` ignores `--work`, the test seam `main` parses: a probe directory holding the
damaged PB3653.md and a good PB3650.md printed the repository's 2922 items, not its two. With the repair the register
reads 2923 items, PB3654 included.
