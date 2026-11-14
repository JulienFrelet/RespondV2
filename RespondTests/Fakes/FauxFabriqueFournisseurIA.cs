using RespondMetier.Models;
using RespondMetier.Services.Interfaces;

namespace RespondTests.Fakes
{
    /// <summary>
    /// Provider factory replaced by one that always returns the same fake.
    /// </summary>
    /// <remarks>
    /// Records the provider and the key it was asked for, so a test can check
    /// that the service passed on the credential it was given.
    /// </remarks>
    internal class FauxFabriqueFournisseurIA : IFabriqueFournisseurIA
    {
        private readonly FauxFournisseurIA fournisseur;
        private Fournisseur? fournisseurDemande;
        private string cleDemandee;

        /// <summary>
        /// Initializes the factory with the fake provider it will hand out
        /// </summary>
        /// <param name="fournisseur">the fake provider to return</param>
        public FauxFabriqueFournisseurIA(FauxFournisseurIA fournisseur)
        {
            this.fournisseur = fournisseur;
            this.fournisseurDemande = null;
            this.cleDemandee = string.Empty;
        }

        /// <summary>
        /// Gets the provider that was asked for, null when never asked
        /// </summary>
        public Fournisseur? FournisseurDemande
        {
            get { return this.fournisseurDemande; }
        }

        /// <summary>
        /// Gets the key that was passed to the factory
        /// </summary>
        public string CleDemandee
        {
            get { return this.cleDemandee; }
        }

        /// <summary>
        /// Records the request then returns the fake provider
        /// </summary>
        /// <param name="fournisseur">the provider asked for</param>
        /// <param name="cle">the key asked for</param>
        /// <returns>the fake provider</returns>
        public IFournisseurIA Creer(Fournisseur fournisseur, string cle)
        {
            this.fournisseurDemande = fournisseur;
            this.cleDemandee = cle;
            return this.fournisseur;
        }
    }
}
