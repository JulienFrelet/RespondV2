using Microsoft.EntityFrameworkCore;
using RespondMetier.Models;

namespace RespondData
{
    /// <summary>
    /// Represents the connection and configuration of the SQLite database
    /// </summary>
    public class AppDbContext : DbContext
    {
        #region DbSets

        /// <summary>
        /// Represents the company profiles table
        /// </summary>
        public DbSet<Profil> Profils
        {
            get { return Set<Profil>(); }
        }

        /// <summary>
        /// Represents the generated replies history table
        /// </summary>
        public DbSet<ReponseGeneree> Historique
        {
            get { return Set<ReponseGeneree>(); }
        }

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes the database context
        /// </summary>
        /// <param name="options">The context configuration options</param>
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        #endregion

        #region Configuration

        /// <summary>
        /// Configures the database entities
        /// </summary>
        /// <param name="modelBuilder">The entity configuration tool</param>
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configures company profiles
            modelBuilder.Entity<Profil>(entity =>
            {
                entity.ToTable("Profil");
                entity.HasKey(profil => profil.Id);
                entity.Property(profil => profil.Id).ValueGeneratedOnAdd();
                entity.Property(profil => profil.Nom).IsRequired();
                entity.HasIndex(profil => profil.Nom).IsUnique();
                entity.Property(profil => profil.Service).IsRequired();
                entity.Property(profil => profil.Entreprise).IsRequired();
                entity.Property(profil => profil.Langue).IsRequired();

                // The signature has no identity of its own: it only exists as
                // part of a profile, and is deleted with it. Mapping it as an
                // owned type keeps its columns in the Profil table instead of
                // creating a second table joined on a meaningless key.
                entity.OwnsOne(profil => profil.Signature, signature =>
                {
                    signature.Property(valeur => valeur.Nom).HasColumnName("SignatureNom");
                    signature.Property(valeur => valeur.Telephone).HasColumnName("SignatureTelephone");
                    signature.Property(valeur => valeur.Email).HasColumnName("SignatureEmail");
                    signature.Property(valeur => valeur.InclureNom).HasColumnName("SignatureInclureNom");
                    signature.Property(valeur => valeur.InclureService).HasColumnName("SignatureInclureService");
                    signature.Property(valeur => valeur.InclureEntreprise).HasColumnName("SignatureInclureEntreprise");
                    signature.Property(valeur => valeur.InclureTelephone).HasColumnName("SignatureInclureTelephone");
                    signature.Property(valeur => valeur.InclureEmail).HasColumnName("SignatureInclureEmail");
                });
            });

            // Configures the history of generated replies
            modelBuilder.Entity<ReponseGeneree>(entity =>
            {
                entity.ToTable("ReponseGeneree");
                entity.HasKey(reponse => reponse.Id);
                entity.Property(reponse => reponse.Id).ValueGeneratedOnAdd();
                entity.Property(reponse => reponse.Date).IsRequired();
                entity.Property(reponse => reponse.Texte).IsRequired();

                // The history is almost always read sorted by date, and trimmed
                // by date when it exceeds its size limit.
                entity.HasIndex(reponse => reponse.Date);
            });
        }

        #endregion
    }
}
