using APBD7.DTOs;
using APBD7.Models;
using APBD7.Service;
using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;

namespace APBD7.EndPoints;


public static class WarehouseEndpoints
{
    public static void RegisterWarehouseEndpoints(this WebApplication app)
    {
        app.MapPost("api/WarehouseAdd", WarehouseAdd);
    }
    

    public static async Task<IResult> WarehouseAdd(WarehouseDTOs request, IDbServiceDapper db,
        IValidator<WarehouseDTOs> validator)
    {
        var validation = await validator.ValidateAsync(request);
        if (!validation.IsValid)
        {
            return Results.ValidationProblem(validation.ToDictionary());
        }

        var product = await db.ValidateProductAndWarehouse(request.IdProduct, request.IdWarehouse);
        if (product is false)
        {
            return Results.Problem();
        }

        var order = await db.CanAddProductToWarehouse(request.IdProduct, request.Amount, DateTime.Now);
        if (order is null)
        {
            return Results.NotFound();
        }

        var isRealised = await db.IsOrderDone(order.IdOrder);
        if (isRealised is false)
        {
            return Results.Problem();
        }

        if (order.CreatedAt > request.CreatedAt)
        {
            return Results.Conflict();
        }

        var update = await db.UpdatedOrderFullfiledAt(order.IdOrder);
        var wstaw = await db.InsertProductToWarehouse(request.IdProduct, order.IdOrder, request.IdWarehouse);
        var last = await db.GetLatestWarehouseRecordId();
        return Results.Created("", last);
    }
}