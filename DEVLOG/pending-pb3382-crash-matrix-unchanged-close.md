## PB3382: the keyed-store engine's crash matrix stopped refusing the sync of an unchanged CLOSE

The operator files kb/Work PB3382, a LOW lead from the Mythos lane's review of PB3333 (`mythos-a3-review-r4`). Since
`1a2728f7a`, `PagedFileCrashMatrixTests`' CLOSE scene always changes store A first, so no test refuses the sync of a
store that completed no statement since its last promotion. A regression answering '00' for that refused sync would
pass every gate. The note asks for a `PagePath.CloseUnchanged` scene, refused at the page layer and through the
connector, as MANDATORY-PRACTICES I11 requires. It sits in cluster PB3330, blocked by PB3333, whose branch carries the
code. Notes only.
