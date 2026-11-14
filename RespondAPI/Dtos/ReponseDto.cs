namespace RespondAPI.Dtos
{
    /// <summary>
    /// What the API returns once a reply has been generated
    /// </summary>
    public class ReponseDto
    {
        #region Attributs

        private string objet;
        private string corps;

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the subject line, empty when the model gave none
        /// </summary>
        public string Objet
        {
            get { return objet; }
            set { objet = value; }
        }

        /// <summary>
        /// Gets or sets the body of the reply
        /// </summary>
        public string Corps
        {
            get { return corps; }
            set { corps = value; }
        }

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes an empty reply
        /// </summary>
        public ReponseDto()
        {
            objet = string.Empty;
            corps = string.Empty;
        }

        #endregion
    }
}
