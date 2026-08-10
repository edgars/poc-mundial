# Product Brief — Mundial - Conferência

## Overview

Mundial - Conferência is a modernization of a legacy system that has been reverse-engineered by RNC. The product manages 2 core entities with full create/read/update/delete workflows and enforces 70 extracted business rules. The goal is to deliver a functionally-equivalent application on a modern, supported technology stack.

## Problem Statement

The current legacy application runs on outdated technology that is expensive to maintain and difficult to evolve. This modernization will reduce maintenance costs and enable future feature development.

## Target Users

Existing operators of the legacy system. All current data and workflows will be preserved to ensure continuity.

## In Scope

- **Conferencias** management (`/conferencias`)
- **Estoqs** management (`/estoqs`)
- Full CRUD workflows for both entities
- All 70 business rules from the legacy system

## Out of Scope (Phase 1)

- Data migration from the legacy database
- New features not present in the legacy system

## Target Technology Stack

| Layer | Technology |
|-------|------------|
| Frontend | Angular |
| Backend | .NET Core + Prisma |
| Database | SQL Server |