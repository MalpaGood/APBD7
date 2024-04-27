using APBD7.EndPoints;
using APBD7.Validators;
using APBD7.Service;
using FluentValidation;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddScoped<IDbServiceDapper, DBServiceDapper>();
builder.Services.AddValidatorsFromAssemblyContaining<WarehouseAddProductValidator>();

    
var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.RegisterWarehouseEndpoints();

app.Run();
