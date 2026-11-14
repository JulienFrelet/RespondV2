using RespondData;

namespace RespondTests.DAO
{
    /// <summary>
    /// Tests for the conversion of a PostgreSQL connection URL into the form
    /// Npgsql expects
    /// </summary>
    public class ChaineConnexionTests
    {
        #region Detection du moteur

        [Theory]
        [InlineData("postgresql://moi:secret@hote.neon.tech/respond")]
        [InlineData("postgres://moi:secret@hote/respond")]
        [InlineData("POSTGRESQL://moi:secret@hote/respond")]
        [InlineData("Host=hote;Database=respond;Username=moi")]
        public void EstPostgres_ReconnaitPostgres(string chaine)
        {
            Assert.True(ChaineConnexion.EstPostgres(chaine));
        }

        [Theory]
        [InlineData("Data Source=respond.db")]
        [InlineData("Data Source=/donnees/respond.db")]
        [InlineData("Data Source=:memory:")]
        [InlineData("")]
        [InlineData("   ")]
        public void EstPostgres_LaisseSqliteAUneAutreBranche(string chaine)
        {
            Assert.False(ChaineConnexion.EstPostgres(chaine));
        }

        #endregion

        #region Conversion

        [Fact]
        public void Normaliser_TraduitUneUrlComplete()
        {
            string resultat = ChaineConnexion.Normaliser(
                "postgresql://julien:motdepasse@ep-abc.eu-central-1.aws.neon.tech:5432/respond?sslmode=require");

            Assert.Contains("Host=ep-abc.eu-central-1.aws.neon.tech", resultat);
            Assert.Contains("Port=5432", resultat);
            Assert.Contains("Database=respond", resultat);
            Assert.Contains("Username=julien", resultat);
            Assert.Contains("Password=motdepasse", resultat);
            Assert.Contains("SSL Mode=require", resultat);
        }

        /// <summary>
        /// Un mot de passe genere contient souvent un caractere special, que
        /// l'URL transporte pourcent-encode. Sans decodage il serait transmis
        /// faux, et l'authentification echouerait sur un message qui ne dit
        /// pas pourquoi.
        /// </summary>
        [Fact]
        public void Normaliser_DecodeLeMotDePasse()
        {
            string resultat = ChaineConnexion.Normaliser(
                "postgresql://moi:mot%40de%2Fpasse@hote/respond");

            Assert.Contains("Password=mot@de/passe", resultat);
        }

        /// <summary>
        /// sslmode est le nom employe par les hebergeurs, Npgsql attend
        /// SSL Mode. Transmis tel quel, il provoque une erreur de mot-cle
        /// inconnu au demarrage.
        /// </summary>
        [Fact]
        public void Normaliser_RenommeSslmode()
        {
            string resultat = ChaineConnexion.Normaliser(
                "postgresql://moi:secret@hote/respond?sslmode=require");

            Assert.Contains("SSL Mode=require", resultat);
            Assert.DoesNotContain("sslmode=", resultat);
        }

        [Fact]
        public void Normaliser_ConserveLesAutresParametres()
        {
            string resultat = ChaineConnexion.Normaliser(
                "postgresql://moi:secret@hote/respond?sslmode=require&channel_binding=require");

            Assert.Contains("channel_binding=require", resultat);
        }

        [Fact]
        public void Normaliser_AccepteUneUrlSansPort()
        {
            string resultat = ChaineConnexion.Normaliser("postgresql://moi:secret@hote/respond");

            Assert.Contains("Host=hote", resultat);
            Assert.Contains("Database=respond", resultat);
        }

        /// <summary>
        /// Une chaine deja au format attendu, ou une chaine SQLite, doit
        /// traverser la fonction sans etre touchee.
        /// </summary>
        [Theory]
        [InlineData("Data Source=respond.db")]
        [InlineData("Host=hote;Database=respond;Username=moi;Password=secret")]
        public void Normaliser_LaisseIntacteUneChaineDejaValide(string chaine)
        {
            Assert.Equal(chaine, ChaineConnexion.Normaliser(chaine));
        }

        [Fact]
        public void Normaliser_RendUneChaineVidePourUneEntreeVide()
        {
            Assert.Equal(string.Empty, ChaineConnexion.Normaliser("   "));
        }

        #endregion
    }
}
