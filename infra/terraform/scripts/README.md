# Terraform Cloud Variable & Run Scripts

Helpers for managing TFC workspace variables and triggering runs for the `winged-bean` GitHub repo configuration without needing the local Terraform CLI.

## Scripts

### `Set-TfcVariable.ps1`
Upsert a single variable.

Parameters:
- `-Organization` (TFC org)
- `-Workspace` (workspace name)
- `-Name` (variable key)
- `-Value` (variable value)
- `-Category` `terraform|env` (default terraform)
- `-Hcl` (switch) treat value as HCL
- `-Sensitive` (switch) mark sensitive

Example:
```powershell
./Set-TfcVariable.ps1 -Organization giantcroissant-lunar -Workspace winged-bean-github -Name github_owner -Value GiantCroissant-Lunar
```

### `BulkApply-TfcVariables.ps1`
Apply variables from a JSON descriptor (see `variables.sample.json`).

Example:
```powershell
./BulkApply-TfcVariables.ps1 -Organization giantcroissant-lunar -Workspace winged-bean-github -File ./variables.json
```

### `Apply-SecretsJson.ps1`
Load key/value pairs from `../github/secrets/terraform-github-vars.json` (or encrypted variants) and push them:
- `GITHUB_TOKEN` => env category, sensitive
- Keys ending in `_SECRET`, `_TOKEN`, `_KEY`, `_PASS`, `_PASSWORD` become sensitive terraform vars
- Booleans / numbers / list `[ ... ]` / map `{ ... }` forms auto-flagged as HCL
- Others => terraform string vars

Example:
```powershell
./Apply-SecretsJson.ps1 -Organization giantcroissant-lunar -Workspace winged-bean-github
```

### `Queue-TfcRun.ps1`
Queue a new plan/apply run in Terraform Cloud.

Parameters:
- `-Organization` (org name)
- `-Workspace` (workspace name)
- `-Message` optional run message (default "Queued via API script")
- `-IsDestroy` switch to create a destroy plan

Returns the Run ID (GUID).

Example:
```powershell
$runId = ./Queue-TfcRun.ps1 -Organization giantcroissant-lunar -Workspace winged-bean-github -Message "Initial config"
```

### `Get-TfcRunStatus.ps1`
Fetch current status of a run.

Parameters:
- `-RunId` run GUID

Outputs object with: RunId, Status, PlanId, ApplyId.

Example:
```powershell
./Get-TfcRunStatus.ps1 -RunId $runId
```

Common statuses: pending, planning, planned, cost_estimating, cost_estimated, applying, applied, discarded, errored, canceled.

### `Get-TfcOutputs.ps1`
Retrieve current state outputs for a workspace.

Parameters:
- `-Organization`
- `-Workspace`

Returns Name / Value / Sensitive for each output (sensitive values not redacted in raw API if not flagged sensitive in Terraform config—treat carefully).

Example:
```powershell
./Get-TfcOutputs.ps1 -Organization giantcroissant-lunar -Workspace winged-bean-github
```

## Workflow Without Local Terraform CLI
1. Ensure variables are populated:
   ```powershell
   ./Apply-SecretsJson.ps1 -Organization giantcroissant-lunar -Workspace winged-bean-github
   ```
2. Queue a run:
   ```powershell
   $runId = ./Queue-TfcRun.ps1 -Organization giantcroissant-lunar -Workspace winged-bean-github -Message "GitHub repo mgmt initial"
   ```
3. Poll status (repeat until Status is planned, applied, or errored):
   ```powershell
   ./Get-TfcRunStatus.ps1 -RunId $runId
   ```
4. (Optional) If run requires confirmation (auto-apply disabled) approve it manually in UI, or add an approval API script (future enhancement).
5. After applied, fetch outputs:
   ```powershell
   ./Get-TfcOutputs.ps1 -Organization giantcroissant-lunar -Workspace winged-bean-github
   ```

## Auth
Set `TFC_TOKEN` in your shell:
```powershell
$env:TFC_TOKEN = '<terraform_cloud_token>'
```

## Tips
- Sensitive vars always fully sent (no diff detection here)
- For complex maps/lists ensure valid HCL when using `-Hcl` or `"hcl": true`
- Use version control for JSON file but exclude actual secret values
- Queue multiple runs sequentially; TFC auto-cancels superseded runs if speculative.
- Use `-IsDestroy` to produce a destroy plan (won't apply unless approved if auto-apply off).

Add this to a `.gitignore` if you create a real variables file:
```
infra/terraform/scripts/variables.json
```

## SOPS-based Secret Encryption
Use [SOPS](https://getsops.io) with age to encrypt secrets. The SOPS config file is at `infra/terraform/.sops.yaml`.

### Files (Current Layout)
- `infra/terraform/.sops.yaml` - SOPS configuration
- `infra/terraform/github/secrets/terraform-github-vars.json` - plaintext (gitignored)
- `infra/terraform/github/secrets/github-vars.json.encrypted` - encrypted committed file
- `infra/terraform/secrets/age.key` - age private key (gitignored)

### Scripts
- `New-AgeKeyPair.ps1` - Generate age encryption key pair
- `Encrypt-Secrets.ps1` - Encrypt secrets using SOPS + age
- `Apply-SecretsJson.ps1` - Auto-detect and decrypt secrets, then push to TFC

### Setup

#### 1. Generate Age Key
```powershell
./New-AgeKeyPair.ps1
# Or specify custom location:
./New-AgeKeyPair.ps1 -OutFile ./infra/terraform/github/secrets/age.key
```

#### 2. Update SOPS Config
Edit `infra/terraform/.sops.yaml` and add your age public key to the recipients list.

#### 3. Prepare Plaintext Secrets
Create `infra/terraform/github/secrets/terraform-github-vars.json`:
```json
{
  "github_owner": "GiantCroissant-Lunar",
  "repository_name": "winged-bean",
  "GITHUB_TOKEN": "ghp_your_token_here"
}
```

#### 4. Encrypt Secrets
```powershell
./Encrypt-Secrets.ps1 -Force
```

#### 5. Apply to TFC
```powershell
./Apply-SecretsJson.ps1 -Organization giantcroissant-lunar -Workspace winged-bean-github
```

### Auto-Detection Order
`Apply-SecretsJson.ps1` searches for secrets in this order:
1. `../github/secrets/github-vars.json.encrypted`
2. `../github/secrets/terraform-github-vars.json`
3. `../github/secrets/terraform.json.encrypted`
4. `../github/secrets/terraform.json`
5. `../secrets/github-vars.json.encrypted`
6. `../secrets/terraform.json.encrypted`
7. `../secrets/terraform.json`

### Rotating / Updating Secrets
1. Edit plaintext JSON file
2. Re-run encryption with `-Force` to overwrite:
   ```powershell
   ./Encrypt-Secrets.ps1 -Force
   ```
3. Commit updated encrypted file
4. Re-run Apply script

### Key Rotation
- Generate new key file
- Add new recipient to `.sops.yaml`
- Re-encrypt with both old & new (SOPS supports multiple recipients)
- Distribute new key, then remove old recipient and re-encrypt

### Security Notes
- Never commit `age.key` or plaintext JSON files (already in `.gitignore`)
- CI pipeline needs the age key injected (secret variable) for decrypting
- `Apply-SecretsJson.ps1` only decrypts locally where `sops` & key are present

## GitHub Actions CI (Plan + Apply)
You can create a workflow file: `.github/workflows/terraform.yml`

Secrets required:
- `TFC_TOKEN` - Terraform Cloud user/org token
- `AGE_KEY` - contents of your age private key file

Process steps:
1. Checkout
2. Install Terraform & SOPS
3. Write age key to `infra/terraform/secrets/age.key`
4. Decrypt secrets using SOPS
5. Run `Apply-SecretsJson.ps1` to sync TFC vars
6. `terraform init`, `validate`, `plan`, `apply` (apply only on main branch)

## Future Enhancements
- Diff mode (only show pending changes)
- Delete vars not in file (safe prune)
- Export script to dump current non-sensitive vars
- Approval script (`/runs/:id/actions/apply`)
- Auto polling helper that waits until terminal state
