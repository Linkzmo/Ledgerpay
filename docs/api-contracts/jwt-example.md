# Local JWT Example

Use HS256 with:

- `iss`: `ledgerpay.local`
- `aud`: `ledgerpay.api`
- signing key: `ledgerpay-super-secret-signing-key-change-me`

Example payload for write/read scopes:

```json
{
  "sub": "candidate-user",
  "scope": "payments.write payments.read ledger.read",
  "iss": "ledgerpay.local",
  "aud": "ledgerpay.api",
  "exp": 1893456000
}
```

You can generate this token using any JWT tool (jwt.io, postman pre-request script, or a local script).
