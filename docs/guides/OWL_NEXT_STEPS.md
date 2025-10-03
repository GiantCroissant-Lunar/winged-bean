# OWL Planner Next Steps Checklist

Use this checklist to complete the OWL planner integration.

## ✅ Completed (Setup Phase)

- [x] Created MCP server infrastructure (`planning/owl/`)
- [x] Added task plan template (`planning/plan.yaml`)
- [x] Built validation and rendering scripts
- [x] Configured Claude Code MCP integration (`.mcp.json`)
- [x] Added CI workflow for plan validation
- [x] Updated agent routing documentation (`AGENTS.md`)
- [x] Created comprehensive guides and quick reference
- [x] Tested validation script (7 tasks, generates Mermaid)

## 🔄 Next Actions (This Session or Next)

### Immediate Testing

- [ ] **Test OWL planner in Claude Code**
  ```
  Ask: "Call owl-planner.plan_dag with goal='Test MCP integration' and return the result"
  Expected: MCP tool executes, returns demo DAG
  ```

- [ ] **Customize plan.yaml**
  - [ ] Replace example tasks with real project tasks
  - [ ] Add tasks from existing RFCs (e.g., PTY integration)
  - [ ] Ensure dependencies match actual work order
  - [ ] Run validation: `python3 planning/scripts/validate_and_render.py`

- [ ] **Preview issue creation**
  ```bash
  python3 planning/scripts/open_issues_from_plan.py --dry-run
  ```
  - [ ] Review generated issue titles and bodies
  - [ ] Check that dependencies are correctly referenced
  - [ ] Verify label assignments (agent:*)

### Optional: Codex CLI Setup

- [ ] **Configure Codex CLI** (if you use it)
  - [ ] Copy `planning/codex-config-template.toml` to `~/.codex/config.toml`
  - [ ] Replace `/absolute/path/to/winged-bean` with actual path
  - [ ] Set `GITHUB_TOKEN` environment variable
  - [ ] Test: `codex` → ask "list MCP tools"

## 🔧 Integration Phase (Short-term)

### Install Camel-AI OWL

- [ ] **Clone and install OWL**
  ```bash
  cd ref-projects
  git clone https://github.com/camel-ai/camel.git
  cd camel
  pip install -e ".[mcp]"
  ```

- [ ] **Verify installation**
  ```bash
  python3 -c "import camel; print(camel.__version__)"
  ```

### Wire OWL Agent

- [ ] **Update `planner_server.py`**
  - [ ] Import CAMEL agent classes
  - [ ] Replace stub `plan_dag()` with actual OWL agent
  - [ ] Configure role-based prompts for decomposition
  - [ ] Add context reading (RFCs, READMEs, code structure)

- [ ] **Connect OWL's MCP clients**
  - [ ] Enable filesystem MCP server (read RFCs)
  - [ ] Enable GitHub MCP server (query issues, PRs)
  - [ ] Test OWL can access repo context

- [ ] **Test end-to-end**
  - [ ] Ask Claude Code to call `owl-planner.plan_dag` with real feature
  - [ ] Verify OWL reads context and returns structured plan
  - [ ] Check task labels match routing conventions

### CI Integration

- [ ] **Test CI workflow locally**
  ```bash
  # If you have 'act' installed
  act pull_request -W .github/workflows/plan-check.yml
  ```

- [ ] **Create a test PR**
  - [ ] Modify `planning/plan.yaml`
  - [ ] Push to branch and open PR
  - [ ] Verify CI validates DAG
  - [ ] Check PR comment has Mermaid diagram

### GitHub Issues

- [ ] **Create issues from plan**
  ```bash
  # First PR: create initial batch
  python3 planning/scripts/open_issues_from_plan.py
  ```

- [ ] **Label and assign**
  - [ ] Review created issues
  - [ ] Add project/milestone associations
  - [ ] Link related RFCs/ADRs in issue bodies

## 🚀 Workflow Phase (Use It!)

### Route Work to Agents

- [ ] **Copilot Agent** (parallel leaves)
  - [ ] Find issue labeled `agent:copilot` with no blockers
  - [ ] In Claude Code: mention `#github-pull-request_copilot-coding-agent`
  - [ ] Review PR when Copilot completes

- [ ] **Windsurf Cascade** (multi-file, frontend)
  - [ ] Find issue labeled `agent:cascade`
  - [ ] Open Windsurf, start Cascade session
  - [ ] Reference issue and implement

- [ ] **Claude Code** (integration, risky)
  - [ ] Find issue labeled `agent:claude`
  - [ ] Work interactively in Claude Code
  - [ ] Approve changes step-by-step

### Re-planning

- [ ] **Update plan based on PR feedback**
  - [ ] Ask OWL to refine plan: `owl-planner.refine_plan`
  - [ ] Commit updated `plan.yaml`
  - [ ] CI validates and updates Mermaid
  - [ ] Update or close/reopen issues as needed

## 🎨 Customization (Ongoing)

### Tune Routing Rules

- [ ] **Adjust thresholds**
  - [ ] Review `max_touch_files_per_task` (currently 15)
  - [ ] Tune based on team experience
  - [ ] Update `plan.yaml` meta rules

- [ ] **Add custom labels**
  - [ ] Consider: `priority:high`, `area:testing`, `tech:unity`
  - [ ] Document label conventions in `AGENTS.md`
  - [ ] Update validation script to check new labels

### Integrate PM Tools

- [ ] **Zenhub/Linear/Jira** (if using)
  - [ ] Map `plan.yaml` to PM tool's data model
  - [ ] Sync dependencies (blockers) automatically
  - [ ] Two-way sync: PM → plan.yaml, plan.yaml → PM

- [ ] **GitHub Projects** (simpler option)
  - [ ] Add issues to project board
  - [ ] Use column automation for status tracking

## 🧪 Advanced Features (Optional)

### Flip Topology: OWL Drives Edits

- [ ] **Start Claude Code as MCP server**
  ```bash
  claude mcp serve
  # Exposes Claude's editor tools to external MCP clients
  ```

- [ ] **Configure OWL as MCP client**
  - [ ] Point OWL to Claude's MCP server
  - [ ] Give OWL control of file editing
  - [ ] Useful for: autonomous refactoring, bulk changes

### Local LLM for OWL

- [ ] **Install Ollama or vLLM**
  ```bash
  # Ollama example
  brew install ollama
  ollama pull llama3.1
  ```

- [ ] **Point OWL to local model**
  - [ ] Update OWL config to use Ollama API
  - [ ] Test: OWL can do LLM reasoning without API cost
  - [ ] Useful for: cost-free brainstorming, large plans

### Plan Evolution Tracking

- [ ] **Version plan.yaml**
  - [ ] Track changes via git blame/log
  - [ ] Visualize plan evolution over time
  - [ ] Generate "plan changelog" from commits

- [ ] **Metrics and analytics**
  - [ ] Track task completion rate by agent
  - [ ] Measure estimate accuracy
  - [ ] Identify bottlenecks (tasks with many dependents)

## 📊 Success Criteria

You'll know the integration is working when:

1. ✅ Claude Code can call `owl-planner.plan_dag` without errors
2. ✅ Validation script passes on `plan.yaml` (no cycles)
3. ✅ CI posts Mermaid diagrams to PRs
4. ✅ GitHub issues are created with correct labels and dependencies
5. ✅ Copilot Agent handles parallel leaves autonomously
6. ✅ Re-planning updates the DAG and issues reflect changes
7. ✅ Team uses `plan.yaml` as single source of truth

## 🐛 Troubleshooting Reference

| Issue | Solution |
|-------|----------|
| MCP tool not found | Verify `.mcp.json` exists, test `planner_server.py` manually |
| Validation fails | Run script, read error, fix cycle/missing ref |
| Issue script fails | Ensure `pyyaml` installed: `pip install pyyaml` |
| CI doesn't trigger | Check workflow paths, ensure `plan.yaml` is modified |
| OWL returns stub data | Expected until you wire real CAMEL agent |
| Copilot doesn't pick up task | Check label, ensure no unmet dependencies |

## 📚 Documentation Quick Links

- **Full guide**: `docs/guides/owl-planner-integration.md`
- **Quick reference**: `docs/guides/OWL_QUICK_REFERENCE.md`
- **Setup summary**: `docs/guides/OWL_INTEGRATION_SUMMARY.md`
- **Planning README**: `planning/README.md`
- **Agent routing**: `AGENTS.md` (updated with routing rules)

---

**Current Status**: ✅ Infrastructure complete, ready for testing and customization

**Next milestone**: Test MCP integration in Claude Code, customize plan.yaml with real tasks

**Long-term goal**: Multi-agent workflow where OWL plans, Copilot executes leaves, Cascade handles UI, Claude integrates
