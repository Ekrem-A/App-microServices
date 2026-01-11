-- Drop existing tables if they exist
DROP TABLE IF EXISTS refunds CASCADE;
DROP TABLE IF EXISTS payment_attempts CASCADE;
DROP TABLE IF EXISTS outbox_messages CASCADE;
DROP TABLE IF EXISTS payments CASCADE;
DROP TABLE IF EXISTS "__EFMigrationsHistory" CASCADE;

-- Create payments table
CREATE TABLE payments (
    payment_id UUID PRIMARY KEY,
    order_id UUID NOT NULL UNIQUE,
    user_id UUID NOT NULL,
    amount DECIMAL(18, 2) NOT NULL,
    currency VARCHAR(10) NOT NULL,
    status INTEGER NOT NULL,
    provider_reference VARCHAR(500),
    failure_reason VARCHAR(1000),
    created_at TIMESTAMP NOT NULL,
    updated_at TIMESTAMP NOT NULL
);

CREATE INDEX ix_payments_order_id ON payments(order_id);

-- Create payment_attempts table
CREATE TABLE payment_attempts (
    attempt_id UUID PRIMARY KEY,
    payment_id UUID NOT NULL REFERENCES payments(payment_id) ON DELETE CASCADE,
    provider VARCHAR(50) NOT NULL,
    provider_reference VARCHAR(500),
    status INTEGER NOT NULL,
    request_payload TEXT,
    response_payload TEXT,
    error_message VARCHAR(1000),
    created_at TIMESTAMP NOT NULL,
    completed_at TIMESTAMP
);

CREATE INDEX ix_payment_attempts_provider_reference ON payment_attempts(provider_reference);
CREATE INDEX ix_payment_attempts_payment_id ON payment_attempts(payment_id);

-- Create outbox_messages table
CREATE TABLE outbox_messages (
    message_id UUID PRIMARY KEY,
    type VARCHAR(200) NOT NULL,
    payload TEXT NOT NULL,
    correlation_id VARCHAR(100),
    occurred_at TIMESTAMP NOT NULL,
    processed_at TIMESTAMP,
    retry_count INTEGER NOT NULL DEFAULT 0,
    last_error VARCHAR(2000)
);

CREATE INDEX ix_outbox_pending ON outbox_messages(processed_at, occurred_at);

-- Create refunds table
CREATE TABLE refunds (
    refund_id UUID PRIMARY KEY,
    payment_id UUID NOT NULL REFERENCES payments(payment_id) ON DELETE CASCADE,
    amount DECIMAL(18, 2) NOT NULL,
    currency VARCHAR(10) NOT NULL,
    status INTEGER NOT NULL,
    reason VARCHAR(500),
    provider_reference VARCHAR(500),
    failure_reason VARCHAR(1000),
    created_at TIMESTAMP NOT NULL,
    completed_at TIMESTAMP
);

CREATE INDEX ix_refunds_payment_id ON refunds(payment_id);

-- Create EF Migrations History table (optional, for future migrations)
CREATE TABLE "__EFMigrationsHistory" (
    "MigrationId" VARCHAR(150) PRIMARY KEY,
    "ProductVersion" VARCHAR(32) NOT NULL
);

-- Insert initial migration record
INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20250107_InitialCreate', '8.0.11');
