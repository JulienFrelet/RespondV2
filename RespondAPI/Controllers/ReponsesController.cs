using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using Microsoft.AspNetCore.RateLimiting;
using RespondAPI.Dtos;
using RespondAPI.Services;
using RespondMetier.Data.Interfaces;
using RespondMetier.Exceptions;
using RespondMetier.Models;
using RespondMetier.Services.Interfaces;

namespace RespondAPI.Controllers
{
    /// <summary>
    /// Generates replies to emails
    /// </summary>
    [ApiController]
    [Route("api/reponses")]
    public class ReponsesController : ControllerBase
    {
        #region Properties

        private readonly IGenerationService generation;
        private readonly IProfilDAO profils;
        private readonly IConfiguration configuration;
        private readonly ISessionVisiteur session;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the ReponsesController class
        /// </summary>
        /// <param name="generation">the generation service</param>
        /// <param name="profils">the profile data access</param>
        /// <param name="configuration">
        /// used to read the shared demo key, which lives in user secrets during
        /// development and in an environment variable once deployed
        /// </param>
        /// <param name="session">supplies the visitor's anonymous identifier</param>
        public ReponsesController(
            IGenerationService generation,
            IProfilDAO profils,
            IConfiguration configuration,
            ISessionVisiteur session)
        {
            this.generation = generation;
            this.profils = profils;
            this.configuration = configuration;
            this.session = session;
        }

        #endregion

        #region Constants

        /// <summary>
        /// Header carrying a key the visitor supplies for their own request.
        /// </summary>
        /// <remarks>
        /// A header rather than the body, for two reasons. The rate limiter
        /// runs before the body is read, so it could not otherwise tell a
        /// visitor using their own quota from one using the shared demo key.
        /// And a header never lands in a URL, so the key cannot leak into an
        /// access log or a browser history.
        /// </remarks>
        public const string EnTeteCle = "X-Cle-Api";

        #endregion

        #region Methods

        /// <summary>
        /// Generates a reply to an email
        /// </summary>
        /// <param name="demandeDto">the profile, message and options to use</param>
        /// <param name="jeton">token carried by the HTTP request</param>
        /// <returns>the subject and body of the generated reply</returns>
        [HttpPost]
        [EnableRateLimiting(NomPolitiqueDebit.Generation)]
        public async Task<ActionResult<ReponseDto>> Generer(
            [FromBody] DemandeReponseDto demandeDto, CancellationToken jeton)
        {
            string sessionId = this.session.ObtenirOuCreer(this.HttpContext);

            Profil? profil = this.profils.ChargerParId(demandeDto.ProfilId, sessionId);

            if (profil == null)
            {
                return this.NotFound(new { erreur = "Ce profil n'existe pas." });
            }

            CleApi? cle = this.LireCleUtilisateur() ?? this.LireCleDemo();

            if (cle == null)
            {
                // A misconfigured server, not a bad request: say so plainly
                // rather than blaming the visitor's input.
                return this.StatusCode(
                    StatusCodes.Status503ServiceUnavailable,
                    new { erreur = "Le mode démonstration n'est pas configuré sur ce serveur." });
            }

            DemandeGeneration demande = new DemandeGeneration();
            demande.Profil = profil;
            demande.EmailRecu = demandeDto.EmailRecu;
            demande.Contexte = demandeDto.Contexte;
            demande.Longueur = demandeDto.Longueur;
            demande.Regles = demandeDto.Regles ?? new ReglesRedaction();
            demande.SessionId = sessionId;

            try
            {
                ResultatGeneration resultat =
                    await this.generation.GenererAsync(demande, cle, jeton);

                ReponseDto reponse = new ReponseDto();
                reponse.Objet = resultat.Objet;
                reponse.Corps = resultat.Corps;

                return this.Ok(reponse);
            }
            catch (ErreurDemandeInvalide erreur)
            {
                return this.BadRequest(new { erreur = erreur.Message });
            }
            catch (ErreurQuotaDepasse erreur)
            {
                return this.StatusCode(
                    StatusCodes.Status429TooManyRequests, new { erreur = erreur.Message });
            }
            catch (ErreurFournisseurIndisponible erreur)
            {
                return this.StatusCode(
                    StatusCodes.Status503ServiceUnavailable, new { erreur = erreur.Message });
            }
            catch (ErreurFournisseurIA erreur)
            {
                // Catches the remaining provider failures: refused key, no
                // model left, empty answer. All three mean the server side is
                // at fault, so none of them is a 4xx.
                return this.StatusCode(
                    StatusCodes.Status502BadGateway, new { erreur = erreur.Message });
            }
        }

        /// <summary>
        /// Builds a credential from the key the visitor supplied, if any
        /// </summary>
        /// <remarks>
        /// The key is used for this single request and never written anywhere:
        /// not to the database, not to a log, not to the history, which only
        /// ever stores a label.
        /// </remarks>
        /// <returns>the visitor's credential, or null when none was supplied</returns>
        private CleApi? LireCleUtilisateur()
        {
            if (!this.Request.Headers.TryGetValue(EnTeteCle, out StringValues valeurs))
            {
                return null;
            }

            string secret = valeurs.ToString().Trim();

            if (secret.Length == 0)
            {
                return null;
            }

            CleApi cle = new CleApi();
            cle.Libelle = "Ma clé";
            cle.Fournisseur = Fournisseur.Groq;
            cle.Secret = secret;
            return cle;
        }

        /// <summary>
        /// Builds the credential used by the demo from the configuration
        /// </summary>
        /// <returns>the demo credential, or null when none is configured</returns>
        private CleApi? LireCleDemo()
        {
            string? secret = this.configuration["Respond:CleDemo"];

            if (string.IsNullOrWhiteSpace(secret))
            {
                return null;
            }

            CleApi cle = new CleApi();
            cle.Libelle = "Groq (démonstration)";
            cle.Fournisseur = Fournisseur.Groq;
            cle.Secret = secret;
            return cle;
        }

        #endregion
    }
}
