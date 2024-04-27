using System.ComponentModel.DataAnnotations;

namespace APBD7.DTOs;

public record WarehouseDTOs(int IdProduct, int IdWarehouse, int Amount, DateTime CreatedAt);