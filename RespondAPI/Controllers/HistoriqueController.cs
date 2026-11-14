using Microsoft.AspNetCore.Mvc;
using RespondAPI.Services;
using RespondMetier.Data.Interfaces;
using RespondMetier.Models;

namespace RespondAPI.Controllers
{
    /// <summary>
    /// Exposes the replies this visitor has generated.
    /// </summary>
    /// <remarks>
    /// Every route is scoped to the visitor's own session. The history holds
    /// the emails people pasted, so it is the most sensitive data the
    /// application stores.
    /// </remarks>
    [ApiController]
    [Route("api/historique")]
    public class HistoriqueController : ControllerBase
    {
        #region Constants

        /// <summary>
        /// Largest page the API will return.
        /// </summary>
        /// <remarks>
        /// Bounds what one request can pull out of the database, and keeps a
        /// mistyped parameter from loading an entire history into memory.
        /// </remarks>
        private const int TaillePageMaximale = 100;

        #endregion

        #region Properties

        private readonly IHistoriqueDAO historique;
        private readonly ISessionVisiteur session;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the HistoriqueController class
        /// </summary>
        /// <param name="historique">the history data access</param>
        /// <param name="session">supplies the visitor's anonymous identifier</param>
        public HistoriqueController(IHistoriqueDAO historique, ISessionVisiteur session)
        {
            this.historique = historique;
            this.session = session;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Lists this visitor's replies, optionally filtered by a keyword
        /// </summary>
        /// <param name="recherche">optional keyword</param>
        /// <param name="limite">optional maximum number of entries</param>
        /// <returns>the matching entries, most recent first</returns>
        [HttpGet]
        public ActionResult<List<ReponseGeneree>> Lister(
            [FromQuery] string? recherche, [FromQuery] int? limite)
        {
            string sessionId = this.session.ObtenirOuCreer(this.HttpContext);

            if (!string.IsNullOrWhiteSpace(recherche))
            {
                return this.Ok(this.historique.Rechercher(sessionId, recherche));
            }

            if (limite.HasValue)
            {
                return this.Ok(this.historique.ListerPremiers(
                    sessionId, Math.Min(limite.Value, TaillePageMaximale)));
            }

            return this.Ok(this.historique.ListerPremiers(sessionId, TaillePageMaximale));
        }

        /// <summary>
        /// Deletes one of this visitor's entries
        /// </summary>
        /// <param name="id">the identifier of the entry to delete</param>
        /// <returns>204 when deleted, 404 when it does not exist</returns>
        [HttpDelete("{id:int}")]
        public IActionResult Supprimer(int id)
        {
            try
            {
                this.historique.Supprimer(id, this.session.ObtenirOuCreer(this.HttpContext));
                return this.NoContent();
            }
            catch (InvalidOperationException)
            {
                // An entry belonging to someone else lands here too, and is
                // reported as absent: whether it exists must stay unknowable.
                return this.NotFound(new { erreur = "Cette entrée n'existe pas." });
            }
        }

        #endregion
    }
}
