# API Contracts

## Payments API

### `POST /api/payments`
Creates a payment intent.

Headers:
- `Authorization: Bearer <token>`
- `Idempotency-Key: <unique-key>`
- `X-Correlation-ID: <optional-id>`

Body:
```json
{
  "amount": 100.50,
  "currency": "BRL",
  "payerId": "customer-001",
  "merchantId": "merchant-abc"
}
```

Responses:
- `201 Created` new payment
- `200 OK` idempotent replay with same payload
- `409 Conflict` same key with different payload

### `GET /api/payments/{paymentId}`
Gets payment status.

### `POST /api/payments/{paymentId}/reverse`
Requests payment reversal.

## Ledger API

### `GET /api/ledger/payment/{paymentId}`
Returns debit/credit entries.

### `GET /api/reconciliation`
Compares net posted payments vs net ledger balance.

## Event Contracts

- `payment.created.v1`
- `payment.approved.v1`
- `payment.rejected.v1`
- `payment.reversed.v1`
- `ledger.posted.v1`

All events are wrapped in `EventEnvelope` with:
- `eventId`
- `eventType`
- `correlationId`
- `source`
- `occurredAtUtc`
- `payload`
- `headers`
