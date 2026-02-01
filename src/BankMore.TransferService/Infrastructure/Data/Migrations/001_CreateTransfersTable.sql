-- Migration: 001_CreateTransferenciaTable.sql
-- Description: Cria tabelas transferencia e idempotencia

CREATE TABLE transferencia (
    idtransferencia TEXT(37) PRIMARY KEY,
    idcontacorrente_origem TEXT(37) NOT NULL,
    idcontacorrente_destino TEXT(37) NOT NULL,
    datamovimento TEXT(25) NOT NULL,
    valor REAL NOT NULL
);

CREATE TABLE idempotencia (
    chave_idempotencia TEXT(37) PRIMARY KEY,
    requisicao TEXT(1000),
    resultado TEXT(1000)
);
