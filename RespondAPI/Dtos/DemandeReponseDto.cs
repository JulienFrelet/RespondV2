using RespondMetier.Models;

namespace RespondAPI.Dtos
{
    /// <summary>
    /// What the web client sends to ask for a reply.
    /// </summary>
    /// <remarks>
    /// A type of its own rather than the business model, so that the shape of
    /// the HTTP contract can change without dragging the domain with it, and
    /// so that a client cannot set fields it has no business setting, such as
    /// an identifier or a credential secret.
    /// </remarks>
    public class DemandeReponseDto
    {
        #region Attributs

        private int profilId;
        private string emailRecu;
        private string contexte;
        private LongueurReponse longueur;
        private ReglesRedaction? regles;

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the identifier of the profile to write from
        /// </summary>
        public int ProfilId
        {
            get { return profilId; }
            set { profilId = value; }
        }

        /// <summary>
        /// Gets or sets the message to answer
        /// </summary>
        public string EmailRecu
        {
            get { return emailRecu; }
            set { emailRecu = value; }
        }

        /// <summary>
        /// Gets or sets extra facts for this reply only
        /// </summary>
        public string Contexte
        {
            get { return contexte; }
            set { contexte = value; }
        }

        /// <summary>
        /// Gets or sets how long the reply should be
        /// </summary>
        public LongueurReponse Longueur
        {
            get { return longueur; }
            set { longueur = value; }
        }

        /// <summary>
        /// Gets or sets the writing rules, or null to use the defaults
        /// </summary>
        public ReglesRedaction? Regles
        {
            get { return regles; }
            set { regles = value; }
        }

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes an empty request
        /// </summary>
        public DemandeReponseDto()
        {
            profilId = 0;
            emailRecu = string.Empty;
            contexte = string.Empty;
            longueur = LongueurReponse.Courte;
            regles = null;
        }

        #endregion
    }
}
