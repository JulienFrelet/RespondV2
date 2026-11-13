using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using RespondData;
using RespondData.DAO;
using RespondMetier.Models;

namespace RespondTests.DAO
{
    /// <summary>
    /// Tests for the ProfilDAO, run against an in-memory SQLite database.
    /// xUnit creates a new instance of this class for each test, so each test gets an empty database.
    /// </summary>
    public class ProfilDAOTests : IDisposable
    {
        private readonly SqliteConnection connexion;
        private readonly DbContextOptions<AppDbContext> options;

        /// <summary>
        /// Opens an in-memory database and creates the tables
        /// </summary>
        public ProfilDAOTests()
        {
            // The in-memory database lives as long as this connection stays open
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
        /// Builds a usable profile without touching the database
        /// </summary>
        private static Profil Construire(string nom)
        {
            Profil profil = new Profil();
            profil.Nom = nom;
            profil.Service = "Service client";
            profil.Entreprise = "Electromenager Plus";
            profil.Domaines = "Vente et reparation";
            profil.Ton = "professionnel et chaleureux";
            profil.Signature.Nom = "Julien";
            profil.Signature.Telephone = "03 80 00 00 00";
            profil.Signature.InclureTelephone = false;
            return profil;
        }

        /// <summary>
        /// Adds a profile directly in the database and returns its generated id
        /// </summary>
        private int Inserer(string nom)
        {
            using AppDbContext context = NouveauContexte();
            Profil profil = Construire(nom);
            new ProfilDAO(context).Inserer(profil);
            return profil.Id;
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
        /// Lister returns an empty list when there is no profile
        /// </summary>
        [Fact]
        public void ListerBaseVide()
        {
            using AppDbContext context = NouveauContexte();

            List<Profil> resultat = new ProfilDAO(context).Lister();

            Assert.Empty(resultat);
        }

        /// <summary>
        /// Lister returns the profiles sorted by name
        /// </summary>
        [Fact]
        public void ListerTrieParNom()
        {
            Inserer("Service technique");
            Inserer("Accueil");
            Inserer("Facturation");

            using AppDbContext context = NouveauContexte();
            List<Profil> resultat = new ProfilDAO(context).Lister();

            Assert.Equal(["Accueil", "Facturation", "Service technique"], resultat.Select(profil => profil.Nom));
        }

        #endregion

        #region Inserer

        /// <summary>
        /// Inserer lets the database assign the identifier
        /// </summary>
        [Fact]
        public void InsererAttribueUnId()
        {
            int id = Inserer("Accueil");

            Assert.True(id > 0);
        }

        /// <summary>
        /// Inserer stores the signature in the same row and reads it back unchanged
        /// </summary>
        [Fact]
        public void InsererConserveLaSignature()
        {
            int id = Inserer("Accueil");

            using AppDbContext context = NouveauContexte();
            Profil? relu = new ProfilDAO(context).ChargerParId(id);

            Assert.NotNull(relu);
            Assert.Equal("Julien", relu.Signature.Nom);
            Assert.Equal("03 80 00 00 00", relu.Signature.Telephone);
            Assert.False(relu.Signature.InclureTelephone);
            Assert.True(relu.Signature.InclureNom);
        }

        /// <summary>
        /// Inserer refuses two profiles sharing the same name
        /// </summary>
        [Fact]
        public void InsererRefuseUnNomEnDouble()
        {
            Inserer("Accueil");

            using AppDbContext context = NouveauContexte();
            ProfilDAO dao = new ProfilDAO(context);

            Assert.Throws<DbUpdateException>(() => dao.Inserer(Construire("Accueil")));
        }

        #endregion

        #region ChargerParId

        /// <summary>
        /// ChargerParId returns null when no profile matches
        /// </summary>
        [Fact]
        public void ChargerParIdInexistant()
        {
            using AppDbContext context = NouveauContexte();

            Profil? resultat = new ProfilDAO(context).ChargerParId(404);

            Assert.Null(resultat);
        }

        #endregion

        #region MettreAJour

        /// <summary>
        /// MettreAJour writes the new values, signature included
        /// </summary>
        [Fact]
        public void MettreAJourModifieLesChamps()
        {
            int id = Inserer("Accueil");

            using (AppDbContext context = NouveauContexte())
            {
                Profil modifie = Construire("Accueil renomme");
                modifie.Id = id;
                modifie.Ton = "formel et professionnel";
                modifie.Signature.Email = "contact@exemple.fr";

                new ProfilDAO(context).MettreAJour(modifie);
            }

            using AppDbContext verification = NouveauContexte();
            Profil? relu = new ProfilDAO(verification).ChargerParId(id);

            Assert.NotNull(relu);
            Assert.Equal("Accueil renomme", relu.Nom);
            Assert.Equal("formel et professionnel", relu.Ton);
            Assert.Equal("contact@exemple.fr", relu.Signature.Email);
        }

        /// <summary>
        /// MettreAJour refuses a profile that does not exist
        /// </summary>
        [Fact]
        public void MettreAJourProfilInexistantLeve()
        {
            using AppDbContext context = NouveauContexte();
            ProfilDAO dao = new ProfilDAO(context);

            Profil absent = Construire("Fantome");
            absent.Id = 404;

            Assert.Throws<InvalidOperationException>(() => dao.MettreAJour(absent));
        }

        #endregion

        #region Supprimer

        /// <summary>
        /// Supprimer removes the profile from the database
        /// </summary>
        [Fact]
        public void SupprimerRetireLeProfil()
        {
            int id = Inserer("Accueil");

            using (AppDbContext context = NouveauContexte())
            {
                new ProfilDAO(context).Supprimer(id);
            }

            using AppDbContext verification = NouveauContexte();

            Assert.Empty(new ProfilDAO(verification).Lister());
        }

        /// <summary>
        /// Supprimer refuses an identifier that does not exist
        /// </summary>
        [Fact]
        public void SupprimerProfilInexistantLeve()
        {
            using AppDbContext context = NouveauContexte();
            ProfilDAO dao = new ProfilDAO(context);

            Assert.Throws<InvalidOperationException>(() => dao.Supprimer(404));
        }

        #endregion
    }
}
