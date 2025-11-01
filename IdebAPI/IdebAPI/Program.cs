using OfficeOpenXml;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

var builder = WebApplication.CreateBuilder(args);
ExcelPackage.License.SetNonCommercialOrganization("EDwatch");

builder.Services.AddCors(options =>
{
    options.AddPolicy(name: "AllowReactApp",
                      policy =>
                      {
                          // Substitua pela URL do seu front-end React. 
                          // A porta 3000 � a padr�o do create-react-app.
                          policy.WithOrigins("https://escolafinder.up.railway.app")
                                .AllowAnyHeader()
                                .AllowAnyMethod();
                      });
});

// Pegue a string de conexão do Railway
var connectionString = Environment.GetEnvironmentVariable("mysql-c2ad.railway.internal:3306_ferrovia");

// Se não estiver no Railway (rodando local), pegue do appsettings.json
if (string.IsNullOrEmpty(connectionString))
{
    connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
}

// Add services to the container.
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // Configurar para usar reflex�o ao inv�s de source generation
        options.JsonSerializerOptions.TypeInfoResolverChain.Clear();
        options.JsonSerializerOptions.TypeInfoResolverChain.Add(new DefaultJsonTypeInfoResolver());

        // Opcional: configura��es adicionais
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.WriteIndented = true;
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddScoped<IdebAPI.Models.IIdebService, IdebAPI.Models.IdebService>();
builder.Services.AddScoped<Cadastro.Models.ICadastroDAO, Cadastro.Models.CadastroDAO>(); 

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage(); // Mostra erros detalhados
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("AllowReactApp");
app.UseAuthorization();
app.MapControllers();

app.Run();