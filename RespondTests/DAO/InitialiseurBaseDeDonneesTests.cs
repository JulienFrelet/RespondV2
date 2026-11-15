using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using RespondData;
using RespondMetier.Models;

namespace RespondTests.DAO
{
    /// <summary>
    /// Tests for the seeding of the demonstration profiles
    /// </summary>
    public class InitialiseurBaseDeDonneesTests : IDisposable
    {
        private readonly SqliteConnection connexion;
        private readonly DbContextOptions<AppDbContext> options;

        /// <summary>
        /// Opens an in-memory database and creates the tables
        /// </summary>
        public InitialiseurBaseDeDonneesTests()
        {
            connexion = new SqliteConnection("Data Source=:memory:");
            connexion.Open();

            options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite(connexion)
                .Options;
        }

        private AppDbContext NouveauContexte() => new AppDbContext(options);

        private void Initialiser()
        {
            using AppDbContext context = NouveauContexte();
            new InitialiseurBaseDeDonnees(context).Initialiser();
        }

        private static List<Profil> Demonstration(AppDbContext context)
        {
            return context.Set<Profil>()
                .Where(profil => profil.SessionId == string.Empty)
                .ToList();
        }

        #region Base vierge

        [Fact]
        public void Initialiser_InsereLesProfilsDeDemonstration()
        {
            Initialiser();

            using AppDbContext context = NouveauContexte();
            List<Profil> profils = Demonstration(context);

            Assert.NotEmpty(profils);
            Assert.All(profils, profil => Assert.True(profil.EstProfilDeDemonstration));
        }

        /// <summary>
        /// Deux demarrages ne doivent pas doubler les profils.
        /// </summary>
        [Fact]
        public void Initialiser_NeDoublePasLesProfilsAuSecondDemarrage()
        {
            Initialiser();

            int apresLePremier;

            using (AppDbContext context = NouveauContexte())
            {
                apresLePremier = Demonstration(context).Count;
            }

            Initialiser();

            using (AppDbContext context = NouveauContexte())
            {
                Assert.Equal(apresLePremier, Demonstration(context).Count);
            }
        }

        #endregion

        #region Base deja remplie

        /// <summary>
        /// C'est le cas qui comptait : avec une insertion conditionnee a une
        /// table vide, un profil de demonstration ajoute apres coup
        /// n'apparaissait jamais en production. Il aurait fallu vider la base
        /// pour le voir, donc effacer l'historique de tous les visiteurs.
        /// </summary>
        [Fact]
        public void Initialiser_AjouteUnProfilManquantSurUneBaseDejaRemplie()
        {
            Initialiser();

            string retire;

            using (AppDbContext context = NouveauContexte())
            {
                Profil profil = Demonstration(context).First();
                retire = profil.Nom;
                context.Set<Profil>().Remove(profil);
                context.SaveChanges();
            }

            Initialiser();

            using (AppDbContext context = NouveauContexte())
            {
                Assert.Contains(Demonstration(context), profil => profil.Nom == retire);
            }
        }

        /// <summary>
        /// Les profils crees par un visiteur ne doivent ni bloquer le semis,
        /// ni etre touches par lui.
        /// </summary>
        [Fact]
        public void Initialiser_IgnoreLesProfilsDesVisiteurs()
        {
            using (AppDbContext context = NouveauContexte())
            {
                context.Database.EnsureCreated();

                Profil sien = new Profil();
                sien.Nom = "Mon profil";
                sien.Service = "Service";
                sien.Entreprise = "Entreprise";
                sien.Langue = "français";
                sien.SessionId = "session-visiteur";
                context.Set<Profil>().Add(sien);
                context.SaveChanges();
            }

            Initialiser();

            using (AppDbContext context = NouveauContexte())
            {
                Assert.NotEmpty(Demonstration(context));
                Assert.Single(context.Set<Profil>()
                    .Where(profil => profil.SessionId == "session-visiteur"));
            }
        }

        #endregion

        #region Schema deja en place

        /// <summary>
        /// Le cas qui a casse la production : EnsureCreated ne cree le schema
        /// que sur une base vide. Une table ajoutee au modele apres coup
        /// n'apparaissait jamais sur une base existante, et toute requete la
        /// touchant echouait sur « no such table ».
        /// </summary>
        [Fact]
        public void Initialiser_RecreeUneTableAbsenteDUneBaseExistante()
        {
            Initialiser();

            using (AppDbContext context = NouveauContexte())
            {
                context.Database.ExecuteSqlRaw("DROP TABLE \"CompteurEcritures\"");
            }

            Initialiser();

            using (AppDbContext context = NouveauContexte())
            {
                // Une requete suffit : elle echouerait si la table manquait.
                Assert.Empty(context.Set<CompteurEcritures>().ToList());
            }
        }

        /// <summary>
        /// Le mecanisme ajoute, il ne doit rien detruire : les donnees des
        /// tables deja presentes survivent a un second demarrage.
        /// </summary>
        [Fact]
        public void Initialiser_NeTouchePasAuxDonneesExistantes()
        {
            Initialiser();

            using (AppDbContext context = NouveauContexte())
            {
                CompteurEcritures compteur = new CompteurEcritures();
                compteur.Cle = "203.0.113.9";
                compteur.Nombre = 7;
                context.Set<CompteurEcritures>().Add(compteur);
                context.SaveChanges();
            }

            Initialiser();

            using (AppDbContext context = NouveauContexte())
            {
                CompteurEcritures? garde = context.Set<CompteurEcritures>()
                    .FirstOrDefault(c => c.Cle == "203.0.113.9");

                Assert.NotNull(garde);
                Assert.Equal(7, garde.Nombre);
            }
        }

        #endregion

        public void Dispose()
        {
            connexion.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
