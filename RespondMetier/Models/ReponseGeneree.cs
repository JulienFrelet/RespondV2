using System;

namespace RespondMetier.Models
{
    /// <summary>
    /// A reply the application produced, kept so the user can find it again.
    /// </summary>
    /// <remarks>
    /// The profile and provider are stored by name rather than by reference:
    /// a history entry must stay readable even after the profile that produced
    /// it has been renamed or deleted.
    /// </remarks>
    public class ReponseGeneree
    {
        #region Attributs

        private int id;
        private DateTime date;
        private string nomProfil;
        private string nomCleApi;
        private string objet;
        private string destinataire;
        private string texte;
        private string sessionId;

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the entry identifier
        /// </summary>
        public int Id
        {
            get { return id; }
            set { id = value; }
        }

        /// <summary>
        /// Gets or sets the moment the reply was generated
        /// </summary>
        public DateTime Date
        {
            get { return date; }
            set { date = value; }
        }

        /// <summary>
        /// Gets or sets the name of the profile used, kept as plain text
        /// </summary>
        public string NomProfil
        {
            get { return nomProfil; }
            set { nomProfil = value; }
        }

        /// <summary>
        /// Gets or sets the name of the credential used, kept as plain text
        /// </summary>
        public string NomCleApi
        {
            get { return nomCleApi; }
            set { nomCleApi = value; }
        }

        /// <summary>
        /// Gets or sets the subject line the model produced
        /// </summary>
        public string Objet
        {
            get { return objet; }
            set { objet = value; }
        }

        /// <summary>
        /// Gets or sets the email address detected in the incoming message
        /// </summary>
        public string Destinataire
        {
            get { return destinataire; }
            set { destinataire = value; }
        }

        /// <summary>
        /// Gets or sets the body of the generated reply
        /// </summary>
        public string Texte
        {
            get { return texte; }
            set { texte = value; }
        }

        /// <summary>
        /// Gets or sets the visitor this entry belongs to.
        /// </summary>
        /// <remarks>
        /// Every query is filtered on this value. Without it the history would
        /// be shared by every visitor of the public demonstration, and anyone
        /// could read the emails someone else pasted.
        /// </remarks>
        public string SessionId
        {
            get { return sessionId; }
            set { sessionId = value; }
        }

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes an empty history entry
        /// </summary>
        public ReponseGeneree()
        {
            id = 0;
            date = DateTime.Now;
            nomProfil = string.Empty;
            nomCleApi = string.Empty;
            objet = string.Empty;
            destinataire = string.Empty;
            texte = string.Empty;
            sessionId = string.Empty;
        }

        #endregion
    }
}
