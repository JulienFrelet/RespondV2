using RespondMetier.Models;

namespace RespondAPI.Dtos
{
    /// <summary>
    /// What the API returns when describing a history entry.
    /// </summary>
    /// <remarks>
    /// Exists for the same reason as <see cref="ProfilVueDto"/>: keeping the
    /// session identifier off the wire. The history is the most sensitive
    /// thing this application stores, so its responses are the last place to
    /// publish an internal identifier.
    /// </remarks>
    public class ReponseGenereeVueDto
    {
        #region Attributs

        private int id;
        private DateTime date;
        private string nomProfil;
        private string nomCleApi;
        private string objet;
        private string destinataire;
        private string texte;

        #endregion

        #region Properties

        /// <summary>Gets or sets the entry identifier</summary>
        public int Id
        {
            get { return id; }
            set { id = value; }
        }

        /// <summary>Gets or sets the moment the reply was generated</summary>
        public DateTime Date
        {
            get { return date; }
            set { date = value; }
        }

        /// <summary>Gets or sets the name of the profile used</summary>
        public string NomProfil
        {
            get { return nomProfil; }
            set { nomProfil = value; }
        }

        /// <summary>Gets or sets the label of the credential used, never its secret</summary>
        public string NomCleApi
        {
            get { return nomCleApi; }
            set { nomCleApi = value; }
        }

        /// <summary>Gets or sets the subject line</summary>
        public string Objet
        {
            get { return objet; }
            set { objet = value; }
        }

        /// <summary>Gets or sets the email address detected in the incoming message</summary>
        public string Destinataire
        {
            get { return destinataire; }
            set { destinataire = value; }
        }

        /// <summary>Gets or sets the body of the generated reply</summary>
        public string Texte
        {
            get { return texte; }
            set { texte = value; }
        }

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes an empty view of a history entry
        /// </summary>
        public ReponseGenereeVueDto()
        {
            id = 0;
            date = DateTime.MinValue;
            nomProfil = string.Empty;
            nomCleApi = string.Empty;
            objet = string.Empty;
            destinataire = string.Empty;
            texte = string.Empty;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Builds the view of a history entry
        /// </summary>
        /// <param name="reponse">the entry to describe</param>
        /// <returns>the view, without the session identifier</returns>
        public static ReponseGenereeVueDto Depuis(ReponseGeneree reponse)
        {
            ReponseGenereeVueDto vue = new ReponseGenereeVueDto();
            vue.Id = reponse.Id;
            vue.Date = reponse.Date;
            vue.NomProfil = reponse.NomProfil;
            vue.NomCleApi = reponse.NomCleApi;
            vue.Objet = reponse.Objet;
            vue.Destinataire = reponse.Destinataire;
            vue.Texte = reponse.Texte;
            return vue;
        }

        /// <summary>
        /// Builds the view of several history entries
        /// </summary>
        /// <param name="reponses">the entries to describe</param>
        /// <returns>the views, in the same order</returns>
        public static List<ReponseGenereeVueDto> Depuis(IEnumerable<ReponseGeneree> reponses)
        {
            return reponses.Select(Depuis).ToList();
        }

        #endregion
    }
}
