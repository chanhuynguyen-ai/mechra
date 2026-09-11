# Upgrade / test a new candidate

The full source archive is independent of your Git checkout. Do not reapply earlier v0.2 patches on top of this package.

Follow [RUN_WINDOWS.md](RUN_WINDOWS.md) to extract into a new versioned folder, stop the old agent by its verified project path, build, and register the new add-in. Registration points SOLIDWORKS to this candidate's output DLL. The COM GUID and assembly name stay compatible with the original add-in.

To return to the prior version, close SOLIDWORKS, stop the new agent, and build/register/run the prior folder again. Keep both source folders until the new candidate passes the live gates.

No git reset, force-push or repository re-creation is required. After acceptance, transfer reviewed source changes to your existing `dev` branch and commit. Do not tag `v0.2.0` while the live gate is open.
