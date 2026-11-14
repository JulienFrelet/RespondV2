import { Profil } from '../models/profil.js';

/**
 * Seul point de contact avec l'API. Les vues n'appellent jamais fetch
 * directement : l'adresse des routes et la forme des erreurs restent ici.
 */
export class ServiceReponses {
  /**
   * Charge les profils disponibles.
   * @returns {Promise<Profil[]>} les profils
   */
  async listerProfils() {
    const reponse = await fetch('/api/profils');

    if (!reponse.ok) {
      throw new Error('Impossible de charger les profils.');
    }

    const donnees = await reponse.json();
    return donnees.map((element) => new Profil(element));
  }

  /**
   * Demande une reponse a l'API.
   * @param {object} demande profilId, emailRecu, contexte, longueur
   * @returns {Promise<{objet: string, corps: string}>} la reponse generee
   */
  async generer(demande) {
    const reponse = await fetch('/api/reponses', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(demande),
    });

    if (reponse.ok) {
      return reponse.json();
    }

    throw new Error(await this.#lireErreur(reponse));
  }

  /**
   * Traduit une reponse d'erreur en message affichable.
   *
   * Le serveur renvoie un champ « erreur » deja redige pour l'utilisateur.
   * Les codes traites ici sont ceux ou ce champ peut manquer, notamment le 429
   * de la limitation de debit, rejete avant d'atteindre le controleur.
   * @param {Response} reponse la reponse HTTP en echec
   * @returns {Promise<string>} le message a afficher
   */
  async #lireErreur(reponse) {
    try {
      const corps = await reponse.json();

      if (corps && typeof corps.erreur === 'string') {
        return corps.erreur;
      }
    } catch {
      // Corps vide ou non JSON : on retombe sur le message par code.
    }

    if (reponse.status === 429) {
      return "Limite de la demonstration atteinte. Reessayez dans une heure.";
    }

    if (reponse.status === 404) {
      return "Ce profil n'existe pas.";
    }

    return `Le serveur a repondu une erreur (HTTP ${reponse.status}).`;
  }
}
