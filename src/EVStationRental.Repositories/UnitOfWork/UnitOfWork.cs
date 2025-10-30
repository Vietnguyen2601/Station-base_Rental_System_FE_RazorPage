using System;
using System.Threading.Tasks;
using EVStationRental.Repositories.DBContext;
using EVStationRental.Repositories.IRepositories;
using Microsoft.EntityFrameworkCore;

namespace EVStationRental.Repositories.UnitOfWork
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly ElectricVehicleDBContext _context;

        private IAccountRepository accountRepository;
        private IVehicleRepository vehicleRepository;
        private IVehicleModelRepository vehicleModelRepository;
        private IStationRepository stationRepository;
        private IRoleRepository roleRepository;
        private IVehicleTypeRepository vehicleTypeRepository;
        private IPromotionRepository promotionRepository;
        private IReportRepository reportRepository;
        private IOrderRepository orderRepository;
        private IPaymentRepository paymentRepository;
        private IFeedbackRepository feedbackRepository;

        public UnitOfWork()
        {
          _context = new ElectricVehicleDBContext();
        }

        public IAccountRepository AccountRepository
        {
            get
            {
                return accountRepository ??= new Repositories.AccountRepository(_context);
            }
        }

        public IVehicleRepository VehicleRepository
        {
            get
            {
                return vehicleRepository ??= new Repositories.VehicleRepository(_context);
            }
        }

        public IVehicleModelRepository VehicleModelRepository
        {
            get
            {
                return vehicleModelRepository ??= new Repositories.VehicleModelRepository(_context);
            }
        }

        public IVehicleTypeRepository VehicleTypeRepository
        {
            get
            {
                return vehicleTypeRepository ??= new Repositories.VehicleTypeRepository(_context);
            }
        }

        public IStationRepository StationRepository
        {
            get
            {
                return stationRepository ??= new Repositories.StationRepository(_context);
            }
        }

        public IRoleRepository RoleRepository
        {
            get
            {
                return roleRepository ??= new Repositories.RoleRepository(_context);
            }
        }

        public IPromotionRepository PromotionRepository
        {
            get
            {
                return promotionRepository ??= new Repositories.PromotionRepository(_context);
            }
        }

        public IReportRepository ReportRepository
        {
            get
            {
                return reportRepository ??= new Repositories.ReportRepository(_context);
            }
        }

        public IOrderRepository OrderRepository
        {
            get
            {
                return orderRepository ??= new Repositories.OrderRepository(_context);
            }
        }

        public IPaymentRepository PaymentRepository
        {
            get
            {
                return paymentRepository ??= new Repositories.PaymentRepository(_context);
            }
        }

        public IFeedbackRepository FeedbackRepository
        {
            get
            {
                return feedbackRepository ??= new Repositories.FeedbackRepository(_context);
            }
        }
    }
}