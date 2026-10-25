-- Banco de licenças do ARCA Teleprompter (Cloudflare D1 / SQLite).
-- Uma conta (site) tem licenças; uma licença = um código, preso a um computador depois de ativado.

-- Contas do site (Minha conta). Senha em PBKDF2-SHA256 com sal.
CREATE TABLE IF NOT EXISTS users (
  id            INTEGER PRIMARY KEY AUTOINCREMENT,
  email         TEXT NOT NULL UNIQUE COLLATE NOCASE,
  name          TEXT NOT NULL,
  company       TEXT NOT NULL,
  phone         TEXT NOT NULL,
  password      TEXT NOT NULL,                        -- pbkdf2$iterações$sal$hash
  created_at    TEXT NOT NULL,
  created_ip    TEXT
);

-- Sessões do site: guarda só o SHA-256 do token do cookie.
CREATE TABLE IF NOT EXISTS sessions (
  token         TEXT PRIMARY KEY,
  user_id       INTEGER NOT NULL,
  created_at    TEXT NOT NULL,
  expires_at    TEXT NOT NULL
);
CREATE INDEX IF NOT EXISTS sessions_user ON sessions(user_id);

CREATE TABLE IF NOT EXISTS licenses (
  id            INTEGER PRIMARY KEY AUTOINCREMENT,
  code          TEXT NOT NULL UNIQUE,                 -- ARCA-XXXX-XXXX-XXXX
  user_id       INTEGER,                              -- dono (conta do site)
  name          TEXT NOT NULL,                        -- nome, e-mail e empresa vão assinados na licença
  email         TEXT NOT NULL COLLATE NOCASE,
  company       TEXT NOT NULL,
  phone         TEXT NOT NULL,
  kind          TEXT NOT NULL DEFAULT 'vitalicia',    -- vitalicia (depois: anual, paga…)
  status        TEXT NOT NULL DEFAULT 'ativa',        -- ativa | bloqueada
  created_at    TEXT NOT NULL,
  created_ip    TEXT,
  machine       TEXT,                                 -- identificação do computador (SHA-256), NULL = livre
  machine_name  TEXT,
  activated_at  TEXT,
  last_seen     TEXT,
  app_version   TEXT,
  notes         TEXT
);
CREATE INDEX IF NOT EXISTS licenses_user ON licenses(user_id);
CREATE INDEX IF NOT EXISTS licenses_email ON licenses(email);

-- Histórico (cadastro, entrada, ativação, recusa, desativação, ações do administrador).
CREATE TABLE IF NOT EXISTS events (
  id       INTEGER PRIMARY KEY AUTOINCREMENT,
  code     TEXT,
  kind     TEXT NOT NULL,
  machine  TEXT,
  detail   TEXT,
  ip       TEXT,
  at       TEXT NOT NULL
);
CREATE INDEX IF NOT EXISTS events_code ON events(code);
CREATE INDEX IF NOT EXISTS events_ip_at ON events(ip, at);
