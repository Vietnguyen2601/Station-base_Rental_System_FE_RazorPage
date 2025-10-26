using System;
using System.ComponentModel.DataAnnotations;

namespace EVStationRental.Common.DTOs.VehicleDTOs
{
    public class GetVehicleByModelAndStationRequestDTO
    {
        [Required(ErrorMessage = "Model ID là b?t bu?c")]
        public Guid VehicleModelId { get; set; }

        [Required(ErrorMessage = "Station ID là b?t bu?c")]
        public Guid StationId { get; set; }
    }
}
