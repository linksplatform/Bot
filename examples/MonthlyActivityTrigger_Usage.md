# Monthly Commit and Review Activity Trigger

This document explains how to use the `MonthlyCommitAndReviewActivityTrigger` feature that was implemented to solve GitHub issue #92.

## Purpose

This trigger collects users who made commits and/or reviews in a specified month using GitHub Bot or GitHub API.

## How to Use

1. **Create an Issue** with a title containing one of these phrases:
   - "monthly commit and review activity" 
   - "collect users who made commits"

2. **Specify Month and Year** in the issue body using this format:
   ```
   month: 11
   year: 2023
   ```

3. **Optional: Ignore Repositories** by including Lino-formatted links in the issue body:
   ```
   ignore repository_name repository.
   ignore another_repo repository.
   ```

## Example Issue

**Title:** Monthly commit and review activity for November 2023

**Body:**
```
Please collect all users who made commits and/or reviews in the specified month.

month: 11
year: 2023

ignore test-repo repository.
ignore archived-repo repository.
```

## Response Format

The bot will respond with a formatted comment containing:

1. **Summary**: Total number of active users
2. **User List**: Each user with their activities:
   - Commits made in the specified month
   - Reviews submitted in the specified month
   - Up to 5 activities shown per user (with count if more exist)

## Example Response

```markdown
# Users with commit and/or review activity in November 2023

**Total active users: 5**

## @alice
Activities (7):
- Commit in project-a: Add new authentication module
- Commit in project-b: Fix bug in user validation
- Review in project-c: PR #123 - Update documentation
- Commit in project-a: Refactor login component
- Review in project-a: PR #124 - Add unit tests
- ... and 2 more activities

## @bob
Activities (3):
- Review in project-b: PR #125 - Security improvements
- Commit in project-d: Initial commit
- Review in project-d: PR #126 - Add CI/CD pipeline
```

## Default Behavior

- If month or year is not specified, defaults to the previous month
- Only processes repositories the bot has access to
- Respects ignored repositories list
- Closes the issue after posting the response

## Date Range

The trigger searches for:
- **Commits**: Created during the specified month
- **Reviews**: Submitted during the specified month

The search includes the entire month (1st to last day) in the organization's repositories.