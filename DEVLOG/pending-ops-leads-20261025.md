## The CI outage's record, the Linux gate's missing self-tests, and the build guard's wrong tree (PB3308, PB2431, PB3770)

PB3308 records Microsoft's issue dotnet/install-scripts#745 and how the outage ended: the aka.ms redirect was
repointed, the origin still refuses, and the fix's merge group was green before then. PB2431 gains the review lead
that the targeted Linux gate runs no script self-test. PB3770 files the Mythos lane's report that the build guard
reads every rolling-wave implementer as a main-tree agent and blocks scratch probe builds for hours.
