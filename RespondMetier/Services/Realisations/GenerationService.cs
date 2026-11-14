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
        private readonly IFabriqueFournisseurIA fabrique;
        private readonly IAnalyseurReponse analyseur;
        private readonly IHistoriqueDAO historique;
        private readonly int limiteHistorique;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the GenerationService class
        /// </summary>
        /// <param name="constructeur">builds the prompt from the request</param>
        /// <param name="fabrique">builds the client for the chosen provider</param>
        /// <param name="analyseur">splits the answer into subject and body</param>
        /// <param name="historique">stores the generated replies</param>
        /// <param name="limiteHistorique">how many entries the history keeps</param>
        public GenerationService(
            IConstructeurPrompt constructeur,
            IFabriqueFournisseurIA fabrique,
            IAnalyseurReponse analyseur,
            IHistoriqueDAO historique,
            int limiteHistorique)
        {
            this.constructeur = constructeur;
            this.fabrique = fabrique;
            this.analyseur = analyseur;
            this.historique = historique;
            this.limiteHistorique = limiteHistorique;
        }

        #endregion

        #region Methods

        public async Task<ResultatGeneration> GenererAsync(
            DemandeGeneration demande, CleApi cleApi, CancellationToken jeton)
        {
            // Building the prompt validates the request, so an unusable request
            // fails here rather than after a round trip to the provider.
            PromptGeneration prompt = this.constructeur.Construire(demande);

            IFournisseurIA fournisseur = this.fabrique.Creer(cleApi.Fournisseur, cleApi.Secret);

            string texte = await fournisseur.GenererAsync(prompt, jeton);

            ResultatGeneration resultat = this.analyseur.Separer(texte);

            this.Enregistrer(demande, cleApi, resultat);

            return resultat;
        }

        /// <summary>
        /// Stores the reply, then trims the history to its configured size
        /// </summary>
        /// <param name="demande">the request that produced the reply</param>
        /// <param name="cleApi">the credential used, stored by label only</param>
        /// <param name="resultat">the reply to store</param>
        private void Enregistrer(
            DemandeGeneration demande, CleApi cleApi, ResultatGeneration resultat)
        {
            ReponseGeneree entree = new ReponseGeneree();
            entree.Date = DateTime.Now;
            entree.NomProfil = demande.Profil.Nom;
            entree.SessionId = demande.SessionId;

            // The label, never the secret: a history row must stay safe to
            // read, to export and to display.
            entree.NomCleApi = cleApi.Libelle;

            entree.Objet = resultat.Objet;
            entree.Texte = resultat.Corps;

            this.historique.Inserer(entree);
            this.historique.LimiterA(demande.SessionId, this.limiteHistorique);
        }

        #endregion
    }
}
