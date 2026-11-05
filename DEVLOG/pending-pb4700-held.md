## kb/Work PB4700 held until the message-control design lands

PB4700, the messaging facility the owner chose to implement, is now `status: blocked`. Mythos is writing its design and
is evaluating, at the owner's request, which transport both message facilities should share. PB4677's landed
`MessageStore` file store is one candidate, not the settled answer. Fix-lane wave 20261871 had planned PB4700 at 21:17,
before the hold existed. The operator stopped that wave at 21:30, and its checkpointed branch is left for the design to
adopt or abandon.
