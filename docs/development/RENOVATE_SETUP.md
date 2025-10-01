# Renovate Setup Guide

This guide will help you enable and configure Renovate for automated dependency management in the winged-bean project.

## ✅ Configuration Status

- [x] `renovate.json` created and committed
- [x] Configuration pushed to GitHub
- [ ] Renovate GitHub App installed
- [ ] Repository settings configured

## 📋 Installation Steps

### Step 1: Install Renovate GitHub App

The installation page should have opened in your browser. If not, visit:
**https://github.com/apps/renovate/installations/new**

1. **Select Account**: Choose `GiantCroissant-Lunar` organization
2. **Repository Access**: Choose one of:
   - **All repositories** (recommended for organization-wide use)
   - **Only select repositories** → Select `winged-bean`
3. Click **Install**

### Step 2: Verify Installation

After installation, Renovate will:

1. **Create an Onboarding PR** within a few minutes
   - This PR validates your `renovate.json` configuration
   - Review and merge it to activate Renovate

2. **Create a Dependency Dashboard Issue**
   - Shows all available updates
   - Allows you to approve major updates
   - Provides overview of Renovate activity

### Step 3: Enable Auto-merge (Optional but Recommended)

To allow Renovate to auto-merge safe updates:

```bash
# Enable auto-merge for the repository
gh repo edit GiantCroissant-Lunar/winged-bean --enable-auto-merge

# Or via GitHub UI:
# Settings → General → Pull Requests → Allow auto-merge ✓
```

### Step 4: Configure Branch Protection (Recommended)

Set up branch protection for `main`:

```bash
# Via GitHub CLI
gh api repos/GiantCroissant-Lunar/winged-bean/branches/main/protection \
  --method PUT \
  --field required_status_checks[strict]=true \
  --field required_status_checks[contexts][]=MegaLinter \
  --field enforce_admins=false \
  --field required_pull_request_reviews[required_approving_review_count]=0 \
  --field restrictions=null

# Or via GitHub UI:
# Settings → Branches → Add rule for 'main'
# - Require status checks to pass before merging
# - Require branches to be up to date before merging
# - Status checks: MegaLinter
```

## 🔍 What Renovate Will Do

### Automatic Updates

Renovate will create PRs for:

| Dependency Type | Grouping | Schedule | Auto-merge |
|----------------|----------|----------|------------|
| **.NET NuGet** | Grouped by language | Monday 6am | Patch only (dev deps) |
| **Node.js (pnpm)** | Grouped by language | Monday 6am | Patch only (dev deps) |
| **Python** | Grouped by language | Monday 6am | Patch only (dev deps) |
| **Terraform** | Grouped by tool | Monday 6am | No |
| **GitHub Actions** | Grouped, pinned to SHA | Monday 6am | No |

### Update Types

- **Patch** (1.0.0 → 1.0.1): Auto-merged for dev dependencies
- **Minor** (1.0.0 → 1.1.0): Separate PR, requires review
- **Major** (1.0.0 → 2.0.0): Requires approval in Dependency Dashboard

### Lock File Maintenance

- Runs on the **first day of each month**
- Updates lock files without changing dependency versions
- Helps keep lock files clean and up-to-date

## 🎯 Expected Behavior

### First Run (After Onboarding PR is Merged)

Renovate will scan your repository and create:

1. **Grouped PRs** for each language/tool:
   - `Update .NET dependencies`
   - `Update Node.js dependencies`
   - `Update Python dependencies`
   - `Update Terraform dependencies`
   - `Update GitHub Actions`

2. **Dependency Dashboard Issue** showing:
   - All available updates
   - Rate-limited updates
   - Pending approvals for major versions

### Ongoing Behavior

- **Every Monday before 6am** (Asia/Singapore time):
  - Scans for new dependency updates
  - Creates/updates PRs as needed
  - Auto-merges safe patch updates

- **First day of each month**:
  - Updates lock files

- **Immediately** (for security vulnerabilities):
  - Creates PR with `security` label
  - Bypasses rate limiting

## 🔧 Customization

### Adjust Schedule

Edit `renovate.json` to change update timing:

```json
{
  "schedule": [
    "before 6am on Monday"  // Change to your preferred time
  ]
}
```

### Add Assignees/Reviewers

```json
{
  "assignees": ["your-github-username"],
  "reviewers": ["team-member-1", "team-member-2"]
}
```

### Ignore Specific Dependencies

```json
{
  "ignoreDeps": [
    "some-package-name",
    "another-package"
  ]
}
```

### Change Auto-merge Behavior

```json
{
  "packageRules": [
    {
      "description": "Auto-merge all minor updates",
      "matchUpdateTypes": ["minor", "patch"],
      "automerge": true
    }
  ]
}
```

## 🐛 Troubleshooting

### Renovate Not Creating PRs

1. **Check Dependency Dashboard Issue**
   - Look for rate limiting messages
   - Check for configuration errors

2. **Validate Configuration**
   ```bash
   # Use Renovate's config validator
   npx --yes --package renovate -- renovate-config-validator
   ```

3. **Check Renovate Logs**
   - Visit: https://app.renovatebot.com/dashboard
   - Sign in with GitHub
   - View logs for your repository

### PRs Not Auto-merging

1. **Verify auto-merge is enabled**:
   ```bash
   gh repo view GiantCroissant-Lunar/winged-bean --json autoMergeAllowed
   ```

2. **Check branch protection rules**:
   - Ensure required status checks are passing
   - Verify auto-merge is not blocked by protection rules

3. **Review Renovate configuration**:
   - Ensure `automerge: true` is set for desired package rules
   - Check that `automergeType` and `automergeStrategy` are configured

## 📚 Resources

- **Renovate Documentation**: https://docs.renovatebot.com/
- **Configuration Options**: https://docs.renovatebot.com/configuration-options/
- **Preset Configs**: https://docs.renovatebot.com/presets-config/
- **Renovate Dashboard**: https://app.renovatebot.com/dashboard

## ✅ Verification Checklist

After installation, verify:

- [ ] Onboarding PR created and merged
- [ ] Dependency Dashboard issue created
- [ ] First set of update PRs created
- [ ] Auto-merge working for patch updates (if enabled)
- [ ] MegaLinter passing on Renovate PRs
- [ ] No configuration errors in Dependency Dashboard

## 🎉 Success!

Once Renovate is running, you'll have:

- **Automated dependency updates** every Monday
- **Security vulnerability alerts** immediately
- **Organized PRs** grouped by language/tool
- **Reduced maintenance burden** with auto-merge
- **Full visibility** via Dependency Dashboard

Your dependencies will stay up-to-date with minimal manual effort! 🚀
