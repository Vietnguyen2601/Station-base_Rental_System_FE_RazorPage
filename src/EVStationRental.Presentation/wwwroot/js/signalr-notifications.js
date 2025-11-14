(function () {
    if (!window.signalR) {
        console.warn("SignalR client library is missing. Realtime updates are disabled.");
        return;
    }

    const formatCurrency = (value) => {
        const number = typeof value === "number" ? value : Number(value || 0);
        return number.toLocaleString("vi-VN") + " ₫";
    };

    const isCurrentPath = (segment) => window.location.pathname.toLowerCase().startsWith(segment);

    const connection = new signalR.HubConnectionBuilder()
        .withUrl("/hubs/realtime")
        .withAutomaticReconnect()
        .build();

    connection.on("WalletUpdated", (payload) => {
        if (!payload) {
            return;
        }

        const headerBalance = document.querySelector("[data-wallet-balance-header]");
        if (headerBalance) {
            headerBalance.textContent = formatCurrency(payload.newBalance);
        }

        const pageBalance = document.querySelector("[data-wallet-balance-page]");
        if (pageBalance) {
            pageBalance.textContent = formatCurrency(payload.newBalance);
        }

        const historyContainer = document.querySelector("[data-wallet-history-table]");
        if (historyContainer) {
            window.location.reload();
        }
    });

    connection.on("OrderCreated", (order) => {
        if (!order) {
            return;
        }

        if (isCurrentPath("/orders/my")) {
            window.location.reload();
        }
    });

    connection.on("OrderStatusChanged", (order) => {
        if (!order) {
            return;
        }

        const selector = `[data-order-row='${order.orderId}']`;
        const row = document.querySelector(selector);
        if (row) {
            const statusBadge = row.querySelector("[data-order-status]");
            if (statusBadge) {
                statusBadge.textContent = order.status;
            }

            const totalCell = row.querySelector("[data-order-total]");
            if (totalCell && typeof order.totalPrice !== "undefined") {
                totalCell.textContent = formatCurrency(order.totalPrice);
            }
        } else if (isCurrentPath("/orders/my")) {
            window.location.reload();
        }
    });

    connection.on("AccountChanged", (account) => {
        if (!account || !isCurrentPath("/admin")) {
            return;
        }

        const selector = `[data-account-row='${account.userId}']`;
        const row = document.querySelector(selector);
        if (row) {
            const emailCell = row.querySelector("[data-account-email]");
            if (emailCell) {
                emailCell.textContent = account.email;
            }

            const nameCell = row.querySelector("[data-account-name]");
            if (nameCell) {
                nameCell.textContent = account.fullName || "";
            }

            const roleCell = row.querySelector("[data-account-role]");
            if (roleCell) {
                roleCell.textContent = account.roleName || "";
            }

            const statusCell = row.querySelector("[data-account-status]");
            if (statusCell) {
                statusCell.textContent = account.isActive ? "Hoạt động" : "Khoá";
                statusCell.classList.toggle("text-success", account.isActive);
                statusCell.classList.toggle("text-danger", !account.isActive);
            }
        } else {
            window.location.reload();
        }
    });

    connection.start().catch((err) => console.error("SignalR connection error", err));
})();
