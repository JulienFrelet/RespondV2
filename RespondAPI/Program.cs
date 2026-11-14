using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
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

// L'application tourne derriere le proxy de l'hebergeur, qui termine TLS et
// transmet la requete en clair. Sans lire les en-tetes qu'il ajoute,
// l'application se croit en HTTP : UseHttpsRedirection renvoie alors vers
// HTTPS, le proxy retransmet en clair, et le navigateur boucle.
//
// ForwardLimit vaut 1, et ce n'est pas un detail : X-Forwarded-For est une
// liste a laquelle n'importe qui peut ajouter une entree avant d'atteindre le
// proxy. ASP.NET lit la liste par la droite, donc la derniere entree, celle
// que le proxy a ecrite lui-meme. Accepter plus loin reviendrait a croire le
// client sur parole, et la limitation de debit, qui partitionne par adresse,
// se contournerait en ajoutant un en-tete.
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.ForwardLimit = 1;

    // L'adresse du proxy n'est pas connue a l'avance et change au gre des
    // redeploiements de l'hebergeur. La protection ne repose donc pas sur une
    // liste d'adresses de confiance mais sur ForwardLimit ci-dessus.
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

// Configure the database.
//
// Deux moteurs sont acceptes, choisis d'apres la chaine de connexion : SQLite
// en developpement, ou aucune installation n'est souhaitable, et PostgreSQL
// en production, ou l'hebergement gratuit ne fournit pas de disque persistant
// et ou un fichier disparaitrait a chaque redeploiement.
//
// Le choix se lit dans la configuration plutot que dans un reglage separe :
// une chaine PostgreSQL et un moteur SQLite ne peuvent donc pas se
// contredire.
string chaineConnexion = builder.Configuration.GetConnectionString("Default")
    ?? "Data Source=respond.db";

chaineConnexion = ChaineConnexion.Normaliser(chaineConnexion);

if (ChaineConnexion.EstPostgres(chaineConnexion))
{
    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseNpgsql(chaineConnexion));
}
else
{
    // SQLite ouvre un fichier, il ne cree pas le dossier qui le contient. Si
    // ce dossier n'existe pas, le demarrage echoue sur une erreur qui ne dit
    // pas laquelle des deux causes est en jeu : chemin absent ou droits
    // insuffisants. On le cree donc ici, ou l'echec est explicite.
    string fichierBase = new SqliteConnectionStringBuilder(chaineConnexion).DataSource;
    string dossierBase = Path.GetDirectoryName(Path.GetFullPath(fichierBase)) ?? string.Empty;

    if (dossierBase.Length > 0)
    {
        Directory.CreateDirectory(dossierBase);
    }

    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseSqlite(chaineConnexion));
}

builder.Services.AddScoped<InitialiseurBaseDeDonnees>();

// Identifie un visiteur sans compte. Le cookie porte une valeur signee par la
// pile de protection des donnees : un identifiant brut serait forgeable, et
// n'importe qui pourrait lire l'historique d'un autre en recopiant sa valeur.
//
// Les cles de signature vivent en base, avec le reste des donnees.
//
// Par defaut, ASP.NET les range dans le profil de l'utilisateur courant. Dans
// un conteneur, ce dossier est ephemere : au moindre redeploiement les cles
// changent, tous les cookies de session deviennent invalides, et chaque
// visiteur perd son historique. Un volume persistant reglait le probleme au
// prix d'un montage obligatoire, et interdisait toujours plusieurs instances,
// chacune ayant eu ses propres cles.
//
// En base, les cles survivent au redeploiement et sont partagees par toutes
// les instances. Ces lignes sont un secret : qui les lit peut forger un
// cookie de session. Elles ne sont exposees par aucune route.
builder.Services.AddDataProtection()
    .PersistKeysToDbContext<AppDbContext>()
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

    // Ecritures en base : creation et modification de profils.
    //
    // Le nombre de profils est deja plafonne par visiteur, mais un visiteur
    // n'est qu'un cookie : il suffit de ne pas le renvoyer pour repartir a
    // zero autant de fois qu'on veut. Le plafond par session ne borne donc
    // rien du tout face a un script, et la base, sur une offre gratuite
    // limitee en taille, se remplirait jusqu'a refuser d'ecrire.
    //
    // La limite porte sur l'adresse, que le visiteur ne choisit pas. Elle est
    // large : un usage normal cree quelques profils, jamais soixante par
    // heure.
    options.AddPolicy(NomPolitiqueDebit.Ecriture, contexte =>
    {
        string adresse = contexte.Connection.RemoteIpAddress?.ToString() ?? "inconnu";

        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: "ecriture:" + adresse,
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = builder.Configuration.GetValue<int>("Respond:EcrituresParHeure"),
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
//
// En tout premier : les en-tetes du proxy doivent etre appliques avant que
// quoi que ce soit ne lise le protocole ou l'adresse du client.
app.UseForwardedHeaders();

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

    /// <summary>Policy applied to the endpoints that write to the database</summary>
    public const string Ecriture = "ecriture";
}
