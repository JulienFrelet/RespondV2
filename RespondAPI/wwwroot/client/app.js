import { ServiceReponses } from './service/serviceReponses.js';
import { VueGenerer } from './view/vueGenerer.js';

/**
 * Point d'entree du client. Assemble le service et la vue, rien de plus :
 * la logique vit dans la vue, les appels reseau dans le service.
 */
const vue = new VueGenerer(new ServiceReponses());

vue.demarrer().catch((erreur) => {
  // Dernier filet : une erreur au demarrage doit etre visible, pas silencieuse.
  const cible = document.getElementById('err-demande');

  if (cible) {
    cible.textContent = `Demarrage impossible : ${erreur.message}`;
  }
});
