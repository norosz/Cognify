# Project Rules & Operational Guidelines

This document defines the mandatory workflow and rules for any AI agent or developer working on **Cognify**.

## 1. Context & Compliance
- **Mandatory Learning**: Before starting any task, you **MUST** ensure you have read and understood:
  - `PROJECT_SPEC.md`: The source of truth for features and architecture.
  - `BACKEND_AGENT_RULES.md`: Hard constraints for ASP.NET Core development.
  - `FRONTEND_AGENT_RULES.md`: Hard constraints for Angular development.
  - `TEST_RULES.md`: Guidelines for verification and testing.
- **Strict Adherence**: Deviating from the patterns defined in these files is **forbidden** unless explicitly approved by the user.

## 2. Documentation Maintenance
**MANDATORY**: You must update `worklog.md` and `status.md` for every single task. Failure to do so is a violation of project rules.
You must keep the project documentation up-to-date **in real-time**.

### 2.1 Status Board (`status.md`)
- **Update Frequency**: Every time a task changes state (ToDo -> In Progress -> Done).
- **Structure**: Maintain the Kanban-style headers (`To Do`, `In Progress`, `Done`).

### 2.2 Worklog (`worklog.md`)
- **Purpose**: A living history of all changes, decisions, and progress.
- **Update Frequency**: After every logical chunk of work or completed task.
- **Format**:
  > Format is STRICT. Each entry starts with `## ENTRY` and ends before `---`.

  ```markdown
  ## ENTRY
  **Timestamp:** YYYY-MM-DD HH:MM  
  **Author:** <author>  

  **DONE**
  - List of completed items  

  **CHANGED FILES**
  - Files modified or created  

  **DECISIONS**
  - Key technical decisions  

  **NEXT**
  - Immediate next steps  

  **BLOCKERS**
  - Issues preventing progress (or "None")  

  ---
  ```

## 3. Workflow
1. **Plan**: Define your approach (and update `status.md` to "In Progress").
2. **Execute**: Implement the changes following the Agent Rules.
3. **Verify**: Run builds and tests to ensure correctness.
4. **Document**: Update `worklog.md` and move the task to "Done" in `status.md`.

## 4. Communication
- **Clarification First**: Always ask the user if a request or question is not clear enough. Do not make assumptions.

## 5. Tool Usage Restrictions
- **No PowerShell for File Content Extension**: You **MUST NOT** use PowerShell commands (like `Add-Content`, `Out-File -Append`, or `>>`) to append text to files or extend their content. Always use the provided agent tools (e.g., `replace_file_content`) to modify files.
