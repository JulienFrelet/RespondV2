import { ServiceReponses } from './service/serviceReponses.js';
import { VueGenerer } from './view/vueGenerer.js';
import { VueTheme } from './view/vueTheme.js';

/**
 * Point d'entree du client. Assemble les services et les vues, rien de plus :
 * la logique vit dans les vues, les appels reseau dans le service.
 */
new VueTheme().demarrer();

const vue = new VueGenerer(new ServiceReponses());

vue.demarrer().catch((erreur) => {
  // Dernier filet : une erreur au demarrage doit etre visible, pas silencieuse.
  const cible = document.getElementById('err-demande');

  if (cible) {
    cible.textContent = `Demarrage impossible : ${erreur.message}`;
  }
});
