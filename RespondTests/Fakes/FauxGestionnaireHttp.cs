using System.Net;
using System.Text;

namespace RespondTests.Fakes
{
    /// <summary>
    /// HTTP layer replaced by a queue of prepared answers.
    /// </summary>
    /// <remarks>
    /// Lets the provider clients be tested without a network: every case the
    /// first version of this application got wrong — a refused key, an
    /// exhausted quota, a retired model, a timeout, an answer with no text —
    /// becomes an ordinary unit test that runs in milliseconds.
    /// <para>
    /// It also records what was actually sent, so a test can assert that the
    /// instructions and the untrusted message really travelled as two separate
    /// messages, and that the API key was placed where it belongs.
    /// </para>
    /// </remarks>
    internal class FauxGestionnaireHttp : HttpMessageHandler
    {
        private readonly Queue<Func<HttpRequestMessage, HttpResponseMessage>> reponses;
        private readonly List<string> corpsEnvoyes;
        private readonly List<string> entetesAutorisation;

        /// <summary>
        /// Initializes the fake with no answer queued
        /// </summary>
        public FauxGestionnaireHttp()
        {
            this.reponses = new Queue<Func<HttpRequestMessage, HttpResponseMessage>>();
            this.corpsEnvoyes = new List<string>();
            this.entetesAutorisation = new List<string>();
        }

        /// <summary>
        /// Gets the bodies of the requests that were sent, in order
        /// </summary>
        public List<string> CorpsEnvoyes
        {
            get { return this.corpsEnvoyes; }
        }

        /// <summary>
        /// Gets the Authorization headers that were sent, in order
        /// </summary>
        public List<string> EntetesAutorisation
        {
            get { return this.entetesAutorisation; }
        }

        /// <summary>
        /// Gets how many requests the client actually performed
        /// </summary>
        public int NombreAppels
        {
            get { return this.corpsEnvoyes.Count; }
        }

        /// <summary>
        /// Queues an answer with a status code and a raw body
        /// </summary>
        /// <param name="code">the status code to return</param>
        /// <param name="corps">the body to return</param>
        /// <returns>this fake, so calls can be chained</returns>
        public FauxGestionnaireHttp Repondre(HttpStatusCode code, string corps)
        {
            this.reponses.Enqueue(_ => new HttpResponseMessage(code)
            {
                Content = new StringContent(corps, Encoding.UTF8, "application/json"),
            });

            return this;
        }

        /// <summary>
        /// Queues a successful answer shaped like a chat completion
        /// </summary>
        /// <param name="texte">the text the model is supposed to have produced</param>
        /// <param name="raisonnement">
        /// optional reasoning field, as returned by reasoning models, which the
        /// client must ignore
        /// </param>
        /// <returns>this fake, so calls can be chained</returns>
        public FauxGestionnaireHttp RepondreAvecTexte(string texte, string? raisonnement = null)
        {
            string message = raisonnement == null
                ? "{\"content\":" + Encoder(texte) + "}"
                : "{\"reasoning\":" + Encoder(raisonnement) + ",\"content\":" + Encoder(texte) + "}";

            return this.Repondre(
                HttpStatusCode.OK,
                "{\"choices\":[{\"message\":" + message + "}]}");
        }

        /// <summary>
        /// Queues a failure thrown by the transport itself, such as a lost connection
        /// </summary>
        /// <param name="erreur">the exception to throw instead of answering</param>
        /// <returns>this fake, so calls can be chained</returns>
        public FauxGestionnaireHttp Lever(Exception erreur)
        {
            this.reponses.Enqueue(_ => throw erreur);
            return this;
        }

        /// <summary>
        /// Records the outgoing request then returns the next queued answer
        /// </summary>
        /// <param name="request">the request the client is sending</param>
        /// <param name="cancellationToken">the token carried by the call</param>
        /// <returns>the next prepared answer</returns>
        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            this.entetesAutorisation.Add(request.Headers.Authorization?.ToString() ?? string.Empty);
            this.corpsEnvoyes.Add(request.Content == null
                ? string.Empty
                : await request.Content.ReadAsStringAsync(cancellationToken));

            if (this.reponses.Count == 0)
            {
                throw new InvalidOperationException(
                    "Le client a fait plus d'appels que le test n'a prepare de reponses.");
            }

            return this.reponses.Dequeue()(request);
        }

        /// <summary>
        /// Escapes a string so it can be embedded in a JSON body
        /// </summary>
        /// <param name="valeur">the string to escape</param>
        /// <returns>the quoted and escaped string</returns>
        private static string Encoder(string valeur)
        {
            return System.Text.Json.JsonSerializer.Serialize(valeur);
        }
    }
}
