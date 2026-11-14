using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using RespondMetier.Exceptions;
using RespondMetier.Models;
using RespondMetier.Services.Interfaces;

namespace RespondAPI.Clients
{
    /// <summary>
    /// Talks to the Groq API, which follows the OpenAI chat completions format.
    /// </summary>
    /// <remarks>
    /// The key is passed to the constructor rather than read from the
    /// configuration here, so the same class serves the shared demo key and a
    /// key supplied by a visitor for their own request.
    /// </remarks>
    public class GroqClient : IFournisseurIA
    {
        #region Properties

        private readonly HttpClient client;
        private readonly OptionsGroq options;
        private readonly string cle;
        private readonly ILogger<GroqClient> journal;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the GroqClient class
        /// </summary>
        /// <param name="client">the HTTP client used to reach the provider</param>
        /// <param name="options">the base address, models and timeout to use</param>
        /// <param name="cle">the API key authorising the request</param>
        /// <param name="journal">the logger, which must never receive the key</param>
        public GroqClient(HttpClient client, OptionsGroq options, string cle, ILogger<GroqClient> journal)
        {
            this.client = client;
            this.options = options;
            this.cle = cle;
            this.journal = journal;
        }

        #endregion

        #region Methods

        public async Task<string> GenererAsync(PromptGeneration prompt, CancellationToken jeton)
        {
            if (string.IsNullOrWhiteSpace(this.cle))
            {
                throw new ErreurCleRefusee("Aucune clé API n'est configurée pour ce fournisseur.");
            }

            List<string> modeles = this.options.Modeles;

            if (modeles == null || modeles.Count == 0)
            {
                throw new ErreurAucunModeleDisponible(
                    "Aucun modèle n'est configuré pour ce fournisseur.");
            }

            // The models are tried in order. A model refused as unknown moves
            // on to the next one; any other failure stops immediately, because
            // retrying a refused key or an exhausted quota on another model
            // would only waste time and hide the real cause.
            string dernierRefus = string.Empty;

            foreach (string modele in modeles)
            {
                try
                {
                    return await this.AppelerModeleAsync(prompt, modele, jeton);
                }
                catch (ErreurModeleInconnu erreur)
                {
                    dernierRefus = erreur.Message;
                    this.journal.LogWarning(
                        "Le modèle {Modele} est refusé par Groq, essai du suivant.", modele);
                }
            }

            throw new ErreurAucunModeleDisponible(
                "Aucun des modèles configurés n'est disponible chez ce fournisseur. "
                + "Dernier refus : " + dernierRefus);
        }

        /// <summary>
        /// Performs one generation attempt against a single model
        /// </summary>
        /// <param name="prompt">the instructions and the text to analyse</param>
        /// <param name="modele">the model identifier to call</param>
        /// <param name="jeton">token used to abandon a request that takes too long</param>
        /// <returns>the text produced by the model</returns>
        /// <exception cref="ErreurModeleInconnu">
        /// when this particular model is refused, so that the caller tries the next one
        /// </exception>
        private async Task<string> AppelerModeleAsync(
            PromptGeneration prompt, string modele, CancellationToken jeton)
        {
            // A hard deadline on top of the caller's token. Without it, a
            // filtered network, a proxy or a VPN that swallows the request
            // leaves the call hanging for as long as the server allows, and
            // the user waits in front of a spinner that never stops.
            using CancellationTokenSource limite = CancellationTokenSource.CreateLinkedTokenSource(jeton);
            limite.CancelAfter(TimeSpan.FromSeconds(this.options.DelaiMaximalSecondes));

            using HttpRequestMessage requete = new HttpRequestMessage(
                HttpMethod.Post, this.options.UrlDeBase.TrimEnd('/') + "/chat/completions");

            try
            {
                requete.Headers.Authorization = new AuthenticationHeaderValue("Bearer", this.cle);
            }
            catch (FormatException)
            {
                // .NET refuse les retours ligne et les caracteres nuls dans une
                // valeur d'en-tete, ce qui bloque toute tentative d'injection.
                // Sans cette traduction l'exception echappe au controleur et
                // devient un 500 opaque : une cle malformee est une cle
                // refusee, pas une panne du serveur. Le message ne reprend
                // jamais la valeur fautive.
                throw new ErreurCleRefusee(
                    "La clé API contient des caractères interdits.");
            }

            requete.Content = new StringContent(
                ConstruireCorps(prompt, modele, this.options.Temperature),
                Encoding.UTF8,
                "application/json");

            HttpResponseMessage reponse;

            try
            {
                reponse = await this.client.SendAsync(requete, limite.Token);
            }
            catch (OperationCanceledException) when (!jeton.IsCancellationRequested)
            {
                throw new ErreurFournisseurIndisponible(
                    "Le fournisseur n'a pas répondu en " + this.options.DelaiMaximalSecondes
                    + " secondes. Vérifiez votre connexion puis réessayez.");
            }
            catch (HttpRequestException erreur)
            {
                // The provider message is logged, never the key: the exception
                // only carries what the user can act on.
                this.journal.LogWarning(erreur, "Appel à Groq impossible.");
                throw new ErreurFournisseurIndisponible(
                    "Connexion impossible au fournisseur. Vérifiez votre connexion internet.");
            }

            using (reponse)
            {
                string corps = await reponse.Content.ReadAsStringAsync(limite.Token);

                if (!reponse.IsSuccessStatusCode)
                {
                    throw TraduireEchec(reponse.StatusCode, corps);
                }

                return LireTexte(corps);
            }
        }

        /// <summary>
        /// Builds the request body, with the instructions and the untrusted
        /// text as two separate messages
        /// </summary>
        /// <param name="prompt">the prompt to send</param>
        /// <param name="modele">the model identifier</param>
        /// <param name="temperature">the sampling temperature</param>
        /// <returns>the JSON body of the request</returns>
        private static string ConstruireCorps(PromptGeneration prompt, string modele, double temperature)
        {
            var corps = new
            {
                model = modele,
                temperature,
                messages = new[]
                {
                    new { role = "system", content = prompt.Instructions },
                    new { role = "user", content = prompt.DonneesAAnalyser },
                },
            };

            return JsonSerializer.Serialize(corps);
        }

        /// <summary>
        /// Turns a failed HTTP answer into the matching business exception
        /// </summary>
        /// <param name="code">the status code returned by the provider</param>
        /// <param name="corps">the raw body, used to recognise a retired model</param>
        /// <returns>the exception to throw</returns>
        private static Exception TraduireEchec(HttpStatusCode code, string corps)
        {
            if (code == HttpStatusCode.Unauthorized || code == HttpStatusCode.Forbidden)
            {
                return new ErreurCleRefusee(
                    "Clé API refusée par le fournisseur. Vérifiez-la dans les paramètres.");
            }

            if (code == HttpStatusCode.TooManyRequests)
            {
                return new ErreurQuotaDepasse(
                    "Quota atteint pour cette clé. Réessayez plus tard, "
                    + "ou utilisez votre propre clé pour lever la limite.");
            }

            if (code == HttpStatusCode.NotFound || DecritUnModeleRetire(corps))
            {
                return new ErreurModeleInconnu("Le fournisseur ne connaît pas ce modèle.");
            }

            if ((int)code >= 500)
            {
                return new ErreurFournisseurIndisponible(
                    "Le service d'IA est temporairement indisponible. Réessayez dans un moment.");
            }

            return new ErreurFournisseurIndisponible(
                "Le fournisseur a répondu une erreur inattendue (HTTP " + (int)code + ").");
        }

        /// <summary>
        /// Recognises the wordings providers use for a model that no longer exists
        /// </summary>
        /// <remarks>
        /// Providers do not agree on a status code for this case: some answer
        /// 404, others a 400 whose message explains that the model is gone.
        /// Matching on the wording is fragile, which is why it only ever
        /// triggers a fallback to the next model and never a hard failure.
        /// </remarks>
        /// <param name="corps">the raw body returned by the provider</param>
        /// <returns>true when the body describes a retired or unknown model</returns>
        private static bool DecritUnModeleRetire(string corps)
        {
            if (string.IsNullOrEmpty(corps))
            {
                return false;
            }

            string minuscules = corps.ToLowerInvariant();

            return minuscules.Contains("does not exist")
                || minuscules.Contains("model_not_found")
                || minuscules.Contains("model_decommissioned")
                || minuscules.Contains("decommissioned")
                || minuscules.Contains("unknown model")
                || minuscules.Contains("no longer available")
                || minuscules.Contains("no longer supported");
        }

        /// <summary>
        /// Reads the reply out of a successful answer
        /// </summary>
        /// <remarks>
        /// Only the content field is read. Reasoning models such as gpt-oss
        /// also return a reasoning field holding their intermediate thinking;
        /// sending that to a customer would be both confusing and wrong, so it
        /// is ignored. Using JsonDocument rather than a typed object means the
        /// extra fields are never even looked at.
        /// </remarks>
        /// <param name="corps">the raw body returned by the provider</param>
        /// <returns>the generated text</returns>
        private static string LireTexte(string corps)
        {
            string? texte = null;

            try
            {
                using JsonDocument document = JsonDocument.Parse(corps);

                if (document.RootElement.TryGetProperty("choices", out JsonElement choix)
                    && choix.ValueKind == JsonValueKind.Array
                    && choix.GetArrayLength() > 0
                    && choix[0].TryGetProperty("message", out JsonElement message)
                    && message.TryGetProperty("content", out JsonElement contenu))
                {
                    texte = contenu.GetString();
                }
            }
            catch (JsonException)
            {
                throw new ErreurFournisseurIndisponible(
                    "Le fournisseur a renvoyé une réponse illisible.");
            }

            if (string.IsNullOrWhiteSpace(texte))
            {
                throw new ErreurReponseVide(
                    "Le modèle a répondu sans texte exploitable. Réessayez.");
            }

            return texte.Trim();
        }

        #endregion
    }

    /// <summary>
    /// Internal signal meaning "this model is refused, try the next one".
    /// </summary>
    /// <remarks>
    /// Deliberately not part of the business error contract: it never leaves
    /// this class. Callers only ever see ErreurAucunModeleDisponible, once
    /// every model in the list has been refused.
    /// </remarks>
    internal class ErreurModeleInconnu : Exception
    {
        /// <summary>
        /// Initializes the signal with the provider wording
        /// </summary>
        /// <param name="message">what the provider said about the model</param>
        public ErreurModeleInconnu(string message) : base(message)
        {
        }
    }
}
