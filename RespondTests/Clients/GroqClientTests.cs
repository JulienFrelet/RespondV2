using System.Net;
using Microsoft.Extensions.Logging.Abstractions;
using RespondAPI.Clients;
using RespondMetier.Exceptions;
using RespondMetier.Models;
using RespondTests.Fakes;

namespace RespondTests.Clients
{
    /// <summary>
    /// Tests for the GroqClient, run against a hand written HTTP layer.
    /// No network is involved, so every provider failure is reproducible.
    /// </summary>
    public class GroqClientTests
    {
        private const string CleFactice = "gsk_cle_de_test_sans_valeur";

        /// <summary>
        /// Builds a client wired to the fake HTTP layer
        /// </summary>
        private static GroqClient Construire(
            FauxGestionnaireHttp gestionnaire,
            List<string>? modeles = null,
            string cle = CleFactice)
        {
            OptionsGroq options = new OptionsGroq();
            options.Modeles = modeles ?? ["openai/gpt-oss-120b"];
            options.DelaiMaximalSecondes = 90;

            HttpClient client = new HttpClient(gestionnaire);

            return new GroqClient(client, options, cle, NullLogger<GroqClient>.Instance);
        }

        /// <summary>
        /// Builds a usable prompt with recognisable content
        /// </summary>
        private static PromptGeneration ConstruirePrompt()
        {
            PromptGeneration prompt = new PromptGeneration();
            prompt.Instructions = "Tu rediges des reponses professionnelles.";
            prompt.DonneesAAnalyser = "Bonjour, ou est ma commande ?";
            return prompt;
        }

        #region Cas nominal

        /// <summary>
        /// A successful answer returns the generated text, trimmed
        /// </summary>
        [Fact]
        public async Task ReponseValideRenvoieLeTexte()
        {
            FauxGestionnaireHttp gestionnaire = new FauxGestionnaireHttp()
                .RepondreAvecTexte("  OBJET: Re: commande\n\nBonjour,  ");

            string resultat = await Construire(gestionnaire).GenererAsync(
                ConstruirePrompt(), CancellationToken.None);

            Assert.StartsWith("OBJET: Re: commande", resultat);
            Assert.EndsWith("Bonjour,", resultat);
        }

        /// <summary>
        /// The reasoning field of a reasoning model is ignored: only content
        /// is the reply to hand to a customer
        /// </summary>
        [Fact]
        public async Task ChampRaisonnementIgnore()
        {
            FauxGestionnaireHttp gestionnaire = new FauxGestionnaireHttp()
                .RepondreAvecTexte(
                    texte: "Bonjour, votre commande part demain.",
                    raisonnement: "L'utilisateur demande ou est sa commande, je dois verifier...");

            string resultat = await Construire(gestionnaire).GenererAsync(
                ConstruirePrompt(), CancellationToken.None);

            Assert.Equal("Bonjour, votre commande part demain.", resultat);
            Assert.DoesNotContain("je dois verifier", resultat);
        }

        #endregion

        #region Forme de la requete

        /// <summary>
        /// The instructions and the untrusted message travel as two separate
        /// messages, which is the structural defence against prompt injection
        /// </summary>
        [Fact]
        public async Task InstructionsEtDonneesEnvoyeesSeparement()
        {
            FauxGestionnaireHttp gestionnaire = new FauxGestionnaireHttp()
                .RepondreAvecTexte("ok");

            await Construire(gestionnaire).GenererAsync(ConstruirePrompt(), CancellationToken.None);

            string corps = gestionnaire.CorpsEnvoyes[0];

            Assert.Contains("\"role\":\"system\"", corps);
            Assert.Contains("\"role\":\"user\"", corps);
            Assert.Contains("Tu rediges des reponses professionnelles.", corps);
            Assert.Contains("ou est ma commande", corps);
        }

        /// <summary>
        /// The key is sent as a bearer token and nowhere else, in particular
        /// never in the URL where it would land in logs
        /// </summary>
        [Fact]
        public async Task CleEnvoyeeEnEnteteSeulement()
        {
            FauxGestionnaireHttp gestionnaire = new FauxGestionnaireHttp()
                .RepondreAvecTexte("ok");

            await Construire(gestionnaire).GenererAsync(ConstruirePrompt(), CancellationToken.None);

            Assert.Equal("Bearer " + CleFactice, gestionnaire.EntetesAutorisation[0]);
            Assert.DoesNotContain(CleFactice, gestionnaire.CorpsEnvoyes[0]);
        }

        #endregion

        #region Cle refusee

        /// <summary>
        /// A refused key is reported as such, for both status codes used for it
        /// </summary>
        [Theory]
        [InlineData(HttpStatusCode.Unauthorized)]
        [InlineData(HttpStatusCode.Forbidden)]
        public async Task CleRefuseeSignalee(HttpStatusCode code)
        {
            FauxGestionnaireHttp gestionnaire = new FauxGestionnaireHttp()
                .Repondre(code, "{\"error\":{\"message\":\"Invalid API Key\"}}");

            await Assert.ThrowsAsync<ErreurCleRefusee>(
                () => Construire(gestionnaire).GenererAsync(ConstruirePrompt(), CancellationToken.None));
        }

        /// <summary>
        /// A refused key stops immediately instead of retrying every model:
        /// the key will not become valid on another model
        /// </summary>
        [Fact]
        public async Task CleRefuseeNeDeclenchePasDeBascule()
        {
            FauxGestionnaireHttp gestionnaire = new FauxGestionnaireHttp()
                .Repondre(HttpStatusCode.Unauthorized, "{}");

            GroqClient client = Construire(gestionnaire, ["modele-a", "modele-b", "modele-c"]);

            await Assert.ThrowsAsync<ErreurCleRefusee>(
                () => client.GenererAsync(ConstruirePrompt(), CancellationToken.None));

            Assert.Equal(1, gestionnaire.NombreAppels);
        }

        /// <summary>
        /// An absent key fails before any request leaves the application
        /// </summary>
        [Fact]
        public async Task CleVideEchoueSansAppel()
        {
            FauxGestionnaireHttp gestionnaire = new FauxGestionnaireHttp();

            GroqClient client = Construire(gestionnaire, cle: "   ");

            await Assert.ThrowsAsync<ErreurCleRefusee>(
                () => client.GenererAsync(ConstruirePrompt(), CancellationToken.None));

            Assert.Equal(0, gestionnaire.NombreAppels);
        }

        /// <summary>
        /// No exception message ever carries the key, whatever the failure
        /// </summary>
        [Fact]
        public async Task CleJamaisPresenteDansUnMessageDErreur()
        {
            FauxGestionnaireHttp gestionnaire = new FauxGestionnaireHttp()
                .Repondre(HttpStatusCode.Unauthorized, "{\"error\":\"bad key " + CleFactice + "\"}");

            ErreurCleRefusee erreur = await Assert.ThrowsAsync<ErreurCleRefusee>(
                () => Construire(gestionnaire).GenererAsync(ConstruirePrompt(), CancellationToken.None));

            Assert.DoesNotContain(CleFactice, erreur.Message);
        }

        #endregion

        #region Quota

        /// <summary>
        /// A quota being reached is its own error, not a refused key:
        /// the key is valid, there is simply nothing left for now
        /// </summary>
        [Fact]
        public async Task QuotaDepasseSignale()
        {
            FauxGestionnaireHttp gestionnaire = new FauxGestionnaireHttp()
                .Repondre((HttpStatusCode)429, "{\"error\":{\"message\":\"Rate limit reached\"}}");

            await Assert.ThrowsAsync<ErreurQuotaDepasse>(
                () => Construire(gestionnaire).GenererAsync(ConstruirePrompt(), CancellationToken.None));
        }

        #endregion

        #region Bascule de modele

        /// <summary>
        /// A model refused with a 404 falls back to the next one in the list
        /// </summary>
        [Fact]
        public async Task ModeleIntrouvableBasculeSurLeSuivant()
        {
            FauxGestionnaireHttp gestionnaire = new FauxGestionnaireHttp()
                .Repondre(HttpStatusCode.NotFound, "{\"error\":{\"message\":\"model not found\"}}")
                .RepondreAvecTexte("Bonjour, voici votre reponse.");

            string resultat = await Construire(gestionnaire, ["modele-retire", "modele-valide"])
                .GenererAsync(ConstruirePrompt(), CancellationToken.None);

            Assert.Equal("Bonjour, voici votre reponse.", resultat);
            Assert.Equal(2, gestionnaire.NombreAppels);
        }

        /// <summary>
        /// A retired model announced in the body of a 400 also falls back:
        /// providers do not agree on a status code for this case
        /// </summary>
        [Theory]
        [InlineData("The model `llama-3.3-70b-versatile` does not exist")]
        [InlineData("{\"error\":{\"code\":\"model_decommissioned\"}}")]
        [InlineData("This model is no longer supported")]
        public async Task ModeleRetireAnnonceDansLeCorpsBascule(string corps)
        {
            FauxGestionnaireHttp gestionnaire = new FauxGestionnaireHttp()
                .Repondre(HttpStatusCode.BadRequest, corps)
                .RepondreAvecTexte("ok");

            string resultat = await Construire(gestionnaire, ["modele-retire", "modele-valide"])
                .GenererAsync(ConstruirePrompt(), CancellationToken.None);

            Assert.Equal("ok", resultat);
            Assert.Equal(2, gestionnaire.NombreAppels);
        }

        /// <summary>
        /// When every model is refused, the error says exactly that rather than
        /// blaming the key or the network
        /// </summary>
        [Fact]
        public async Task TousLesModelesRefusesSignale()
        {
            FauxGestionnaireHttp gestionnaire = new FauxGestionnaireHttp()
                .Repondre(HttpStatusCode.NotFound, "{}")
                .Repondre(HttpStatusCode.NotFound, "{}");

            GroqClient client = Construire(gestionnaire, ["modele-a", "modele-b"]);

            await Assert.ThrowsAsync<ErreurAucunModeleDisponible>(
                () => client.GenererAsync(ConstruirePrompt(), CancellationToken.None));

            Assert.Equal(2, gestionnaire.NombreAppels);
        }

        /// <summary>
        /// An empty model list fails before any request leaves the application
        /// </summary>
        [Fact]
        public async Task AucunModeleConfigureEchoueSansAppel()
        {
            FauxGestionnaireHttp gestionnaire = new FauxGestionnaireHttp();

            GroqClient client = Construire(gestionnaire, []);

            await Assert.ThrowsAsync<ErreurAucunModeleDisponible>(
                () => client.GenererAsync(ConstruirePrompt(), CancellationToken.None));

            Assert.Equal(0, gestionnaire.NombreAppels);
        }

        #endregion

        #region Fournisseur indisponible

        /// <summary>
        /// A provider side error is reported as temporary, since retrying may work
        /// </summary>
        [Theory]
        [InlineData(HttpStatusCode.InternalServerError)]
        [InlineData(HttpStatusCode.BadGateway)]
        [InlineData(HttpStatusCode.ServiceUnavailable)]
        public async Task ErreurServeurSignaleeCommeIndisponibilite(HttpStatusCode code)
        {
            FauxGestionnaireHttp gestionnaire = new FauxGestionnaireHttp().Repondre(code, "{}");

            await Assert.ThrowsAsync<ErreurFournisseurIndisponible>(
                () => Construire(gestionnaire).GenererAsync(ConstruirePrompt(), CancellationToken.None));
        }

        /// <summary>
        /// A transport failure is not reported as an invalid key
        /// </summary>
        [Fact]
        public async Task EchecReseauNEstPasUneCleInvalide()
        {
            FauxGestionnaireHttp gestionnaire = new FauxGestionnaireHttp()
                .Lever(new HttpRequestException("Le nom d'hote est introuvable."));

            await Assert.ThrowsAsync<ErreurFournisseurIndisponible>(
                () => Construire(gestionnaire).GenererAsync(ConstruirePrompt(), CancellationToken.None));
        }

        /// <summary>
        /// A timeout is translated into an availability problem, with the delay
        /// stated so the user knows how long was waited
        /// </summary>
        [Fact]
        public async Task DelaiDepasseSignaleAvecLaDuree()
        {
            FauxGestionnaireHttp gestionnaire = new FauxGestionnaireHttp()
                .Lever(new TaskCanceledException("Le delai est depasse."));

            ErreurFournisseurIndisponible erreur =
                await Assert.ThrowsAsync<ErreurFournisseurIndisponible>(
                    () => Construire(gestionnaire).GenererAsync(
                        ConstruirePrompt(), CancellationToken.None));

            Assert.Contains("90", erreur.Message);
        }

        /// <summary>
        /// An unreadable body is an availability problem, not a crash
        /// </summary>
        [Fact]
        public async Task CorpsIllisibleSignale()
        {
            FauxGestionnaireHttp gestionnaire = new FauxGestionnaireHttp()
                .Repondre(HttpStatusCode.OK, "ceci n'est pas du JSON");

            await Assert.ThrowsAsync<ErreurFournisseurIndisponible>(
                () => Construire(gestionnaire).GenererAsync(ConstruirePrompt(), CancellationToken.None));
        }

        #endregion

        #region Reponse vide

        /// <summary>
        /// An answer with no usable text is refused rather than handed over empty
        /// </summary>
        [Theory]
        [InlineData("{\"choices\":[{\"message\":{\"content\":\"\"}}]}")]
        [InlineData("{\"choices\":[{\"message\":{\"content\":\"   \"}}]}")]
        [InlineData("{\"choices\":[{\"message\":{\"reasoning\":\"j'ai reflechi\"}}]}")]
        [InlineData("{\"choices\":[]}")]
        [InlineData("{}")]
        public async Task ReponseSansTexteRefusee(string corps)
        {
            FauxGestionnaireHttp gestionnaire = new FauxGestionnaireHttp()
                .Repondre(HttpStatusCode.OK, corps);

            await Assert.ThrowsAsync<ErreurReponseVide>(
                () => Construire(gestionnaire).GenererAsync(ConstruirePrompt(), CancellationToken.None));
        }

        #endregion
    }
}
