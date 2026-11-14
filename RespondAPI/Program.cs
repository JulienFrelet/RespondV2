using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Primitives;
using RespondAPI.Clients;
using RespondAPI.Controllers;
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
string chaineConnexion = builder.Configuration.GetConnectionString("Default")
    ?? "Data Source=respond.db";

// SQLite ouvre un fichier, il ne cree pas le dossier qui le contient. En
// production la base vit sur un volume persistant, et si ce dossier n'existe
// pas encore le demarrage echoue sur une erreur qui ne dit pas laquelle des
// deux causes est en jeu : chemin absent ou droits insuffisants. On cree donc
// le dossier ici, ou l'echec est explicite.
string fichierBase = new SqliteConnectionStringBuilder(chaineConnexion).DataSource;
string dossierBase = Path.GetDirectoryName(Path.GetFullPath(fichierBase)) ?? string.Empty;

if (dossierBase.Length > 0)
{
    Directory.CreateDirectory(dossierBase);
}

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(chaineConnexion));

builder.Services.AddScoped<InitialiseurBaseDeDonnees>();

// Identifie un visiteur sans compte. Le cookie porte une valeur signee par la
// pile de protection des donnees : un identifiant brut serait forgeable, et
// n'importe qui pourrait lire l'historique d'un autre en recopiant sa valeur.
// Les cles de signature sont persistees dans un dossier configurable.
//
// Par defaut, ASP.NET les range dans le profil de l'utilisateur courant. Dans
// un conteneur, ce dossier est ephemere : au moindre redeploiement les cles
// changent, tous les cookies de session deviennent invalides, et chaque
// visiteur perd son historique. Avec plusieurs instances, chacune aurait ses
// propres cles et un visiteur basculerait d'identite a chaque requete.
//
// Le chemin pointe vers un volume persistant en production. Le dossier doit
// etre traite comme un secret : qui le lit peut forger des cookies de session.
string dossierCles = builder.Configuration["Respond:DossierCles"]
    ?? Path.Combine(builder.Environment.ContentRootPath, "cles-protection");

Directory.CreateDirectory(dossierCles);

builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(dossierCles))
    .SetApplicationName("Respond");
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
    {
        string adresse = contexte.Connection.RemoteIpAddress?.ToString() ?? "inconnu";

        // Un visiteur qui apporte sa propre cle paie son propre quota chez le
        // fournisseur : la limite stricte de la demonstration n'a plus lieu
        // d'etre. Elle reste large plutot qu'absente, pour que le serveur ne
        // devienne pas un relais ouvert.
        bool cleFournie = contexte.Request.Headers
            .TryGetValue(ReponsesController.EnTeteCle, out StringValues cle)
            && !string.IsNullOrWhiteSpace(cle.ToString());

        int limite = cleFournie
            ? builder.Configuration.GetValue<int>("Respond:ClePersonnelle:GenerationsParHeure")
            : builder.Configuration.GetValue<int>("Respond:Demo:GenerationsParHeure");

        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: (cleFournie ? "cle:" : "demo:") + adresse,
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = limite,
                Window = TimeSpan.FromHours(1),
                QueueLimit = 0,
            });
    });
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
