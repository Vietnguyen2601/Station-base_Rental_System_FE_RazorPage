using EVStationRental.Repositories.IRepositories;
using System;
using System.Threading.Tasks;

namespace EVStationRental.Repositories.UnitOfWork
{
    public interface IUnitOfWork
    {
        IAccountRepository AccountRepository { get; }
        IVehicleRepository VehicleRepository { get; }
        IVehicleModelRepository VehicleModelRepository { get; }
        IStationRepository StationRepository { get; }
        IRoleRepository RoleRepository { get; }
        IVehicleTypeRepository VehicleTypeRepository { get; }
        IPromotionRepository PromotionRepository { get; }
        IReportRepository ReportRepository { get; }
        IOrderRepository OrderRepository { get; }
        IPaymentRepository PaymentRepository { get; }
    }
}