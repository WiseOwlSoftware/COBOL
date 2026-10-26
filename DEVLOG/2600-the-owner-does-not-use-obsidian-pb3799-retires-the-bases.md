## Entry 2600 — 2026-10-25 19:18 PDT — The owner does not use Obsidian: PB3799 retires the Bases views and moves the register's views into work.py

The owner has not opened Obsidian in months, and no agent reads a Bases view, yet `work.py check` keeps `kb/Work.base`
in step and notes keep paying for it (PB3389, PB3789, and the Obsidian windows of PB3798). PB3799 adds a work.py
subcommand for each view a reader needs, then deletes the two `.base` files, `.obsidian/` and `check_base_agrees`. The
plain-Markdown pages of `kb/` stay.

