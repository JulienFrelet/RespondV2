using RespondMetier.Models;
using RespondMetier.Services.Interfaces;
using RespondMetier.Services.Realisations;

namespace RespondTests.Services
{
    /// <summary>
    /// Tests for the AnalyseurReponse, which splits a model answer in two.
    /// Every case here was observed with a real model: the format is asked
    /// for in the prompt, not guaranteed.
    /// </summary>
    public class AnalyseurReponseTests
    {
        private readonly IAnalyseurReponse analyseur = new AnalyseurReponse();

        #region Cas nominal

        /// <summary>
        /// The expected format gives a subject and a body
        /// </summary>
        [Fact]
        public void FormatAttenduSeparéCorrectement()
        {
            ResultatGeneration resultat = this.analyseur.Separer(
                "OBJET: Re: votre commande\n\nBonjour,\n\nVotre colis part demain.\n\nCordialement,");

            Assert.Equal("Re: votre commande", resultat.Objet);
            Assert.StartsWith("Bonjour,", resultat.Corps);
            Assert.EndsWith("Cordialement,", resultat.Corps);
            Assert.DoesNotContain("OBJET", resultat.Corps);
        }

        /// <summary>
        /// Windows line endings are handled like Unix ones
        /// </summary>
        [Fact]
        public void FinsDeLigneWindowsGerees()
        {
            ResultatGeneration resultat = this.analyseur.Separer(
                "OBJET: Test\r\n\r\nBonjour,");

            Assert.Equal("Test", resultat.Objet);
            Assert.Equal("Bonjour,", resultat.Corps);
        }

        #endregion

        #region Variantes de format

        /// <summary>
        /// The markdown emphasis models add on their own is stripped
        /// </summary>
        [Theory]
        [InlineData("**OBJET:** Re: devis")]
        [InlineData("**OBJET :** Re: devis")]
        [InlineData("*OBJET:* Re: devis")]
        [InlineData("OBJET: **Re: devis**")]
        public void EmphaseMarkdownRetiree(string ligne)
        {
            ResultatGeneration resultat = this.analyseur.Separer(ligne + "\n\nBonjour,");

            Assert.Equal("Re: devis", resultat.Objet);
        }

        /// <summary>
        /// The English spelling and a lowercase one are both accepted
        /// </summary>
        [Theory]
        [InlineData("SUBJECT: Re: order")]
        [InlineData("Objet: Re: order")]
        [InlineData("objet : Re: order")]
        public void OrthographesAlternativesAcceptees(string ligne)
        {
            ResultatGeneration resultat = this.analyseur.Separer(ligne + "\n\nHello,");

            Assert.Equal("Re: order", resultat.Objet);
        }

        /// <summary>
        /// Quotes around the subject are removed
        /// </summary>
        [Theory]
        [InlineData("OBJET: \"Re: commande\"")]
        [InlineData("OBJET: «Re: commande»")]
        public void GuillemetsRetires(string ligne)
        {
            ResultatGeneration resultat = this.analyseur.Separer(ligne + "\n\nBonjour,");

            Assert.Equal("Re: commande", resultat.Objet);
        }

        /// <summary>
        /// A short preamble before the subject is tolerated
        /// </summary>
        [Fact]
        public void PreambuleAvantObjetTolere()
        {
            ResultatGeneration resultat = this.analyseur.Separer(
                "Voici la réponse :\n\nOBJET: Re: commande\n\nBonjour,");

            Assert.Equal("Re: commande", resultat.Objet);
            Assert.Equal("Bonjour,", resultat.Corps);
        }

        #endregion

        #region Absence d objet

        /// <summary>
        /// Without a subject, the whole answer is kept as the body.
        /// Losing a reply the model did produce would be the worst outcome.
        /// </summary>
        [Fact]
        public void SansObjetToutLeTexteDevientLeCorps()
        {
            ResultatGeneration resultat = this.analyseur.Separer("Bonjour,\n\nVotre colis part demain.");

            Assert.Equal(string.Empty, resultat.Objet);
            Assert.Equal("Bonjour,\n\nVotre colis part demain.", resultat.Corps);
        }

        /// <summary>
        /// A subject appearing too late is not treated as one: it would most
        /// likely be a line of the body
        /// </summary>
        [Fact]
        public void ObjetTropLoinIgnore()
        {
            string texte = string.Join('\n', Enumerable.Repeat("ligne", 10))
                + "\nOBJET: trop tard";

            ResultatGeneration resultat = this.analyseur.Separer(texte);

            Assert.Equal(string.Empty, resultat.Objet);
            Assert.Contains("OBJET: trop tard", resultat.Corps);
        }

        /// <summary>
        /// An empty or blank answer gives an empty result rather than a crash
        /// </summary>
        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData(null)]
        public void TexteVideDonneResultatVide(string? texte)
        {
            ResultatGeneration resultat = this.analyseur.Separer(texte!);

            Assert.Equal(string.Empty, resultat.Objet);
            Assert.Equal(string.Empty, resultat.Corps);
        }

        #endregion
    }
}
