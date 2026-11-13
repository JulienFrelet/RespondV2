using RespondMetier.Data.Interfaces;
using RespondMetier.Models;
using RespondMetier.Services.Interfaces;

namespace RespondMetier.Services.Realisations
{
    /// <summary>
    /// Orchestrates one generation: prompt, provider, parsing, history
    /// </summary>
    public class GenerationService : IGenerationService
    {
        #region Properties

        private readonly IConstructeurPrompt constructeur;
        private readonly IFournisseurIA fournisseur;
        private readonly IAnalyseurReponse analyseur;
        private readonly IHistoriqueDAO historique;
        private readonly int limiteHistorique;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the GenerationService class
        /// </summary>
        /// <param name="constructeur">builds the prompt from the request</param>
        /// <param name="fournisseur">sends the prompt to an AI provider</param>
        /// <param name="analyseur">splits the answer into subject and body</param>
        /// <param name="historique">stores the generated replies</param>
        /// <param name="limiteHistorique">how many entries the history keeps</param>
        public GenerationService(
            IConstructeurPrompt constructeur,
            IFournisseurIA fournisseur,
            IAnalyseurReponse analyseur,
            IHistoriqueDAO historique,
            int limiteHistorique)
        {
            this.constructeur = constructeur;
            this.fournisseur = fournisseur;
            this.analyseur = analyseur;
            this.historique = historique;
            this.limiteHistorique = limiteHistorique;
        }

        #endregion

        #region Methods

        public async Task<ResultatGeneration> GenererAsync(
            DemandeGeneration demande, string nomCleApi, CancellationToken jeton)
        {
            // Building the prompt validates the request, so an unusable request
            // fails here rather than after a round trip to the provider.
            PromptGeneration prompt = this.constructeur.Construire(demande);

            string texte = await this.fournisseur.GenererAsync(prompt, jeton);

            ResultatGeneration resultat = this.analyseur.Separer(texte);

            this.Enregistrer(demande, nomCleApi, resultat);

            return resultat;
        }

        /// <summary>
        /// Stores the reply, then trims the history to its configured size
        /// </summary>
        /// <param name="demande">the request that produced the reply</param>
        /// <param name="nomCleApi">the label of the credential used</param>
        /// <param name="resultat">the reply to store</param>
        private void Enregistrer(
            DemandeGeneration demande, string nomCleApi, ResultatGeneration resultat)
        {
            ReponseGeneree entree = new ReponseGeneree();
            entree.Date = DateTime.Now;
            entree.NomProfil = demande.Profil.Nom;
            entree.NomCleApi = nomCleApi;
            entree.Objet = resultat.Objet;
            entree.Texte = resultat.Corps;

            this.historique.Inserer(entree);
            this.historique.LimiterA(this.limiteHistorique);
        }

        #endregion
    }
}
