## PB5068: a drained loop never holds

While winding the loop down for a CLI update, the operator found eleven consecutive empty Opus wave units in
`orchestrate-operator-20261109b.log` (19:03 to 19:30). Every one planned 0 groups because campaign PB4812's notes all wait on
PB4997 and the fix lane had nothing fixable. The tenth handoff set `next_unit` to null and said not to start another wave,
but `next_unit.py` has no idle outcome: `base_choice` and `campaign_choice` both default to a wave. Filed kb/Work PB5068 with
the sites and a fix shape: an `idle` choice decided from the disk (no ready lane, an empty `work.fixable`, no branch waiting),
during which only the review and landing ticks run, with two tests.
