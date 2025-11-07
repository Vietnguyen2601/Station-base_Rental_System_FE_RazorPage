using System;

namespace EVStationRental.Common.Enums.EnumModel
{
    public enum TransactionType
    {
        DEPOSIT,    // Nạp tiền hoặc trừ tiền cọc
        PAYMENT,    // Thanh toán (trừ tiền còn lại)
        REFUND      // Hoàn tiền
    }
}
