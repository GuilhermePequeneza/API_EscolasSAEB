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
                          // Se o seu frontend estiver em 'courageous-nurturing-production-xxxx.up.railway.app'
                          // e/ou no domínio personalizado 'escolafinder.com', adicione-os aqui.
                          policy.WithOrigins(
                              "https://escolafinder.up.railway.app", // Provável URL do seu FRONTEND
                              "https://prolific-delight-production-432f.up.railway.app", // Exemplo, verifique o seu
                              "https://escolafinder.com", // Seu domínio personalizado do FRONTEND
                              "http://localhost:3000", // Para desenvolvimento local do frontend
                              "http://localhost:5173" // Para desenvolvimento local do frontend (Vite)
                          )
                                .AllowAnyHeader()
                                .AllowAnyMethod()
                                .AllowCredentials();
                      });
});

// Pegue a string de conexão do Railway
// Certifique-se de que a variável de ambiente está correta,
// 'mysql-c2ad.railway.internal:3306_ferrovia' parece um nome de variável de ambiente,
// mas a string de conexão real seria algo como "Server=mysql-c2ad.railway.internal;Port=3306;Database=seu_db;Uid=seu_user;Pwd=sua_senha;"
var connectionString = Environment.GetEnvironmentVariable("MYSQL_URL");

if (!string.IsNullOrEmpty(connectionString) && connectionString.StartsWith("mysql://"))
{
    // Converte de mysql://user:pass@host:port/db para Connection String
    var uri = new Uri(connectionString);
    var userInfo = uri.UserInfo.Split(':');
    connectionString = $"Server={uri.Host};Port={uri.Port};Database={uri.AbsolutePath.TrimStart('/')};Uid={userInfo[0]};Pwd={userInfo[1]};SslMode=Preferred;";
}
else
{
    // Fallback para desenvolvimento local
    connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
}

// --- ADICIONE ESTA PARTE PARA CONFIGURAR A PORTA DO KESTREL ---
// Configurar Kestrel para ouvir na porta do Railway
var port = Environment.GetEnvironmentVariable("PORT") ?? "8080"; // Fallback para 8080 localmente
builder.WebHost.UseUrls($"http://*:{port}"); // Importante: Use HTTP internamente no Railway
// -------------------------------------------------------------

// Add services to the container.
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.TypeInfoResolverChain.Clear();
        options.JsonSerializerOptions.TypeInfoResolverChain.Add(new DefaultJsonTypeInfoResolver());
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
else
{
    // Em produção, desative ou adapte o redirecionamento HTTPS.
    // O Railway já lida com HTTPS, então seu app deve ouvir HTTP internamente.
    // Remova ou comente app.UseHttpsRedirection()
    // app.UseHttpsRedirection(); // <--- COMENTE OU REMOVA ESTA LINHA EM PRODUÇÃO
}

app.UseCors("AllowReactApp");
app.UseAuthorization();
app.MapControllers();

app.Run();