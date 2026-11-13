using RespondMetier.Models;

namespace RespondMetier.Services.Interfaces
{
    /// <summary>
    /// Splits the raw text returned by a model into a subject and a body.
    /// </summary>
    /// <remarks>
    /// Separate from the generation service because it is the part most likely
    /// to need adjusting: models do not always honour the requested format,
    /// and each new wording they invent is a case to handle. Isolating it
    /// means those cases are unit tests, not debugging sessions.
    /// </remarks>
    public interface IAnalyseurReponse
    {
        #region Methods

        /// <summary>
        /// Separates the subject line from the body
        /// </summary>
        /// <param name="texte">the raw text returned by the model</param>
        /// <returns>
        /// the subject and the body. The subject is empty when none was found,
        /// and the body then holds the whole text rather than nothing
        /// </returns>
        ResultatGeneration Separer(string texte);

        #endregion
    }
}
