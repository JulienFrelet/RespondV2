using Microsoft.Extensions.Options;
using RespondMetier.Exceptions;
using RespondMetier.Models;
using RespondMetier.Services.Interfaces;

namespace RespondAPI.Clients
{
    /// <summary>
    /// Builds the provider client matching a provider and a key
    /// </summary>
    public class FabriqueFournisseurIA : IFabriqueFournisseurIA
    {
        #region Properties

        private readonly IHttpClientFactory fabriqueHttp;
        private readonly OptionsGroq optionsGroq;
        private readonly ILoggerFactory fabriqueJournal;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the FabriqueFournisseurIA class
        /// </summary>
        /// <param name="fabriqueHttp">
        /// supplies pooled HTTP clients. Creating an HttpClient per request
        /// instead would exhaust the machine's sockets under load
        /// </param>
        /// <param name="optionsGroq">the Groq configuration read from appsettings</param>
        /// <param name="fabriqueJournal">supplies the logger given to each client</param>
        public FabriqueFournisseurIA(
            IHttpClientFactory fabriqueHttp,
            IOptions<OptionsGroq> optionsGroq,
            ILoggerFactory fabriqueJournal)
        {
            this.fabriqueHttp = fabriqueHttp;
            this.optionsGroq = optionsGroq.Value;
            this.fabriqueJournal = fabriqueJournal;
        }

        #endregion

        #region Methods

        public IFournisseurIA Creer(Fournisseur fournisseur, string cle)
        {
            if (fournisseur == Fournisseur.Groq)
            {
                return new GroqClient(
                    this.fabriqueHttp.CreateClient(NomClientHttp.Groq),
                    this.optionsGroq,
                    cle,
                    this.fabriqueJournal.CreateLogger<GroqClient>());
            }

            // No silent fallback to another provider: that would send the
            // user's email somewhere they did not choose.
            throw new ErreurFournisseurNonSupporte(
                "Le fournisseur " + fournisseur + " n'est pas encore disponible.");
        }

        #endregion
    }

    /// <summary>
    /// Names of the HTTP clients registered in the dependency injection container
    /// </summary>
    public static class NomClientHttp
    {
        /// <summary>Named client configured for the Groq API</summary>
        public const string Groq = "groq";
    }
}
