namespace RespondMetier.Exceptions
{
    /// <summary>
    /// Raised when a demonstration profile is modified or deleted
    /// </summary>
    /// <remarks>
    /// The demonstration profiles are what a first time visitor sees, and they
    /// are shared by everyone. Letting anyone rename or delete them would
    /// break the demonstration for every later visitor, so they are read only
    /// by design rather than by convention.
    /// </remarks>
    public class ErreurProfilEnLectureSeule : Exception
    {
        /// <summary>
        /// Initializes the error with the reason shown to the user
        /// </summary>
        /// <param name="message">why the profile cannot be changed</param>
        public ErreurProfilEnLectureSeule(string message) : base(message)
        {
        }
    }
}
