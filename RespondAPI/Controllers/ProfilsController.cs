using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using RespondAPI.Dtos;
using RespondAPI.Services;
using RespondMetier.Data.Interfaces;
using RespondMetier.Exceptions;
using RespondMetier.Models;

namespace RespondAPI.Controllers
{
    /// <summary>
    /// Exposes the company profiles a reply can be written from.
    /// </summary>
    /// <remarks>
    /// Every route is scoped to the visitor's session. The demonstration
    /// profiles are visible to all and writable by none.
    /// </remarks>
    [ApiController]
    [Route("api/profils")]
    public class ProfilsController : ControllerBase
    {
        #region Constants

        /// <summary>
        /// Largest number of profiles one visitor may create.
        /// </summary>
        /// <remarks>
        /// Bounds what a single anonymous visitor can write into a shared
        /// database. Without accounts there is no other brake.
        /// </remarks>
        private const int ProfilsMaximumParVisiteur = 20;

        #endregion

        #region Properties

        private readonly IProfilDAO profils;
        private readonly ISessionVisiteur session;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the ProfilsController class
        /// </summary>
        /// <param name="profils">the profile data access</param>
        /// <param name="session">supplies the visitor's anonymous identifier</param>
        public ProfilsController(IProfilDAO profils, ISessionVisiteur session)
        {
            this.profils = profils;
            this.session = session;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Lists the demonstration profiles plus this visitor's own
        /// </summary>
        /// <returns>the visible profiles</returns>
        [HttpGet]
        public ActionResult<List<ProfilVueDto>> Lister()
        {
            return this.Ok(ProfilVueDto.Depuis(this.profils.Lister(this.SessionCourante())));
        }

        /// <summary>
        /// Returns a single profile
        /// </summary>
        /// <param name="id">the identifier of the profile wanted</param>
        /// <returns>the profile, or 404 when it does not exist or is not visible</returns>
        [HttpGet("{id:int}")]
        public ActionResult<ProfilVueDto> Charger(int id)
        {
            Profil? profil = this.profils.ChargerParId(id, this.SessionCourante());

            return profil == null ? this.NotFound() : this.Ok(ProfilVueDto.Depuis(profil));
        }

        /// <summary>
        /// Creates a profile belonging to this visitor
        /// </summary>
        /// <param name="profilDto">the profile to create</param>
        /// <returns>the created profile, with its identifier</returns>
        [HttpPost]
        [EnableRateLimiting(NomPolitiqueDebit.Ecriture)]
        public ActionResult<ProfilVueDto> Creer([FromBody] ProfilDto profilDto)
        {
            string sessionId = this.SessionCourante();

            int miens = this.profils.Lister(sessionId)
                .Count(profil => !profil.EstProfilDeDemonstration);

            if (miens >= ProfilsMaximumParVisiteur)
            {
                return this.BadRequest(new
                {
                    erreur = "Vous avez atteint la limite de "
                        + ProfilsMaximumParVisiteur + " profils.",
                });
            }

            Profil nouveau = profilDto.VersProfil(0, sessionId);

            try
            {
                this.profils.Inserer(nouveau);
            }
            catch (Exception erreur) when (EstConflitDeNom(erreur))
            {
                return this.Conflict(new { erreur = "Vous avez déjà un profil portant ce nom." });
            }

            return this.CreatedAtAction(
                nameof(this.Charger), new { id = nouveau.Id }, ProfilVueDto.Depuis(nouveau));
        }

        /// <summary>
        /// Updates one of this visitor's profiles
        /// </summary>
        /// <param name="id">the identifier of the profile to update</param>
        /// <param name="profilDto">the new values</param>
        /// <returns>the updated profile</returns>
        [HttpPut("{id:int}")]
        [EnableRateLimiting(NomPolitiqueDebit.Ecriture)]
        public ActionResult<ProfilVueDto> Modifier(int id, [FromBody] ProfilDto profilDto)
        {
            string sessionId = this.SessionCourante();

            try
            {
                this.profils.MettreAJour(profilDto.VersProfil(id, sessionId), sessionId);
            }
            catch (ErreurProfilEnLectureSeule erreur)
            {
                return this.StatusCode(
                    StatusCodes.Status403Forbidden, new { erreur = erreur.Message });
            }
            catch (InvalidOperationException)
            {
                return this.NotFound(new { erreur = "Ce profil n'existe pas." });
            }
            catch (Exception erreur) when (EstConflitDeNom(erreur))
            {
                return this.Conflict(new { erreur = "Vous avez déjà un profil portant ce nom." });
            }

            Profil? modifie = this.profils.ChargerParId(id, sessionId);

            return modifie == null
                ? this.NotFound()
                : this.Ok(ProfilVueDto.Depuis(modifie));
        }

        /// <summary>
        /// Deletes one of this visitor's profiles
        /// </summary>
        /// <param name="id">the identifier of the profile to delete</param>
        /// <returns>204 when deleted</returns>
        [HttpDelete("{id:int}")]
        public IActionResult Supprimer(int id)
        {
            try
            {
                this.profils.Supprimer(id, this.SessionCourante());
                return this.NoContent();
            }
            catch (ErreurProfilEnLectureSeule erreur)
            {
                return this.StatusCode(
                    StatusCodes.Status403Forbidden, new { erreur = erreur.Message });
            }
            catch (InvalidOperationException)
            {
                return this.NotFound(new { erreur = "Ce profil n'existe pas." });
            }
        }

        /// <summary>
        /// Returns the visitor's identifier, creating one when needed
        /// </summary>
        /// <returns>the session identifier</returns>
        private string SessionCourante()
        {
            return this.session.ObtenirOuCreer(this.HttpContext);
        }

        /// <summary>
        /// Recognises a unique index violation on the profile name
        /// </summary>
        /// <remarks>
        /// The database enforces that a visitor cannot have two profiles with
        /// the same name. Catching the violation rather than checking first
        /// avoids a race between the check and the insert.
        /// </remarks>
        /// <param name="erreur">the exception raised by the data layer</param>
        /// <returns>true when the name is already taken</returns>
        private static bool EstConflitDeNom(Exception erreur)
        {
            string message = (erreur.InnerException?.Message ?? erreur.Message).ToLowerInvariant();

            return message.Contains("unique") || message.Contains("constraint");
        }

        #endregion
    }
}
