import { Profil } from '../models/profil.js';
import { lireErreur } from './erreurs.js';

/**
 * Acces aux profils. Seul endroit du client qui connait les routes.
 */
export class ServiceProfils {
  /**
   * Charge les profils visibles : ceux de demonstration et les miens.
   * @returns {Promise<Profil[]>} les profils
   */
  async lister() {
    const reponse = await fetch('/api/profils');

    if (!reponse.ok) {
      throw new Error(await lireErreur(reponse));
    }

    return (await reponse.json()).map((element) => new Profil(element));
  }

  /**
   * Cree un profil.
   * @param {object} profil les champs du profil
   * @returns {Promise<Profil>} le profil cree, avec son identifiant
   */
  async creer(profil) {
    const reponse = await fetch('/api/profils', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(profil),
    });

    if (!reponse.ok) {
      throw new Error(await lireErreur(reponse));
    }

    return new Profil(await reponse.json());
  }

  /**
   * Modifie un profil.
   * @param {number} id l'identifiant du profil
   * @param {object} profil les nouveaux champs
   * @returns {Promise<Profil>} le profil modifie
   */
  async modifier(id, profil) {
    const reponse = await fetch(`/api/profils/${id}`, {
      method: 'PUT',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(profil),
    });

    if (!reponse.ok) {
      throw new Error(await lireErreur(reponse));
    }

    return new Profil(await reponse.json());
  }

  /**
   * Supprime un profil.
   * @param {number} id l'identifiant du profil
   * @returns {Promise<void>}
   */
  async supprimer(id) {
    const reponse = await fetch(`/api/profils/${id}`, { method: 'DELETE' });

    if (!reponse.ok) {
      throw new Error(await lireErreur(reponse));
    }
  }
}
