import { ReponseGeneree } from '../models/reponseGeneree.js';
import { lireErreur } from './erreurs.js';

/**
 * Acces a l'historique des reponses generees.
 */
export class ServiceHistorique {
  /**
   * Charge l'historique, filtre par mot-cle si fourni.
   * @param {string} recherche mot-cle, ou chaine vide
   * @returns {Promise<ReponseGeneree[]>} les entrees, plus recentes d'abord
   */
  async lister(recherche = '') {
    const parametres = recherche.trim().length > 0
      ? `?recherche=${encodeURIComponent(recherche.trim())}`
      : '';

    const reponse = await fetch(`/api/historique${parametres}`);

    if (!reponse.ok) {
      throw new Error(await lireErreur(reponse));
    }

    return (await reponse.json()).map((element) => new ReponseGeneree(element));
  }

  /**
   * Supprime une entree.
   * @param {number} id l'identifiant de l'entree
   * @returns {Promise<void>}
   */
  async supprimer(id) {
    const reponse = await fetch(`/api/historique/${id}`, { method: 'DELETE' });

    if (!reponse.ok) {
      throw new Error(await lireErreur(reponse));
    }
  }
}
