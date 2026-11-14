using RespondMetier.Data.Interfaces;
using RespondMetier.Models;

namespace RespondTests.Fakes
{
    /// <summary>
    /// Counter storage replaced by a dictionary held in the test.
    /// </summary>
    /// <remarks>
    /// Garder les compteurs dans le test permet de verifier la regle sans
    /// base de donnees, et surtout de fabriquer une fenetre deja ecoulee, ce
    /// qui prendrait une heure autrement.
    /// </remarks>
    internal class FauxCompteurEcrituresDAO : ICompteurEcrituresDAO
    {
        private readonly Dictionary<string, CompteurEcritures> compteurs;
        private int purges;

        /// <summary>
        /// Initializes an empty storage
        /// </summary>
        public FauxCompteurEcrituresDAO()
        {
            compteurs = new Dictionary<string, CompteurEcritures>();
            purges = 0;
        }

        /// <summary>
        /// Gets how many times the stale counters were purged
        /// </summary>
        public int Purges
        {
            get { return purges; }
        }

        /// <summary>
        /// Retrieves the counter of one client address
        /// </summary>
        /// <param name="cle">the client address</param>
        /// <returns>the counter, or null when none exists yet</returns>
        public CompteurEcritures? Charger(string cle)
        {
            return compteurs.TryGetValue(cle, out CompteurEcritures? compteur) ? compteur : null;
        }

        /// <summary>
        /// Saves a counter
        /// </summary>
        /// <param name="compteur">the counter to persist</param>
        public void Enregistrer(CompteurEcritures compteur)
        {
            compteurs[compteur.Cle] = compteur;
        }

        /// <summary>
        /// Deletes the counters whose window ended before the given instant
        /// </summary>
        /// <param name="avant">the instant before which counters are stale</param>
        public void Purger(DateTime avant)
        {
            purges++;

            foreach (string cle in compteurs
                .Where(paire => paire.Value.DebutFenetre < avant)
                .Select(paire => paire.Key)
                .ToList())
            {
                compteurs.Remove(cle);
            }
        }

        /// <summary>
        /// Moves a counter's window back in time, to simulate elapsed time
        /// </summary>
        /// <param name="cle">the client address</param>
        /// <param name="duree">how far back the window must be moved</param>
        public void Vieillir(string cle, TimeSpan duree)
        {
            compteurs[cle].DebutFenetre = compteurs[cle].DebutFenetre - duree;
        }

        /// <summary>
        /// Gets how many counters are stored
        /// </summary>
        public int Nombre
        {
            get { return compteurs.Count; }
        }
    }
}
