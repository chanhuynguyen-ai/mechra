# Release and Git Process

## Branches
- `main`: only milestones that have passed their current acceptance gate.
- `dev`: integration work for the next milestone.
- Feature branches: `feat/<short-name>`, `fix/<short-name>`, `docs/<short-name>`.

## Versioning
Use semantic versions: `vMAJOR.MINOR.PATCH`.
- Minor: a new milestone/capability (`0.1 → 0.2`).
- Patch: fixes that do not change the milestone contract (`0.2.0 → 0.2.1`).
- Major: commercial/stable compatibility break.

## Commit style
Use Conventional Commits:
- `feat:` capability
- `fix:` defect
- `refactor:` internal architecture
- `test:` tests/benchmarks
- `docs:` documentation
- `build:` build/installer/tooling
- `chore:` maintenance

## Release gate
Before tagging a release:
1. Update `VERSION`.
2. Update `CHANGELOG.md`.
3. Update `docs/MVP_ACCEPTANCE.md` and `docs/ROADMAP.md`.
4. Run Python tests/contracts.
5. Build x64 .NET add-in against the target SOLIDWORKS API.
6. Register/load on real SOLIDWORKS.
7. Run milestone-specific live acceptance scenario.
8. Record known limitations; never mark an untested behavior as passed.
9. Commit and tag: `git tag -a vX.Y.Z -m "Mechra vX.Y.Z"`.
