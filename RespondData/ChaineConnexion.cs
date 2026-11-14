using System.Text;

namespace RespondData
{
    /// <summary>
    /// Identifies the database engine behind a connection string, and puts
    /// that string into the form its provider expects
    /// </summary>
    /// <remarks>
    /// Les hebergeurs qui fournissent une base PostgreSQL la publient sous
    /// forme d'URL, par exemple postgresql://utilisateur:secret@hote/base.
    /// Npgsql n'accepte pas cette forme : il attend des paires cle=valeur.
    /// Passer l'URL telle quelle echoue au demarrage sur un message qui parle
    /// de mot-cle inconnu, sans indiquer que le probleme est le format.
    ///
    /// La conversion est isolee ici plutot qu'ecrite dans le demarrage, parce
    /// qu'elle se teste : c'est une fonction pure.
    /// </remarks>
    public static class ChaineConnexion
    {
        #region Constantes

        /// <summary>
        /// The schemes a PostgreSQL connection URL may use
        /// </summary>
        private static readonly string[] SchemasPostgres = { "postgres://", "postgresql://" };

        #endregion

        #region Methods

        /// <summary>
        /// Tells whether a connection string designates a PostgreSQL database
        /// </summary>
        /// <param name="chaine">the configured connection string</param>
        /// <returns>true for PostgreSQL, false for SQLite</returns>
        public static bool EstPostgres(string chaine)
        {
            if (string.IsNullOrWhiteSpace(chaine))
            {
                return false;
            }

            string valeur = chaine.Trim();

            foreach (string schema in SchemasPostgres)
            {
                if (valeur.StartsWith(schema, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            // La forme cle=valeur de Npgsql se reconnait a son hote. SQLite,
            // lui, n'a jamais que Data Source.
            return valeur.Contains("Host=", StringComparison.OrdinalIgnoreCase)
                || valeur.Contains("Server=", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Converts a PostgreSQL connection URL into the key=value form Npgsql
        /// expects, and leaves any other string untouched
        /// </summary>
        /// <param name="chaine">the configured connection string</param>
        /// <returns>a string the provider accepts</returns>
        public static string Normaliser(string chaine)
        {
            if (string.IsNullOrWhiteSpace(chaine))
            {
                return string.Empty;
            }

            string valeur = chaine.Trim();
            bool estUrl = false;

            foreach (string schema in SchemasPostgres)
            {
                if (valeur.StartsWith(schema, StringComparison.OrdinalIgnoreCase))
                {
                    estUrl = true;
                }
            }

            if (!estUrl)
            {
                return valeur;
            }

            Uri adresse = new Uri(valeur);
            StringBuilder resultat = new StringBuilder();

            resultat.Append("Host=").Append(adresse.Host);

            if (adresse.Port > 0)
            {
                resultat.Append(";Port=").Append(adresse.Port);
            }

            string baseDeDonnees = adresse.AbsolutePath.TrimStart('/');

            if (baseDeDonnees.Length > 0)
            {
                resultat.Append(";Database=").Append(DesEchapper(baseDeDonnees));
            }

            string[] identifiants = adresse.UserInfo.Split(':', 2);

            if (identifiants[0].Length > 0)
            {
                resultat.Append(";Username=").Append(DesEchapper(identifiants[0]));
            }

            if (identifiants.Length > 1 && identifiants[1].Length > 0)
            {
                // Le mot de passe est pourcent-encode dans l'URL. Sans le
                // decoder, un mot de passe contenant un caractere special
                // serait transmis faux et l'authentification echouerait.
                resultat.Append(";Password=").Append(DesEchapper(identifiants[1]));
            }

            AjouterParametres(resultat, adresse.Query);

            return resultat.ToString();
        }

        /// <summary>
        /// Copies the query string parameters of the URL into the result
        /// </summary>
        /// <param name="resultat">the connection string being built</param>
        /// <param name="requete">the query part of the URL, leading ? included</param>
        private static void AjouterParametres(StringBuilder resultat, string requete)
        {
            if (string.IsNullOrEmpty(requete))
            {
                return;
            }

            foreach (string paire in requete.TrimStart('?').Split('&'))
            {
                if (paire.Length == 0)
                {
                    continue;
                }

                string[] morceaux = paire.Split('=', 2);
                string cle = DesEchapper(morceaux[0]);
                string valeur = morceaux.Length > 1 ? DesEchapper(morceaux[1]) : string.Empty;

                // sslmode est le seul parametre que les hebergeurs ajoutent et
                // que Npgsql nomme autrement.
                if (string.Equals(cle, "sslmode", StringComparison.OrdinalIgnoreCase))
                {
                    resultat.Append(";SSL Mode=").Append(valeur);
                }
                else
                {
                    resultat.Append(';').Append(cle).Append('=').Append(valeur);
                }
            }
        }

        /// <summary>
        /// Decodes the percent-encoded parts of a URL
        /// </summary>
        /// <param name="valeur">the encoded text</param>
        /// <returns>the decoded text</returns>
        private static string DesEchapper(string valeur)
        {
            return Uri.UnescapeDataString(valeur);
        }

        #endregion
    }
}
