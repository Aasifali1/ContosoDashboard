<!--
Sync Impact Report
- Version change: 0.0.0 -> 1.0.0
- Modified principles: none (new constitution baseline)
- Added sections: Core Principles, Operational Constraints, Development Workflow, Governance
- Removed sections: none
- Deferred items: RATIFICATION_DATE pending original adoption date confirmation
-->

# ContosoDashboard Constitution

## Core Principles

### I. Training-Safe Scope
This repository exists for learning and demonstration, not for production deployment.
All features, security practices, and architectural choices must be clearly labeled as
training-oriented, and no change may claim production readiness without an explicit upgrade
path and validation outside this training context.

### II. Security by Default
Every user-facing workflow must enforce authentication, authorization, and data isolation.
Mock authentication may be used only for training; any security control must protect against
unauthorized access, misuse, and direct object reference attacks by default.

### III. Validation Before Completion
No feature is complete until the relevant behavior is validated with a real execution path.
Changes must be testable, reproducible, and checked in the application environment where
possible before merge or approval. When a requirement cannot be validated, the gap must be
documented rather than assumed.

### IV. Architecture Clarity
The codebase must favor clear separation of concerns, explicit contracts, and maintainable
patterns over clever shortcuts. Shared logic belongs in services or domain boundaries, not in
duplicated page logic; infrastructure dependencies must remain replaceable for offline or
cloud migration scenarios.

### V. Offline-First and Learning-Oriented Delivery
The project must remain runnable in a local, offline environment without external cloud
dependencies. New features must preserve the training goal, minimize operational friction,
and document any required assumptions, limitations, or migration paths.

## Operational Constraints

The project is intentionally limited to a mock, educational environment. Use local
development data, local authentication flows, and file-based or in-memory storage unless the
work explicitly introduces a documented training demonstration. Any change that adds external
services, external identities, or production-grade security controls must state the rationale
and whether it remains a training-only adaptation.

## Development Workflow

All changes must be made in small, reviewable units with clear intent and direct links to
stated requirements. Feature work must preserve the existing training narrative, keep the code
understandable to junior developers, and avoid hidden state or undocumented integration
points. Reviews must check compliance with the principles above, security boundaries, and
clear documentation of trade-offs.

## Governance

This constitution supersedes informal conventions within this repository. Amendments require
a documented rationale, explicit approval from the maintainers, and a clear statement of the
impact on existing practices. Changes that alter architecture, security assumptions, or
required validation paths must include migration notes when relevant.

The project is governed by the following rules:
- All substantive changes must remain consistent with the five core principles.
- Security-sensitive changes require explicit review of access control, authentication,
  and data isolation assumptions.
- Work that changes the runtime environment, deployment model, or external dependencies
  must document the risk and the offline-training impact.
- Quality gates are mandatory: behavior must be validated, and unresolved assumptions must
  be recorded before completion.
- Versioning follows semantic versioning: MAJOR for breaking governance or architectural
  changes, MINOR for new principles or materially expanded guidance, and PATCH for
  clarifications or wording refinements.

**Version**: 1.0.0 | **Ratified**: TODO(RATIFICATION_DATE): original adoption date not yet recorded | **Last Amended**: 2026-09-13
