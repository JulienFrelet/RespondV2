/*
  Applique le theme memorise avant le premier affichage.

  Ce fichier est charge de maniere synchrone dans le <head>, volontairement
  separe du reste du client et non modulaire. Un module est differe : la page
  s'afficherait d'abord dans le theme du systeme, puis basculerait, ce qui
  produit un flash blanc desagreable sur un ecran sombre.

  La politique de securite du contenu interdit les scripts en ligne, donc ce
  code ne peut pas etre mis directement dans la page : il lui faut son fichier.
*/
(function appliquerThemeMemorise() {
  try {
    var choix = localStorage.getItem('respond-theme');

    if (choix === 'clair' || choix === 'sombre') {
      document.documentElement.setAttribute(
        'data-theme', choix === 'clair' ? 'light' : 'dark');
    }
  } catch (erreur) {
    // Stockage refuse (navigation privee, cookies bloques) : on retombe sur
    // la preference du systeme, qui reste un comportement correct.
  }
})();
