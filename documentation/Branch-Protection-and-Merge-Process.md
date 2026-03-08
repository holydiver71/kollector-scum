Branch Protection and Merge Process
=================================

Overview
--------
All feature branches should be merged into `dev` and tested on staging before any changes land in `master`/production.

Steps for repository maintainers
--------------------------------
1. Create or enable the following branch protection rule for `master` in GitHub repository Settings → Branches:
   - Require pull request reviews before merging (1+ approvals)
   - Require status checks to pass before merging
   - Require branches to be up to date before merging
   - Restrict who can push to matching branches (optional: administrators excluded during emergencies)

2. Add the status check to require: `Prevent PRs Targeting Master` (the workflow added to `.github/workflows/prevent-pr-to-master.yml`).

3. Make `dev` the default branch (Settings → Branches → Default branch) so PRs target `dev` by default.

4. Standard merge workflow:
   - Create a feature branch off `dev`.
   - Open a PR targeting `dev` and ensure CI and staging deployment pass.
   - After verification on staging, open a PR from `dev` into `master` (or merge `dev` into `master`) following release procedure.

Emergency releases
------------------
If an emergency production change is required, use an approved bypass process:
 - Create a PR targeting `master` and add the label `emergency-release`.
 - A designated repo admin must review and perform the merge manually (admins can be allowed to push if branch protection allows it).
 - Document each emergency release in the release notes with justification.

PR template and enforcement
---------------------------
We've added a PR template in `.github/pull_request_template.md` to remind contributors to target `dev` and verify staging. Use that when opening PRs.

Questions or changes
--------------------
If you'd like an allowlist bypass in the workflow (e.g., allow a label or specific user/team to bypass), I can add that to the Action and update branch-protection guidance.
