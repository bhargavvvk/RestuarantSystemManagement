using System.Text;
using Azure.Identity;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using RestaurantAPI;
using RestaurantAPI.Contexts;
using RestaurantAPI.mapper;
using RestaurantAPI.Middlewares;
using RestaurantAPI.Repositories;
using RestaurantAPI.RepositoryInterfaces;
using RestaurantAPI.ServiceInterfaces;
using RestaurantAPI.Services;
using RestaurantAPI.Services.AI.Claude;
using RestaurantAPI.Services.AI.Configuration;
using RestaurantAPI.Services.AI.Contracts;
using RestaurantAPI.Services.AI.Conversation;
using RestaurantAPI.Services.AI.Tools;
using RestaurantAPI.Services.AI.Tools.Menu;
using RestaurantAPI.Services.AI.Tools.Restaurant;
using RestaurantAPI.Services.AI;
using Serilog;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);
var keyVaultUri = builder.Configuration["KeyVaultUri"];
if (!string.IsNullOrEmpty(keyVaultUri))
{
    // If a URI is present, pull secrets from Azure Key Vault
    builder.Configuration.AddAzureKeyVault(
        new Uri(keyVaultUri),
        new DefaultAzureCredential()
    );
}
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .CreateLogger();
builder.Host.UseSerilog();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("https://localhost:4200", "http://localhost:4200","http://localhost:4201","http://localhost:4202","http://localhost:4203")
              .AllowAnyHeader()
              .AllowAnyMethod()
             .AllowCredentials();
    });
});


builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Restaurant Management API",
        Version = "v1"
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Description = "Enter JWT token",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Id = "Bearer",
                    Type = ReferenceType.SecurityScheme
                }
            },
            Array.Empty<string>()
        }
    });
});
builder.Services.AddSignalR();
builder.Services.AddAutoMapper(a => a.AddProfile<Mapping>());



#region Contexts
builder.Services.AddDbContext<RestaurantContext>(options =>
{
    options.UseNpgsql(builder.Configuration.GetConnectionString("Default"));
});
builder.Services.AddDbContext<ArchiveContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Archive")));
#endregion

#region Authenticaion
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;

}).AddJwtBearer(opts =>
{
    opts.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = false,
        ValidIssuer = builder.Configuration["JWT:Issuer"],
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["JWT:Key"])),
        ValidateLifetime = true
    };
    opts.Events
    = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var accessToken = context.Request.Query["access_token"];
            var path = context.HttpContext.Request.Path;
            if (!string.IsNullOrEmpty(accessToken) &&
                (path.StartsWithSegments("/notificationHub")))
            {
                context.Token = accessToken;
            }

            return Task.CompletedTask;
        }
    };
});

#endregion
builder.Services.Configure<AnthropicSettings>(
    builder.Configuration.GetSection(AnthropicSettings.SectionName));

builder.Services.AddHttpClient<IClaudeClient, ClaudeClient>((sp, client) =>
{
    var settings = sp.GetRequiredService<IOptions<AnthropicSettings>>().Value;
    client.BaseAddress = new Uri(settings.BaseUrl);
});

#region Repositories
builder.Services.AddScoped<IDiningSessionRepository, DiningSessionRepository>();
builder.Services.AddScoped<IRestaurentTableRepository, RestaurantTableRepository>();
builder.Services.AddScoped<ICustomerRepository, CustomerRepository>();
builder.Services.AddScoped<ICartRepository, CartRepository>();
builder.Services.AddScoped<IMenuItemRepository, MenuItemRepository>();
builder.Services.AddScoped<ICustomerRequestRepository, CustomerRequestRepository>();
builder.Services.AddScoped<ICartItemRepository, CartItemRepository>();
builder.Services.AddScoped<IBillRepository, BillRepository>();
builder.Services.AddScoped<ITaxConfigurationRepository, TaxConfigurationRepository>();
builder.Services.AddScoped<IOrderRepository, OrderRepository>();
builder.Services.AddScoped<IOrderItemRepository, OrderItemRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IAuditLogRepository, AuditLogRepository>();
builder.Services.AddScoped<IArchiveAuditLogRepository, ArchiveAuditLogRepository>();
builder.Services.AddScoped<ICategoryRepository, CategoryRepository>();
builder.Services.AddScoped<IInventoryItemRepository,InventoryItemRepository>();
builder.Services.AddScoped<IIngredientRepository, IngredientRepository>();
builder.Services.AddScoped<IMenuItemIngredientRepository, MenuItemIngredientRepository>();
builder.Services.AddScoped<IMenuItemNutritionRepository, MenuItemNutritionRepository>();
builder.Services.AddScoped<IRestaurantConfigurationRepository, RestaurantConfigurationRepository>();
#endregion

#region Services
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IEncryptionService, EncryptionService>();
builder.Services.AddScoped<IMenuService, MenuService>();
builder.Services.AddScoped<ICartService, CartService>();
builder.Services.AddScoped<IIOrderService, OrderService>();
builder.Services.AddScoped<IBillService, BillService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<ITableService, TableService>();
builder.Services.AddScoped<IWaiterService, WaiterService>();
builder.Services.AddScoped<ICustomerRequestService, CustomerRequestService>();
builder.Services.AddScoped<IDiningSessionService, DiningSessionService>();
builder.Services.AddScoped<IAuditService, AuditService>();
builder.Services.AddScoped<IAuditArchiveService, AuditArchiveService>();
builder.Services.AddScoped<IAdminService, AdminService>();
builder.Services.AddScoped<IInventoryService, InventoryService>();
builder.Services.AddScoped<ITaxConfigurationService,TaxConfigurationService>();
builder.Services.AddScoped<IKitchenService, KitchenService>();
builder.Services.AddScoped<IQrCodeService, QrCodeService>();
builder.Services.AddScoped<IBlobStorageService, BlobStorageService>();
builder.Services.AddScoped<IIngredientService, IngredientService>();
builder.Services.AddScoped<IRestaurantConfigurationService, RestaurantConfigurationService>();
builder.Services.AddSingleton<IConversationService, ConversationService>();
builder.Services.AddScoped<IToolDispatcher, ToolDispatcher>();
builder.Services.AddScoped<GetMenuCatalogTool>();
builder.Services.AddScoped<GetMenuItemDetailsTool>();
builder.Services.AddScoped<GetMenuItemsByIngredientTool>();
builder.Services.AddScoped<GetRestaurantInfoTool>();
builder.Services.AddScoped<IAIService, AIService>();
#endregion
var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
var provider = new FileExtensionContentTypeProvider();

provider.Mappings[".avif"] = "image/avif";
provider.Mappings[".webp"] = "image/webp";
app.UseHttpsRedirection();
app.UseMiddleware<ExceptionMiddleware>();
app.UseCors("AllowFrontend");
app.UseAuthentication();
app.UseAuthorization();
app.UseStaticFiles(new StaticFileOptions
{
    ContentTypeProvider = provider
});
app.MapControllers();

app.MapHub<NotificationHub>("/notificationHub");

app.Run();