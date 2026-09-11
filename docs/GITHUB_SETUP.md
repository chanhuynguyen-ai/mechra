# Mechra — GitHub Setup

## Repository
Create a **private** GitHub repository named `Mechra` with no generated README, .gitignore, or license. The local source already contains the canonical project files.

## First push — v0.1.0
From the extracted project root:

```powershell
cd C:\AI_project\Mechra

git init
git branch -M main
git add .
git status
git commit -m "feat: release Mechra v0.1.0 foundation"

git remote add origin https://github.com/<YOUR_GITHUB_USERNAME>/Mechra.git
git push -u origin main

git tag -a v0.1.0 -m "Mechra v0.1.0 - Foundation"
git push origin v0.1.0
```

## Development branch for v0.2

```powershell
git checkout -b dev
git push -u origin dev
```

Feature work should branch from `dev`, for example:

```powershell
git checkout dev
git pull
git checkout -b feat/text-to-native-part
```

Do not merge v0.2 into `main` until its live SOLIDWORKS acceptance gate passes.

## Before every push

```powershell
git status
git diff --cached
```

Never commit `.venv`, `bin`, `obj`, `.env`, API keys, generated `.tlb` files, or local SOLIDWORKS binaries.
