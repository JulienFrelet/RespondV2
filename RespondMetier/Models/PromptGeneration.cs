namespace RespondMetier.Models
{
    /// <summary>
    /// A prompt split in two parts, kept separate on purpose.
    /// </summary>
    /// <remarks>
    /// The instructions and the message to answer are never concatenated into
    /// a single block of text. They travel to the provider as two distinct
    /// messages: the instructions as the system message, the received email as
    /// the user message.
    /// <para>
    /// This is the structural half of the defence against prompt injection.
    /// Delimiting untrusted text inside a single prompt relies on the model
    /// respecting the delimiters, which is a convention, not a boundary.
    /// Putting the instructions in a different message gives the model a real
    /// signal about what is an order and what is data to analyse. Neither is a
    /// guarantee, which is why nothing is ever sent automatically: the user
    /// always reads the reply before it leaves.
    /// </para>
    /// </remarks>
    public class PromptGeneration
    {
        #region Attributs

        private string instructions;
        private string donneesAAnalyser;

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the instructions, sent as the system message
        /// </summary>
        public string Instructions
        {
            get { return instructions; }
            set { instructions = value; }
        }

        /// <summary>
        /// Gets or sets the untrusted text to analyse, sent as the user message
        /// </summary>
        public string DonneesAAnalyser
        {
            get { return donneesAAnalyser; }
            set { donneesAAnalyser = value; }
        }

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes an empty prompt
        /// </summary>
        public PromptGeneration()
        {
            instructions = string.Empty;
            donneesAAnalyser = string.Empty;
        }

        #endregion
    }
}
