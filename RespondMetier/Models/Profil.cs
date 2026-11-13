namespace RespondMetier.Models
{
    /// <summary>
    /// Company context used to generate a reply: who the user is,
    /// what the company does, and how it talks to its customers.
    /// </summary>
    public class Profil
    {
        #region Attributs

        private int id;
        private string nom;
        private string service;
        private string entreprise;
        private string domaines;
        private string description;
        private string ton;
        private string langue;
        private Signature signature;

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the profile identifier
        /// </summary>
        public int Id
        {
            get { return id; }
            set { id = value; }
        }

        /// <summary>
        /// Gets or sets the display name of the profile, ex: Service client
        /// </summary>
        public string Nom
        {
            get { return nom; }
            set { nom = value; }
        }

        /// <summary>
        /// Gets or sets the job title the reply is written from
        /// </summary>
        public string Service
        {
            get { return service; }
            set { service = value; }
        }

        /// <summary>
        /// Gets or sets the company name
        /// </summary>
        public string Entreprise
        {
            get { return entreprise; }
            set { entreprise = value; }
        }

        /// <summary>
        /// Gets or sets the business areas of the company
        /// </summary>
        public string Domaines
        {
            get { return domaines; }
            set { domaines = value; }
        }

        /// <summary>
        /// Gets or sets a free description of the company
        /// </summary>
        public string Description
        {
            get { return description; }
            set { description = value; }
        }

        /// <summary>
        /// Gets or sets the communication tone, ex: professionnel et chaleureux
        /// </summary>
        public string Ton
        {
            get { return ton; }
            set { ton = value; }
        }

        /// <summary>
        /// Gets or sets the language the reply is written in
        /// </summary>
        public string Langue
        {
            get { return langue; }
            set { langue = value; }
        }

        /// <summary>
        /// Gets or sets the signature block of this profile
        /// </summary>
        public Signature Signature
        {
            get { return signature; }
            set { signature = value; }
        }

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes an empty profile with an empty signature
        /// </summary>
        public Profil()
        {
            id = 0;
            nom = string.Empty;
            service = string.Empty;
            entreprise = string.Empty;
            domaines = string.Empty;
            description = string.Empty;
            ton = string.Empty;
            langue = "français";
            signature = new Signature();
        }

        #endregion
    }
}
