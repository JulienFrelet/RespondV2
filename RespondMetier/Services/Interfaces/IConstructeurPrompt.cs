using RespondMetier.Models;

namespace RespondMetier.Services.Interfaces
{
    /// <summary>
    /// Turns a generation request into the prompt sent to an AI provider.
    /// </summary>
    /// <remarks>
    /// Implementations must be pure: same request in, same prompt out, with no
    /// clock, no randomness, no network and no disk. That is what makes the
    /// prompt, which is the hardest part of this application to get right,
    /// testable by simple comparison.
    /// </remarks>
    public interface IConstructeurPrompt
    {
        #region Methods

        /// <summary>
        /// Builds the prompt for a request
        /// </summary>
        /// <param name="demande">the request to build the prompt from</param>
        /// <returns>the instructions and the text to analyse, kept separate</returns>
        /// <exception cref="Exceptions.ErreurDemandeInvalide">
        /// when the request has no message to answer, or one too long to send
        /// </exception>
        PromptGeneration Construire(DemandeGeneration demande);

        #endregion
    }
}
