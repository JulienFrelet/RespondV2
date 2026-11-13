namespace RespondMetier.Models
{
    /// <summary>
    /// Writing rules applied to every generated reply: opening line,
    /// closing line, and any extra rule the user wants to add.
    /// </summary>
    public class ReglesRedaction
    {
        #region Attributs

        private bool utiliserFormuleOuverture;
        private string formuleOuverture;
        private bool utiliserFormuleCloture;
        private string formuleCloture;
        private string reglesPersonnalisees;

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets whether the reply must start with an opening line
        /// </summary>
        public bool UtiliserFormuleOuverture
        {
            get { return utiliserFormuleOuverture; }
            set { utiliserFormuleOuverture = value; }
        }

        /// <summary>
        /// Gets or sets the opening line, ex: Bonjour,
        /// </summary>
        public string FormuleOuverture
        {
            get { return formuleOuverture; }
            set { formuleOuverture = value; }
        }

        /// <summary>
        /// Gets or sets whether the reply must end with a closing line
        /// </summary>
        public bool UtiliserFormuleCloture
        {
            get { return utiliserFormuleCloture; }
            set { utiliserFormuleCloture = value; }
        }

        /// <summary>
        /// Gets or sets the closing line, ex: Cordialement,
        /// </summary>
        public string FormuleCloture
        {
            get { return formuleCloture; }
            set { formuleCloture = value; }
        }

        /// <summary>
        /// Gets or sets extra rules written by the user, one per line
        /// </summary>
        public string ReglesPersonnalisees
        {
            get { return reglesPersonnalisees; }
            set { reglesPersonnalisees = value; }
        }

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes the rules with the default French business formulas
        /// </summary>
        public ReglesRedaction()
        {
            utiliserFormuleOuverture = true;
            formuleOuverture = "Bonjour,";
            utiliserFormuleCloture = true;
            formuleCloture = "Cordialement,";
            reglesPersonnalisees = string.Empty;
        }

        #endregion
    }
}
