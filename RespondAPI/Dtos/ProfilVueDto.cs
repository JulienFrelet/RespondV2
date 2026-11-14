using RespondMetier.Models;

namespace RespondAPI.Dtos
{
    /// <summary>
    /// What the API returns when describing a profile.
    /// </summary>
    /// <remarks>
    /// Exists to keep <see cref="Profil.SessionId"/> off the wire. Returning
    /// the domain model directly published the visitor's session identifier in
    /// every response, which undid the point of putting it in an HttpOnly
    /// cookie: JavaScript could not read the cookie, but could read the same
    /// value in the response body.
    /// <para>
    /// The identifier is not directly usable by an attacker, since the cookie
    /// carries a signed form of it and forging one needs the signing key. It
    /// is still an internal value with no reason to leave the server.
    /// </para>
    /// </remarks>
    public class ProfilVueDto
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
        private bool estProfilDeDemonstration;
        private Signature signature;

        #endregion

        #region Properties

        /// <summary>Gets or sets the profile identifier</summary>
        public int Id
        {
            get { return id; }
            set { id = value; }
        }

        /// <summary>Gets or sets the display name of the profile</summary>
        public string Nom
        {
            get { return nom; }
            set { nom = value; }
        }

        /// <summary>Gets or sets the job title the reply is written from</summary>
        public string Service
        {
            get { return service; }
            set { service = value; }
        }

        /// <summary>Gets or sets the company name</summary>
        public string Entreprise
        {
            get { return entreprise; }
            set { entreprise = value; }
        }

        /// <summary>Gets or sets the business areas of the company</summary>
        public string Domaines
        {
            get { return domaines; }
            set { domaines = value; }
        }

        /// <summary>Gets or sets a free description of the company</summary>
        public string Description
        {
            get { return description; }
            set { description = value; }
        }

        /// <summary>Gets or sets the communication tone</summary>
        public string Ton
        {
            get { return ton; }
            set { ton = value; }
        }

        /// <summary>Gets or sets the language the reply is written in</summary>
        public string Langue
        {
            get { return langue; }
            set { langue = value; }
        }

        /// <summary>
        /// Gets or sets whether this profile is read only.
        /// </summary>
        /// <remarks>
        /// Told to the client so the interface can hide the edit controls,
        /// never relied on as a permission: the server refuses the write
        /// whatever the client believes.
        /// </remarks>
        public bool EstProfilDeDemonstration
        {
            get { return estProfilDeDemonstration; }
            set { estProfilDeDemonstration = value; }
        }

        /// <summary>Gets or sets the signature block of this profile</summary>
        public Signature Signature
        {
            get { return signature; }
            set { signature = value; }
        }

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes an empty view of a profile
        /// </summary>
        public ProfilVueDto()
        {
            id = 0;
            nom = string.Empty;
            service = string.Empty;
            entreprise = string.Empty;
            domaines = string.Empty;
            description = string.Empty;
            ton = string.Empty;
            langue = string.Empty;
            estProfilDeDemonstration = false;
            signature = new Signature();
        }

        #endregion

        #region Methods

        /// <summary>
        /// Builds the view of a profile
        /// </summary>
        /// <param name="profil">the profile to describe</param>
        /// <returns>the view, without the session identifier</returns>
        public static ProfilVueDto Depuis(Profil profil)
        {
            ProfilVueDto vue = new ProfilVueDto();
            vue.Id = profil.Id;
            vue.Nom = profil.Nom;
            vue.Service = profil.Service;
            vue.Entreprise = profil.Entreprise;
            vue.Domaines = profil.Domaines;
            vue.Description = profil.Description;
            vue.Ton = profil.Ton;
            vue.Langue = profil.Langue;
            vue.EstProfilDeDemonstration = profil.EstProfilDeDemonstration;
            vue.Signature = profil.Signature;
            return vue;
        }

        /// <summary>
        /// Builds the view of several profiles
        /// </summary>
        /// <param name="profils">the profiles to describe</param>
        /// <returns>the views, in the same order</returns>
        public static List<ProfilVueDto> Depuis(IEnumerable<Profil> profils)
        {
            return profils.Select(Depuis).ToList();
        }

        #endregion
    }
}
