using RespondMetier.Exceptions;
using RespondMetier.Models;
using RespondMetier.Services.Realisations;
using RespondTests.Fakes;

namespace RespondTests.Services
{
    /// <summary>
    /// Tests for the GenerationService, run with fake collaborators.
    /// No network, no database: the service only owns the order of operations,
    /// and that is what is checked here.
    /// </summary>
    public class GenerationServiceTests
    {
        private const int LimiteHistorique = 20;
        private const string SecretFactice = "gsk_secret_de_test";

        /// <summary>
        /// Builds a usable request
        /// </summary>
        private static DemandeGeneration ConstruireDemande(string email = "Ou est ma commande ?")
        {
            DemandeGeneration demande = new DemandeGeneration();
            demande.Profil.Nom = "SAV";
            demande.Profil.Service = "Service client";
            demande.Profil.Entreprise = "Electromenager Plus";
            demande.EmailRecu = email;
            return demande;
        }

        /// <summary>
        /// Builds the credential handed to the service
        /// </summary>
        private static CleApi ConstruireCle()
        {
            CleApi cle = new CleApi();
            cle.Libelle = "Groq demo";
            cle.Fournisseur = Fournisseur.Groq;
            cle.Secret = SecretFactice;
            return cle;
        }

        /// <summary>
        /// Wires the service with the given provider and history
        /// </summary>
        private static GenerationService Construire(
            FauxFabriqueFournisseurIA fabrique, FauxHistoriqueDAO historique)
        {
            return new GenerationService(
                new ConstructeurPrompt(),
                fabrique,
                new AnalyseurReponse(),
                historique,
                LimiteHistorique);
        }

        #region Cas nominal

        /// <summary>
        /// A successful generation returns the subject and body separated
        /// </summary>
        [Fact]
        public async Task GenerationRenvoieObjetEtCorps()
        {
            FauxFabriqueFournisseurIA fabrique = new FauxFabriqueFournisseurIA(
                new FauxFournisseurIA("OBJET: Re: commande\n\nBonjour,\n\nVotre colis part demain."));
            FauxHistoriqueDAO historique = new FauxHistoriqueDAO();

            ResultatGeneration resultat = await Construire(fabrique, historique)
                .GenererAsync(ConstruireDemande(), ConstruireCle(), CancellationToken.None);

            Assert.Equal("Re: commande", resultat.Objet);
            Assert.Contains("Votre colis part demain.", resultat.Corps);
        }

        /// <summary>
        /// The prompt handed to the provider keeps the instructions and the
        /// received message in two separate parts
        /// </summary>
        [Fact]
        public async Task PromptTransmisAvecSeparationConservee()
        {
            FauxFournisseurIA fournisseur = new FauxFournisseurIA("OBJET: Re\n\nBonjour,");
            FauxFabriqueFournisseurIA fabrique = new FauxFabriqueFournisseurIA(fournisseur);
            FauxHistoriqueDAO historique = new FauxHistoriqueDAO();

            await Construire(fabrique, historique).GenererAsync(
                ConstruireDemande("Mon four est casse."), ConstruireCle(), CancellationToken.None);

            Assert.NotNull(fournisseur.PromptRecu);
            Assert.Equal("Mon four est casse.", fournisseur.PromptRecu.DonneesAAnalyser);
            Assert.Contains("Service client", fournisseur.PromptRecu.Instructions);
            Assert.DoesNotContain("Mon four est casse.", fournisseur.PromptRecu.Instructions);
        }

        /// <summary>
        /// The credential is passed on to the factory as given
        /// </summary>
        [Fact]
        public async Task CleTransmiseALaFabrique()
        {
            FauxFabriqueFournisseurIA fabrique = new FauxFabriqueFournisseurIA(
                new FauxFournisseurIA("OBJET: Re\n\nBonjour,"));
            FauxHistoriqueDAO historique = new FauxHistoriqueDAO();

            await Construire(fabrique, historique).GenererAsync(
                ConstruireDemande(), ConstruireCle(), CancellationToken.None);

            Assert.Equal(Fournisseur.Groq, fabrique.FournisseurDemande);
            Assert.Equal(SecretFactice, fabrique.CleDemandee);
        }

        #endregion

        #region Historique

        /// <summary>
        /// The reply is stored with the profile and credential names
        /// </summary>
        [Fact]
        public async Task ReponseEnregistreeDansHistorique()
        {
            FauxFabriqueFournisseurIA fabrique = new FauxFabriqueFournisseurIA(
                new FauxFournisseurIA("OBJET: Re: commande\n\nBonjour,"));
            FauxHistoriqueDAO historique = new FauxHistoriqueDAO();

            await Construire(fabrique, historique).GenererAsync(
                ConstruireDemande(), ConstruireCle(), CancellationToken.None);

            ReponseGeneree entree = Assert.Single(historique.Entrees);
            Assert.Equal("SAV", entree.NomProfil);
            Assert.Equal("Groq demo", entree.NomCleApi);
            Assert.Equal("Re: commande", entree.Objet);
            Assert.Contains("Bonjour,", entree.Texte);
        }

        /// <summary>
        /// The secret never reaches the history: only the label is stored.
        /// A history row must stay safe to read, export and display.
        /// </summary>
        [Fact]
        public async Task SecretJamaisEnregistreDansHistorique()
        {
            FauxFabriqueFournisseurIA fabrique = new FauxFabriqueFournisseurIA(
                new FauxFournisseurIA("OBJET: Re\n\nBonjour,"));
            FauxHistoriqueDAO historique = new FauxHistoriqueDAO();

            await Construire(fabrique, historique).GenererAsync(
                ConstruireDemande(), ConstruireCle(), CancellationToken.None);

            ReponseGeneree entree = Assert.Single(historique.Entrees);
            Assert.DoesNotContain(SecretFactice, entree.NomCleApi);
            Assert.DoesNotContain(SecretFactice, entree.Texte);
            Assert.DoesNotContain(SecretFactice, entree.Objet);
        }

        /// <summary>
        /// The history is trimmed after every insertion, so it cannot grow
        /// without bound between two manual cleanups
        /// </summary>
        [Fact]
        public async Task HistoriqueLimiteApresChaqueEnregistrement()
        {
            FauxFabriqueFournisseurIA fabrique = new FauxFabriqueFournisseurIA(
                new FauxFournisseurIA("OBJET: Re\n\nBonjour,"));
            FauxHistoriqueDAO historique = new FauxHistoriqueDAO();

            await Construire(fabrique, historique).GenererAsync(
                ConstruireDemande(), ConstruireCle(), CancellationToken.None);

            Assert.Equal(LimiteHistorique, historique.DerniereLimiteAppliquee);
        }

        #endregion

        #region Echecs

        /// <summary>
        /// An unusable request fails before the provider is called, so no quota
        /// is spent on a request that could never work
        /// </summary>
        [Fact]
        public async Task DemandeInvalideEchoueSansAppelerLeFournisseur()
        {
            FauxFournisseurIA fournisseur = new FauxFournisseurIA("jamais utilise");
            FauxFabriqueFournisseurIA fabrique = new FauxFabriqueFournisseurIA(fournisseur);
            FauxHistoriqueDAO historique = new FauxHistoriqueDAO();

            await Assert.ThrowsAsync<ErreurDemandeInvalide>(
                () => Construire(fabrique, historique).GenererAsync(
                    ConstruireDemande(""), ConstruireCle(), CancellationToken.None));

            Assert.Equal(0, fournisseur.NombreAppels);
            Assert.Null(fabrique.FournisseurDemande);
            Assert.Empty(historique.Entrees);
        }

        /// <summary>
        /// A provider failure is passed through untouched: the presentation
        /// layer decides what to show, the service does not swallow it
        /// </summary>
        [Fact]
        public async Task EchecFournisseurRemonteTelQuel()
        {
            FauxFabriqueFournisseurIA fabrique = new FauxFabriqueFournisseurIA(
                new FauxFournisseurIA(new ErreurQuotaDepasse("Quota atteint.")));
            FauxHistoriqueDAO historique = new FauxHistoriqueDAO();

            await Assert.ThrowsAsync<ErreurQuotaDepasse>(
                () => Construire(fabrique, historique).GenererAsync(
                    ConstruireDemande(), ConstruireCle(), CancellationToken.None));
        }

        /// <summary>
        /// A failed generation leaves nothing in the history: a reply that was
        /// never produced must not appear in the list
        /// </summary>
        [Fact]
        public async Task EchecNEnregistreRien()
        {
            FauxFabriqueFournisseurIA fabrique = new FauxFabriqueFournisseurIA(
                new FauxFournisseurIA(new ErreurFournisseurIndisponible("Injoignable.")));
            FauxHistoriqueDAO historique = new FauxHistoriqueDAO();

            await Assert.ThrowsAsync<ErreurFournisseurIndisponible>(
                () => Construire(fabrique, historique).GenererAsync(
                    ConstruireDemande(), ConstruireCle(), CancellationToken.None));

            Assert.Empty(historique.Entrees);
            Assert.Equal(-1, historique.DerniereLimiteAppliquee);
        }

        #endregion
    }
}
