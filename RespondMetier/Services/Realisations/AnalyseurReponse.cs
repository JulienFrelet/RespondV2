using System.Text.RegularExpressions;
using RespondMetier.Models;
using RespondMetier.Services.Interfaces;

namespace RespondMetier.Services.Realisations
{
    /// <summary>
    /// Splits a model answer into a subject and a body
    /// </summary>
    public class AnalyseurReponse : IAnalyseurReponse
    {
        #region Constants

        /// <summary>
        /// How many leading lines are searched for the subject.
        /// </summary>
        /// <remarks>
        /// The prompt asks the model to start with the subject, but models
        /// sometimes add a short preamble. Searching a few lines absorbs that
        /// without risking a line from the body being taken for a subject.
        /// </remarks>
        private const int LignesExaminees = 8;

        /// <summary>
        /// Matches the subject line, tolerating the markdown emphasis models add
        /// on their own (<c>**OBJET:**</c>) and the English spelling
        /// </summary>
        private static readonly Regex MotifObjet = new Regex(
            @"^\*{0,2}(?:OBJET|SUBJECT|Objet)\s*:\*{0,2}\s*(.+)$",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        #endregion

        #region Methods

        public ResultatGeneration Separer(string texte)
        {
            ResultatGeneration resultat = new ResultatGeneration();

            if (string.IsNullOrWhiteSpace(texte))
            {
                return resultat;
            }

            string[] lignes = texte.Replace("\r\n", "\n").Split('\n');
            int limite = Math.Min(lignes.Length, LignesExaminees);

            for (int index = 0; index < limite; index++)
            {
                Match correspondance = MotifObjet.Match(lignes[index].Trim());

                if (!correspondance.Success)
                {
                    continue;
                }

                resultat.Objet = Nettoyer(correspondance.Groups[1].Value);
                resultat.Corps = string.Join('\n', lignes.Skip(index + 1)).TrimStart('\n').Trim();
                return resultat;
            }

            // No subject found. The body must still hold the whole answer:
            // returning nothing would lose a reply the model did produce.
            resultat.Corps = texte.Trim();
            return resultat;
        }

        /// <summary>
        /// Strips the emphasis and quotes models leave around a subject
        /// </summary>
        /// <param name="valeur">the captured subject</param>
        /// <returns>the subject without decoration</returns>
        private static string Nettoyer(string valeur)
        {
            string propre = valeur.Trim();
            propre = propre.Trim('*').Trim();
            propre = propre.Trim('"').Trim('«').Trim('»').Trim();
            return propre;
        }

        #endregion
    }
}
