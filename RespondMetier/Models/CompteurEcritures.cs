namespace RespondMetier.Models
{
    /// <summary>
    /// Counts the writes made from one client address during one time window
    /// </summary>
    /// <remarks>
    /// Le compteur vit en base et non en memoire.
    ///
    /// La limitation fournie par le serveur garde ses compteurs dans le
    /// processus. L'hebergement gratuit endort le service apres quelques
    /// minutes sans visite, et chaque reveil redemarre le processus : les
    /// compteurs repartent alors a zero, et il suffit d'attendre pour ecrire
    /// de nouveau sans limite. Le defaut ne se voyait pas en developpement,
    /// ou le processus ne s'arrete jamais.
    ///
    /// En base, le compteur survit au redemarrage et serait partage par
    /// plusieurs instances.
    /// </remarks>
    public class CompteurEcritures
    {
        #region Attributs

        private int id;
        private string cle;
        private DateTime debutFenetre;
        private int nombre;

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the counter identifier
        /// </summary>
        public int Id
        {
            get { return id; }
            set { id = value; }
        }

        /// <summary>
        /// Gets or sets what the writes are counted against, the client address
        /// </summary>
        /// <remarks>
        /// L'adresse, et non la session : une session n'est qu'un cookie, que
        /// l'on peut ne pas renvoyer pour repartir a zero autant de fois que
        /// l'on veut.
        /// </remarks>
        public string Cle
        {
            get { return cle; }
            set { cle = value; }
        }

        /// <summary>
        /// Gets or sets when the current window started, in UTC
        /// </summary>
        public DateTime DebutFenetre
        {
            get { return debutFenetre; }
            set { debutFenetre = value; }
        }

        /// <summary>
        /// Gets or sets how many writes were made during the current window
        /// </summary>
        public int Nombre
        {
            get { return nombre; }
            set { nombre = value; }
        }

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes an empty counter
        /// </summary>
        public CompteurEcritures()
        {
            id = 0;
            cle = string.Empty;
            debutFenetre = DateTime.UtcNow;
            nombre = 0;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Tells whether the current window has elapsed at the given instant
        /// </summary>
        /// <param name="maintenant">the current instant, in UTC</param>
        /// <param name="fenetre">how long a window lasts</param>
        /// <returns>true when a new window must start</returns>
        public bool FenetreExpiree(DateTime maintenant, TimeSpan fenetre)
        {
            return maintenant - debutFenetre >= fenetre;
        }

        #endregion
    }
}
