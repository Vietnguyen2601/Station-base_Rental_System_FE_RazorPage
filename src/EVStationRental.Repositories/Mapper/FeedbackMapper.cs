using EVStationRental.Common.DTOs.FeedbackDTOs;
using EVStationRental.Common.DTOs.VehicleTypeDTOs;
using EVStationRental.Repositories.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EVStationRental.Repositories.Mapper
{
    public static class FeedbackMapper
    {
        public static ViewFeedbackResponseDTO ToViewFeedbackDTO(this Feedback feedback)
        {
            return new ViewFeedbackResponseDTO
            {
                FeedbackId = feedback.FeedbackId,
                CustomerId = feedback.CustomerId,
                OrderId = feedback.OrderId,
                Rating = feedback.Rating,
                Comment = feedback.Comment,
                CreatedAt = feedback.CreatedAt,
                UpdatedAt = feedback.UpdatedAt,
                IsActive = feedback.Isactive
            };
        }

        public static Feedback ToFeedback(this CreateFeedbackRequestDTO dto)
        {
            return new Feedback
            {
                FeedbackId = Guid.NewGuid(),
                CustomerId = dto.CustomerId,
                OrderId = dto.OrderId,
                Rating = dto.Rating,
                Comment = dto.Comment,
                FeedbackDate = DateTime.Now,
                CreatedAt = DateTime.Now,
                Isactive = true
            };
        }

    }
}
