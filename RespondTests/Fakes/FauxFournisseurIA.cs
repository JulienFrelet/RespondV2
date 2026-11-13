using RespondMetier.Models;
using RespondMetier.Services.Interfaces;

namespace RespondTests.Fakes
{
    /// <summary>
    /// AI provider replaced by a prepared answer, or a prepared failure.
    /// </summary>
    /// <remarks>
    /// Records the prompt it received, so a test can check what the service
    /// actually sent without touching the network.
    /// </remarks>
    internal class FauxFournisseurIA : IFournisseurIA
    {
        private readonly string texte;
        private readonly Exception? erreur;
        private PromptGeneration? promptRecu;
        private int nombreAppels;

        /// <summary>
        /// Initializes the fake with the text it will return
        /// </summary>
        /// <param name="texte">the answer the provider is supposed to produce</param>
        public FauxFournisseurIA(string texte)
        {
            this.texte = texte;
            this.erreur = null;
            this.promptRecu = null;
            this.nombreAppels = 0;
        }

        /// <summary>
        /// Initializes the fake with the failure it will raise instead of answering
        /// </summary>
        /// <param name="erreur">the exception to throw</param>
        public FauxFournisseurIA(Exception erreur)
        {
            this.texte = string.Empty;
            this.erreur = erreur;
            this.promptRecu = null;
            this.nombreAppels = 0;
        }

        /// <summary>
        /// Gets the prompt received on the last call, null when never called
        /// </summary>
        public PromptGeneration? PromptRecu
        {
            get { return this.promptRecu; }
        }

        /// <summary>
        /// Gets how many times the provider was called
        /// </summary>
        public int NombreAppels
        {
            get { return this.nombreAppels; }
        }

        /// <summary>
        /// Returns the prepared answer, or raises the prepared failure
        /// </summary>
        /// <param name="prompt">the prompt sent by the service</param>
        /// <param name="jeton">the token carried by the call</param>
        /// <returns>the prepared text</returns>
        public Task<string> GenererAsync(PromptGeneration prompt, CancellationToken jeton)
        {
            this.promptRecu = prompt;
            this.nombreAppels++;

            if (this.erreur != null)
            {
                throw this.erreur;
            }

            return Task.FromResult(this.texte);
        }
    }
}
