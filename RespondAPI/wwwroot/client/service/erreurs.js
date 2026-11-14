/**
 * Traduit une reponse HTTP en echec en message affichable.
 *
 * Le serveur renvoie un champ « erreur » deja redige pour l'utilisateur. Les
 * codes traites ici sont ceux ou ce champ peut manquer, notamment le 429 de la
 * limitation de debit, rejete avant d'atteindre le controleur.
 *
 * Aucun detail technique n'est invente : si le serveur ne dit rien d'utile, on
 * donne le code plutot qu'une phrase rassurante qui masquerait le probleme.
 * @param {Response} reponse la reponse HTTP en echec
 * @returns {Promise<string>} le message a afficher
 */
export async function lireErreur(reponse) {
  try {
    const corps = await reponse.json();

    if (corps && typeof corps.erreur === 'string') {
      return corps.erreur;
    }

    // ASP.NET renvoie les erreurs de validation dans un champ « errors »,
    // indexe par nom de champ.
    if (corps && corps.errors && typeof corps.errors === 'object') {
      const premier = Object.values(corps.errors).flat()[0];

      if (typeof premier === 'string') {
        return premier;
      }
    }
  } catch {
    // Corps vide ou non JSON : on retombe sur le message par code.
  }

  switch (reponse.status) {
    case 400:
      return 'La demande est incomplète ou invalide.';
    case 403:
      return "Cette action n'est pas autorisée sur ce profil.";
    case 404:
      return "Cet élément n'existe pas.";
    case 409:
      return 'Un élément portant ce nom existe déjà.';
    case 429:
      return 'Limite de la démonstration atteinte. Réessayez dans une heure, '
        + 'ou ajoutez votre propre clé dans Paramètres.';
    case 503:
      return 'Le service est temporairement indisponible. Réessayez dans un moment.';
    default:
      return `Le serveur a répondu une erreur (HTTP ${reponse.status}).`;
  }
}
