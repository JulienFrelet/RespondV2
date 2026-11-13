namespace RespondMetier.Models
{
    /// <summary>
    /// Signature block appended at the end of a generated reply.
    /// Each line can be turned off individually by the user.
    /// </summary>
    public class Signature
    {
        #region Attributs

        private string nom;
        private string telephone;
        private string email;
        private bool inclureNom;
        private bool inclureService;
        private bool inclureEntreprise;
        private bool inclureTelephone;
        private bool inclureEmail;

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the full name of the sender
        /// </summary>
        public string Nom
        {
            get { return nom; }
            set { nom = value; }
        }

        /// <summary>
        /// Gets or sets the phone number shown in the signature
        /// </summary>
        public string Telephone
        {
            get { return telephone; }
            set { telephone = value; }
        }

        /// <summary>
        /// Gets or sets the email address shown in the signature
        /// </summary>
        public string Email
        {
            get { return email; }
            set { email = value; }
        }

        /// <summary>
        /// Gets or sets whether the sender name appears in the signature
        /// </summary>
        public bool InclureNom
        {
            get { return inclureNom; }
            set { inclureNom = value; }
        }

        /// <summary>
        /// Gets or sets whether the job title appears in the signature
        /// </summary>
        public bool InclureService
        {
            get { return inclureService; }
            set { inclureService = value; }
        }

        /// <summary>
        /// Gets or sets whether the company name appears in the signature
        /// </summary>
        public bool InclureEntreprise
        {
            get { return inclureEntreprise; }
            set { inclureEntreprise = value; }
        }

        /// <summary>
        /// Gets or sets whether the phone number appears in the signature
        /// </summary>
        public bool InclureTelephone
        {
            get { return inclureTelephone; }
            set { inclureTelephone = value; }
        }

        /// <summary>
        /// Gets or sets whether the email address appears in the signature
        /// </summary>
        public bool InclureEmail
        {
            get { return inclureEmail; }
            set { inclureEmail = value; }
        }

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes an empty signature with every line enabled
        /// </summary>
        public Signature()
        {
            nom = string.Empty;
            telephone = string.Empty;
            email = string.Empty;
            inclureNom = true;
            inclureService = true;
            inclureEntreprise = true;
            inclureTelephone = true;
            inclureEmail = true;
        }

        #endregion
    }
}