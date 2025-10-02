#!/usr/bin/env python3
"""Event router for agent automation workflows (RFC-0015 Phase 2).

This script processes GitHub webhook events and dispatches them to appropriate
specialized handler workflows. Part of the hub-and-spoke architecture to
minimize runner minute consumption.

Usage:
    python event_router.py --event-type issues --payload $GITHUB_EVENT_PATH
    python event_router.py --event-type pull_request --payload /path/to/payload.json
"""

import argparse
import json
import os
import subprocess
import sys
from pathlib import Path
from typing import Any, Dict, List, Optional


class EventRouter:
    """Routes GitHub events to specialized handler workflows."""

    def __init__(self, repo: str, token: Optional[str] = None):
        """Initialize event router.

        Args:
            repo: Repository in owner/name format
            token: GitHub token for API calls
        """
        self.repo = repo
        self.token = token or os.environ.get('GITHUB_TOKEN', '')
        self.routes: List[Dict[str, Any]] = []

    def load_event_payload(self, payload_path: Path) -> Dict[str, Any]:
        """Load event payload from GitHub Actions event file.

        Args:
            payload_path: Path to event JSON file

        Returns:
            Event payload dictionary
        """
        try:
            with open(payload_path, 'r', encoding='utf-8') as f:
                return json.load(f)
        except Exception as e:
            print(f"Error loading event payload: {e}", file=sys.stderr)
            sys.exit(1)

    def extract_issue_metadata(self, issue_body: str) -> Optional[Dict[str, Any]]:
        """Extract YAML metadata from issue body.

        Args:
            issue_body: Issue body text

        Returns:
            Metadata dictionary or None
        """
        import re
        try:
            import yaml
        except ImportError:
            print("Warning: PyYAML not installed, cannot parse metadata",
                  file=sys.stderr)
            return None

        # Try YAML frontmatter
        frontmatter_match = re.match(r'^---\n(.*?)\n---', issue_body, re.DOTALL)
        if frontmatter_match:
            try:
                return yaml.safe_load(frontmatter_match.group(1))
            except yaml.YAMLError as e:
                print(f"Warning: Invalid YAML in issue: {e}", file=sys.stderr)
                return None

        # Try code blocks
        code_block_pattern = re.compile(
            r'```(?:yaml|yml)\n(.*?)\n```',
            re.DOTALL | re.MULTILINE
        )
        for match in code_block_pattern.finditer(issue_body):
            try:
                metadata = yaml.safe_load(match.group(1))
                if isinstance(metadata, dict) and 'rfc' in metadata:
                    return metadata
            except yaml.YAMLError:
                continue

        return None

    def route_issue_event(
        self,
        action: str,
        issue_number: int,
        issue_body: str,
        issue_state: str
    ) -> None:
        """Route issue event to appropriate workflows.

        Args:
            action: Issue action (opened, closed, etc.)
            issue_number: Issue number
            issue_body: Issue body text
            issue_state: Issue state (open, closed)
        """
        metadata = self.extract_issue_metadata(issue_body)

        if action == 'opened' and metadata:
            # Check if agent-assignable
            if metadata.get('agent_assignable', False):
                # Check dependencies
                depends_on = metadata.get('depends_on', [])
                if self.are_dependencies_resolved(depends_on):
                    # Route to assign agent workflow
                    self.routes.append({
                        'workflow': 'agent-assign.yml',
                        'inputs': {
                            'issue_number': str(issue_number)
                        }
                    })
                else:
                    print(f"Issue #{issue_number} has unresolved dependencies, "
                          f"skipping assignment")

    def route_pr_event(
        self,
        action: str,
        pr_number: int,
        pr_state: str,
        pr_head_sha: str
    ) -> None:
        """Route pull request event to appropriate workflows.

        Args:
            action: PR action (opened, closed, synchronize, etc.)
            pr_number: PR number
            pr_state: PR state (open, closed)
            pr_head_sha: HEAD commit SHA
        """
        if action == 'closed' and pr_state == 'closed':
            # PR closed - might need cleanup if not merged
            # Let cleanup workflow handle stalled PRs
            pass

    def route_workflow_run_event(
        self,
        workflow_name: str,
        conclusion: str,
        run_id: int
    ) -> None:
        """Route workflow_run event to appropriate workflows.

        Args:
            workflow_name: Name of the workflow
            conclusion: Workflow conclusion (success, failure, etc.)
            run_id: Workflow run ID
        """
        if conclusion == 'failure':
            # Route to watchdog for retry logic
            self.routes.append({
                'workflow': 'agent-watchdog.yml',
                'inputs': {
                    'run_id': str(run_id),
                    'workflow_name': workflow_name
                }
            })

    def are_dependencies_resolved(self, depends_on: List[int]) -> bool:
        """Check if all dependencies are resolved (closed).

        Args:
            depends_on: List of issue numbers

        Returns:
            True if all dependencies closed, False otherwise
        """
        if not depends_on:
            return True

        for dep in depends_on:
            result = subprocess.run(
                ['gh', 'issue', 'view', str(dep), '--json', 'state'],
                capture_output=True,
                text=True,
                timeout=10
            )
            if result.returncode != 0:
                print(f"Warning: Could not check issue #{dep}", file=sys.stderr)
                return False

            try:
                issue_data = json.loads(result.stdout)
                if issue_data.get('state') != 'CLOSED':
                    return False
            except json.JSONDecodeError:
                print(f"Warning: Invalid JSON from gh for issue #{dep}",
                      file=sys.stderr)
                return False

        return True

    def dispatch_workflows(self) -> None:
        """Dispatch all collected workflow routes."""
        if not self.routes:
            print("No workflows to dispatch")
            return

        for route in self.routes:
            workflow = route['workflow']
            inputs = route.get('inputs', {})
            print(f"Dispatching workflow: {workflow} with inputs: {inputs}")

            cmd = [
                'gh', 'workflow', 'run', workflow,
                '--repo', self.repo,
                '--ref', 'main'
            ]

            for key, value in inputs.items():
                cmd.extend(['-f', f'{key}={value}'])

            try:
                result = subprocess.run(
                    cmd,
                    capture_output=True,
                    text=True,
                    timeout=30
                )
                if result.returncode != 0:
                    print(f"Error dispatching {workflow}: {result.stderr}",
                          file=sys.stderr)
                else:
                    print(f"Successfully dispatched {workflow}")
            except subprocess.TimeoutExpired:
                print(f"Timeout dispatching {workflow}", file=sys.stderr)
            except Exception as e:
                print(f"Error dispatching {workflow}: {e}", file=sys.stderr)


def main():
    parser = argparse.ArgumentParser(
        description="Route GitHub events to specialized handler workflows "
                    "(RFC-0015 Phase 2)"
    )

    parser.add_argument(
        '--event-type',
        required=True,
        choices=['issues', 'pull_request', 'workflow_run'],
        help='Type of GitHub event'
    )

    parser.add_argument(
        '--payload',
        type=Path,
        required=True,
        help='Path to event payload JSON file'
    )

    parser.add_argument(
        '--repo',
        help='Repository (owner/name). Defaults to GITHUB_REPOSITORY env var.'
    )

    args = parser.parse_args()

    # Get repository from env if not provided
    repo = args.repo or os.environ.get('GITHUB_REPOSITORY', '')
    if not repo:
        print("Error: Repository not specified and GITHUB_REPOSITORY not set",
              file=sys.stderr)
        sys.exit(1)

    router = EventRouter(repo=repo)
    payload = router.load_event_payload(args.payload)

    # Route based on event type
    if args.event_type == 'issues':
        issue = payload.get('issue', {})
        router.route_issue_event(
            action=payload.get('action', ''),
            issue_number=issue.get('number', 0),
            issue_body=issue.get('body', ''),
            issue_state=issue.get('state', '')
        )
    elif args.event_type == 'pull_request':
        pr = payload.get('pull_request', {})
        router.route_pr_event(
            action=payload.get('action', ''),
            pr_number=pr.get('number', 0),
            pr_state=pr.get('state', ''),
            pr_head_sha=pr.get('head', {}).get('sha', '')
        )
    elif args.event_type == 'workflow_run':
        workflow_run = payload.get('workflow_run', {})
        router.route_workflow_run_event(
            workflow_name=workflow_run.get('name', ''),
            conclusion=workflow_run.get('conclusion', ''),
            run_id=workflow_run.get('id', 0)
        )

    # Dispatch collected workflows
    router.dispatch_workflows()


if __name__ == '__main__':
    main()
