namespace RespondMetier.Models
{
    /// <summary>
    /// Everything needed to produce one reply: the context to write from,
    /// and the message to answer.
    /// </summary>
    public class DemandeGeneration
    {
        #region Attributs

        private Profil profil;
        private ReglesRedaction regles;
        private LongueurReponse longueur;
        private string emailRecu;
        private string contexte;

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the company profile the reply is written from
        /// </summary>
        public Profil Profil
        {
            get { return profil; }
            set { profil = value; }
        }

        /// <summary>
        /// Gets or sets the writing rules to apply
        /// </summary>
        public ReglesRedaction Regles
        {
            get { return regles; }
            set { regles = value; }
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
        /// Gets or sets the message to answer.
        /// </summary>
        /// <remarks>
        /// This is untrusted input. It comes from whoever wrote the email and
        /// must never be treated as an instruction to the model.
        /// </remarks>
        public string EmailRecu
        {
            get { return emailRecu; }
            set { emailRecu = value; }
        }

        /// <summary>
        /// Gets or sets extra facts the user provides for this reply only,
        /// such as a tracking number or a delay
        /// </summary>
        public string Contexte
        {
            get { return contexte; }
            set { contexte = value; }
        }

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes an empty request with a short reply by default
        /// </summary>
        public DemandeGeneration()
        {
            profil = new Profil();
            regles = new ReglesRedaction();
            longueur = LongueurReponse.Courte;
            emailRecu = string.Empty;
            contexte = string.Empty;
        }

        #endregion
    }
}
