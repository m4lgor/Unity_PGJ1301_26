README - TP BugHunt : rapport de bogues

Situation :

Un petit jeu de plateforme que vous connaissez par coeur : on court, on saute, on tape les blocs "?" par en dessous,
on ramasse des pieces, on ecrase les ennemis en leur tombant dessus, on prend l'ascenseur. Un collegue l'a programme
avant de partir en vacances. Le testeur a ouvert des tickets.

Vous savez deja ce que le jeu DOIT faire : ce sont les regles que vous connaissez. Il vous reste a trouver pourquoi il ne
le fait pas. Aucun ticket ne dit OU est le probleme : il dit ce que le testeur a vu.

Les scripts sont dans ce dossier (Mini*.cs). Les commentaires disent ce que le code DOIT faire, jamais ce qui ne va pas.
MiniCamera.cs et MiniGame.cs sont corrects. 
Certains bogues peuvent en cacher ou en changer d'autres : commencez par les plus simples a reproduire.

Les tickets :

-----------------------------------------------------------------------------------------------
PERF-01  Verifier le status de la scene physique
-----------------------------------------------------------------------------------------------
Il semblerait que plusieurs actions physiques ne soient pas nécessaires.
Verifier l'etat du monde physique, et enlever ou réduire les actions inutiles.

-----------------------------------------------------------------------------------------------
PERF-02  Le jeu ne depasse pas 25 images par seconde
-----------------------------------------------------------------------------------------------
Meme apres avoir allege le travail du MiniDirector, le jeu ne depasse jamais environ 25 images par seconde, sur n'importe quel PC.
Investiguez le probleme de performances pour revenir au dessus de 60Hz. 

-----------------------------------------------------------------------------------------------
BUG-01   Le saut rate parfois 
-----------------------------------------------------------------------------------------------
Parfois on appuie sur Espace, sur le sol, et rien ne se passe. Pas toujours : une fois sur quelques-unes.
Attendu : un saut a chaque appui sur Espace quand le heros est au sol, toujours de la meme hauteur.
N'arrive que quand le frame rate est supérieur à 50 fps.

-----------------------------------------------------------------------------------------------
BUG-02   On ne peut pas aller a gauche
-----------------------------------------------------------------------------------------------
A et la fleche gauche ne font rien. La droite fonctionne.
Attendu : A / fleche gauche font courir le heros vers la gauche.

-----------------------------------------------------------------------------------------------
BUG-03   Apres une mort, le heros ne revient pas au depart
-----------------------------------------------------------------------------------------------
Quand le heros meurt (chute dans la fosse, ennemi), il reapparait ailleurs que la ou il a commence le niveau.
Attendu : le heros revient au point de depart du niveau.

-----------------------------------------------------------------------------------------------
BUG-04   Les pieces ne tournent pas
-----------------------------------------------------------------------------------------------
Les pieces ont l'air immobiles.
Attendu : elles tournent sur elles-memes autour de l'axe vertical, comme une piece qu'on fait tourner debout.

-----------------------------------------------------------------------------------------------
BUG-05   Certaines pieces ne disparaissent pas
-----------------------------------------------------------------------------------------------
Parfois, apres avoir ramasse une piece, elle est toujours la, et en la retouchant on gagne encore des points.
Attendu : une piece ramassee disparait, et ne rapporte qu'une fois.

-----------------------------------------------------------------------------------------------
BUG-06   Les blocs "?" marchent aussi par le dessus
-----------------------------------------------------------------------------------------------
On obtient aussi une piece en atterrissant sur un bloc "?".
Attendu : une piece ne sort que quand on tape le bloc de la tete, par en dessous.

-----------------------------------------------------------------------------------------------
BUG-07   Les ennemis restent colles aux murs
-----------------------------------------------------------------------------------------------
Un ennemi qui arrive contre un mur continue de pousser dessus, immobile, au lieu de repartir dans l'autre sens.
Attendu : il se retourne au mur.

-----------------------------------------------------------------------------------------------
BUG-08   Les ennemis tombent dans la fosse
-----------------------------------------------------------------------------------------------
Un ennemi qui arrive au bord de la fosse continue tout droit et tombe.
Attendu : il se retourne au bord, comme au bord de n'importe quelle plateforme.

-----------------------------------------------------------------------------------------------
BUG-09   Sauter sur un ennemi ne le tue pas
-----------------------------------------------------------------------------------------------
On tombe sur un ennemi de haut en bas : il ne meurt pas, et c'est le heros qui meurt.
Attendu : l'ennemi meurt (+100 au score) et le heros rebondit.

-----------------------------------------------------------------------------------------------
BUG-10   L'ascenseur bouge tout seul
-----------------------------------------------------------------------------------------------
Meme quand il n'y a personne dessus, l'ascenseur monte et descend sans arret.
Attendu : l'ascenseur ne bouge que lorsque le HEROS est dessus.

-----------------------------------------------------------------------------------------------
BUG-11   L'ascenseur ecrase ce qui est dessous
-----------------------------------------------------------------------------------------------
En descendant, l'ascenseur ne s'arrete pas quand quelque chose est sous lui : il pousse un ennemi (ou le heros) dans le sol, et
l'ennemi se retrouve enfonce sous le niveau.
Etapes : laisser un ennemi venir dans le puits sous l'ascenseur pendant qu'il est en haut, puis le regarder redescendre.
Attendu : l'ascenseur s'arrete des qu'il touche quelque chose par en dessous, et attend que cette chose s'en aille.
