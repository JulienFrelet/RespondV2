/**
 * Ce que l'application conserve dans le navigateur.
 *
 * Deux stockages distincts, et la difference est volontaire.
 *
 * Les regles de redaction vont dans localStorage : ce sont des preferences
 * sans valeur pour un attaquant, et l'utilisateur veut les retrouver demain.
 *
 * La cle API va dans sessionStorage, qui s'efface a la fermeture de l'onglet.
 * Une cle laissee en localStorage survivrait des mois sur un poste partage,
 * et la garder le temps d'une session suffit a l'usage.
 *
 * Tous les acces sont enveloppes : en navigation privee ou avec les cookies
 * bloques, le stockage leve une exception a la lecture comme a l'ecriture.
 * L'application doit continuer de fonctionner, sans memoire.
 */

const CLE_REGLES = 'respond-regles';
const CLE_API = 'respond-cle-api';

/**
 * Regles appliquees par defaut, identiques a celles du serveur.
 * @returns {object} les regles par defaut
 */
export function reglesParDefaut() {
  return {
    utiliserFormuleOuverture: true,
    formuleOuverture: 'Bonjour,',
    utiliserFormuleCloture: true,
    formuleCloture: 'Cordialement,',
    reglesPersonnalisees: '',
  };
}

/**
 * Lit les regles memorisees, ou les valeurs par defaut.
 * @returns {object} les regles de redaction
 */
export function lireRegles() {
  try {
    const brut = localStorage.getItem(CLE_REGLES);

    if (brut === null) {
      return reglesParDefaut();
    }

    // Fusion avec les valeurs par defaut : une version plus ancienne de
    // l'application peut avoir enregistre moins de champs qu'aujourd'hui.
    return { ...reglesParDefaut(), ...JSON.parse(brut) };
  } catch {
    return reglesParDefaut();
  }
}

/**
 * Memorise les regles de redaction.
 * @param {object} regles les regles a conserver
 * @returns {boolean} vrai si l'enregistrement a reussi
 */
export function ecrireRegles(regles) {
  try {
    localStorage.setItem(CLE_REGLES, JSON.stringify(regles));
    return true;
  } catch {
    return false;
  }
}

/**
 * Lit la cle API de la session en cours.
 * @returns {string} la cle, ou une chaine vide
 */
export function lireCleApi() {
  try {
    return sessionStorage.getItem(CLE_API) ?? '';
  } catch {
    return '';
  }
}

/**
 * Memorise la cle API pour la duree de l'onglet.
 * @param {string} cle la cle a conserver
 * @returns {boolean} vrai si l'enregistrement a reussi
 */
export function ecrireCleApi(cle) {
  try {
    sessionStorage.setItem(CLE_API, cle);
    return true;
  } catch {
    return false;
  }
}

/**
 * Oublie la cle API.
 */
export function effacerCleApi() {
  try {
    sessionStorage.removeItem(CLE_API);
  } catch {
    // Rien a faire : si le stockage est inaccessible, il n'y a rien dedans.
  }
}

/**
 * Efface tout ce que l'application a mis dans ce navigateur.
 *
 * Ne touche pas au cookie de session, qui est HttpOnly et donc hors de portee
 * du JavaScript : les profils et l'historique restent sur le serveur.
 */
export function toutEffacer() {
  try {
    localStorage.removeItem(CLE_REGLES);
    sessionStorage.removeItem(CLE_API);
  } catch {
    // Stockage inaccessible : il n'y a rien a effacer.
  }
}
