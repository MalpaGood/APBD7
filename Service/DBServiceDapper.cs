using System.Data;
using System.Data.SqlClient;
using APBD7.Models;
using Dapper;
namespace APBD7.Service;


public interface IDbServiceDapper
{
    //Podstawowe
    Task<IEnumerable<Warehouse>> GetWarehouse();
    Task<IEnumerable<Product>> GetProduct();
    Task<Warehouse?> GetWarehouseById(int idWarehouse);
    Task<Product?> GetProductById(int idProduct);
    
    //Zadanie 
    
    //Popdunkt 1
    Task<bool> ValidateProductAndWarehouse(int id_produktu, int warehouseId);
    //Podpunkt 2
    Task<Order?> CanAddProductToWarehouse(int id_produktu, int amount, DateTime req);
    //Podpunkt 3
    Task<bool> IsOrderDone(int id_zamówienia);
    //Podpunkt 4
    Task<int> UpdatedOrderFullfiledAt(int id_zamówienia);
    //Podpunkt 5
    Task<int> InsertProductToWarehouse(int id_zamówienia, int productId, int warehouseId);
    //Podpunkt 6
    Task<int?> GetLatestWarehouseRecordId();
}

public class DBServiceDapper(IConfiguration configuration) : IDbServiceDapper
{
    //Podstawowe metody asynchroniczne
    private async Task<SqlConnection> GetConnection()
    {
        var connection = new SqlConnection(configuration.GetConnectionString("Default"));
        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync();
        }

        return connection;
    }
    
    public async Task<IEnumerable<Warehouse?>>  GetWarehouse()
    {
        await using var connection = await GetConnection();

        var warehouses = await connection.QueryAsync<Warehouse>(
            "select * from Warehouse ");
        return warehouses;
    }
    
    public async Task<IEnumerable<Product?>>  GetProduct()
    {
        await using var connection = await GetConnection();

        var warehouses = await connection.QueryAsync<Product>(
            "select * from Product ");
        return warehouses;
    }

    public async Task<Warehouse?> GetWarehouseById(int id_warehouse)
    {
        await using var connection = await GetConnection();

        var warehouses = await connection.QueryFirstOrDefaultAsync<Warehouse>(
            "select * from Warehouse where id_warehouse=@id_warehouse",
            new { id_warehouse = @id_warehouse });
        return warehouses;
    }
    
    public async Task<Product?> GetProductById(int id_product)
    {
        await using var connection = await GetConnection();

        var warehouses = await connection.QueryFirstOrDefaultAsync<Product>(
            "select * from Product where id_product=@id_product",
            new { id_product = @id_product });
        return warehouses;
    }

    //Podpunkt 1
    public async Task<bool> ValidateProductAndWarehouse(int id_produktu, int warehouseId)
    {

        await using var connection = await GetConnection();

        var productExists = await connection.ExecuteScalarAsync<bool>(
            "SELECT CASE WHEN EXISTS (SELECT 1 FROM Products WHERE Id = @id_produktu) THEN CAST(1 AS BIT) ELSE CAST(0 AS BIT) END", 
            new { Id_produktu = id_produktu }
        );

        if (!productExists)
        {
            return false;
        }
        
        var warehouseExists = await connection.ExecuteScalarAsync<bool>(
            "SELECT CASE WHEN EXISTS (SELECT 1 FROM Warehouses WHERE Id = @WarehouseId) THEN CAST(1 AS BIT) ELSE CAST(0 AS BIT) END", 
            new { WarehouseId = warehouseId }
        );

        if (!warehouseExists)
        {
            return false;
        }
        return true;
    }
    
    //Podpunkt 2
    public async Task<Order?> CanAddProductToWarehouse(int productId, int amount, DateTime requestDate)
    {
        await using var connection = await GetConnection();
            var query = @"
            SELECT TOP 1 *
            FROM [Order]
            WHERE ProductId = @ProductId
              AND Amount >= @Amount
              AND CreatedAt < @RequestDate
            ORDER BY CreatedAt DESC;"; 

            var order = await connection.QueryFirstOrDefaultAsync<Order>(query, new 
            {
                ProductId = productId,
                Amount = amount,
                RequestDate = requestDate
            });

            return order; 
    }
    
    //Podpunkt 3
    public async Task<bool> IsOrderDone(int id_zamówienia)
    {
        await using var connection = await GetConnection();

        var exist = await connection.ExecuteScalarAsync<bool>(
            "SELECT CASE WHEN EXISTS (SELECT 1 FROM Product_Warehouse WHERE SELECT CASE WHEN EXISTS " +
            "(SELECT 1 FROM Product_Warehouse WHERE id_zamówienia = @id_zamówienia) THEN CAST(1 AS BIT) ELSE CAST(0 AS BIT) END",
            new { Id_zamówienia = id_zamówienia });
        return exist;
    }

    //Podpunkt 4
    public async Task<int> UpdatedOrderFullfiledAt(int id_zamówienia)
    {
        await using var connection = await GetConnection();
        using (var transaction = connection.BeginTransaction())
        {
            try
            {
                DateTime fulfillmentDate = DateTime.Now;
                var affectedRows = await connection.ExecuteAsync(
                    "UPDATE [Order] SET FulfilledAt = @FulfillmentDate WHERE Id = @OrderId",
                    new { FulfillmentDate = fulfillmentDate, Id_zamówienia = id_zamówienia },
                    transaction: transaction
                );

                transaction.Commit();
                return affectedRows;
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }
    }
    
    //Podpunkt 5
    public async Task<int> InsertProductToWarehouse(int id_zamówienia, int productId, int warehouseId)
    {
        await using var connection = await GetConnection();
        using (var transaction = connection.BeginTransaction())
        {
            try
            {
                var orderDetails = await connection.QueryFirstOrDefaultAsync(
                    "SELECT Price, Amount FROM [Order] WHERE Id = @OrderId",
                    new { Id_zamówienia = id_zamówienia },
                    transaction: transaction
                );

                if (orderDetails == null)
                {
                    throw new Exception("Order details not found.");
                }

                decimal price = orderDetails.Price;
                int amount = orderDetails.Amount;
                decimal totalPrice = price * amount;

                var affectedRows = await connection.ExecuteAsync(
                    "INSERT INTO Product_Warehouse (ProductId, WarehouseId, Price, CreatedAt) VALUES (@ProductId, @WarehouseId, @TotalPrice, @CreatedAt)",
                    new { ProductId = productId, WarehouseId = warehouseId, TotalPrice = totalPrice, CreatedAt = DateTime.Now },
                    transaction: transaction
                );

                transaction.Commit();
                return affectedRows;
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }
    }

    //Podpunkt 6
    public async Task<int?> GetLatestWarehouseRecordId()
    {
        await using var connection = await GetConnection();
        
        string sqlQuery = @" SELECT TOP 1 IDFROM WarehouseORDER BY CreatedAt DESC;";
        
        var latestId = await connection.ExecuteScalarAsync<int?>(sqlQuery);

        return latestId;
    }
}