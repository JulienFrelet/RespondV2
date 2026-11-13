namespace RespondAPI.Clients
{
    /// <summary>
    /// Configuration of the Groq client, bound from appsettings.
    /// </summary>
    /// <remarks>
    /// The list of models lives in configuration rather than in the code, so
    /// that a model retired by the provider can be replaced by editing a file
    /// and restarting, without rebuilding and redeploying the application.
    /// <para>
    /// The API key is deliberately absent from this class. It is read from the
    /// configuration under its own name so that it never ends up in an object
    /// that could be logged or serialised whole.
    /// </para>
    /// </remarks>
    public class OptionsGroq
    {
        #region Attributs

        private string urlDeBase;
        private List<string> modeles;
        private int delaiMaximalSecondes;
        private double temperature;

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the base address of the provider API
        /// </summary>
        public string UrlDeBase
        {
            get { return urlDeBase; }
            set { urlDeBase = value; }
        }

        /// <summary>
        /// Gets or sets the models tried in order until one answers
        /// </summary>
        public List<string> Modeles
        {
            get { return modeles; }
            set { modeles = value; }
        }

        /// <summary>
        /// Gets or sets how long a single generation may take, in seconds
        /// </summary>
        public int DelaiMaximalSecondes
        {
            get { return delaiMaximalSecondes; }
            set { delaiMaximalSecondes = value; }
        }

        /// <summary>
        /// Gets or sets the sampling temperature sent to the model
        /// </summary>
        public double Temperature
        {
            get { return temperature; }
            set { temperature = value; }
        }

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes the options with the values used when configuration is silent
        /// </summary>
        public OptionsGroq()
        {
            urlDeBase = "https://api.groq.com/openai/v1";
            modeles = new List<string>();
            delaiMaximalSecondes = 90;
            temperature = 0.4;
        }

        #endregion
    }
}
