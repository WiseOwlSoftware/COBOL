---
title: Release notes
description: Where the release notes of each WiseOwl COBOL version live and how they are written.
sidebar:
  order: 6
---

No version of the compiler has been released yet, so there are no release notes to read. Each release will add a section
to this page, newest first, headed with the version and its date.

## How a release's notes are written

A release's section is derived from the development log entries (the repository's `DEVLOG/` directory) landed between
the previous release tag and this one. It is edited, not copied: a reader of the notes needs to know what changed for
them, and the log's entries record how each change was made.

A section uses these headings, and leaves out any that is empty:

| Heading | What goes under it |
|---|---|
| Added | a construct, option, diagnostic or runtime facility that is new |
| Changed | a behavior that now differs, with the clause of the standard or the conformance determination that required it |
| Fixed | a program that was refused, miscompiled or run wrongly and now is not, with the diagnostic or the wrong result it replaces |
| Removed | an option or behavior that no longer exists |
| Known limitations | what a user will meet and cannot yet avoid, for example a documented gap in the conformance statement |

A change to a diagnostic code, a command-line option or an exit status is always listed, because scripts depend on them.
A change in what the compiler accepts is listed with the edition it applies to.
