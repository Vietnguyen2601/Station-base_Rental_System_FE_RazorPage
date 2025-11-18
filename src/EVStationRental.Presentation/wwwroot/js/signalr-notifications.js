(function () {
    if (!window.signalR) {
        console.warn("SignalR client library is missing. Realtime updates are disabled.");
        return;
    }

    const formatCurrency = (value) => {
        const number = typeof value === "number" ? value : Number(value || 0);
        return number.toLocaleString("vi-VN") + " ₫";
    };

    const currentPath = window.location.pathname.toLowerCase();
    const isCurrentPath = (segment) => currentPath.startsWith(segment);
    const isCustomerOrdersPage = () => isCurrentPath("/orders/my");
    const isStaffOrdersPage = () => isCurrentPath("/staff/orders");
    const isStaffVehiclesPage = () => isCurrentPath("/staff/vehicles");
    const isStaffStationsPage = () => isCurrentPath("/staff/stations");
    const isAdminStationsPage = () =>
        currentPath === "/stations" || currentPath.startsWith("/stations/index");

    const orderStatusMap = {
        PENDING: { label: "Chờ xác nhận", staffClass: "bg-warning text-dark", customerClass: "badge-warning" },
        CONFIRMED: { label: "Đã xác nhận", staffClass: "bg-info text-dark", customerClass: "badge-info" },
        ONGOING: { label: "Đang thuê", staffClass: "bg-primary", customerClass: "badge-warning" },
        COMPLETED: { label: "Hoàn thành", staffClass: "bg-success", customerClass: "badge-success" },
        CANCELED: { label: "Đã hủy", staffClass: "bg-danger", customerClass: "badge-danger" },
        REFUNDED: { label: "Đã hoàn tiền", staffClass: "bg-secondary", customerClass: "badge-info" }
    };

    const updateOrderStatusDisplay = (element, status) => {
        if (!element) {
            return;
        }
        const meta = orderStatusMap[(status || "").toUpperCase()] || {
            label: status || "Không xác định",
            staffClass: "bg-secondary",
            customerClass: "badge-secondary"
        };

        if (element.tagName === "TD") {
            element.innerHTML = `<span class="badge ${meta.staffClass}">${meta.label}</span>`;
        } else {
            element.textContent = meta.label;
            if (element.classList.contains("badge")) {
                element.className = `badge ${meta.customerClass}`;
            }
        }
    };

    const showNotification = (title, message, type = 'info') => {
        const alertTypeMap = {
            'info': 'alert-info',
            'success': 'alert-success',
            'warning': 'alert-warning',
            'danger': 'alert-danger'
        };
        
        const alertClass = alertTypeMap[type] || 'alert-info';
        
        const notification = document.createElement('div');
        notification.className = `alert ${alertClass} alert-dismissible fade show position-fixed`;
        notification.style.cssText = 'top: 20px; right: 20px; z-index: 9999; min-width: 300px; max-width: 400px;';
        notification.innerHTML = `
            <strong>${title}</strong><br/>
            ${message}
            <button type="button" class="btn-close" data-bs-dismiss="alert" aria-label="Close"></button>
        `;
        
        document.body.appendChild(notification);
        
        // Auto-hide after 5 seconds
        setTimeout(() => {
            if (notification.parentNode) {
                const bsAlert = bootstrap.Alert.getOrCreateInstance(notification);
                bsAlert.close();
            }
        }, 5000);
    };

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

        // Show notification for staff
        if (isStaffOrdersPage()) {
            showNotification(
                'Đơn hàng mới',
                `Đơn hàng ${order.orderCode || ''} vừa được tạo bởi khách hàng`,
                'info'
            );
            
            // Reload after showing notification
            setTimeout(() => {
                window.location.reload();
            }, 1500);
        } else if (isCustomerOrdersPage()) {
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
            updateOrderStatusDisplay(statusBadge, order.status);

            const totalCell = row.querySelector("[data-order-total]");
            if (totalCell && typeof order.totalPrice !== "undefined") {
                totalCell.textContent = formatCurrency(order.totalPrice);
            }
        } else {
            if (isCustomerOrdersPage() || isStaffOrdersPage()) {
                window.location.reload();
            }
        }

        if (isStaffOrdersPage()) {
            const staffRow = document.querySelector(`[data-order-id='${order.orderId}']`);
            if (staffRow) {
                const statusCell = staffRow.querySelector("[data-order-status]");
                updateOrderStatusDisplay(statusCell, order.status);
                
                // Highlight the row briefly
                staffRow.classList.add('table-warning');
                setTimeout(() => {
                    staffRow.classList.remove('table-warning');
                }, 2000);
            }
            
            // Show notification
            const statusMeta = orderStatusMap[(order.status || "").toUpperCase()];
            showNotification(
                'Cập nhật đơn hàng',
                `Đơn hàng ${order.orderCode || ''} đã chuyển sang: ${statusMeta?.label || order.status}`,
                'success'
            );
        }
    });

    connection.on("OrderUpdatedByStaff", (payload) => {
        if (!payload || !isStaffOrdersPage()) {
            return;
        }

        const row = document.querySelector(`[data-order-id='${payload.orderId}']`);
        if (row) {
            const statusCell = row.querySelector("[data-order-status]");
            updateOrderStatusDisplay(statusCell, payload.newStatus);
        } else {
            window.location.reload();
        }
    });

    connection.on("VehicleUpdated", (payload) => {
        if (!payload || !isStaffVehiclesPage()) {
            return;
        }
        window.location.reload();
    });

    connection.on("StationUpdated", (payload) => {
        if (!payload || (!isAdminStationsPage() && !isStaffStationsPage())) {
            return;
        }
        window.location.reload();
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

    connection.start()
        .then(() => {
            console.log("SignalR connected successfully");
        })
        .catch((err) => {
            console.error("SignalR connection error", err);
        });

    // Export connection for page-specific usage
    window.signalRConnection = connection;
})();
