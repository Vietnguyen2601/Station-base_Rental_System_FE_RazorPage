-- =========================================
-- TẠO DATABASE NẾU CHƯA TỒN TẠI
-- =========================================
DO $$
BEGIN
    IF NOT EXISTS (SELECT FROM pg_database WHERE datname = 'electric_vehicle_db') THEN
        EXECUTE 'CREATE DATABASE electric_vehicle_db';
        RAISE NOTICE 'Database electric_vehicle_db đã được tạo.';
    ELSE
        RAISE NOTICE 'Database electric_vehicle_db đã tồn tại.';
    END IF;
END
$$;

-- NOTE: If you run this inside psql connected to a specific DB, you can skip the DB creation block above.

-- =========================================
-- EXTENSIONS
-- =========================================
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";
CREATE EXTENSION IF NOT EXISTS "pgcrypto";

-- =========================================
-- BASIC TRIGGER FUNCTIONS
-- =========================================
CREATE OR REPLACE FUNCTION set_created_at_column()
RETURNS TRIGGER AS $$
BEGIN
    NEW.created_at = CURRENT_TIMESTAMP;
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

-- =========================================
-- ENUM TYPES (vehicle status, order status, payment type)
-- =========================================
DO $$
BEGIN
    IF NOT EXISTS (SELECT FROM pg_type WHERE typname = 'vehicle_status') THEN
        CREATE TYPE vehicle_status AS ENUM ('AVAILABLE', 'RENTED', 'MAINTENANCE', 'CHARGING');
    END IF;
END
$$;

DO $$
BEGIN
    IF NOT EXISTS (SELECT FROM pg_type WHERE typname = 'order_status') THEN
        CREATE TYPE order_status AS ENUM ('PENDING', 'CONFIRMED', 'ONGOING', 'COMPLETED', 'CANCELED');
    END IF;
END
$$;

DO $$
BEGIN
    IF NOT EXISTS (SELECT FROM pg_type WHERE typname = 'payment_type_enum') THEN
        CREATE TYPE payment_type_enum AS ENUM ('DEPOSIT', 'FINAL', 'REFUND');
    END IF;
END
$$;

-- =========================================
-- DROP TABLES (safe to run)
-- =========================================
DROP TABLE IF EXISTS "Staff_Revenues" CASCADE;
DROP TABLE IF EXISTS "Reports" CASCADE;
DROP TABLE IF EXISTS "Feedbacks" CASCADE;
DROP TABLE IF EXISTS "Payments" CASCADE;
DROP TABLE IF EXISTS "Orders" CASCADE;
DROP TABLE IF EXISTS "Promotions" CASCADE;
DROP TABLE IF EXISTS "Vehicles" CASCADE;
DROP TABLE IF EXISTS "VehicleModels" CASCADE;
DROP TABLE IF EXISTS "VehicleTypes" CASCADE;
DROP TABLE IF EXISTS "Stations" CASCADE;
DROP TABLE IF EXISTS "Roles" CASCADE;
DROP TABLE IF EXISTS "Accounts" CASCADE;

-- =========================================
-- CREATE TABLES
-- =========================================
CREATE TABLE "Roles" (
  role_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  role_name VARCHAR(100) NOT NULL,
  isActive BOOLEAN NOT NULL DEFAULT TRUE,
  created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
  updated_at TIMESTAMP
);

CREATE TABLE "Accounts" (
  account_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  username VARCHAR(100) NOT NULL,
  password VARCHAR(255) NOT NULL,
  email VARCHAR(150) NOT NULL,
  contact_number VARCHAR(20),
  role_id UUID NOT NULL,
  isActive BOOLEAN NOT NULL DEFAULT TRUE,
  created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
  updated_at TIMESTAMP,
  FOREIGN KEY (role_id) REFERENCES "Roles"(role_id)
);

CREATE TABLE "VehicleTypes" (
  vehicle_type_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  type_name VARCHAR(100) UNIQUE NOT NULL,
  description TEXT,
  created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
  isActive BOOLEAN NOT NULL DEFAULT TRUE,
  updated_at TIMESTAMP
);

CREATE TABLE "VehicleModels" (
  vehicle_model_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  type_id UUID NOT NULL,
  name VARCHAR(100) NOT NULL,
  manufacturer VARCHAR(100) NOT NULL,
  price_per_hour DECIMAL NOT NULL,
  specs VARCHAR(255),
  created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
  isActive BOOLEAN NOT NULL DEFAULT TRUE,
  updated_at TIMESTAMP,
  FOREIGN KEY (type_id) REFERENCES "VehicleTypes"(vehicle_type_id)
);

CREATE TABLE "Stations" (
  station_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  name VARCHAR(100) NOT NULL,
  address VARCHAR(255) NOT NULL,
  lat DECIMAL NOT NULL,
  long DECIMAL NOT NULL,
  capacity INT NOT NULL,
  image_url VARCHAR(255),
  isActive BOOLEAN NOT NULL DEFAULT TRUE,
  created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
  updated_at TIMESTAMP
);

CREATE TABLE "Vehicles" (
  vehicle_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  serial_number VARCHAR(100) UNIQUE NOT NULL,
  model_id UUID NOT NULL,
  station_id UUID,
  status vehicle_status NOT NULL DEFAULT 'AVAILABLE',
  battery_level INT,
  battery_capacity INT,
  range INT,
  color VARCHAR(50),
  last_maintenance DATE,
  img VARCHAR(255),
  created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
  isActive BOOLEAN NOT NULL DEFAULT TRUE,
  updated_at TIMESTAMP,
  FOREIGN KEY (model_id) REFERENCES "VehicleModels"(vehicle_model_id),
  FOREIGN KEY (station_id) REFERENCES "Stations"(station_id)
);

CREATE TABLE "Promotions" (
  promotion_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  promo_code VARCHAR(50) NOT NULL,
  discount_percentage DECIMAL NOT NULL,
  start_date TIMESTAMP NOT NULL,
  end_date TIMESTAMP NOT NULL,
  created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
  updated_at TIMESTAMP,
  isActive BOOLEAN NOT NULL DEFAULT TRUE
);

CREATE TABLE "Orders" (
  order_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  customer_id UUID NOT NULL,
  vehicle_id UUID NOT NULL,
  order_date TIMESTAMP NOT NULL,
  start_time TIMESTAMP NOT NULL,
  end_time TIMESTAMP,
  base_price DECIMAL NOT NULL,
  total_price DECIMAL NOT NULL,
  status order_status NOT NULL DEFAULT 'PENDING',
  promotion_id UUID,
  staff_id UUID,
  created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
  updated_at TIMESTAMP,
  isActive BOOLEAN NOT NULL DEFAULT TRUE,
  FOREIGN KEY (customer_id) REFERENCES "Accounts"(account_id),
  FOREIGN KEY (vehicle_id) REFERENCES "Vehicles"(vehicle_id),
  FOREIGN KEY (promotion_id) REFERENCES "Promotions"(promotion_id),
  FOREIGN KEY (staff_id) REFERENCES "Accounts"(account_id)
);

CREATE TABLE "Payments" (
  payment_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  order_id UUID NOT NULL,
  amount DECIMAL NOT NULL,
  payment_date TIMESTAMP NOT NULL,
  payment_method VARCHAR(100) NOT NULL,
  payment_type payment_type_enum NOT NULL,
  status VARCHAR(50) NOT NULL, -- PENDING / COMPLETED / FAILED / REFUNDED (kept as text for simplicity)
  gateway_tx_id VARCHAR(255),
  gateway_response JSONB,
  idempotency_key VARCHAR(255),
  created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
  updated_at TIMESTAMP,
  isActive BOOLEAN NOT NULL DEFAULT TRUE,
  FOREIGN KEY (order_id) REFERENCES "Orders"(order_id)
);

CREATE TABLE "Feedbacks" (
  feedback_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  customer_id UUID NOT NULL,
  order_id UUID NOT NULL,
  rating INT NOT NULL,
  comment TEXT,
  feedback_date TIMESTAMP NOT NULL,
  created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
  updated_at TIMESTAMP,
  isActive BOOLEAN NOT NULL DEFAULT TRUE,
  FOREIGN KEY (customer_id) REFERENCES "Accounts"(account_id),
  FOREIGN KEY (order_id) REFERENCES "Orders"(order_id)
);

CREATE TABLE "Reports" (
  report_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  report_type VARCHAR(100) NOT NULL,
  generated_date TIMESTAMP NOT NULL,
  text VARCHAR(255) NOT NULL,
  account_id UUID NOT NULL,
  vehicle_id UUID NOT NULL,
  created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
  updated_at TIMESTAMP,
  isActive BOOLEAN NOT NULL DEFAULT TRUE,
  FOREIGN KEY (account_id) REFERENCES "Accounts"(account_id),
  FOREIGN KEY (vehicle_id) REFERENCES "Vehicles"(vehicle_id)
);

CREATE TABLE "Staff_Revenues" (
  staff_revenue_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  staff_id UUID NOT NULL,
  revenue_date TIMESTAMP NOT NULL,
  total_revenue DECIMAL NOT NULL,
  commission DECIMAL,
  created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
  updated_at TIMESTAMP,
  isActive BOOLEAN NOT NULL DEFAULT TRUE,
  FOREIGN KEY (staff_id) REFERENCES "Accounts"(account_id)
);

-- =========================================
-- TRIGGERS created_at
-- =========================================
CREATE TRIGGER set_roles_created_at BEFORE INSERT ON "Roles" FOR EACH ROW EXECUTE FUNCTION set_created_at_column();
CREATE TRIGGER set_accounts_created_at BEFORE INSERT ON "Accounts" FOR EACH ROW EXECUTE FUNCTION set_created_at_column();
CREATE TRIGGER set_vehicle_types_created_at BEFORE INSERT ON "VehicleTypes" FOR EACH ROW EXECUTE FUNCTION set_created_at_column();
CREATE TRIGGER set_vehicle_models_created_at BEFORE INSERT ON "VehicleModels" FOR EACH ROW EXECUTE FUNCTION set_created_at_column();
CREATE TRIGGER set_stations_created_at BEFORE INSERT ON "Stations" FOR EACH ROW EXECUTE FUNCTION set_created_at_column();
CREATE TRIGGER set_vehicles_created_at BEFORE INSERT ON "Vehicles" FOR EACH ROW EXECUTE FUNCTION set_created_at_column();
CREATE TRIGGER set_promotions_created_at BEFORE INSERT ON "Promotions" FOR EACH ROW EXECUTE FUNCTION set_created_at_column();
CREATE TRIGGER set_orders_created_at BEFORE INSERT ON "Orders" FOR EACH ROW EXECUTE FUNCTION set_created_at_column();
CREATE TRIGGER set_payments_created_at BEFORE INSERT ON "Payments" FOR EACH ROW EXECUTE FUNCTION set_created_at_column();
CREATE TRIGGER set_feedbacks_created_at BEFORE INSERT ON "Feedbacks" FOR EACH ROW EXECUTE FUNCTION set_created_at_column();
CREATE TRIGGER set_reports_created_at BEFORE INSERT ON "Reports" FOR EACH ROW EXECUTE FUNCTION set_created_at_column();
CREATE TRIGGER set_staff_revenues_created_at BEFORE INSERT ON "Staff_Revenues" FOR EACH ROW EXECUTE FUNCTION set_created_at_column();

-- =========================================
-- TRIGGERS updated_at
-- =========================================
CREATE TRIGGER update_accounts_updated_at BEFORE UPDATE ON "Accounts" FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();
CREATE TRIGGER update_roles_updated_at BEFORE UPDATE ON "Roles" FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();
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

-- =========================================
-- INDEXES (useful for gateway ids and idempotency)
-- =========================================
CREATE UNIQUE INDEX IF NOT EXISTS ux_payments_gateway_tx_id ON "Payments"(gateway_tx_id) WHERE gateway_tx_id IS NOT NULL;
CREATE UNIQUE INDEX IF NOT EXISTS ux_payments_idempotency ON "Payments"(idempotency_key) WHERE idempotency_key IS NOT NULL;
-- Optional: ensure at most one DEPOSIT PENDING per order (uncomment if desired)
-- CREATE UNIQUE INDEX IF NOT EXISTS ux_order_deposit_pending ON "Payments"(order_id) WHERE payment_type = 'DEPOSIT' AND status = 'PENDING' AND isActive = TRUE;

-- =========================================
-- FUNCTIONS: payment/order flows (functions run within caller transaction)
-- - create_order_with_deposit  -> creates Order + DEPOSIT payment (PENDING) and marks vehicle RENTED
-- - cancel_order_refund        -> creates REFUND payment (PENDING) for completed deposits and marks order canceled
-- - complete_order_finalize_payment -> creates FINAL or REFUND payment (PENDING) and marks order completed
-- - mark_payment_completed     -> app calls after gateway success to mark payment COMPLETED and store gateway tx info
-- =========================================

-- 1) create_order_with_deposit
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
    -- Lock vehicle row to avoid race condition
    SELECT status INTO v_vehicle_status
    FROM "Vehicles"
    WHERE vehicle_id = p_vehicle_id
    FOR UPDATE;

    IF NOT FOUND THEN
        RAISE EXCEPTION 'Vehicle % does not exist', p_vehicle_id;
    END IF;

    IF v_vehicle_status <> 'AVAILABLE' THEN
        RAISE EXCEPTION 'Vehicle % not available (status=%)', p_vehicle_id, v_vehicle_status;
    END IF;

    -- Insert order (CONFIRMED)
    INSERT INTO "Orders" (
        order_id, customer_id, vehicle_id, order_date, start_time, end_time,
        base_price, total_price, status, promotion_id, staff_id, created_at, isActive
    ) VALUES (
        gen_random_uuid(), p_customer_id, p_vehicle_id, p_order_date, p_start_time, p_end_time,
        p_base_price, p_total_price, 'CONFIRMED', p_promotion_id, p_staff_id, CURRENT_TIMESTAMP, TRUE
    ) RETURNING order_id INTO v_order_id;

    -- Insert deposit payment as PENDING: app should call gateway and then set this payment.status = 'COMPLETED' and set gateway_tx_id
    INSERT INTO "Payments" (
        payment_id, order_id, amount, payment_date, payment_method, payment_type, status, created_at, isActive
    ) VALUES (
        gen_random_uuid(), v_order_id, p_deposit_amount, CURRENT_TIMESTAMP, p_payment_method, 'DEPOSIT', 'PENDING', CURRENT_TIMESTAMP, TRUE
    );

    -- Mark vehicle as RENTED (prevent double-booking)
    UPDATE "Vehicles" SET status = 'RENTED', updated_at = CURRENT_TIMESTAMP WHERE vehicle_id = p_vehicle_id;

    RETURN v_order_id;
END;
$$ LANGUAGE plpgsql;

-- 2) cancel_order_refund
CREATE OR REPLACE FUNCTION cancel_order_refund(
    p_order_id UUID,
    p_refund_method VARCHAR DEFAULT 'ORIGINAL'
) RETURNS BOOLEAN AS $$
DECLARE
    v_status order_status;
    v_vehicle_id UUID;
    v_deposit_sum NUMERIC := 0;
BEGIN
    -- Lock order row
    SELECT status, vehicle_id INTO v_status, v_vehicle_id
    FROM "Orders"
    WHERE order_id = p_order_id
    FOR UPDATE;

    IF NOT FOUND THEN
        RAISE EXCEPTION 'Order % does not exist', p_order_id;
    END IF;

    IF v_status NOT IN ('PENDING','CONFIRMED') THEN
        RAISE EXCEPTION 'Cannot cancel order % with status %', p_order_id, v_status;
    END IF;

    -- Sum only deposits that were COMPLETED (actually charged)
    SELECT COALESCE(SUM(amount),0) INTO v_deposit_sum
    FROM "Payments"
    WHERE order_id = p_order_id AND payment_type = 'DEPOSIT' AND status = 'COMPLETED' AND isActive = TRUE;

    IF v_deposit_sum > 0 THEN
        -- Create REFUND payment record with PENDING status; app must call gateway and then update to COMPLETED
        INSERT INTO "Payments" (
            payment_id, order_id, amount, payment_date, payment_method, payment_type, status, created_at, isActive
        ) VALUES (
            gen_random_uuid(), p_order_id, v_deposit_sum, CURRENT_TIMESTAMP, p_refund_method, 'REFUND', 'PENDING', CURRENT_TIMESTAMP, TRUE
        );
    END IF;

    -- Update order status to CANCELED
    UPDATE "Orders" SET status = 'CANCELED', updated_at = CURRENT_TIMESTAMP WHERE order_id = p_order_id;

    -- If vehicle exists, set to AVAILABLE (only if currently RENTED)
    IF v_vehicle_id IS NOT NULL THEN
        UPDATE "Vehicles" SET status = 'AVAILABLE', updated_at = CURRENT_TIMESTAMP WHERE vehicle_id = v_vehicle_id AND status = 'RENTED';
    END IF;

    RETURN TRUE;
END;
$$ LANGUAGE plpgsql;

-- 3) complete_order_finalize_payment
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
    -- Lock order row
    SELECT total_price, vehicle_id INTO v_total_price, v_vehicle_id
    FROM "Orders"
    WHERE order_id = p_order_id
    FOR UPDATE;

    IF NOT FOUND THEN
        RAISE EXCEPTION 'Order % does not exist', p_order_id;
    END IF;

    -- Sum only deposits that were COMPLETED
    SELECT COALESCE(SUM(amount),0) INTO v_deposit_sum
    FROM "Payments"
    WHERE order_id = p_order_id AND payment_type = 'DEPOSIT' AND status = 'COMPLETED' AND isActive = TRUE;

    v_due := v_total_price - v_deposit_sum;

    IF v_due > 0 THEN
        -- Create FINAL payment PENDING (app will charge)
        INSERT INTO "Payments" (
            payment_id, order_id, amount, payment_date, payment_method, payment_type, status, created_at, isActive
        ) VALUES (
            gen_random_uuid(), p_order_id, v_due, CURRENT_TIMESTAMP, p_final_payment_method, 'FINAL', 'PENDING', CURRENT_TIMESTAMP, TRUE
        );
    ELSIF v_due < 0 THEN
        -- Create REFUND payment PENDING (app will refund)
        INSERT INTO "Payments" (
            payment_id, order_id, amount, payment_date, payment_method, payment_type, status, created_at, isActive
        ) VALUES (
            gen_random_uuid(), p_order_id, ABS(v_due), CURRENT_TIMESTAMP, p_final_payment_method, 'REFUND', 'PENDING', CURRENT_TIMESTAMP, TRUE
        );
    END IF;

    -- Mark order completed
    UPDATE "Orders" SET status = 'COMPLETED', updated_at = CURRENT_TIMESTAMP WHERE order_id = p_order_id;

    -- Set vehicle available if currently RENTED
    IF v_vehicle_id IS NOT NULL THEN
        UPDATE "Vehicles" SET status = 'AVAILABLE', updated_at = CURRENT_TIMESTAMP WHERE vehicle_id = v_vehicle_id AND status = 'RENTED';
    END IF;

    RETURN v_due;
END;
$$ LANGUAGE plpgsql;

-- 4) mark_payment_completed
-- App should call this after gateway confirms charge/refund success.
CREATE OR REPLACE FUNCTION mark_payment_completed(
    p_payment_id UUID,
    p_gateway_tx_id VARCHAR,
    p_gateway_response JSONB DEFAULT NULL,
    p_status VARCHAR DEFAULT 'COMPLETED',  -- expected: 'COMPLETED' or 'FAILED'
    p_idempotency_key VARCHAR DEFAULT NULL
) RETURNS BOOLEAN AS $$
DECLARE
    v_rows INT;
BEGIN
    -- Optionally enforce idempotency: if idempotency_key provided and already used, prevent duplicate
    IF p_idempotency_key IS NOT NULL THEN
        -- If another payment already used this idempotency_key, raise
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

-- =========================================
-- SAMPLE USAGE (ví dụ)
-- Lưu ý: thay UUID bằng giá trị thực khi test
-- =========================================
-- 1) Tạo order với deposit (trong transaction của app nếu muốn)
-- BEGIN;
-- SELECT create_order_with_deposit(
--   'customer-uuid'::uuid,
--   'vehicle-uuid'::uuid,
--   now(),
--   now() + interval '1 hour',
--   now() + interval '5 hour',
--   50,        -- base_price
--   200,       -- total_price
--   50,        -- deposit_amount
--   'MOMO',    -- payment_method
--   NULL,      -- promotion_id
--   NULL       -- staff_id
-- );
-- COMMIT;
--
-- -> App lấy deposit payment_id:
-- SELECT payment_id FROM "Payments" WHERE order_id = '<order_id>' AND payment_type = 'DEPOSIT' ORDER BY created_at DESC LIMIT 1;
-- -> App calls Momo sandbox to charge deposit; when Momo returns success (transId...), app updates:
-- SELECT mark_payment_completed('<payment_id>'::uuid, 'momo_tx_123', '{"momo": "response"}'::jsonb, 'COMPLETED', 'idemp-key-1');

-- 2) Hủy order và hoàn cọc (nếu deposit already COMPLETED)
-- BEGIN;
-- SELECT cancel_order_refund('<order_id>'::uuid, 'MOMO');
-- COMMIT;
-- -> App finds refund payment (status = 'PENDING'), calls Momo refund API, then:
-- SELECT mark_payment_completed('<refund_payment_id>'::uuid, 'momo_refund_456', '{"momo": "refund_response"}'::jsonb, 'COMPLETED', 'idemp-refund-1');

-- 3) Hoàn tất trả xe và finalize payment
-- BEGIN;
-- SELECT complete_order_finalize_payment('<order_id>'::uuid, 'MOMO');
-- COMMIT;
-- -> If a FINAL payment was created (PENDING), app charges via gateway and then:
-- SELECT mark_payment_completed('<final_payment_id>'::uuid, 'momo_final_789', '{"momo": "final_response"}'::jsonb, 'COMPLETED', 'idemp-final-1');

-- =========================================
-- CHECKS (gợi ý)
-- 1) Sau create_order_with_deposit: Orders.status = 'CONFIRMED', Vehicles.status = 'RENTED', Payments has DEPOSIT PENDING
-- 2) After mark_payment_completed on deposit: Payments.status = 'COMPLETED' and gateway_tx_id stored
-- 3) After cancel_order_refund: Orders.status = 'CANCELED', Payments contains REFUND (PENDING)
-- 4) After complete_order_finalize_payment: Orders.status = 'COMPLETED', Payments contains FINAL/REFUND (PENDING)
-- =========================================


--===========Insert Data===========--
-- ---------------------------
-- Roles
-- ---------------------------
INSERT INTO "Roles" (role_id, role_name, isActive) VALUES
(gen_random_uuid(), 'Customer', TRUE),
(gen_random_uuid(), 'Staff', TRUE),
(gen_random_uuid(), 'Admin', TRUE);

-- ---------------------------
-- Accounts
-- ---------------------------
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

-- ---------------------------
-- VehicleTypes
-- ---------------------------
INSERT INTO "VehicleTypes" (vehicle_type_id, type_name, description, created_at, isActive) VALUES
(gen_random_uuid(), 'Sedan', 'Four-door passenger car', CURRENT_TIMESTAMP, TRUE),
(gen_random_uuid(), 'SUV', 'Sport Utility Vehicle', CURRENT_TIMESTAMP, TRUE),
(gen_random_uuid(), 'Truck', 'Heavy-duty vehicle for cargo', CURRENT_TIMESTAMP, TRUE),
(gen_random_uuid(), 'Van', 'Large vehicle for passengers', CURRENT_TIMESTAMP, TRUE),
(gen_random_uuid(), 'Electric', 'Battery-powered vehicle', CURRENT_TIMESTAMP, TRUE),
(gen_random_uuid(), 'Hybrid', 'Gasoline and electric powered', CURRENT_TIMESTAMP, TRUE),
(gen_random_uuid(), 'Motorcycle', 'Two-wheeled vehicle', CURRENT_TIMESTAMP, TRUE),
(gen_random_uuid(), 'Convertible', 'Car with retractable roof', CURRENT_TIMESTAMP, TRUE),
(gen_random_uuid(), 'Coupe', 'Two-door sporty car', CURRENT_TIMESTAMP, TRUE),
(gen_random_uuid(), 'Hatchback', 'Car with rear door', CURRENT_TIMESTAMP, TRUE);

-- ---------------------------
-- VehicleModels
-- ---------------------------
INSERT INTO "VehicleModels" (vehicle_model_id, type_id, name, manufacturer, price_per_hour, specs, created_at, isActive) VALUES
(gen_random_uuid(), (SELECT vehicle_type_id FROM "VehicleTypes" WHERE type_name = 'Sedan' LIMIT 1), 'Camry', 'Toyota', 15.00, '2.5L 4-cylinder', CURRENT_TIMESTAMP, TRUE),
(gen_random_uuid(), (SELECT vehicle_type_id FROM "VehicleTypes" WHERE type_name = 'Sedan' LIMIT 1), 'Accord', 'Honda', 14.50, '1.5L Turbo', CURRENT_TIMESTAMP, TRUE),
(gen_random_uuid(), (SELECT vehicle_type_id FROM "VehicleTypes" WHERE type_name = 'SUV' LIMIT 1), 'RAV4', 'Toyota', 20.00, '2.5L 4-cylinder', CURRENT_TIMESTAMP, TRUE),
(gen_random_uuid(), (SELECT vehicle_type_id FROM "VehicleTypes" WHERE type_name = 'SUV' LIMIT 1), 'CR-V', 'Honda', 19.50, '1.5L Turbo', CURRENT_TIMESTAMP, TRUE),
(gen_random_uuid(), (SELECT vehicle_type_id FROM "VehicleTypes" WHERE type_name = 'Electric' LIMIT 1), 'Model 3', 'Tesla', 25.00, 'Electric 75 kWh', CURRENT_TIMESTAMP, TRUE),
(gen_random_uuid(), (SELECT vehicle_type_id FROM "VehicleTypes" WHERE type_name = 'Electric' LIMIT 1), 'Leaf', 'Nissan', 22.00, 'Electric 40 kWh', CURRENT_TIMESTAMP, TRUE),
(gen_random_uuid(), (SELECT vehicle_type_id FROM "VehicleTypes" WHERE type_name = 'Truck' LIMIT 1), 'F-150', 'Ford', 30.00, '3.5L V6', CURRENT_TIMESTAMP, TRUE),
(gen_random_uuid(), (SELECT vehicle_type_id FROM "VehicleTypes" WHERE type_name = 'Van' LIMIT 1), 'Sienna', 'Toyota', 28.00, '3.5L V6', CURRENT_TIMESTAMP, TRUE),
(gen_random_uuid(), (SELECT vehicle_type_id FROM "VehicleTypes" WHERE type_name = 'Motorcycle' LIMIT 1), 'Ninja', 'Kawasaki', 10.00, '400cc', CURRENT_TIMESTAMP, TRUE),
(gen_random_uuid(), (SELECT vehicle_type_id FROM "VehicleTypes" WHERE type_name = 'Coupe' LIMIT 1), 'Mustang', 'Ford', 18.00, '5.0L V8', CURRENT_TIMESTAMP, TRUE);

-- ---------------------------
-- Stations
-- ---------------------------
INSERT INTO "Stations" (station_id, name, address, lat, long, capacity, image_url, isActive) VALUES
(gen_random_uuid(), 'Downtown Hub', '123 Main St, City', 10.123456, 106.123456, 50, 'http://example.com/station1.jpg', TRUE),
(gen_random_uuid(), 'North Station', '456 North Rd, City', 10.234567, 106.234567, 30, 'http://example.com/station2.jpg', TRUE),
(gen_random_uuid(), 'South Station', '789 South Rd, City', 10.345678, 106.345678, 40, 'http://example.com/station3.jpg', TRUE),
(gen_random_uuid(), 'East Station', '101 East Ave, City', 10.456789, 106.456789, 25, 'http://example.com/station4.jpg', TRUE),
(gen_random_uuid(), 'West Station', '202 West Blvd, City', 10.567890, 106.567890, 35, 'http://example.com/station5.jpg', TRUE),
(gen_random_uuid(), 'Central Hub', '303 Central St, City', 10.678901, 106.678901, 60, 'http://example.com/station6.jpg', TRUE),
(gen_random_uuid(), 'Airport Station', '404 Airport Rd, City', 10.789012, 106.789012, 45, 'http://example.com/station7.jpg', TRUE),
(gen_random_uuid(), 'Suburban Stop', '505 Suburb Ln, City', 10.890123, 106.890123, 20, 'http://example.com/station8.jpg', TRUE),
(gen_random_uuid(), 'City Park', '606 Park Ave, City', 10.901234, 106.901234, 30, 'http://example.com/station9.jpg', TRUE),
(gen_random_uuid(), 'Mall Station', '707 Mall Rd, City', 10.012345, 106.012345, 50, 'http://example.com/station10.jpg', TRUE);

-- ---------------------------
-- Vehicles
-- Note: statuses chosen to match Orders below:
--   - Vehicles for ONGOING orders -> RENTED
--   - Vehicles for COMPLETED/PENDING orders -> AVAILABLE
-- ---------------------------
INSERT INTO "Vehicles" (vehicle_id, serial_number, model_id, station_id, status, battery_level, battery_capacity, range, color, last_maintenance, img, created_at, isActive) VALUES
(gen_random_uuid(), 'SN123456', (SELECT vehicle_model_id FROM "VehicleModels" WHERE name = 'Camry' LIMIT 1), (SELECT station_id FROM "Stations" WHERE name = 'Downtown Hub' LIMIT 1), 'AVAILABLE', NULL, NULL, NULL, 'Blue', '2025-09-01', 'http://example.com/vehicle1.jpg', CURRENT_TIMESTAMP, TRUE),
(gen_random_uuid(), 'SN123457', (SELECT vehicle_model_id FROM "VehicleModels" WHERE name = 'Accord' LIMIT 1), (SELECT station_id FROM "Stations" WHERE name = 'North Station' LIMIT 1), 'AVAILABLE', NULL, NULL, NULL, 'Red', '2025-09-02', 'http://example.com/vehicle2.jpg', CURRENT_TIMESTAMP, TRUE),
(gen_random_uuid(), 'SN123458', (SELECT vehicle_model_id FROM "VehicleModels" WHERE name = 'Model 3' LIMIT 1), (SELECT station_id FROM "Stations" WHERE name = 'South Station' LIMIT 1), 'RENTED', 80, 75, 300, 'White', '2025-09-03', 'http://example.com/vehicle3.jpg', CURRENT_TIMESTAMP, TRUE),
(gen_random_uuid(), 'SN123459', (SELECT vehicle_model_id FROM "VehicleModels" WHERE name = 'Leaf' LIMIT 1), (SELECT station_id FROM "Stations" WHERE name = 'East Station' LIMIT 1), 'AVAILABLE', 60, 40, 150, 'Black', '2025-09-04', 'http://example.com/vehicle4.jpg', CURRENT_TIMESTAMP, TRUE),
(gen_random_uuid(), 'SN123460', (SELECT vehicle_model_id FROM "VehicleModels" WHERE name = 'RAV4' LIMIT 1), (SELECT station_id FROM "Stations" WHERE name = 'West Station' LIMIT 1), 'AVAILABLE', NULL, NULL, NULL, 'Silver', '2025-09-05', 'http://example.com/vehicle5.jpg', CURRENT_TIMESTAMP, TRUE),
(gen_random_uuid(), 'SN123461', (SELECT vehicle_model_id FROM "VehicleModels" WHERE name = 'CR-V' LIMIT 1), (SELECT station_id FROM "Stations" WHERE name = 'Central Hub' LIMIT 1), 'RENTED', NULL, NULL, NULL, 'Green', '2025-09-06', 'http://example.com/vehicle6.jpg', CURRENT_TIMESTAMP, TRUE),
(gen_random_uuid(), 'SN123462', (SELECT vehicle_model_id FROM "VehicleModels" WHERE name = 'F-150' LIMIT 1), (SELECT station_id FROM "Stations" WHERE name = 'Airport Station' LIMIT 1), 'AVAILABLE', NULL, NULL, NULL, 'Blue', '2025-09-07', 'http://example.com/vehicle7.jpg', CURRENT_TIMESTAMP, TRUE),
(gen_random_uuid(), 'SN123463', (SELECT vehicle_model_id FROM "VehicleModels" WHERE name = 'Sienna' LIMIT 1), (SELECT station_id FROM "Stations" WHERE name = 'Suburban Stop' LIMIT 1), 'AVAILABLE', NULL, NULL, NULL, 'White', '2025-09-08', 'http://example.com/vehicle8.jpg', CURRENT_TIMESTAMP, TRUE),
(gen_random_uuid(), 'SN123464', (SELECT vehicle_model_id FROM "VehicleModels" WHERE name = 'Ninja' LIMIT 1), (SELECT station_id FROM "Stations" WHERE name = 'City Park' LIMIT 1), 'RENTED', NULL, NULL, NULL, 'Black', '2025-09-09', 'http://example.com/vehicle9.jpg', CURRENT_TIMESTAMP, TRUE),
(gen_random_uuid(), 'SN123465', (SELECT vehicle_model_id FROM "VehicleModels" WHERE name = 'Mustang' LIMIT 1), (SELECT station_id FROM "Stations" WHERE name = 'Mall Station' LIMIT 1), 'AVAILABLE', NULL, NULL, NULL, 'Red', '2025-09-10', 'http://example.com/vehicle10.jpg', CURRENT_TIMESTAMP, TRUE);

-- ---------------------------
-- Promotions
-- ---------------------------
INSERT INTO "Promotions" (promotion_id, promo_code, discount_percentage, start_date, end_date, created_at, isActive) VALUES
(gen_random_uuid(), 'SAVE10', 10.00, '2025-10-01 00:00:00', '2025-10-31 23:59:59', CURRENT_TIMESTAMP, TRUE),
(gen_random_uuid(), 'WELCOME20', 20.00, '2025-10-01 00:00:00', '2025-10-15 23:59:59', CURRENT_TIMESTAMP, TRUE),
(gen_random_uuid(), 'WEEKEND15', 15.00, '2025-10-03 00:00:00', '2025-10-05 23:59:59', CURRENT_TIMESTAMP, TRUE),
(gen_random_uuid(), 'FIRST50', 50.00, '2025-10-01 00:00:00', '2025-10-10 23:59:59', CURRENT_TIMESTAMP, TRUE),
(gen_random_uuid(), 'SUMMER25', 25.00, '2025-10-15 00:00:00', '2025-10-30 23:59:59', CURRENT_TIMESTAMP, TRUE),
(gen_random_uuid(), 'LOYALTY10', 10.00, '2025-10-01 00:00:00', '2025-12-31 23:59:59', CURRENT_TIMESTAMP, TRUE),
(gen_random_uuid(), 'FLASH30', 30.00, '2025-10-12 00:00:00', '2025-10-13 23:59:59', CURRENT_TIMESTAMP, TRUE),
(gen_random_uuid(), 'NEWUSER15', 15.00, '2025-10-01 00:00:00', '2025-10-20 23:59:59', CURRENT_TIMESTAMP, TRUE),
(gen_random_uuid(), 'RENTAL20', 20.00, '2025-10-05 00:00:00', '2025-10-25 23:59:59', CURRENT_TIMESTAMP, TRUE),
(gen_random_uuid(), 'SAVE50', 50.00, '2025-10-10 00:00:00', '2025-10-12 23:59:59', CURRENT_TIMESTAMP, TRUE);

-- ---------------------------
-- Orders (10 sample)
-- Note: base_price included
-- ---------------------------
INSERT INTO "Orders" (order_id, customer_id, vehicle_id, order_date, start_time, end_time, base_price, total_price, status, promotion_id, staff_id, created_at, isActive) VALUES
(gen_random_uuid(), (SELECT account_id FROM "Accounts" WHERE username = 'john_doe' LIMIT 1),  (SELECT vehicle_id FROM "Vehicles" WHERE serial_number = 'SN123456' LIMIT 1), '2025-10-12 10:00:00', '2025-10-12 12:00:00', '2025-10-12 14:00:00', 25.00, 30.00, 'COMPLETED', (SELECT promotion_id FROM "Promotions" WHERE promo_code = 'SAVE10' LIMIT 1), (SELECT account_id FROM "Accounts" WHERE username = 'bob_jones' LIMIT 1), CURRENT_TIMESTAMP, TRUE),
(gen_random_uuid(), (SELECT account_id FROM "Accounts" WHERE username = 'jane_smith' LIMIT 1),  (SELECT vehicle_id FROM "Vehicles" WHERE serial_number = 'SN123457' LIMIT 1), '2025-10-12 11:00:00', '2025-10-12 13:00:00', '2025-10-12 15:00:00', 24.00, 29.00, 'PENDING', NULL, (SELECT account_id FROM "Accounts" WHERE username = 'carol_white' LIMIT 1), CURRENT_TIMESTAMP, TRUE),
(gen_random_uuid(), (SELECT account_id FROM "Accounts" WHERE username = 'alice_wong' LIMIT 1),  (SELECT vehicle_id FROM "Vehicles" WHERE serial_number = 'SN123458' LIMIT 1), '2025-10-12 09:00:00', '2025-10-12 10:00:00', '2025-10-12 12:00:00', 45.00, 50.00, 'ONGOING', (SELECT promotion_id FROM "Promotions" WHERE promo_code = 'WELCOME20' LIMIT 1), (SELECT account_id FROM "Accounts" WHERE username = 'david_brown' LIMIT 1), CURRENT_TIMESTAMP, TRUE),
(gen_random_uuid(), (SELECT account_id FROM "Accounts" WHERE username = 'john_doe' LIMIT 1),  (SELECT vehicle_id FROM "Vehicles" WHERE serial_number = 'SN123459' LIMIT 1), '2025-10-12 08:00:00', '2025-10-12 09:00:00', '2025-10-12 11:00:00', 40.00, 44.00, 'COMPLETED', NULL, (SELECT account_id FROM "Accounts" WHERE username = 'bob_jones' LIMIT 1), CURRENT_TIMESTAMP, TRUE),
(gen_random_uuid(), (SELECT account_id FROM "Accounts" WHERE username = 'jane_smith' LIMIT 1),  (SELECT vehicle_id FROM "Vehicles" WHERE serial_number = 'SN123460' LIMIT 1), '2025-10-12 14:00:00', '2025-10-12 15:00:00', '2025-10-12 17:00:00', 36.00, 40.00, 'PENDING', (SELECT promotion_id FROM "Promotions" WHERE promo_code = 'FLASH30' LIMIT 1), (SELECT account_id FROM "Accounts" WHERE username = 'carol_white' LIMIT 1), CURRENT_TIMESTAMP, TRUE),
(gen_random_uuid(), (SELECT account_id FROM "Accounts" WHERE username = 'alice_wong' LIMIT 1),  (SELECT vehicle_id FROM "Vehicles" WHERE serial_number = 'SN123461' LIMIT 1), '2025-10-12 12:00:00', '2025-10-12 13:00:00', '2025-10-12 15:00:00', 35.00, 39.00, 'ONGOING', NULL, (SELECT account_id FROM "Accounts" WHERE username = 'david_brown' LIMIT 1), CURRENT_TIMESTAMP, TRUE),
(gen_random_uuid(), (SELECT account_id FROM "Accounts" WHERE username = 'john_doe' LIMIT 1),  (SELECT vehicle_id FROM "Vehicles" WHERE serial_number = 'SN123462' LIMIT 1), '2025-10-12 07:00:00', '2025-10-12 08:00:00', '2025-10-12 10:00:00', 55.00, 60.00, 'COMPLETED', (SELECT promotion_id FROM "Promotions" WHERE promo_code = 'SAVE50' LIMIT 1), (SELECT account_id FROM "Accounts" WHERE username = 'bob_jones' LIMIT 1), CURRENT_TIMESTAMP, TRUE),
(gen_random_uuid(), (SELECT account_id FROM "Accounts" WHERE username = 'jane_smith' LIMIT 1),  (SELECT vehicle_id FROM "Vehicles" WHERE serial_number = 'SN123463' LIMIT 1), '2025-10-12 16:00:00', '2025-10-12 17:00:00', '2025-10-12 19:00:00', 50.00, 56.00, 'PENDING', NULL, (SELECT account_id FROM "Accounts" WHERE username = 'carol_white' LIMIT 1), CURRENT_TIMESTAMP, TRUE),
(gen_random_uuid(), (SELECT account_id FROM "Accounts" WHERE username = 'alice_wong' LIMIT 1),  (SELECT vehicle_id FROM "Vehicles" WHERE serial_number = 'SN123464' LIMIT 1), '2025-10-12 10:00:00', '2025-10-12 11:00:00', '2025-10-12 13:00:00', 18.00, 20.00, 'ONGOING', (SELECT promotion_id FROM "Promotions" WHERE promo_code = 'NEWUSER15' LIMIT 1), (SELECT account_id FROM "Accounts" WHERE username = 'david_brown' LIMIT 1), CURRENT_TIMESTAMP, TRUE),
(gen_random_uuid(), (SELECT account_id FROM "Accounts" WHERE username = 'john_doe' LIMIT 1),  (SELECT vehicle_id FROM "Vehicles" WHERE serial_number = 'SN123465' LIMIT 1), '2025-10-12 13:00:00', '2025-10-12 14:00:00', '2025-10-12 16:00:00', 32.00, 36.00, 'COMPLETED', NULL, (SELECT account_id FROM "Accounts" WHERE username = 'bob_jones' LIMIT 1), CURRENT_TIMESTAMP, TRUE);

-- ---------------------------
-- Payments (mix of DEPOSIT / FINAL / REFUND; PENDING/COMPLETED)
-- Note: use subselects with LIMIT 1 to link to orders above
-- ---------------------------

-- Order total 30.00: DEPOSIT completed + FINAL completed
INSERT INTO "Payments" (payment_id, order_id, amount, payment_date, payment_method, payment_type, status, gateway_tx_id, gateway_response, created_at, isActive)
VALUES (
  gen_random_uuid(),
  (SELECT order_id FROM "Orders" WHERE total_price = 30.00 LIMIT 1),
  5.00,
  '2025-10-12 12:05:00',
  'MOMO',
  'DEPOSIT',
  'COMPLETED',
  'momo_tx_dep_1001',
  '{"mock":"deposit completed"}'::jsonb,
  CURRENT_TIMESTAMP,
  TRUE
),
(
  gen_random_uuid(),
  (SELECT order_id FROM "Orders" WHERE total_price = 30.00 LIMIT 1),
  25.00,
  '2025-10-12 14:30:00',
  'Credit Card',
  'FINAL',
  'COMPLETED',
  'cc_tx_2001',
  '{"mock":"final completed"}'::jsonb,
  CURRENT_TIMESTAMP,
  TRUE
);

-- Order total 29.00 (PENDING): deposit PENDING (not yet charged)
INSERT INTO "Payments" (payment_id, order_id, amount, payment_date, payment_method, payment_type, status, created_at, isActive)
VALUES (
  gen_random_uuid(),
  (SELECT order_id FROM "Orders" WHERE total_price = 29.00 LIMIT 1),
  5.00,
  CURRENT_TIMESTAMP,
  'MOMO',
  'DEPOSIT',
  'PENDING',
  CURRENT_TIMESTAMP,
  TRUE
);

-- Order total 50.00 (ONGOING): deposit completed + final pending
INSERT INTO "Payments" (payment_id, order_id, amount, payment_date, payment_method, payment_type, status, gateway_tx_id, gateway_response, created_at, isActive)
VALUES (
  gen_random_uuid(),
  (SELECT order_id FROM "Orders" WHERE total_price = 50.00 LIMIT 1),
  20.00,
  '2025-10-12 10:05:00',
  'MOMO',
  'DEPOSIT',
  'COMPLETED',
  'momo_tx_dep_1002',
  '{"mock":"deposit ok"}'::jsonb,
  CURRENT_TIMESTAMP,
  TRUE
),
(
  gen_random_uuid(),
  (SELECT order_id FROM "Orders" WHERE total_price = 50.00 LIMIT 1),
  30.00,
  CURRENT_TIMESTAMP,
  'MOMO',
  'FINAL',
  'PENDING',
  NULL,
  NULL,
  CURRENT_TIMESTAMP,
  TRUE
);

-- Order total 44.00 (COMPLETED): deposit completed + final completed
INSERT INTO "Payments" (payment_id, order_id, amount, payment_date, payment_method, payment_type, status, gateway_tx_id, gateway_response, created_at, isActive)
VALUES (
  gen_random_uuid(),
  (SELECT order_id FROM "Orders" WHERE total_price = 44.00 LIMIT 1),
  10.00,
  '2025-10-12 09:05:00',
  'MOMO',
  'DEPOSIT',
  'COMPLETED',
  'momo_tx_dep_1003',
  '{"mock":"deposit ok"}'::jsonb,
  CURRENT_TIMESTAMP,
  TRUE
),
(
  gen_random_uuid(),
  (SELECT order_id FROM "Orders" WHERE total_price = 44.00 LIMIT 1),
  34.00,
  '2025-10-12 11:30:00',
  'Bank Transfer',
  'FINAL',
  'COMPLETED',
  'bank_tx_3001',
  '{"mock":"final ok"}'::jsonb,
  CURRENT_TIMESTAMP,
  TRUE
);

-- Order total 40.00 (PENDING): no payments yet (simulate not charged)

-- Order total 39.00 (ONGOING): deposit completed + final pending
INSERT INTO "Payments" (payment_id, order_id, amount, payment_date, payment_method, payment_type, status, gateway_tx_id, gateway_response, created_at, isActive)
VALUES (
  gen_random_uuid(),
  (SELECT order_id FROM "Orders" WHERE total_price = 39.00 LIMIT 1),
  10.00,
  '2025-10-12 12:05:00',
  'MOMO',
  'DEPOSIT',
  'COMPLETED',
  'momo_tx_dep_1004',
  '{"mock":"deposit ok"}'::jsonb,
  CURRENT_TIMESTAMP,
  TRUE
),
(
  gen_random_uuid(),
  (SELECT order_id FROM "Orders" WHERE total_price = 39.00 LIMIT 1),
  29.00,
  CURRENT_TIMESTAMP,
  'MOMO',
  'FINAL',
  'PENDING',
  NULL,
  NULL,
  CURRENT_TIMESTAMP,
  TRUE
);

-- Order total 60.00 (COMPLETED): deposit completed + final completed
INSERT INTO "Payments" (payment_id, order_id, amount, payment_date, payment_method, payment_type, status, gateway_tx_id, gateway_response, created_at, isActive)
VALUES (
  gen_random_uuid(),
  (SELECT order_id FROM "Orders" WHERE total_price = 60.00 LIMIT 1),
  30.00,
  '2025-10-12 08:05:00',
  'MOMO',
  'DEPOSIT',
  'COMPLETED',
  'momo_tx_dep_1005',
  '{"mock":"deposit ok"}'::jsonb,
  CURRENT_TIMESTAMP,
  TRUE
),
(
  gen_random_uuid(),
  (SELECT order_id FROM "Orders" WHERE total_price = 60.00 LIMIT 1),
  30.00,
  '2025-10-12 10:30:00',
  'Credit Card',
  'FINAL',
  'COMPLETED',
  'cc_tx_2002',
  '{"mock":"final ok"}'::jsonb,
  CURRENT_TIMESTAMP,
  TRUE
);

-- Order total 56.00 (PENDING): no payments yet

-- Order total 20.00 (ONGOING): deposit completed + final pending
INSERT INTO "Payments" (payment_id, order_id, amount, payment_date, payment_method, payment_type, status, gateway_tx_id, gateway_response, created_at, isActive)
VALUES (
  gen_random_uuid(),
  (SELECT order_id FROM "Orders" WHERE total_price = 20.00 LIMIT 1),
  5.00,
  '2025-10-12 11:05:00',
  'MOMO',
  'DEPOSIT',
  'COMPLETED',
  'momo_tx_dep_1006',
  '{"mock":"deposit ok"}'::jsonb,
  CURRENT_TIMESTAMP,
  TRUE
),
(
  gen_random_uuid(),
  (SELECT order_id FROM "Orders" WHERE total_price = 20.00 LIMIT 1),
  15.00,
  CURRENT_TIMESTAMP,
  'MOMO',
  'FINAL',
  'PENDING',
  NULL,
  NULL,
  CURRENT_TIMESTAMP,
  TRUE
);

-- Order total 36.00 (COMPLETED): final completed (no deposit)
INSERT INTO "Payments" (payment_id, order_id, amount, payment_date, payment_method, payment_type, status, gateway_tx_id, gateway_response, created_at, isActive)
VALUES (
  gen_random_uuid(),
  (SELECT order_id FROM "Orders" WHERE total_price = 36.00 LIMIT 1),
  36.00,
  '2025-10-12 16:30:00',
  'PayPal',
  'FINAL',
  'COMPLETED',
  'paypal_tx_4001',
  '{"mock":"final completed"}'::jsonb,
  CURRENT_TIMESTAMP,
  TRUE
);

-- ---------------------------
-- Add a REFUND PENDING example (simulate cancel after completed deposit)
-- For order total 29.00 (we created a deposit PENDING earlier), to show refund flow we'll:
-- 1) create a completed deposit for another order and then create refund pending
-- ---------------------------
-- Create a completed deposit on the 29.00 order to simulate user paid earlier (update the PENDING one)
UPDATE "Payments"
SET status = 'COMPLETED', gateway_tx_id = 'momo_tx_dep_1007', gateway_response = '{"mock":"charge success"}'::jsonb, updated_at = CURRENT_TIMESTAMP
WHERE order_id = (SELECT order_id FROM "Orders" WHERE total_price = 29.00 LIMIT 1)
  AND payment_type = 'DEPOSIT'
  AND status = 'PENDING'
RETURNING payment_id;

-- Create REFUND PENDING for that order (cancel flow)
INSERT INTO "Payments" (payment_id, order_id, amount, payment_date, payment_method, payment_type, status, created_at, isActive)
VALUES (
  gen_random_uuid(),
  (SELECT order_id FROM "Orders" WHERE total_price = 29.00 LIMIT 1),
  5.00,
  CURRENT_TIMESTAMP,
  'MOMO',
  'REFUND',
  'PENDING',
  CURRENT_TIMESTAMP,
  TRUE
);

-- ---------------------------
-- Feedbacks
-- ---------------------------
INSERT INTO "Feedbacks" (feedback_id, customer_id, order_id, rating, comment, feedback_date, created_at, isActive) VALUES
(gen_random_uuid(), (SELECT account_id FROM "Accounts" WHERE username = 'john_doe' LIMIT 1),  (SELECT order_id FROM "Orders" WHERE total_price = 30.00 LIMIT 1), 5, 'Great service!', '2025-10-12 14:45:00', CURRENT_TIMESTAMP, TRUE),
(gen_random_uuid(), (SELECT account_id FROM "Accounts" WHERE username = 'jane_smith' LIMIT 1), (SELECT order_id FROM "Orders" WHERE total_price = 29.00 LIMIT 1), 4, 'Good but slow', '2025-10-12 15:45:00', CURRENT_TIMESTAMP, TRUE),
(gen_random_uuid(), (SELECT account_id FROM "Accounts" WHERE username = 'alice_wong' LIMIT 1), (SELECT order_id FROM "Orders" WHERE total_price = 50.00 LIMIT 1), 5, 'Amazing vehicle!', '2025-10-12 12:45:00', CURRENT_TIMESTAMP, TRUE),
(gen_random_uuid(), (SELECT account_id FROM "Accounts" WHERE username = 'john_doe' LIMIT 1),  (SELECT order_id FROM "Orders" WHERE total_price = 44.00 LIMIT 1), 3, 'Battery was low', '2025-10-12 11:45:00', CURRENT_TIMESTAMP, TRUE),
(gen_random_uuid(), (SELECT account_id FROM "Accounts" WHERE username = 'jane_smith' LIMIT 1), (SELECT order_id FROM "Orders" WHERE total_price = 40.00 LIMIT 1), 4, 'Comfortable ride', '2025-10-12 17:45:00', CURRENT_TIMESTAMP, TRUE),
(gen_random_uuid(), (SELECT account_id FROM "Accounts" WHERE username = 'alice_wong' LIMIT 1), (SELECT order_id FROM "Orders" WHERE total_price = 39.00 LIMIT 1), 5, 'Very clean', '2025-10-12 15:45:00', CURRENT_TIMESTAMP, TRUE),
(gen_random_uuid(), (SELECT account_id FROM "Accounts" WHERE username = 'john_doe' LIMIT 1),  (SELECT order_id FROM "Orders" WHERE total_price = 60.00 LIMIT 1), 4, 'Good truck', '2025-10-12 10:45:00', CURRENT_TIMESTAMP, TRUE),
(gen_random_uuid(), (SELECT account_id FROM "Accounts" WHERE username = 'jane_smith' LIMIT 1), (SELECT order_id FROM "Orders" WHERE total_price = 56.00 LIMIT 1), 3, 'Late delivery', '2025-10-12 19:45:00', CURRENT_TIMESTAMP, TRUE),
(gen_random_uuid(), (SELECT account_id FROM "Accounts" WHERE username = 'alice_wong' LIMIT 1), (SELECT order_id FROM "Orders" WHERE total_price = 20.00 LIMIT 1), 5, 'Fun motorcycle!', '2025-10-12 13:45:00', CURRENT_TIMESTAMP, TRUE),
(gen_random_uuid(), (SELECT account_id FROM "Accounts" WHERE username = 'john_doe' LIMIT 1),  (SELECT order_id FROM "Orders" WHERE total_price = 36.00 LIMIT 1), 4, 'Nice car', '2025-10-12 16:45:00', CURRENT_TIMESTAMP, TRUE);

-- ---------------------------
-- Reports
-- ---------------------------
INSERT INTO "Reports" (report_id, report_type, generated_date, text, account_id, vehicle_id, created_at, isActive) VALUES
(gen_random_uuid(), 'Maintenance', '2025-10-12 09:00:00', 'Vehicle needs tire replacement', (SELECT account_id FROM "Accounts" WHERE username = 'bob_jones' LIMIT 1), (SELECT vehicle_id FROM "Vehicles" WHERE serial_number = 'SN123456' LIMIT 1), CURRENT_TIMESTAMP, TRUE),
(gen_random_uuid(), 'Incident', '2025-10-12 10:00:00', 'Minor scratch reported', (SELECT account_id FROM "Accounts" WHERE username = 'carol_white' LIMIT 1), (SELECT vehicle_id FROM "Vehicles" WHERE serial_number = 'SN123457' LIMIT 1), CURRENT_TIMESTAMP, TRUE),
(gen_random_uuid(), 'Usage', '2025-10-12 11:00:00', 'High battery usage', (SELECT account_id FROM "Accounts" WHERE username = 'david_brown' LIMIT 1), (SELECT vehicle_id FROM "Vehicles" WHERE serial_number = 'SN123458' LIMIT 1), CURRENT_TIMESTAMP, TRUE),
(gen_random_uuid(), 'Maintenance', '2025-10-12 12:00:00', 'Battery check required', (SELECT account_id FROM "Accounts" WHERE username = 'bob_jones' LIMIT 1), (SELECT vehicle_id FROM "Vehicles" WHERE serial_number = 'SN123459' LIMIT 1), CURRENT_TIMESTAMP, TRUE),
(gen_random_uuid(), 'Incident', '2025-10-12 13:00:00', 'Bumper damage', (SELECT account_id FROM "Accounts" WHERE username = 'carol_white' LIMIT 1), (SELECT vehicle_id FROM "Vehicles" WHERE serial_number = 'SN123460' LIMIT 1), CURRENT_TIMESTAMP, TRUE),
(gen_random_uuid(), 'Usage', '2025-10-12 14:00:00', 'Low fuel efficiency', (SELECT account_id FROM "Accounts" WHERE username = 'david_brown' LIMIT 1), (SELECT vehicle_id FROM "Vehicles" WHERE serial_number = 'SN123461' LIMIT 1), CURRENT_TIMESTAMP, TRUE),
(gen_random_uuid(), 'Maintenance', '2025-10-12 15:00:00', 'Brake inspection needed', (SELECT account_id FROM "Accounts" WHERE username = 'bob_jones' LIMIT 1), (SELECT vehicle_id FROM "Vehicles" WHERE serial_number = 'SN123462' LIMIT 1), CURRENT_TIMESTAMP, TRUE),
(gen_random_uuid(), 'Incident', '2025-10-12 16:00:00', 'Window crack reported', (SELECT account_id FROM "Accounts" WHERE username = 'carol_white' LIMIT 1), (SELECT vehicle_id FROM "Vehicles" WHERE serial_number = 'SN123463' LIMIT 1), CURRENT_TIMESTAMP, TRUE),
(gen_random_uuid(), 'Usage', '2025-10-12 17:00:00', 'High mileage recorded', (SELECT account_id FROM "Accounts" WHERE username = 'david_brown' LIMIT 1), (SELECT vehicle_id FROM "Vehicles" WHERE serial_number = 'SN123464' LIMIT 1), CURRENT_TIMESTAMP, TRUE),
(gen_random_uuid(), 'Maintenance', '2025-10-12 18:00:00', 'Oil change required', (SELECT account_id FROM "Accounts" WHERE username = 'bob_jones' LIMIT 1), (SELECT vehicle_id FROM "Vehicles" WHERE serial_number = 'SN123465' LIMIT 1), CURRENT_TIMESTAMP, TRUE);

-- ---------------------------
-- Staff_Revenues
-- ---------------------------
INSERT INTO "Staff_Revenues" (staff_revenue_id, staff_id, revenue_date, total_revenue, commission, created_at, isActive) VALUES
(gen_random_uuid(), (SELECT account_id FROM "Accounts" WHERE username = 'bob_jones' LIMIT 1), '2025-10-12 00:00:00', 500.00, 50.00, CURRENT_TIMESTAMP, TRUE),
(gen_random_uuid(), (SELECT account_id FROM "Accounts" WHERE username = 'carol_white' LIMIT 1), '2025-10-12 00:00:00', 450.00, 45.00, CURRENT_TIMESTAMP, TRUE),
(gen_random_uuid(), (SELECT account_id FROM "Accounts" WHERE username = 'david_brown' LIMIT 1), '2025-10-12 00:00:00', 600.00, 60.00, CURRENT_TIMESTAMP, TRUE),
(gen_random_uuid(), (SELECT account_id FROM "Accounts" WHERE username = 'bob_jones' LIMIT 1), '2025-10-11 00:00:00', 400.00, 40.00, CURRENT_TIMESTAMP, TRUE),
(gen_random_uuid(), (SELECT account_id FROM "Accounts" WHERE username = 'carol_white' LIMIT 1), '2025-10-11 00:00:00', 300.00, 30.00, CURRENT_TIMESTAMP, TRUE),
(gen_random_uuid(), (SELECT account_id FROM "Accounts" WHERE username = 'david_brown' LIMIT 1), '2025-10-11 00:00:00', 550.00, 55.00, CURRENT_TIMESTAMP, TRUE),
(gen_random_uuid(), (SELECT account_id FROM "Accounts" WHERE username = 'bob_jones' LIMIT 1), '2025-10-10 00:00:00', 350.00, 35.00, CURRENT_TIMESTAMP, TRUE),
(gen_random_uuid(), (SELECT account_id FROM "Accounts" WHERE username = 'carol_white' LIMIT 1), '2025-10-10 00:00:00', 400.00, 40.00, CURRENT_TIMESTAMP, TRUE),
(gen_random_uuid(), (SELECT account_id FROM "Accounts" WHERE username = 'david_brown' LIMIT 1), '2025-10-10 00:00:00', 500.00, 50.00, CURRENT_TIMESTAMP, TRUE),
(gen_random_uuid(), (SELECT account_id FROM "Accounts" WHERE username = 'bob_jones' LIMIT 1), '2025-10-09 00:00:00', 450.00, 45.00, CURRENT_TIMESTAMP, TRUE);

