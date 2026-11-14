using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using RespondData;
using RespondData.DAO;
using RespondMetier.Models;

namespace RespondTests.DAO
{
    /// <summary>
    /// Tests for the HistoriqueDAO, run against an in-memory SQLite database.
    /// xUnit creates a new instance of this class for each test, so each test gets an empty database.
    /// </summary>
    public class HistoriqueDAOTests : IDisposable
    {
        private readonly SqliteConnection connexion;
        private readonly DbContextOptions<AppDbContext> options;

        private const string Session = "session-visiteur-a";
        private const string AutreSession = "session-visiteur-b";

        private static readonly DateTime dateTest = new DateTime(2026, 11, 14, 9, 0, 0);

        /// <summary>
        /// Opens an in-memory database and creates the tables
        /// </summary>
        public HistoriqueDAOTests()
        {
            connexion = new SqliteConnection("Data Source=:memory:");
            connexion.Open();

            options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite(connexion)
                .Options;

            using AppDbContext context = new AppDbContext(options);
            context.Database.EnsureCreated();
        }

        /// <summary>
        /// Creates a new context on the same database (forces EF to really read the database)
        /// </summary>
        private AppDbContext NouveauContexte() => new AppDbContext(options);

        /// <summary>
        /// Adds a history entry whose date is offset from a fixed reference,
        /// so that ordering is deterministic and does not depend on the clock
        /// </summary>
        private int Inserer(string objet, int decalageHeures,
            string texte = "Bonjour, voici la reponse.", string session = Session)
        {
            using AppDbContext context = NouveauContexte();

            ReponseGeneree reponse = new ReponseGeneree();
            reponse.Date = dateTest.AddHours(decalageHeures);
            reponse.Objet = objet;
            reponse.Texte = texte;
            reponse.NomProfil = "Accueil";
            reponse.NomCleApi = "Groq principal";
            reponse.SessionId = session;

            new HistoriqueDAO(context).Inserer(reponse);
            return reponse.Id;
        }

        /// <summary>
        /// Closes the connection, which deletes the in-memory database
        /// </summary>
        public void Dispose()
        {
            connexion.Dispose();
        }

        #region Lister

        /// <summary>
        /// Lister returns an empty list when the history is empty
        /// </summary>
        [Fact]
        public void ListerBaseVide()
        {
            using AppDbContext context = NouveauContexte();

            Assert.Empty(new HistoriqueDAO(context).Lister(Session));
        }

        /// <summary>
        /// Lister returns the most recent entry first
        /// </summary>
        [Fact]
        public void ListerTriePlusRecentEnPremier()
        {
            Inserer("Ancienne", 0);
            Inserer("Recente", 5);
            Inserer("Intermediaire", 2);

            using AppDbContext context = NouveauContexte();
            List<ReponseGeneree> resultat = new HistoriqueDAO(context).Lister(Session);

            Assert.Equal(["Recente", "Intermediaire", "Ancienne"], resultat.Select(reponse => reponse.Objet));
        }

        #endregion

        #region ListerPremiers

        /// <summary>
        /// ListerPremiers keeps only the requested number of entries
        /// </summary>
        [Fact]
        public void ListerPremiersLimiteLeNombre()
        {
            Inserer("Un", 1);
            Inserer("Deux", 2);
            Inserer("Trois", 3);

            using AppDbContext context = NouveauContexte();
            List<ReponseGeneree> resultat = new HistoriqueDAO(context).ListerPremiers(Session, 2);

            Assert.Equal(["Trois", "Deux"], resultat.Select(reponse => reponse.Objet));
        }

        /// <summary>
        /// ListerPremiers returns nothing when asked for zero or fewer entries
        /// </summary>
        [Theory]
        [InlineData(0)]
        [InlineData(-5)]
        public void ListerPremiersSansResultat(int nbMax)
        {
            Inserer("Un", 1);

            using AppDbContext context = NouveauContexte();

            Assert.Empty(new HistoriqueDAO(context).ListerPremiers(Session, nbMax));
        }

        #endregion

        #region Rechercher

        /// <summary>
        /// Rechercher matches on the subject, ignoring case
        /// </summary>
        [Fact]
        public void RechercheSurObjetIgnoreLaCasse()
        {
            Inserer("Retard de livraison", 1);
            Inserer("Demande de devis", 2);

            using AppDbContext context = NouveauContexte();
            List<ReponseGeneree> resultat = new HistoriqueDAO(context).Rechercher(Session, "LIVRAISON");

            Assert.Single(resultat);
            Assert.Equal("Retard de livraison", resultat[0].Objet);
        }

        /// <summary>
        /// Rechercher also looks inside the body of the reply
        /// </summary>
        [Fact]
        public void RechercheSurTexte()
        {
            Inserer("Un", 1, "Votre colis arrive mardi.");
            Inserer("Deux", 2, "Voici notre devis.");

            using AppDbContext context = NouveauContexte();
            List<ReponseGeneree> resultat = new HistoriqueDAO(context).Rechercher(Session, "colis");

            Assert.Single(resultat);
            Assert.Equal("Un", resultat[0].Objet);
        }

        /// <summary>
        /// Rechercher with a blank keyword returns the whole history rather than nothing
        /// </summary>
        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public void RechercheVideRenvoieTout(string motCle)
        {
            Inserer("Un", 1);
            Inserer("Deux", 2);

            using AppDbContext context = NouveauContexte();

            Assert.Equal(2, new HistoriqueDAO(context).Rechercher(Session, motCle).Count);
        }

        #endregion

        #region LimiterA

        /// <summary>
        /// LimiterA deletes the oldest entries beyond the limit
        /// </summary>
        [Fact]
        public void LimiterASupprimeLesPlusAnciennes()
        {
            Inserer("Ancienne", 1);
            Inserer("Intermediaire", 2);
            Inserer("Recente", 3);

            using (AppDbContext context = NouveauContexte())
            {
                new HistoriqueDAO(context).LimiterA(Session, 2);
            }

            using AppDbContext verification = NouveauContexte();
            List<ReponseGeneree> restantes = new HistoriqueDAO(verification).Lister(Session);

            Assert.Equal(["Recente", "Intermediaire"], restantes.Select(reponse => reponse.Objet));
        }

        /// <summary>
        /// LimiterA does nothing when the history is already short enough
        /// </summary>
        [Fact]
        public void LimiterANeToucheRienSousLaLimite()
        {
            Inserer("Un", 1);

            using (AppDbContext context = NouveauContexte())
            {
                new HistoriqueDAO(context).LimiterA(Session, 20);
            }

            using AppDbContext verification = NouveauContexte();

            Assert.Single(new HistoriqueDAO(verification).Lister(Session));
        }

        /// <summary>
        /// LimiterA with zero empties the history
        /// </summary>
        [Fact]
        public void LimiterAZeroVideTout()
        {
            Inserer("Un", 1);
            Inserer("Deux", 2);

            using (AppDbContext context = NouveauContexte())
            {
                new HistoriqueDAO(context).LimiterA(Session, 0);
            }

            using AppDbContext verification = NouveauContexte();

            Assert.Empty(new HistoriqueDAO(verification).Lister(Session));
        }

        /// <summary>
        /// LimiterA refuses a negative limit rather than silently emptying the history
        /// </summary>
        [Fact]
        public void LimiterANegatifLeve()
        {
            using AppDbContext context = NouveauContexte();
            HistoriqueDAO dao = new HistoriqueDAO(context);

            Assert.Throws<ArgumentOutOfRangeException>(() => dao.LimiterA(Session, -1));
        }

        #endregion

        #region Supprimer

        /// <summary>
        /// Supprimer removes a single entry
        /// </summary>
        [Fact]
        public void SupprimerRetireUneSeuleEntree()
        {
            int id = Inserer("Un", 1);
            Inserer("Deux", 2);

            using (AppDbContext context = NouveauContexte())
            {
                new HistoriqueDAO(context).Supprimer(id, Session);
            }

            using AppDbContext verification = NouveauContexte();
            List<ReponseGeneree> restantes = new HistoriqueDAO(verification).Lister(Session);

            Assert.Single(restantes);
            Assert.Equal("Deux", restantes[0].Objet);
        }

        /// <summary>
        /// Supprimer refuses an identifier that does not exist
        /// </summary>
        [Fact]
        public void SupprimerEntreeInexistanteLeve()
        {
            using AppDbContext context = NouveauContexte();
            HistoriqueDAO dao = new HistoriqueDAO(context);

            Assert.Throws<InvalidOperationException>(() => dao.Supprimer(404, Session));
        }

        #endregion

        #region Cloisonnement par visiteur

        /// <summary>
        /// A visitor never sees another visitor's entries. This is the most
        /// important test of the class: the history holds the emails people
        /// pasted, and the application has no accounts to separate them.
        /// </summary>
        [Fact]
        public void UnVisiteurNeVoitPasLHistoriqueDUnAutre()
        {
            Inserer("A moi", 1, session: Session);
            Inserer("A l'autre", 2, session: AutreSession);

            using AppDbContext context = NouveauContexte();
            List<ReponseGeneree> miennes = new HistoriqueDAO(context).Lister(Session);

            Assert.Single(miennes);
            Assert.Equal("A moi", miennes[0].Objet);
        }

        /// <summary>
        /// The search never reaches across visitors either
        /// </summary>
        [Fact]
        public void RechercheNeTraversePasLesSessions()
        {
            Inserer("Secret", 1, "Information confidentielle", AutreSession);

            using AppDbContext context = NouveauContexte();

            Assert.Empty(new HistoriqueDAO(context).Rechercher(Session, "confidentielle"));
        }

        /// <summary>
        /// Deleting another visitor's entry is refused, and reported as absent
        /// so that its existence stays unknowable
        /// </summary>
        [Fact]
        public void SupprimerLEntreeDUnAutreEstRefuse()
        {
            int id = Inserer("A l'autre", 1, session: AutreSession);

            using AppDbContext context = NouveauContexte();
            HistoriqueDAO dao = new HistoriqueDAO(context);

            Assert.Throws<InvalidOperationException>(() => dao.Supprimer(id, Session));

            using AppDbContext verification = NouveauContexte();
            Assert.Single(new HistoriqueDAO(verification).Lister(AutreSession));
        }

        /// <summary>
        /// Trimming one visitor's history leaves another's untouched: a busy
        /// visitor must not push someone else's entries out
        /// </summary>
        [Fact]
        public void LimiterNeTouchePasLesAutresSessions()
        {
            Inserer("A moi 1", 1, session: Session);
            Inserer("A moi 2", 2, session: Session);
            Inserer("A l'autre", 3, session: AutreSession);

            using (AppDbContext context = NouveauContexte())
            {
                new HistoriqueDAO(context).LimiterA(Session, 1);
            }

            using AppDbContext verification = NouveauContexte();
            HistoriqueDAO dao = new HistoriqueDAO(verification);

            Assert.Single(dao.Lister(Session));
            Assert.Single(dao.Lister(AutreSession));
        }

        /// <summary>
        /// A blank session matches nothing rather than everything: a caller
        /// that forgot to pass one must not receive the whole table
        /// </summary>
        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public void SessionVideNeRenvoieRien(string session)
        {
            Inserer("A moi", 1, session: Session);

            using AppDbContext context = NouveauContexte();

            Assert.Empty(new HistoriqueDAO(context).Lister(session));
        }

        #endregion

    }
}
