using System.ComponentModel.DataAnnotations;
using RespondMetier.Models;

namespace RespondAPI.Dtos
{
    /// <summary>
    /// What the client sends to create or update a profile.
    /// </summary>
    /// <remarks>
    /// Deliberately carries neither Id nor SessionId. The identifier comes
    /// from the route, the session from the cookie: a client that could set
    /// either would be able to write into another visitor's profiles.
    /// </remarks>
    public class ProfilDto
    {
        #region Attributs

        private string nom;
        private string service;
        private string entreprise;
        private string domaines;
        private string description;
        private string ton;
        private string langue;
        private Signature? signature;

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the display name of the profile
        /// </summary>
        [Required(ErrorMessage = "Le nom du profil est obligatoire.")]
        [StringLength(80, ErrorMessage = "Le nom ne peut pas dépasser 80 caractères.")]
        public string Nom
        {
            get { return nom; }
            set { nom = value; }
        }

        /// <summary>
        /// Gets or sets the job title the reply is written from
        /// </summary>
        [Required(ErrorMessage = "Le service ou poste est obligatoire.")]
        [StringLength(120, ErrorMessage = "Le service ne peut pas dépasser 120 caractères.")]
        public string Service
        {
            get { return service; }
            set { service = value; }
        }

        /// <summary>
        /// Gets or sets the company name
        /// </summary>
        [Required(ErrorMessage = "Le nom de l'entreprise est obligatoire.")]
        [StringLength(120, ErrorMessage = "L'entreprise ne peut pas dépasser 120 caractères.")]
        public string Entreprise
        {
            get { return entreprise; }
            set { entreprise = value; }
        }

        /// <summary>
        /// Gets or sets the business areas of the company
        /// </summary>
        [StringLength(300, ErrorMessage = "Les domaines ne peuvent pas dépasser 300 caractères.")]
        public string Domaines
        {
            get { return domaines; }
            set { domaines = value; }
        }

        /// <summary>
        /// Gets or sets a free description of the company
        /// </summary>
        [StringLength(1000, ErrorMessage = "La description ne peut pas dépasser 1000 caractères.")]
        public string Description
        {
            get { return description; }
            set { description = value; }
        }

        /// <summary>
        /// Gets or sets the communication tone
        /// </summary>
        [StringLength(120, ErrorMessage = "Le ton ne peut pas dépasser 120 caractères.")]
        public string Ton
        {
            get { return ton; }
            set { ton = value; }
        }

        /// <summary>
        /// Gets or sets the language the reply is written in
        /// </summary>
        [StringLength(40, ErrorMessage = "La langue ne peut pas dépasser 40 caractères.")]
        public string Langue
        {
            get { return langue; }
            set { langue = value; }
        }

        /// <summary>
        /// Gets or sets the signature block, or null to keep an empty one
        /// </summary>
        public Signature? Signature
        {
            get { return signature; }
            set { signature = value; }
        }

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes an empty profile request
        /// </summary>
        public ProfilDto()
        {
            nom = string.Empty;
            service = string.Empty;
            entreprise = string.Empty;
            domaines = string.Empty;
            description = string.Empty;
            ton = string.Empty;
            langue = "français";
            signature = null;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Builds the domain profile this request describes
        /// </summary>
        /// <param name="id">the identifier, zero when creating</param>
        /// <param name="sessionId">the owner, taken from the cookie</param>
        /// <returns>the profile to persist</returns>
        public Profil VersProfil(int id, string sessionId)
        {
            Profil profil = new Profil();
            profil.Id = id;
            profil.SessionId = sessionId;
            profil.Nom = this.nom.Trim();
            profil.Service = this.service.Trim();
            profil.Entreprise = this.entreprise.Trim();
            profil.Domaines = this.domaines.Trim();
            profil.Description = this.description.Trim();
            profil.Ton = this.ton.Trim();
            profil.Langue = string.IsNullOrWhiteSpace(this.langue) ? "français" : this.langue.Trim();

            if (this.signature != null)
            {
                profil.Signature = this.signature;
            }

            return profil;
        }

        #endregion
    }
}
