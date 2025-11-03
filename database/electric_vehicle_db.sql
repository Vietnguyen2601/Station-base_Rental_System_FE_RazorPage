CREATE EXTENSION IF NOT EXISTS "pgcrypto";

-- ENUM TYPES
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_type WHERE typname = 'transaction_type_enum') THEN
        CREATE TYPE transaction_type_enum AS ENUM ('DEPOSIT','PAYMENT','REFUND');
    END IF;
    IF NOT EXISTS (SELECT 1 FROM pg_type WHERE typname = 'vehicle_status') THEN
        CREATE TYPE vehicle_status AS ENUM ('AVAILABLE','RENTED','MAINTENANCE','CHARGING');
    END IF;
    IF NOT EXISTS (SELECT 1 FROM pg_type WHERE typname = 'order_status') THEN
        CREATE TYPE order_status AS ENUM ('PENDING','CONFIRMED','ONGOING','COMPLETED','CANCELED');
    END IF;
    IF NOT EXISTS (SELECT 1 FROM pg_type WHERE typname = 'payment_type_enum') THEN
        CREATE TYPE payment_type_enum AS ENUM ('DEPOSIT','FINAL','REFUND');
    END IF;
    IF NOT EXISTS (SELECT 1 FROM pg_type WHERE typname = 'damage_level_enum') THEN
        CREATE TYPE damage_level_enum AS ENUM ('MINOR','MODERATE','SEVERE');
    END IF;
END$$;

-- DROP TABLES (order-safe)
DROP TABLE IF EXISTS "WalletTransactions" CASCADE;
DROP TABLE IF EXISTS "Wallets" CASCADE;
DROP TABLE IF EXISTS "Staff_Revenues" CASCADE;
DROP TABLE IF EXISTS "Reports" CASCADE;
DROP TABLE IF EXISTS "Feedbacks" CASCADE;
DROP TABLE IF EXISTS "DamageReports" CASCADE;
DROP TABLE IF EXISTS "Contracts" CASCADE;
DROP TABLE IF EXISTS "Payments" CASCADE;
DROP TABLE IF EXISTS "Orders" CASCADE;
DROP TABLE IF EXISTS "Promotions" CASCADE;
DROP TABLE IF EXISTS "Vehicles" CASCADE;
DROP TABLE IF EXISTS "VehicleModels" CASCADE;
DROP TABLE IF EXISTS "VehicleTypes" CASCADE;
DROP TABLE IF EXISTS "Stations" CASCADE;
DROP TABLE IF EXISTS "Accounts" CASCADE;
DROP TABLE IF EXISTS "Roles" CASCADE;

-- TABLES (create in dependency order)

CREATE TABLE "Roles" (
  role_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  role_name VARCHAR NOT NULL,
  isActive BOOLEAN NOT NULL DEFAULT TRUE,
  created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
  updated_at TIMESTAMP
);

CREATE TABLE "Accounts" (
  account_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  username VARCHAR NOT NULL,
  password VARCHAR NOT NULL,
  email VARCHAR NOT NULL,
  contact_number VARCHAR,
  role_id UUID NOT NULL REFERENCES "Roles"(role_id),
  isActive BOOLEAN NOT NULL DEFAULT TRUE,
  created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
  updated_at TIMESTAMP
);

CREATE TABLE "Wallets" (
  wallet_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  account_id UUID NOT NULL REFERENCES "Accounts"(account_id),
  balance DECIMAL NOT NULL DEFAULT 0,
  created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
  updated_at TIMESTAMP,
  isActive BOOLEAN NOT NULL DEFAULT TRUE
);

CREATE TABLE "VehicleTypes" (
  vehicle_type_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  type_name VARCHAR NOT NULL UNIQUE,
  description TEXT,
  created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
  isActive BOOLEAN NOT NULL DEFAULT TRUE,
  updated_at TIMESTAMP
);

CREATE TABLE "VehicleModels" (
  vehicle_model_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  type_id UUID NOT NULL REFERENCES "VehicleTypes"(vehicle_type_id),
  name VARCHAR NOT NULL,
  manufacturer VARCHAR NOT NULL,
  price_per_hour DECIMAL NOT NULL,
  specs VARCHAR,
  created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
  isActive BOOLEAN NOT NULL DEFAULT TRUE,
  updated_at TIMESTAMP
);

CREATE TABLE "Stations" (
  station_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  name VARCHAR NOT NULL,
  address VARCHAR NOT NULL,
  lat DECIMAL NOT NULL,
  long DECIMAL NOT NULL,
  capacity INT NOT NULL,
  image_url VARCHAR,
  isActive BOOLEAN NOT NULL DEFAULT TRUE,
  created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
  updated_at TIMESTAMP
);

CREATE TABLE "Vehicles" (
  vehicle_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  serial_number VARCHAR NOT NULL UNIQUE,
  model_id UUID NOT NULL REFERENCES "VehicleModels"(vehicle_model_id),
  station_id UUID REFERENCES "Stations"(station_id),
  status vehicle_status NOT NULL DEFAULT 'AVAILABLE',
  battery_level INT,
  battery_capacity INT,
  range INT,
  color VARCHAR,
  last_maintenance DATE,
  img VARCHAR,
  created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
  isActive BOOLEAN NOT NULL DEFAULT TRUE,
  updated_at TIMESTAMP
);

CREATE TABLE "Promotions" (
  promotion_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  promo_code VARCHAR NOT NULL,
  discount_percentage DECIMAL NOT NULL,
  start_date TIMESTAMP NOT NULL,
  end_date TIMESTAMP NOT NULL,
  created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
  updated_at TIMESTAMP,
  isActive BOOLEAN NOT NULL DEFAULT TRUE
);

-- Orders must exist before WalletTransactions (fixed)
CREATE TABLE "Orders" (
  order_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  customer_id UUID NOT NULL REFERENCES "Accounts"(account_id),
  vehicle_id UUID NOT NULL REFERENCES "Vehicles"(vehicle_id),
  order_code VARCHAR NOT NULL UNIQUE,
  order_date TIMESTAMP NOT NULL,
  start_time TIMESTAMP NOT NULL,
  end_time TIMESTAMP,
  return_time TIMESTAMP,
  base_price DECIMAL NOT NULL,
  total_price DECIMAL NOT NULL,
  status order_status NOT NULL DEFAULT 'PENDING',
  promotion_id UUID REFERENCES "Promotions"(promotion_id),
  staff_id UUID REFERENCES "Accounts"(account_id),
  created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
  updated_at TIMESTAMP,
  isActive BOOLEAN NOT NULL DEFAULT TRUE
);

CREATE TABLE "Payments" (
  payment_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  order_id UUID NOT NULL REFERENCES "Orders"(order_id),
  gateway_tx_id VARCHAR,
  amount DECIMAL NOT NULL,
  payment_date TIMESTAMP NOT NULL,
  payment_method VARCHAR NOT NULL,
  payment_type payment_type_enum NOT NULL,
  status VARCHAR NOT NULL,
  created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
  updated_at TIMESTAMP,
  idempotency_key VARCHAR(255),
  gateway_response JSONB,
  isActive BOOLEAN NOT NULL DEFAULT TRUE
);

CREATE TABLE "DamageReports" (
  damage_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  order_id UUID NOT NULL REFERENCES "Orders"(order_id),
  vehicle_id UUID NOT NULL REFERENCES "Vehicles"(vehicle_id),
  damage_level damage_level_enum NOT NULL,
  description TEXT NOT NULL,
  estimated_cost DECIMAL NOT NULL,
  img VARCHAR,
  created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
  updated_at TIMESTAMP,
  isActive BOOLEAN NOT NULL DEFAULT TRUE
);

CREATE TABLE "Contracts" (
  contract_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  order_id UUID NOT NULL REFERENCES "Orders"(order_id),
  customer_id UUID NOT NULL REFERENCES "Accounts"(account_id),
  vehicle_id UUID NOT NULL REFERENCES "Vehicles"(vehicle_id),
  contract_date TIMESTAMP NOT NULL,
  file_url VARCHAR NOT NULL,
  created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
  updated_at TIMESTAMP,
  isActive BOOLEAN NOT NULL DEFAULT TRUE
);

CREATE TABLE "Feedbacks" (
  feedback_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  customer_id UUID NOT NULL REFERENCES "Accounts"(account_id),
  order_id UUID NOT NULL REFERENCES "Orders"(order_id),
  rating INT NOT NULL,
  comment TEXT,
  feedback_date TIMESTAMP NOT NULL,
  created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
  updated_at TIMESTAMP,
  isActive BOOLEAN NOT NULL DEFAULT TRUE
);

CREATE TABLE "Reports" (
  report_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  report_type VARCHAR NOT NULL,
  generated_date TIMESTAMP NOT NULL,
  text VARCHAR NOT NULL,
  account_id UUID NOT NULL REFERENCES "Accounts"(account_id),
  vehicle_id UUID NOT NULL REFERENCES "Vehicles"(vehicle_id),
  img VARCHAR,
  created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
  updated_at TIMESTAMP,
  isActive BOOLEAN NOT NULL DEFAULT TRUE
);

CREATE TABLE "Staff_Revenues" (
  staff_revenue_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  staff_id UUID NOT NULL REFERENCES "Accounts"(account_id),
  revenue_date TIMESTAMP NOT NULL,
  total_revenue DECIMAL NOT NULL,
  commission DECIMAL,
  created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
  updated_at TIMESTAMP,
  isActive BOOLEAN NOT NULL DEFAULT TRUE
);

-- Now create WalletTransactions AFTER Orders exists
CREATE TABLE "WalletTransactions" (
  transaction_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  wallet_id UUID NOT NULL REFERENCES "Wallets"(wallet_id),
  order_id UUID REFERENCES "Orders"(order_id),
  amount DECIMAL NOT NULL,
  transaction_type transaction_type_enum NOT NULL,
  description VARCHAR,
  created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
  isActive BOOLEAN NOT NULL DEFAULT TRUE
);

-- TRIGGERS: created_at / updated_at
CREATE OR REPLACE FUNCTION set_created_at_column()
RETURNS TRIGGER AS $$
BEGIN
    NEW.created_at = COALESCE(NEW.created_at, CURRENT_TIMESTAMP);
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE OR REPLACE FUNCTION update_updated_at_column()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_at = CURRENT_TIMESTAMP;
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

-- Attach created_at triggers
CREATE TRIGGER set_roles_created_at BEFORE INSERT ON "Roles" FOR EACH ROW EXECUTE FUNCTION set_created_at_column();
CREATE TRIGGER set_accounts_created_at BEFORE INSERT ON "Accounts" FOR EACH ROW EXECUTE FUNCTION set_created_at_column();
CREATE TRIGGER set_wallets_created_at BEFORE INSERT ON "Wallets" FOR EACH ROW EXECUTE FUNCTION set_created_at_column();
CREATE TRIGGER set_vehicle_types_created_at BEFORE INSERT ON "VehicleTypes" FOR EACH ROW EXECUTE FUNCTION set_created_at_column();
CREATE TRIGGER set_vehicle_models_created_at BEFORE INSERT ON "VehicleModels" FOR EACH ROW EXECUTE FUNCTION set_created_at_column();
CREATE TRIGGER set_stations_created_at BEFORE INSERT ON "Stations" FOR EACH ROW EXECUTE FUNCTION set_created_at_column();
CREATE TRIGGER set_vehicles_created_at BEFORE INSERT ON "Vehicles" FOR EACH ROW EXECUTE FUNCTION set_created_at_column();
CREATE TRIGGER set_promotions_created_at BEFORE INSERT ON "Promotions" FOR EACH ROW EXECUTE FUNCTION set_created_at_column();
CREATE TRIGGER set_orders_created_at BEFORE INSERT ON "Orders" FOR EACH ROW EXECUTE FUNCTION set_created_at_column();
CREATE TRIGGER set_payments_created_at BEFORE INSERT ON "Payments" FOR EACH ROW EXECUTE FUNCTION set_created_at_column();
CREATE TRIGGER set_wallettx_created_at BEFORE INSERT ON "WalletTransactions" FOR EACH ROW EXECUTE FUNCTION set_created_at_column();
CREATE TRIGGER set_feedbacks_created_at BEFORE INSERT ON "Feedbacks" FOR EACH ROW EXECUTE FUNCTION set_created_at_column();
CREATE TRIGGER set_reports_created_at BEFORE INSERT ON "Reports" FOR EACH ROW EXECUTE FUNCTION set_created_at_column();
CREATE TRIGGER set_staff_revenues_created_at BEFORE INSERT ON "Staff_Revenues" FOR EACH ROW EXECUTE FUNCTION set_created_at_column();
CREATE TRIGGER set_contracts_created_at BEFORE INSERT ON "Contracts" FOR EACH ROW EXECUTE FUNCTION set_created_at_column();

-- Attach updated_at triggers
CREATE TRIGGER update_roles_updated_at BEFORE UPDATE ON "Roles" FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();
CREATE TRIGGER update_accounts_updated_at BEFORE UPDATE ON "Accounts" FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();
CREATE TRIGGER update_wallets_updated_at BEFORE UPDATE ON "Wallets" FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();
CREATE TRIGGER update_wallettx_updated_at BEFORE UPDATE ON "WalletTransactions" FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();
CREATE TRIGGER update_vehicle_types_updated_at BEFORE UPDATE ON "VehicleTypes" FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();
CREATE TRIGGER update_vehicle_models_updated_at BEFORE UPDATE ON "VehicleModels" FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();
CREATE TRIGGER update_vehicles_updated_at BEFORE UPDATE ON "Vehicles" FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();
CREATE TRIGGER update_stations_updated_at BEFORE UPDATE ON "Stations" FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();
CREATE TRIGGER update_promotions_updated_at BEFORE UPDATE ON "Promotions" FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();
CREATE TRIGGER update_orders_updated_at BEFORE UPDATE ON "Orders" FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();
CREATE TRIGGER update_payments_updated_at BEFORE UPDATE ON "Payments" FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();
CREATE TRIGGER update_feedbacks_updated_at BEFORE UPDATE ON "Feedbacks" FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();
CREATE TRIGGER update_reports_updated_at BEFORE UPDATE ON "Reports" FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();
CREATE TRIGGER update_staff_revenues_updated_at BEFORE UPDATE ON "Staff_Revenues" FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();
CREATE TRIGGER update_contracts_updated_at BEFORE UPDATE ON "Contracts" FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

-- INDEXES: only for Payments
CREATE UNIQUE INDEX IF NOT EXISTS ux_payments_gateway_tx_id ON "Payments"(gateway_tx_id) WHERE gateway_tx_id IS NOT NULL;
CREATE UNIQUE INDEX IF NOT EXISTS ux_payments_idempotency ON "Payments"(idempotency_key) WHERE idempotency_key IS NOT NULL;
CREATE INDEX IF NOT EXISTS ix_payments_orderid_type_status ON "Payments"(order_id, payment_type, status);

-- CORE FUNCTIONS (existing flows)
CREATE OR REPLACE FUNCTION create_order_with_deposit(
    p_customer_id UUID,
    p_vehicle_id UUID,
    p_order_date TIMESTAMP,
    p_start_time TIMESTAMP,
    p_end_time TIMESTAMP,
    p_base_price NUMERIC,
    p_total_price NUMERIC,
    p_deposit_amount NUMERIC,
    p_payment_method VARCHAR,
    p_promotion_id UUID DEFAULT NULL,
    p_staff_id UUID DEFAULT NULL
) RETURNS UUID AS $$
DECLARE
    v_order_id UUID;
    v_vehicle_status vehicle_status;
BEGIN
    SELECT status INTO v_vehicle_status FROM "Vehicles" WHERE vehicle_id = p_vehicle_id FOR UPDATE;
    IF NOT FOUND THEN
        RAISE EXCEPTION 'Vehicle % does not exist', p_vehicle_id;
    END IF;
    IF v_vehicle_status <> 'AVAILABLE' THEN
        RAISE EXCEPTION 'Vehicle % not available (status=%)', p_vehicle_id, v_vehicle_status;
    END IF;

    INSERT INTO "Orders"(order_id, customer_id, vehicle_id, order_date, start_time, end_time, base_price, total_price, status, promotion_id, staff_id, created_at, isActive)
    VALUES (gen_random_uuid(), p_customer_id, p_vehicle_id, p_order_date, p_start_time, p_end_time, p_base_price, p_total_price, 'CONFIRMED', p_promotion_id, p_staff_id, CURRENT_TIMESTAMP, TRUE)
    RETURNING order_id INTO v_order_id;

    INSERT INTO "Payments"(payment_id, order_id, amount, payment_date, payment_method, payment_type, status, created_at, isActive)
    VALUES (gen_random_uuid(), v_order_id, p_deposit_amount, CURRENT_TIMESTAMP, p_payment_method, 'DEPOSIT', 'PENDING', CURRENT_TIMESTAMP, TRUE);

    UPDATE "Vehicles" SET status = 'RENTED', updated_at = CURRENT_TIMESTAMP WHERE vehicle_id = p_vehicle_id;

    RETURN v_order_id;
END;
$$ LANGUAGE plpgsql;

CREATE OR REPLACE FUNCTION cancel_order_refund(
    p_order_id UUID,
    p_refund_method VARCHAR DEFAULT 'ORIGINAL'
) RETURNS BOOLEAN AS $$
DECLARE
    v_status order_status;
    v_vehicle_id UUID;
    v_deposit_sum NUMERIC := 0;
BEGIN
    SELECT status, vehicle_id INTO v_status, v_vehicle_id FROM "Orders" WHERE order_id = p_order_id FOR UPDATE;
    IF NOT FOUND THEN
        RAISE EXCEPTION 'Order % does not exist', p_order_id;
    END IF;
    IF v_status NOT IN ('PENDING','CONFIRMED') THEN
        RAISE EXCEPTION 'Cannot cancel order % with status %', p_order_id, v_status;
    END IF;
    SELECT COALESCE(SUM(amount),0) INTO v_deposit_sum FROM "Payments" WHERE order_id = p_order_id AND payment_type = 'DEPOSIT' AND status = 'COMPLETED' AND isActive = TRUE;
    IF v_deposit_sum > 0 THEN
        INSERT INTO "Payments"(payment_id, order_id, amount, payment_date, payment_method, payment_type, status, created_at, isActive)
        VALUES (gen_random_uuid(), p_order_id, v_deposit_sum, CURRENT_TIMESTAMP, p_refund_method, 'REFUND', 'PENDING', CURRENT_TIMESTAMP, TRUE);
    END IF;
    UPDATE "Orders" SET status = 'CANCELED', updated_at = CURRENT_TIMESTAMP WHERE order_id = p_order_id;
    IF v_vehicle_id IS NOT NULL THEN
        UPDATE "Vehicles" SET status = 'AVAILABLE', updated_at = CURRENT_TIMESTAMP WHERE vehicle_id = v_vehicle_id AND status = 'RENTED';
    END IF;
    RETURN TRUE;
END;
$$ LANGUAGE plpgsql;

CREATE OR REPLACE FUNCTION complete_order_finalize_payment(
    p_order_id UUID,
    p_final_payment_method VARCHAR DEFAULT 'CASH'
) RETURNS NUMERIC AS $$
DECLARE
    v_total_price NUMERIC;
    v_deposit_sum NUMERIC := 0;
    v_due NUMERIC;
    v_vehicle_id UUID;
BEGIN
    SELECT total_price, vehicle_id INTO v_total_price, v_vehicle_id FROM "Orders" WHERE order_id = p_order_id FOR UPDATE;
    IF NOT FOUND THEN
        RAISE EXCEPTION 'Order % does not exist', p_order_id;
    END IF;
    SELECT COALESCE(SUM(amount),0) INTO v_deposit_sum FROM "Payments" WHERE order_id = p_order_id AND payment_type = 'DEPOSIT' AND status = 'COMPLETED' AND isActive = TRUE;
    v_due := v_total_price - v_deposit_sum;
    IF v_due > 0 THEN
        INSERT INTO "Payments"(payment_id, order_id, amount, payment_date, payment_method, payment_type, status, created_at, isActive)
        VALUES (gen_random_uuid(), p_order_id, v_due, CURRENT_TIMESTAMP, p_final_payment_method, 'FINAL', 'PENDING', CURRENT_TIMESTAMP, TRUE);
    ELSIF v_due < 0 THEN
        INSERT INTO "Payments"(payment_id, order_id, amount, payment_date, payment_method, payment_type, status, created_at, isActive)
        VALUES (gen_random_uuid(), p_order_id, ABS(v_due), CURRENT_TIMESTAMP, p_final_payment_method, 'REFUND', 'PENDING', CURRENT_TIMESTAMP, TRUE);
    END IF;
    UPDATE "Orders" SET status = 'COMPLETED', updated_at = CURRENT_TIMESTAMP WHERE order_id = p_order_id;
    IF v_vehicle_id IS NOT NULL THEN
        UPDATE "Vehicles" SET status = 'AVAILABLE', updated_at = CURRENT_TIMESTAMP WHERE vehicle_id = v_vehicle_id AND status = 'RENTED';
    END IF;
    RETURN v_due;
END;
$$ LANGUAGE plpgsql;

CREATE OR REPLACE FUNCTION mark_payment_completed(
    p_payment_id UUID,
    p_gateway_tx_id VARCHAR,
    p_gateway_response JSONB DEFAULT NULL,
    p_status VARCHAR DEFAULT 'COMPLETED',
    p_idempotency_key VARCHAR DEFAULT NULL
) RETURNS BOOLEAN AS $$
DECLARE
    v_rows INT;
BEGIN
    IF p_idempotency_key IS NOT NULL THEN
        IF EXISTS (SELECT 1 FROM "Payments" WHERE idempotency_key = p_idempotency_key AND payment_id <> p_payment_id) THEN
            RAISE EXCEPTION 'Idempotency key % already used by another payment', p_idempotency_key;
        END IF;
    END IF;
    UPDATE "Payments"
    SET status = p_status,
        gateway_tx_id = p_gateway_tx_id,
        gateway_response = COALESCE(p_gateway_response, gateway_response),
        idempotency_key = COALESCE(p_idempotency_key, idempotency_key),
        updated_at = CURRENT_TIMESTAMP
    WHERE payment_id = p_payment_id AND isActive = TRUE;
    GET DIAGNOSTICS v_rows = ROW_COUNT;
    IF v_rows = 0 THEN
        RAISE EXCEPTION 'No payment updated for id %', p_payment_id;
    END IF;
    RETURN TRUE;
END;
$$ LANGUAGE plpgsql;

-- =========================
-- NEW: Transactional helper functions for wallet flows
-- =========================

-- 1) create_order_with_deposit_using_wallet
-- Creates order + contract, optionally debits wallet atomically and creates Payments/WalletTransactions.
CREATE OR REPLACE FUNCTION create_order_with_deposit_using_wallet(
  p_customer_id UUID,
  p_vehicle_id UUID,
  p_order_date TIMESTAMP,
  p_start_time TIMESTAMP,
  p_end_time TIMESTAMP,
  p_base_price NUMERIC,
  p_total_price NUMERIC,
  p_deposit_amount NUMERIC,
  p_payment_method VARCHAR,    -- 'WALLET' or external gateway name
  p_promotion_id UUID DEFAULT NULL,
  p_staff_id UUID DEFAULT NULL
) RETURNS UUID AS $$
DECLARE
  v_order_id UUID;
  v_vehicle_status vehicle_status;
  v_wallet_id UUID;
  v_balance NUMERIC;
  v_order_code TEXT;
BEGIN
  -- Lock vehicle
  SELECT status INTO v_vehicle_status FROM "Vehicles" WHERE vehicle_id = p_vehicle_id FOR UPDATE;
  IF NOT FOUND THEN
    RAISE EXCEPTION 'Vehicle % does not exist', p_vehicle_id;
  END IF;
  IF v_vehicle_status <> 'AVAILABLE' THEN
    RAISE EXCEPTION 'Vehicle % not available (status=%)', p_vehicle_id, v_vehicle_status;
  END IF;

  -- Generate unique 6-char order_code
  LOOP
    v_order_code := upper(substr(md5(random()::text || clock_timestamp()::text),1,6));
    EXIT WHEN NOT EXISTS (SELECT 1 FROM "Orders" WHERE order_code = v_order_code);
  END LOOP;

  -- Insert order
  INSERT INTO "Orders"(order_id, customer_id, vehicle_id, order_code, order_date, start_time, end_time, base_price, total_price, status, promotion_id, staff_id, created_at, isActive)
  VALUES (gen_random_uuid(), p_customer_id, p_vehicle_id, v_order_code, p_order_date, p_start_time, p_end_time, p_base_price, p_total_price, 'CONFIRMED', p_promotion_id, p_staff_id, now(), TRUE)
  RETURNING order_id INTO v_order_id;

  -- Create contract record (file_url left blank for staff to confirm later)
  INSERT INTO "Contracts"(contract_id, order_id, customer_id, vehicle_id, contract_date, file_url, created_at, isActive)
  VALUES (gen_random_uuid(), v_order_id, p_customer_id, p_vehicle_id, now(), '', now(), TRUE);

  -- Handle deposit via wallet
  IF upper(coalesce(p_payment_method,'') ) = 'WALLET' AND p_deposit_amount > 0 THEN
    SELECT wallet_id, balance INTO v_wallet_id, v_balance FROM "Wallets" WHERE account_id = p_customer_id FOR UPDATE;
    IF NOT FOUND THEN
      RAISE EXCEPTION 'Wallet for account % not found', p_customer_id;
    END IF;
    IF v_balance < p_deposit_amount THEN
      RAISE EXCEPTION 'Insufficient wallet balance for account %', p_customer_id;
    END IF;

    -- Debit wallet
    UPDATE "Wallets" SET balance = balance - p_deposit_amount, updated_at = now() WHERE wallet_id = v_wallet_id;

    -- Log wallet transaction
    INSERT INTO "WalletTransactions"(transaction_id, wallet_id, order_id, amount, transaction_type, description, created_at, isActive)
    VALUES (gen_random_uuid(), v_wallet_id, v_order_id, p_deposit_amount, 'DEPOSIT', 'Deposit for order ' || v_order_id, now(), TRUE);

    -- Create Payments record as COMPLETED for wallet
    INSERT INTO "Payments"(payment_id, order_id, gateway_tx_id, amount, payment_date, payment_method, payment_type, status, created_at, isActive)
    VALUES (gen_random_uuid(), v_order_id, 'WALLET-' || gen_random_uuid()::text, p_deposit_amount, now(), 'WALLET', 'DEPOSIT', 'COMPLETED', now(), TRUE);
  ELSE
    -- Create Payments as PENDING for external gateway
    INSERT INTO "Payments"(payment_id, order_id, gateway_tx_id, amount, payment_date, payment_method, payment_type, status, created_at, isActive)
    VALUES (gen_random_uuid(), v_order_id, NULL, p_deposit_amount, now(), p_payment_method, 'DEPOSIT', 'PENDING', now(), TRUE);
  END IF;

  -- Mark vehicle as RENTED
  UPDATE "Vehicles" SET status = 'RENTED', updated_at = now() WHERE vehicle_id = p_vehicle_id;

  RETURN v_order_id;
END;
$$ LANGUAGE plpgsql;


-- 2) finalize_return_payment_using_wallet
-- Called when returning vehicle: calculates due, handles wallet debit/refund or creates pending payments for gateway.
CREATE OR REPLACE FUNCTION finalize_return_payment_using_wallet(
  p_order_id UUID,
  p_final_payment_method VARCHAR DEFAULT 'WALLET'   -- 'WALLET' or gateway name
) RETURNS NUMERIC AS $$
DECLARE
  v_total_price NUMERIC;
  v_deposit_sum NUMERIC := 0;
  v_due NUMERIC;
  v_customer_id UUID;
  v_wallet_id UUID;
  v_balance NUMERIC;
  v_vehicle_id UUID;
BEGIN
  -- Lock order
  SELECT total_price, customer_id, vehicle_id INTO v_total_price, v_customer_id, v_vehicle_id FROM "Orders" WHERE order_id = p_order_id FOR UPDATE;
  IF NOT FOUND THEN
    RAISE EXCEPTION 'Order % not found', p_order_id;
  END IF;

  -- Sum completed deposits
  SELECT COALESCE(SUM(amount),0) INTO v_deposit_sum FROM "Payments" WHERE order_id = p_order_id AND payment_type = 'DEPOSIT' AND status = 'COMPLETED' AND isActive = TRUE;

  v_due := v_total_price - v_deposit_sum;

  IF v_due > 0 THEN
    -- Charge customer
    IF upper(coalesce(p_final_payment_method,'')) = 'WALLET' THEN
      SELECT wallet_id, balance INTO v_wallet_id, v_balance FROM "Wallets" WHERE account_id = v_customer_id FOR UPDATE;
      IF NOT FOUND THEN
        RAISE EXCEPTION 'Wallet not found for account %', v_customer_id;
      END IF;
      IF v_balance < v_due THEN
        RAISE EXCEPTION 'Insufficient wallet balance to settle final amount % for order %', v_due, p_order_id;
      END IF;

      -- Debit wallet
      UPDATE "Wallets" SET balance = balance - v_due, updated_at = now() WHERE wallet_id = v_wallet_id;

      -- Log wallet transaction
      INSERT INTO "WalletTransactions"(transaction_id, wallet_id, order_id, amount, transaction_type, description, created_at, isActive)
      VALUES (gen_random_uuid(), v_wallet_id, p_order_id, v_due, 'PAYMENT', 'Final payment for order '||p_order_id, now(), TRUE);

      -- Create Payments record as COMPLETED
      INSERT INTO "Payments"(payment_id, order_id, gateway_tx_id, amount, payment_date, payment_method, payment_type, status, created_at, isActive)
      VALUES (gen_random_uuid(), p_order_id, 'WALLET-'||gen_random_uuid()::text, v_due, now(), 'WALLET', 'FINAL', 'COMPLETED', now(), TRUE);
    ELSE
      -- create FINAL payment PENDING for external gateway
      INSERT INTO "Payments"(payment_id, order_id, gateway_tx_id, amount, payment_date, payment_method, payment_type, status, created_at, isActive)
      VALUES (gen_random_uuid(), p_order_id, NULL, v_due, now(), p_final_payment_method, 'FINAL', 'PENDING', now(), TRUE);
    END IF;

  ELSIF v_due < 0 THEN
    -- Need to refund the customer (create REFUND record PENDING)
    INSERT INTO "Payments"(payment_id, order_id, gateway_tx_id, amount, payment_date, payment_method, payment_type, status, created_at, isActive)
    VALUES (gen_random_uuid(), p_order_id, NULL, ABS(v_due), now(), p_final_payment_method, 'REFUND', 'PENDING', now(), TRUE);
  END IF;

  -- Update order status and vehicle availability
  UPDATE "Orders" SET status = 'COMPLETED', updated_at = now() WHERE order_id = p_order_id;
  IF v_vehicle_id IS NOT NULL THEN
    UPDATE "Vehicles" SET status = 'AVAILABLE', updated_at = now() WHERE vehicle_id = v_vehicle_id AND status = 'RENTED';
  END IF;

  RETURN v_due;
END;
$$ LANGUAGE plpgsql;

--===========Insert Data===========--

-- =========================================
-- ROLES
-- =========================================
INSERT INTO "Roles" (role_id, role_name, isActive) VALUES
(gen_random_uuid(), 'Customer', TRUE),
(gen_random_uuid(), 'Staff', TRUE),
(gen_random_uuid(), 'Admin', TRUE);

-- =========================================
-- ACCOUNTS
-- =========================================
INSERT INTO "Accounts" (account_id, username, password, email, contact_number, role_id, isActive) VALUES
(gen_random_uuid(), 'john_doe', 'hashed_password1', 'john.doe@example.com', '1234567890', (SELECT role_id FROM "Roles" WHERE role_name = 'Customer' LIMIT 1), TRUE),
(gen_random_uuid(), 'jane_smith', 'hashed_password2', 'jane.smith@example.com', '1234567891', (SELECT role_id FROM "Roles" WHERE role_name = 'Customer' LIMIT 1), TRUE),
(gen_random_uuid(), 'alice_wong', 'hashed_password3', 'alice.wong@example.com', '1234567892', (SELECT role_id FROM "Roles" WHERE role_name = 'Customer' LIMIT 1), TRUE),
(gen_random_uuid(), 'bob_jones', 'hashed_password4', 'bob.jones@example.com', '1234567893', (SELECT role_id FROM "Roles" WHERE role_name = 'Staff' LIMIT 1), TRUE),
(gen_random_uuid(), 'carol_white', 'hashed_password5', 'carol.white@example.com', '1234567894', (SELECT role_id FROM "Roles" WHERE role_name = 'Staff' LIMIT 1), TRUE),
(gen_random_uuid(), 'david_brown', 'hashed_password6', 'david.brown@example.com', '1234567895', (SELECT role_id FROM "Roles" WHERE role_name = 'Staff' LIMIT 1), TRUE),
(gen_random_uuid(), 'emma_clark', 'hashed_password7', 'emma.clark@example.com', '1234567896', (SELECT role_id FROM "Roles" WHERE role_name = 'Admin' LIMIT 1), TRUE),
(gen_random_uuid(), 'frank_lee', 'hashed_password8', 'frank.lee@example.com', '1234567897', (SELECT role_id FROM "Roles" WHERE role_name = 'Admin' LIMIT 1), TRUE),
(gen_random_uuid(), 'grace_kim', 'hashed_password9', 'grace.kim@example.com', '1234567898', (SELECT role_id FROM "Roles" WHERE role_name = 'Admin' LIMIT 1), TRUE),
(gen_random_uuid(), 'henry_park', 'hashed_password10', 'henry.park@example.com', '1234567899', (SELECT role_id FROM "Roles" WHERE role_name = 'Admin' LIMIT 1), TRUE);

-- =========================================
-- REPLACED: VEHICLE TYPES (use your provided block)
-- =========================================
INSERT INTO "VehicleTypes" (vehicle_type_id, type_name, description, created_at, isActive)
VALUES
  (gen_random_uuid(), 'SUV', 'Dòng xe gầm cao, thể thao, phù hợp nhiều địa hình', NOW(), true),
  (gen_random_uuid(), 'Sedan', 'Dòng xe 4 chỗ tiện nghi, tiết kiệm nhiên liệu', NOW(), true),
  (gen_random_uuid(), 'Van', 'Dòng xe đa dụng, chở được nhiều người hoặc hàng hóa', NOW(), true);

-- =========================================
-- REPLACED: VEHICLE MODELS (9 models as provided)
-- =========================================

-- SUV Models
INSERT INTO "VehicleModels" (vehicle_model_id, type_id, name, manufacturer, price_per_hour, specs, created_at, isActive)
SELECT gen_random_uuid(), vt.vehicle_type_id, v.name, v.manufacturer, v.price, v.specs, NOW(), true
FROM "VehicleTypes" vt,
     (VALUES
        ('Tesla Model X', 'Tesla', 50000, 'Dual Motor, AWD, 100kWh battery, 565km range'),
        ('Toyota RAV4', 'Toyota', 35000, '2.5L Hybrid, 219 hp, Safety Sense, 500km range'),
        ('Hyundai Tucson', 'Hyundai', 30000, '1.6L Turbo, SmartSense, 480km range')
     ) AS v(name, manufacturer, price, specs)
WHERE vt.type_name = 'SUV';

-- Sedan Models
INSERT INTO "VehicleModels" (vehicle_model_id, type_id, name, manufacturer, price_per_hour, specs, created_at, isActive)
SELECT gen_random_uuid(), vt.vehicle_type_id, v.name, v.manufacturer, v.price, v.specs, NOW(), true
FROM "VehicleTypes" vt,
     (VALUES
        ('Toyota Camry', 'Toyota', 25000, '2.5L, Hybrid option, 5 seats, 550km range'),
        ('Honda Accord', 'Honda', 27000, '1.5L Turbo, Eco Mode, 5 seats, 560km range'),
        ('Tesla Model 3', 'Tesla', 45000, 'RWD, 60kWh, Autopilot, 530km range')
     ) AS v(name, manufacturer, price, specs)
WHERE vt.type_name = 'Sedan';

-- Van Models
INSERT INTO "VehicleModels" (vehicle_model_id, type_id, name, manufacturer, price_per_hour, specs, created_at, isActive)
SELECT gen_random_uuid(), vt.vehicle_type_id, v.name, v.manufacturer, v.price, v.specs, NOW(), true
FROM "VehicleTypes" vt,
     (VALUES
        ('Ford Transit', 'Ford', 40000, '3.5L EcoBoost, 10 seats, 650km range'),
        ('Mercedes-Benz V-Class', 'Mercedes-Benz', 55000, '2.0L Turbo, 7 seats, luxury interior'),
        ('Hyundai Staria', 'Hyundai', 38000, '2.2L Diesel, 9 seats, spacious design')
     ) AS v(name, manufacturer, price, specs)
WHERE vt.type_name = 'Van';

-- =========================================
-- REPLACED: STATIONS (your 4 additional stations)
-- =========================================
INSERT INTO "Stations" (station_id, name, address, lat, long, capacity, image_url, isActive, updated_at)
VALUES
  (gen_random_uuid(), 'Trạm quận 1', '456 Lê Lợi, Quận 1, TP.HCM', 10.7769, 106.7009, 20, 'https://example.com/station_q1.jpg', true, NOW()),
  (gen_random_uuid(), 'Trạm quận 5', '321 Trần Hưng Đạo, Quận 5, TP.HCM', 10.7574, 106.6662, 20, 'https://example.com/station_q5.jpg', true, NOW()),
  (gen_random_uuid(), 'Trạm quận 7', '123 Nguyễn Văn Linh, Quận 7, TP.HCM', 10.7365, 106.7047, 20, 'https://example.com/station_q7.jpg', true, NOW()),
  (gen_random_uuid(), 'Trạm Thủ Đức', '789 Phạm Văn Đồng, TP. Thủ Đức', 10.8523, 106.7521, 20, 'https://example.com/station_td.jpg', true, NOW());

-- =========================================
-- REPLACED: VEHICLES (generated from models/stations)
-- =========================================
INSERT INTO "Vehicles" (vehicle_id, serial_number, model_id, station_id, status, battery_level, battery_capacity, range, color, last_maintenance, img, created_at, isActive)
SELECT
  gen_random_uuid(),
  'SN-' || LEFT(vm.name, 3) || '-' || i::text || '-' || floor(random()*10000)::text AS serial_number,
  vm.vehicle_model_id,
  s.station_id,
  'AVAILABLE',
  (80 + (random() * 20))::INT,
  100,
  (250 + (random() * 100))::INT,
  CASE i
    WHEN 1 THEN 'Đỏ'
    WHEN 2 THEN 'Xanh'
    WHEN 3 THEN 'Trắng'
    ELSE 'Đen'
  END,
  NOW() - (interval '15 day' * random()),
  'https://example.com/' || replace(lower(vm.name), ' ', '_') || '_' || i || '.jpg',
  NOW(),
  true
FROM "VehicleModels" vm
JOIN (SELECT station_id, ROW_NUMBER() OVER () AS idx FROM "Stations") s ON true
CROSS JOIN generate_series(1, 4) AS g(i)
WHERE s.idx = g.i;

-- =========================================
-- PROMOTIONS (sample - kept from previous seed)
-- =========================================
INSERT INTO "Promotions" (promotion_id, promo_code, discount_percentage, start_date, end_date, created_at, isActive) VALUES
(gen_random_uuid(), 'SAVE10', 10.00, '2025-10-01 00:00:00', '2025-10-31 23:59:59', CURRENT_TIMESTAMP, TRUE),
(gen_random_uuid(), 'WELCOME20', 20.00, '2025-10-01 00:00:00', '2025-10-15 23:59:59', CURRENT_TIMESTAMP, TRUE),
(gen_random_uuid(), 'WEEKEND15', 15.00, '2025-10-03 00:00:00', '2025-10-05 23:59:59', CURRENT_TIMESTAMP, TRUE),
(gen_random_uuid(), 'FIRST50', 50.00, '2025-10-01 00:00:00', '2025-10-10 23:59:59', CURRENT_TIMESTAMP, TRUE),
(gen_random_uuid(), 'SUMMER25', 25.00, '2025-10-15 00:00:00', '2025-10-30 23:59:59', CURRENT_TIMESTAMP, TRUE);

-- =========================================
-- ORDERS (sample kept from previous seed)
-- Note: use existing Vehicles inserted above (ORDER BY created_at)
-- =========================================
INSERT INTO "Orders" (order_id, customer_id, vehicle_id, order_code, order_date, start_time, end_time, base_price, total_price, status, promotion_id, staff_id, created_at, isActive) VALUES
(gen_random_uuid(), (SELECT account_id FROM "Accounts" WHERE username = 'john_doe' LIMIT 1),  (SELECT vehicle_id FROM "Vehicles" ORDER BY created_at LIMIT 1), gen_random_uuid()::text, '2025-10-12 10:00:00', '2025-10-12 12:00:00', '2025-10-12 14:00:00', 25.00, 30.00, 'COMPLETED', (SELECT promotion_id FROM "Promotions" WHERE promo_code = 'SAVE10' LIMIT 1), (SELECT account_id FROM "Accounts" WHERE username = 'bob_jones' LIMIT 1), CURRENT_TIMESTAMP, TRUE),
(gen_random_uuid(), (SELECT account_id FROM "Accounts" WHERE username = 'jane_smith' LIMIT 1),  (SELECT vehicle_id FROM "Vehicles" ORDER BY created_at OFFSET 1 LIMIT 1), gen_random_uuid()::text, '2025-10-12 11:00:00', '2025-10-12 13:00:00', '2025-10-12 15:00:00', 24.00, 29.00, 'PENDING', NULL, (SELECT account_id FROM "Accounts" WHERE username = 'carol_white' LIMIT 1), CURRENT_TIMESTAMP, TRUE),
(gen_random_uuid(), (SELECT account_id FROM "Accounts" WHERE username = 'alice_wong' LIMIT 1),  (SELECT vehicle_id FROM "Vehicles" ORDER BY created_at OFFSET 2 LIMIT 1), gen_random_uuid()::text, '2025-10-12 09:00:00', '2025-10-12 10:00:00', '2025-10-12 12:00:00', 45.00, 50.00, 'ONGOING', (SELECT promotion_id FROM "Promotions" WHERE promo_code = 'WELCOME20' LIMIT 1), (SELECT account_id FROM "Accounts" WHERE username = 'david_brown' LIMIT 1), CURRENT_TIMESTAMP, TRUE),
(gen_random_uuid(), (SELECT account_id FROM "Accounts" WHERE username = 'john_doe' LIMIT 1),  (SELECT vehicle_id FROM "Vehicles" ORDER BY created_at OFFSET 3 LIMIT 1), gen_random_uuid()::text, '2025-10-12 08:00:00', '2025-10-12 09:00:00', '2025-10-12 11:00:00', 40.00, 44.00, 'COMPLETED', NULL, (SELECT account_id FROM "Accounts" WHERE username = 'bob_jones' LIMIT 1), CURRENT_TIMESTAMP, TRUE),
(gen_random_uuid(), (SELECT account_id FROM "Accounts" WHERE username = 'jane_smith' LIMIT 1),  (SELECT vehicle_id FROM "Vehicles" ORDER BY created_at OFFSET 4 LIMIT 1), gen_random_uuid()::text, '2025-10-12 14:00:00', '2025-10-12 15:00:00', '2025-10-12 17:00:00', 36.00, 40.00, 'PENDING', (SELECT promotion_id FROM "Promotions" WHERE promo_code = 'WEEKEND15' LIMIT 1), (SELECT account_id FROM "Accounts" WHERE username = 'carol_white' LIMIT 1), CURRENT_TIMESTAMP, TRUE),
(gen_random_uuid(), (SELECT account_id FROM "Accounts" WHERE username = 'alice_wong' LIMIT 1),  (SELECT vehicle_id FROM "Vehicles" ORDER BY created_at OFFSET 5 LIMIT 1), gen_random_uuid()::text, '2025-10-12 12:00:00', '2025-10-12 13:00:00', '2025-10-12 15:00:00', 35.00, 39.00, 'ONGOING', NULL, (SELECT account_id FROM "Accounts" WHERE username = 'david_brown' LIMIT 1), CURRENT_TIMESTAMP, TRUE),
(gen_random_uuid(), (SELECT account_id FROM "Accounts" WHERE username = 'john_doe' LIMIT 1),  (SELECT vehicle_id FROM "Vehicles" ORDER BY created_at OFFSET 6 LIMIT 1), gen_random_uuid()::text, '2025-10-12 07:00:00', '2025-10-12 08:00:00', '2025-10-12 10:00:00', 55.00, 60.00, 'COMPLETED', NULL, (SELECT account_id FROM "Accounts" WHERE username = 'bob_jones' LIMIT 1), CURRENT_TIMESTAMP, TRUE),
(gen_random_uuid(), (SELECT account_id FROM "Accounts" WHERE username = 'jane_smith' LIMIT 1),  (SELECT vehicle_id FROM "Vehicles" ORDER BY created_at OFFSET 7 LIMIT 1), gen_random_uuid()::text, '2025-10-12 16:00:00', '2025-10-12 17:00:00', '2025-10-12 19:00:00', 50.00, 56.00, 'PENDING', NULL, (SELECT account_id FROM "Accounts" WHERE username = 'carol_white' LIMIT 1), CURRENT_TIMESTAMP, TRUE),
(gen_random_uuid(), (SELECT account_id FROM "Accounts" WHERE username = 'alice_wong' LIMIT 1),  (SELECT vehicle_id FROM "Vehicles" ORDER BY created_at OFFSET 8 LIMIT 1), gen_random_uuid()::text, '2025-10-12 10:00:00', '2025-10-12 11:00:00', '2025-10-12 13:00:00', 18.00, 20.00, 'ONGOING', NULL, (SELECT account_id FROM "Accounts" WHERE username = 'david_brown' LIMIT 1), CURRENT_TIMESTAMP, TRUE);

-- =========================================
-- PAYMENTS (sample kept from previous seed)
-- =========================================
INSERT INTO "Payments" (payment_id, order_id, gateway_tx_id, amount, payment_date, payment_method, payment_type, status, created_at, isActive)
VALUES
(gen_random_uuid(), (SELECT order_id FROM "Orders" ORDER BY created_at LIMIT 1), 'cc_tx_2001', 30.00, '2025-10-12 14:30:00', 'Credit Card', 'FINAL', 'COMPLETED', CURRENT_TIMESTAMP, TRUE),
(gen_random_uuid(), (SELECT order_id FROM "Orders" ORDER BY created_at OFFSET 1 LIMIT 1), NULL, 5.00, CURRENT_TIMESTAMP, 'MOMO', 'DEPOSIT', 'PENDING', CURRENT_TIMESTAMP, TRUE),
(gen_random_uuid(), (SELECT order_id FROM "Orders" ORDER BY created_at OFFSET 2 LIMIT 1), 'cc_tx_2003', 50.00, '2025-10-12 12:30:00', 'Credit Card', 'FINAL', 'COMPLETED', CURRENT_TIMESTAMP, TRUE);

-- =========================================
-- FEEDBACKS (sample kept)
-- =========================================
INSERT INTO "Feedbacks" (feedback_id, customer_id, order_id, rating, comment, feedback_date, created_at, isActive) VALUES
(gen_random_uuid(), (SELECT account_id FROM "Accounts" WHERE username = 'john_doe' LIMIT 1), (SELECT order_id FROM "Orders" ORDER BY created_at LIMIT 1), 5, 'Great service!', '2025-10-12 14:45:00', CURRENT_TIMESTAMP, TRUE),
(gen_random_uuid(), (SELECT account_id FROM "Accounts" WHERE username = 'jane_smith' LIMIT 1), (SELECT order_id FROM "Orders" ORDER BY created_at OFFSET 1 LIMIT 1), 4, 'Good but slow', '2025-10-12 15:45:00', CURRENT_TIMESTAMP, TRUE);

-- =========================================
-- REPORTS (sample kept)
-- =========================================
INSERT INTO "Reports" (report_id, report_type, generated_date, text, account_id, vehicle_id, created_at, isActive) VALUES
(gen_random_uuid(), 'Maintenance', '2025-10-12 09:00:00', 'Vehicle needs tire replacement', (SELECT account_id FROM "Accounts" WHERE username = 'bob_jones' LIMIT 1), (SELECT vehicle_id FROM "Vehicles" ORDER BY created_at LIMIT 1), CURRENT_TIMESTAMP, TRUE),
(gen_random_uuid(), 'Incident', '2025-10-12 10:00:00', 'Minor scratch reported', (SELECT account_id FROM "Accounts" WHERE username = 'carol_white' LIMIT 1), (SELECT vehicle_id FROM "Vehicles" ORDER BY created_at OFFSET 1 LIMIT 1), CURRENT_TIMESTAMP, TRUE);

-- =========================================
-- STAFF_REVENUES (sample kept)
-- =========================================
INSERT INTO "Staff_Revenues" (staff_revenue_id, staff_id, revenue_date, total_revenue, commission, created_at, isActive) VALUES
(gen_random_uuid(), (SELECT account_id FROM "Accounts" WHERE username = 'bob_jones' LIMIT 1), '2025-10-12 00:00:00', 500.00, 50.00, CURRENT_TIMESTAMP, TRUE),
(gen_random_uuid(), (SELECT account_id FROM "Accounts" WHERE username = 'carol_white' LIMIT 1), '2025-10-12 00:00:00', 450.00, 45.00, CURRENT_TIMESTAMP, TRUE);

-- =========================================
-- OPTIONAL: Wallets and WalletTransactions (kept sample)
-- =========================================
INSERT INTO "Wallets" (wallet_id, account_id, balance, created_at, isActive)
VALUES
  (gen_random_uuid(), (SELECT account_id FROM "Accounts" WHERE username = 'john_doe' LIMIT 1), 1000.00, CURRENT_TIMESTAMP, TRUE),
  (gen_random_uuid(), (SELECT account_id FROM "Accounts" WHERE username = 'jane_smith' LIMIT 1), 500.00, CURRENT_TIMESTAMP, TRUE);

INSERT INTO "WalletTransactions" (transaction_id, wallet_id, order_id, amount, transaction_type, description, created_at, isActive)
VALUES
  (gen_random_uuid(), (SELECT wallet_id FROM "Wallets" WHERE account_id = (SELECT account_id FROM "Accounts" WHERE username = 'john_doe' LIMIT 1) LIMIT 1), NULL, 1000.00, 'DEPOSIT', 'Initial topup', CURRENT_TIMESTAMP, TRUE),
  (gen_random_uuid(), (SELECT wallet_id FROM "Wallets" WHERE account_id = (SELECT account_id FROM "Accounts" WHERE username = 'jane_smith' LIMIT 1) LIMIT 1), NULL, 500.00, 'DEPOSIT', 'Initial topup', CURRENT_TIMESTAMP, TRUE);
-- =========================================
-- DONE
-- =========================================
