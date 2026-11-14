import { ServiceReponses } from './service/serviceReponses.js';
import { ServiceProfils } from './service/serviceProfils.js';
import { ServiceHistorique } from './service/serviceHistorique.js';

import { Navigation } from './view/navigation.js';
import { VueTheme } from './view/vueTheme.js';
import { VueGenerer } from './view/vueGenerer.js';
import { VueProfils } from './view/vueProfils.js';
import { VueStyle } from './view/vueStyle.js';
import { VueHistorique } from './view/vueHistorique.js';
import { VueParametres } from './view/vueParametres.js';

/**
 * Point d'entree du client.
 *
 * Assemble les services et les vues, puis branche la navigation. Rien d'autre :
 * la logique vit dans les vues, les appels reseau dans les services, et ce
 * fichier se contente de les relier.
 */

const serviceProfils = new ServiceProfils();
const serviceHistorique = new ServiceHistorique();
const serviceReponses = new ServiceReponses();

const vueGenerer = new VueGenerer(serviceReponses, serviceProfils);

// Une modification de profil doit se voir immediatement dans le selecteur de
// la vue Generer, sans que l'utilisateur ait a recharger la page.
const vueProfils = new VueProfils(serviceProfils, () => vueGenerer.chargerProfils());

const vueStyle = new VueStyle();
const vueHistorique = new VueHistorique(serviceHistorique);

// De meme, enregistrer ou oublier une cle change le bandeau de la vue Generer.
const vueParametres = new VueParametres(() => vueGenerer.majBandeau());

new VueTheme().demarrer();
vueProfils.demarrer();
vueStyle.demarrer();
vueHistorique.demarrer();
vueParametres.demarrer();

// Chaque vue se recharge au moment ou elle devient visible, plutot qu'au
// demarrage : inutile d'aller chercher l'historique de quelqu'un qui ne
// l'ouvrira jamais.
new Navigation({
  generer: () => vueGenerer.majBandeau(),
  profils: () => vueProfils.rafraichir(),
  historique: () => vueHistorique.rafraichir(),
  parametres: () => vueParametres.rafraichir(),
  style: () => vueStyle.remplir(),
}).demarrer();

vueGenerer.demarrer().catch((erreur) => {
  // Dernier filet : une erreur au demarrage doit etre visible, pas silencieuse.
  const cible = document.getElementById('err-demande');

  if (cible !== null) {
    cible.textContent = `Démarrage impossible : ${erreur.message}`;
  }
});
