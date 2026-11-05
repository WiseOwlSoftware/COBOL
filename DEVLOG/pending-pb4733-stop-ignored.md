## kb/Work PB4733: a fix-lane implementer ignored the fleet stop for 20 minutes

When the operator stopped wave 20261871, its only implementer (PB4700) checkpointed five minutes later, then kept
writing tests and committed again 19 minutes after that instead of returning SPLIT. The supervisor waits 90 minutes
before killing a wave, so the operator killed it by hand. PB4733 records it and proposes enforcing the stop with a
hook that allows only checkpoint calls while a stop file exists.
