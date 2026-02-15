# Architecture

Ledgerpay is a transaction platform sample focused on consistency, auditability and resilience.

## Services

- `payments-api`: command/query API for payment intents and reversal. Implements idempotency, outbox and saga state transitions.
- `risk-worker`: consumes `payment.created.v1`, computes fake score and publishes approval/rejection.
- `ledger-api`: consumes approval/reversal events, writes double-entry ledger, publishes `ledger.posted.v1`, exposes reconciliation.
- `notifications-worker`: consumes rejection/posting events and sends fake notifications (logs/webhook stub).

## Data ownership

Single PostgreSQL instance with dedicated database + schema per service:

- `paymentsdb` / schema `payments`
- `riskdb` / schema `risk`
- `ledgerdb` / schema `ledger`
- `notificationsdb` / schema `notifications`

Each service has its own `outbox_messages` and/or `inbox_messages` table.

## Event choreography

1. `payments-api` persists payment + outbox (`payment.created.v1`).
2. outbox publisher sends event to RabbitMQ.
3. `risk-worker` consumes, stores assessment, emits `payment.approved.v1` or `payment.rejected.v1`.
4. `ledger-api` consumes approved/reversed and writes entries:
   - Debit `CustomerCashAccount`
   - Credit `MerchantSettlementAccount`
5. `ledger-api` emits `ledger.posted.v1`.
6. `payments-api` updates saga status from events.
7. `notifications-worker` emits confirmation/rejection notifications.

## Reliability patterns

- Outbox Pattern: events are persisted in the same transaction as business changes.
- Inbox Pattern: consumers dedupe by `(EventId, Consumer)` unique index.
- Idempotency: `Idempotency-Key` at API + DB record + Redis cache.
- Retry and DLQ: failed messages go to retry queue with TTL and then DLQ after retry limit.

## Security

- JWT bearer with scope checks.
- Scopes in APIs:
  - `payments.read`, `payments.write`
  - `ledger.read`

## Observability

- Structured logging with Serilog.
- OpenTelemetry traces and metrics exported to OTEL Collector.
- Jaeger for distributed tracing.
- Prometheus + Grafana for metrics dashboards.
