using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using RespondAPI.Clients;
using RespondAPI.Services;
using RespondData;
using RespondData.DAO;
using RespondMetier.Data.Interfaces;
using RespondMetier.Services.Interfaces;
using RespondMetier.Services.Realisations;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Enums are serialized as strings so the JavaScript client reads "Groq"
// rather than the integer 0, which would silently break on reordering.
builder.Services.AddControllers()
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddOpenApi();

// Plafond sur la taille d'une requete. Le constructeur de prompt refuse deja
// un message trop long, mais il le fait apres lecture : ce plafond-ci coupe
// avant, et protege aussi les routes qui n'ont rien a voir avec le prompt.
builder.Services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(
    options => options.MultipartBodyLengthLimit = 128 * 1024);

builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = 128 * 1024;

    // Le serveur n'annonce plus son nom : une version connue est une
    // indication offerte a qui cherche une faille a exploiter.
    options.AddServerHeader = false;
});

// Configure the database.
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("Default")));

builder.Services.AddScoped<InitialiseurBaseDeDonnees>();

// Identifie un visiteur sans compte. Le cookie porte une valeur signee par la
// pile de protection des donnees : un identifiant brut serait forgeable, et
// n'importe qui pourrait lire l'historique d'un autre en recopiant sa valeur.
builder.Services.AddDataProtection();
builder.Services.AddSingleton<ISessionVisiteur, SessionVisiteur>();

// Register DAOs.
builder.Services.AddScoped<IProfilDAO, ProfilDAO>();
builder.Services.AddScoped<IHistoriqueDAO, HistoriqueDAO>();

// Register business services. The prompt builder and the answer parser hold
// no state, so a single instance serves every request.
builder.Services.AddSingleton<IConstructeurPrompt, ConstructeurPrompt>();
builder.Services.AddSingleton<IAnalyseurReponse, AnalyseurReponse>();

// Configure the Groq client.
builder.Services.Configure<OptionsGroq>(builder.Configuration.GetSection("Respond:Groq"));
builder.Services.AddHttpClient(NomClientHttp.Groq);
builder.Services.AddScoped<IFabriqueFournisseurIA, FabriqueFournisseurIA>();

builder.Services.AddScoped<IGenerationService>(fournisseurs =>
    new GenerationService(
        fournisseurs.GetRequiredService<IConstructeurPrompt>(),
        fournisseurs.GetRequiredService<IFabriqueFournisseurIA>(),
        fournisseurs.GetRequiredService<IAnalyseurReponse>(),
        fournisseurs.GetRequiredService<IHistoriqueDAO>(),
        builder.Configuration.GetValue<int>("Respond:LimiteHistorique")));

// Rate limiting, per client address.
//
// The demo runs on a shared API key. Without a limit, one script could drain
// the free allowance in minutes and the demo would be dead for everyone else.
// The real safeguard remains that the key has no payment method attached:
// the worst case is an exhausted quota, never a bill.
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.AddPolicy(NomPolitiqueDebit.Generation, contexte =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: contexte.Connection.RemoteIpAddress?.ToString() ?? "inconnu",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = builder.Configuration.GetValue<int>("Respond:Demo:GenerationsParHeure"),
                Window = TimeSpan.FromHours(1),
                QueueLimit = 0,
            }));
});

var app = builder.Build();

// Initialize the database and seed the demo profiles.
using (IServiceScope portee = app.Services.CreateScope())
{
    portee.ServiceProvider.GetRequiredService<InitialiseurBaseDeDonnees>().Initialiser();
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    // HSTS indique au navigateur de n'utiliser que HTTPS pour ce domaine,
    // y compris si l'utilisateur tape l'adresse sans protocole. Absent en
    // developpement, ou il bloquerait l'acces en clair a localhost.
    app.UseHsts();
    app.UseHttpsRedirection();
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "RespondAPI v1");
    });
}

// Security headers, sent on every response.
//
// The content security policy belongs in a header rather than in a meta tag:
// frame-ancestors is ignored when delivered by a meta element, so clickjacking
// protection would silently not apply. Everything the page loads comes from
// its own origin, fonts included, so no third party ever sees a visitor's
// address.
app.Use(async (contexte, suite) =>
{
    IHeaderDictionary entetes = contexte.Response.Headers;

    entetes["Content-Security-Policy"] =
        "default-src 'self'; script-src 'self'; style-src 'self'; font-src 'self'; "
        + "img-src 'self' data:; connect-src 'self'; base-uri 'none'; "
        + "form-action 'none'; frame-ancestors 'none'";

    entetes["X-Content-Type-Options"] = "nosniff";
    entetes["Referrer-Policy"] = "no-referrer";

    await suite();
});

app.UseDefaultFiles();
app.UseStaticFiles();
app.UseRateLimiter();
app.MapControllers();

app.Run();

/// <summary>
/// Names of the rate limiting policies registered in the container
/// </summary>
public static class NomPolitiqueDebit
{
    /// <summary>Policy applied to the generation endpoint</summary>
    public const string Generation = "generation";
}
