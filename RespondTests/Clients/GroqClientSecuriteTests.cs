using Microsoft.Extensions.Logging.Abstractions;
using RespondAPI.Clients;
using RespondMetier.Exceptions;
using RespondMetier.Models;
using RespondTests.Fakes;

namespace RespondTests.Clients
{
    /// <summary>
    /// Checks what the client does with a hostile API key.
    /// </summary>
    /// <remarks>
    /// The key can come from a visitor through an HTTP header, so it is
    /// untrusted input that ends up in an outbound HTTP header. A value
    /// carrying a carriage return could try to append headers to the request
    /// sent to the provider.
    /// </remarks>
    public class GroqClientSecuriteTests
    {
        /// <summary>
        /// Builds a client wired to the fake HTTP layer
        /// </summary>
        private static GroqClient Construire(FauxGestionnaireHttp gestionnaire, string cle)
        {
            OptionsGroq options = new OptionsGroq();
            options.Modeles = ["openai/gpt-oss-120b"];
            options.DelaiMaximalSecondes = 90;

            return new GroqClient(
                new HttpClient(gestionnaire), options, cle, NullLogger<GroqClient>.Instance);
        }

        /// <summary>
        /// Builds a usable prompt
        /// </summary>
        private static PromptGeneration ConstruirePrompt()
        {
            PromptGeneration prompt = new PromptGeneration();
            prompt.Instructions = "Instructions.";
            prompt.DonneesAAnalyser = "Message.";
            return prompt;
        }

        /// <summary>
        /// A key carrying a line break must not reach the outbound request.
        /// </summary>
        /// <remarks>
        /// Documents the behaviour rather than asserting a specific exception
        /// type: what matters is that the attempt fails and that no request
        /// carrying the injected header is ever sent.
        /// </remarks>
        [Theory]
        [InlineData("gsk_valide\r\nX-Injecte: oui")]
        [InlineData("gsk_valide\nX-Injecte: oui")]
        [InlineData("gsk_valide\rX-Injecte: oui")]
        public async Task CleAvecRetourLigneNEnvoieAucuneRequete(string cleHostile)
        {
            FauxGestionnaireHttp gestionnaire = new FauxGestionnaireHttp()
                .RepondreAvecTexte("ne devrait jamais etre atteint");

            GroqClient client = Construire(gestionnaire, cleHostile);

            // Une cle malformee est une cle refusee, pas une panne du serveur :
            // sans cette traduction, .NET leve une FormatException qui echappe
            // au controleur et devient un 500 opaque.
            await Assert.ThrowsAsync<ErreurCleRefusee>(
                () => client.GenererAsync(ConstruirePrompt(), CancellationToken.None));

            // Le point qui compte : aucune requete n'est partie vers le
            // fournisseur avec l'en-tete injecte.
            Assert.Equal(0, gestionnaire.NombreAppels);
        }
    }
}
