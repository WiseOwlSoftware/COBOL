## Entry 2593 — 2026-10-25 18:21 PDT — The owner decides to split brent-tools so this repository loads only the skills it uses (PB3788)

The owner chose to disable the never-invoked skill listings; /skills refused them as "locked by plugin". Claude Code takes
a plugin skill's state only from an administrator setting or the plugin itself, so account 1's `skillOverrides` for six
brent-tools skills never took effect. Hiding them in their own frontmatter would hide them for every user and break
nine public evals, so the owner chose a second plugin, brent-tools-extras, enabled nowhere in this repository.

