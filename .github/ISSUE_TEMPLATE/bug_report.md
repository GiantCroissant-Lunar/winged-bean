<!-- spec-ground: template=.github/ISSUE_TEMPLATE/bug_report.md version=0.1.0 checksum=5e968fc7e1e3af188a741a5f15c3b7a27d6d305c2031fd0c6d1b8c39138fc193 -->
<!-- This file is managed by spec-ground. Do not edit in-place. -->

name: Bug report
description: Report a bug to help us improve
labels: [bug]

body:
  - type: textarea
    attributes:
      label: Describe the bug
      description: A clear and concise description of what the bug is.
    validations:
      required: true
  - type: textarea
    attributes:
      label: Steps to reproduce
      description: Steps to reproduce the behavior.
    validations:
      required: true
  - type: textarea
    attributes:
      label: Expected behavior
    validations:
      required: true
  - type: textarea
    attributes:
      label: Screenshots / logs
    validations:
      required: false

