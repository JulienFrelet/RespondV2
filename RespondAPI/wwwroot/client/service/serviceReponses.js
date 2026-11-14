import { lireErreur } from './erreurs.js';
import { lireCleApi } from './stockageLocal.js';

/**
 * Demande des reponses a l'API.
 */
export class ServiceReponses {
  /**
   * Genere une reponse.
   * @param {object} demande profilId, emailRecu, contexte, longueur, regles
   * @returns {Promise<{objet: string, corps: string}>} la reponse generee
   */
  async generer(demande) {
    const entetes = { 'Content-Type': 'application/json' };

    // La cle personnelle voyage en en-tete et non dans le corps : un en-tete
    // ne finit jamais dans une URL, donc la cle ne peut pas fuir dans un
    // journal d'acces ou un historique de navigateur.
    const cle = lireCleApi();

    if (cle.length > 0) {
      entetes['X-Cle-Api'] = cle;
    }

    const reponse = await fetch('/api/reponses', {
      method: 'POST',
      headers: entetes,
      body: JSON.stringify(demande),
    });

    if (!reponse.ok) {
      throw new Error(await lireErreur(reponse));
    }

    return reponse.json();
  }
}
