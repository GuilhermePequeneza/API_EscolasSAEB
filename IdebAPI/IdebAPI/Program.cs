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
                          // A porta 3000 é a padrão do create-react-app.
                          policy.WithOrigins("http://localhost:3000")
                                .AllowAnyHeader()
                                .AllowAnyMethod();
                      });
});

// Add services to the container.
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // Configurar para usar reflexão ao invés de source generation
        options.JsonSerializerOptions.TypeInfoResolverChain.Clear();
        options.JsonSerializerOptions.TypeInfoResolverChain.Add(new DefaultJsonTypeInfoResolver());

        // Opcional: configurações adicionais
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