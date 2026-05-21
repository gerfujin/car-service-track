# Instructions

- Following Playwright test failed.
- Explain why, be concise, respect Playwright best practices.
- Provide a snippet of code with the fix, if possible.

# Test info

- Name: e2e\tests\token-refresh.spec.ts >> JWT Refresh-Token flow >> Re-using old refresh token after rotation returns 404 (rotation enforced)
- Location: e2e\tests\token-refresh.spec.ts:46:7

# Error details

```
Error: apiRequestContext.post: connect ECONNREFUSED ::1:80
Call log:
  - → POST http://localhost/api/v1/identity/account/register
    - user-agent: Playwright/1.60.0 (x64; windows 10.0) node/24.14
    - accept: */*
    - accept-encoding: gzip,deflate,br
    - content-type: application/json
    - content-length: 105

```