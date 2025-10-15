<!-- spec-ground: template=.github/ISSUE_TEMPLATE/feature_request.md version=0.1.0 checksum=789a7061422e7a0275072074a83e25b7f59882ff7c0d13cc61291edc878abdc9 -->
<!-- This file is managed by spec-ground. Do not edit in-place. -->

name: Feature request
description: Suggest an idea for this project
labels: [enhancement]

body:
  - type: textarea
    attributes:
      label: Problem statement
      description: What problem are you trying to solve?
    validations:
      required: true
  - type: textarea
    attributes:
      label: Proposed solution
    validations:
      required: true
  - type: textarea
    attributes:
      label: Alternatives considered
    validations:
      required: false

