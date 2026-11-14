using RespondMetier.Services.Realisations;
using RespondTests.Fakes;

namespace RespondTests.Services
{
    /// <summary>
    /// Tests for the write limiter
    /// </summary>
    public class LimiteurEcrituresTests
    {
        private const string Adresse = "203.0.113.7";
        private const string AutreAdresse = "198.51.100.2";

        private static readonly TimeSpan UneHeure = TimeSpan.FromHours(1);

        private readonly FauxCompteurEcrituresDAO compteurs = new FauxCompteurEcrituresDAO();

        private LimiteurEcritures Limiteur(int limite)
        {
            return new LimiteurEcritures(compteurs, limite, UneHeure);
        }

        #region Dans la fenetre

        [Fact]
        public void Autoriser_LaisseLesEcrituresSousLaLimite()
        {
            LimiteurEcritures limiteur = Limiteur(3);

            Assert.True(limiteur.Autoriser(Adresse));
            Assert.True(limiteur.Autoriser(Adresse));
            Assert.True(limiteur.Autoriser(Adresse));
        }

        [Fact]
        public void Autoriser_RefuseAuDelaDeLaLimite()
        {
            LimiteurEcritures limiteur = Limiteur(3);

            limiteur.Autoriser(Adresse);
            limiteur.Autoriser(Adresse);
            limiteur.Autoriser(Adresse);

            Assert.False(limiteur.Autoriser(Adresse));
            Assert.False(limiteur.Autoriser(Adresse));
        }

        /// <summary>
        /// Deux visiteurs derriere deux adresses ne doivent pas se gener.
        /// </summary>
        [Fact]
        public void Autoriser_CompteChaqueAdresseSeparement()
        {
            LimiteurEcritures limiteur = Limiteur(2);

            limiteur.Autoriser(Adresse);
            limiteur.Autoriser(Adresse);

            Assert.False(limiteur.Autoriser(Adresse));
            Assert.True(limiteur.Autoriser(AutreAdresse));
        }

        #endregion

        #region Fenetre ecoulee

        [Fact]
        public void Autoriser_RepartDeZeroUneFoisLaFenetreEcoulee()
        {
            LimiteurEcritures limiteur = Limiteur(2);

            limiteur.Autoriser(Adresse);
            limiteur.Autoriser(Adresse);
            Assert.False(limiteur.Autoriser(Adresse));

            compteurs.Vieillir(Adresse, UneHeure);

            Assert.True(limiteur.Autoriser(Adresse));
        }

        /// <summary>
        /// Une fenetre presque ecoulee reste une fenetre en cours.
        /// </summary>
        [Fact]
        public void Autoriser_NeRepartPasAvantLaFinDeLaFenetre()
        {
            LimiteurEcritures limiteur = Limiteur(1);

            limiteur.Autoriser(Adresse);
            compteurs.Vieillir(Adresse, TimeSpan.FromMinutes(59));

            Assert.False(limiteur.Autoriser(Adresse));
        }

        #endregion

        #region Cas limites

        /// <summary>
        /// Une adresse que le serveur n'a pas su lire ne doit pas ouvrir une
        /// voie sans limite : toutes ces requetes partagent un compteur.
        /// </summary>
        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public void Autoriser_CompteAussiLesAdressesInconnues(string cle)
        {
            LimiteurEcritures limiteur = Limiteur(2);

            Assert.True(limiteur.Autoriser(cle));
            Assert.True(limiteur.Autoriser(cle));
            Assert.False(limiteur.Autoriser(cle));
        }

        /// <summary>
        /// Sans menage, la table garderait une ligne par adresse ayant ecrit
        /// un jour, pour toujours.
        /// </summary>
        [Fact]
        public void Autoriser_PurgeLesCompteursPerimes()
        {
            LimiteurEcritures limiteur = Limiteur(5);

            limiteur.Autoriser(Adresse);
            compteurs.Vieillir(Adresse, TimeSpan.FromHours(3));

            limiteur.Autoriser(AutreAdresse);

            Assert.Equal(1, compteurs.Nombre);
            Assert.Null(compteurs.Charger(Adresse));
        }

        /// <summary>
        /// Le menage ne doit pas tourner a chaque ecriture : une requete de
        /// suppression par ecriture couterait plus cher que ce qu'elle
        /// economise.
        /// </summary>
        [Fact]
        public void Autoriser_NePurgePasAChaqueEcriture()
        {
            LimiteurEcritures limiteur = Limiteur(10);

            limiteur.Autoriser(Adresse);
            limiteur.Autoriser(Adresse);
            limiteur.Autoriser(Adresse);

            Assert.Equal(1, compteurs.Purges);
        }

        #endregion
    }
}
