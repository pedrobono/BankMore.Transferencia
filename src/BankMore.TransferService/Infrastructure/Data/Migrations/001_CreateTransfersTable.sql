-- Migration: 001_CreateTransfersTable.sql
-- Description: Cria tabela transfers com constraint UNIQUE e índices

CREATE TABLE transfers (
    id TEXT PRIMARY KEY,
    request_id TEXT NOT NULL,
    origin_account_id TEXT NOT NULL,
    destination_account_id TEXT NOT NULL,
    value REAL NOT NULL,
    status TEXT NOT NULL,
    error_message TEXT,
    error_type TEXT,
    created_at TEXT NOT NULL,
    updated_at TEXT NOT NULL,
    CONSTRAINT uq_origin_request UNIQUE(origin_account_id, request_id)
);

CREATE INDEX idx_transfers_request_id ON transfers(request_id);
CREATE INDEX idx_transfers_origin_created ON transfers(origin_account_id, created_at);
