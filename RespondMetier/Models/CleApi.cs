namespace RespondMetier.Models
{
    /// <summary>
    /// An API key the user registered for a given provider.
    /// </summary>
    /// <remarks>
    /// The secret itself only ever lives in memory while a request is being
    /// built. It is encrypted before it reaches the disk, and
    /// <see cref="ToString"/> is overridden so that printing or logging an
    /// instance can never reveal it by accident.
    /// </remarks>
    public class CleApi
    {
        #region Attributs

        private int id;
        private string libelle;
        private Fournisseur fournisseur;
        private string secret;

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the credential identifier
        /// </summary>
        public int Id
        {
            get { return id; }
            set { id = value; }
        }

        /// <summary>
        /// Gets or sets the name shown to the user, ex: Groq principal
        /// </summary>
        public string Libelle
        {
            get { return libelle; }
            set { libelle = value; }
        }

        /// <summary>
        /// Gets or sets the provider this key belongs to
        /// </summary>
        public Fournisseur Fournisseur
        {
            get { return fournisseur; }
            set { fournisseur = value; }
        }

        /// <summary>
        /// Gets or sets the secret key itself. Never write this value to a log,
        /// an error message or a crash report.
        /// </summary>
        public string Secret
        {
            get { return secret; }
            set { secret = value; }
        }

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes an empty credential
        /// </summary>
        public CleApi()
        {
            id = 0;
            libelle = string.Empty;
            fournisseur = Fournisseur.Groq;
            secret = string.Empty;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Returns a description that deliberately leaves the secret out
        /// </summary>
        /// <returns>the label and the provider, never the key</returns>
        public override string ToString()
        {
            return libelle + " (" + fournisseur + ")";
        }

        #endregion
    }
}
