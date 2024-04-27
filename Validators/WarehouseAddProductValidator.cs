using APBD7.DTOs;

namespace APBD7.Validators;
using FluentValidation;

public class WarehouseAddProductValidator: AbstractValidator<WarehouseDTOs>
{
        public WarehouseAddProductValidator()
        {
            RuleFor(e => e.IdProduct).NotNull().NotEmpty();
            RuleFor(e => e.IdWarehouse).NotNull().NotEmpty();
            RuleFor(e => e.Amount).NotNull().NotEmpty().GreaterThan(0);
            RuleFor(e => e.CreatedAt).NotNull().NotEmpty().LessThanOrEqualTo(DateTime.Now);
        }
}