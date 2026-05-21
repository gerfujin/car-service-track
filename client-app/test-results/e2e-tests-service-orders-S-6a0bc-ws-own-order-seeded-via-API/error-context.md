# Instructions

- Following Playwright test failed.
- Explain why, be concise, respect Playwright best practices.
- Provide a snippet of code with the fix, if possible.

# Test info

- Name: e2e\tests\service-orders.spec.ts >> Service Orders >> Order list shows own order seeded via API
- Location: e2e\tests\service-orders.spec.ts:45:7

# Error details

```
Error: apiRequestContext.post: connect ECONNREFUSED ::1:80
Call log:
  - → POST http://localhost/api/v1/identity/account/register
    - user-agent: Playwright/1.60.0 (x64; windows 10.0) node/24.14
    - accept: */*
    - accept-encoding: gzip,deflate,br
    - content-type: application/json
    - content-length: 107

```